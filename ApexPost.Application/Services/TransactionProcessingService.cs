using ApexPost.Application.Contracts;
using ApexPost.Application.Abstractions.Persistence;
using ApexPost.Application.Abstractions.Services;
using ApexPost.Domain.Entities;
using ApexPost.Domain.Enums;
using ApexPost.Domain.ValueObjects;

namespace ApexPost.Application.Services;
public sealed class TransactionProcessingService : ITransactionProcessingService
{
    private readonly IBranchRepository _branches;
    private readonly ICounterRepository _counters;
    private readonly IStaffRepository _staff;
    private readonly IProductRepository _products;
    private readonly ITransactionRepository _transactions;
    private readonly IBranchStockRepository _stock;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPaymentRepository _payments;
    public TransactionProcessingService(IBranchRepository branches, ICounterRepository counters, IStaffRepository staff, IProductRepository products, ITransactionRepository transactions, IBranchStockRepository stock, IUnitOfWork unitOfWork, IPaymentRepository payments)
    {
        _branches = branches;
        _counters = counters;
        _staff = staff;
        _products = products;
        _transactions = transactions;
        _stock = stock;
        _unitOfWork = unitOfWork;
        _payments = payments;
    }

    public async Task<Transaction> StartAsync(Guid branchId, Guid counterId, Guid staffId, DateTimeOffset occurredAt, CancellationToken cancellationToken = default)
    {
        var branch = await RequiredAsync(_branches.GetByIdAsync(branchId, cancellationToken), "Branch", branchId);
        var counter = await RequiredAsync(_counters.GetByIdAsync(counterId, cancellationToken), "Counter", counterId);
        var staff = await RequiredAsync(_staff.GetByIdAsync(staffId, cancellationToken), "Staff", staffId);
        Transaction transaction;
        try
        {
            transaction = Transaction.Start(branch, counter, staff, occurredAt);
        }
        catch (InvalidOperationException ex)
        {
            throw new ApexPost.Application.Exceptions.DomainRuleException(ex.Message, ex);
        }

        await _transactions.AddAsync(transaction, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return transaction;
    }

    public async Task AddProductAsync(Guid transactionId, Guid productId, Quantity quantity, CancellationToken cancellationToken = default)
    {
        var transaction = await RequiredAsync(_transactions.GetByIdAsync(transactionId, cancellationToken), "Transaction", transactionId);
        var product = await RequiredAsync(_products.GetByIdAsync(productId, cancellationToken), "Product", productId);
        try
        {
            transaction.AddProduct(product, quantity);
        }
        catch (InvalidOperationException ex)
        {
            throw new ApexPost.Application.Exceptions.DomainRuleException(ex.Message, ex);
        }

        await _transactions.SaveAsync(transaction, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task AddServiceAsync(Guid transactionId, string serviceCode, TransactionLineType serviceType, Money price, ShippingServiceLevel? shippingLevel = null, CancellationToken cancellationToken = default)
    {
        var transaction = await RequiredAsync(_transactions.GetByIdAsync(transactionId, cancellationToken), "Transaction", transactionId);
        try
        {
            transaction.AddService(serviceCode, serviceType, price, shippingLevel);
        }
        catch (InvalidOperationException ex)
        {
            throw new ApexPost.Application.Exceptions.DomainRuleException(ex.Message, ex);
        }

        await _transactions.SaveAsync(transaction, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task PayAsync(Guid transactionId, CancellationToken cancellationToken = default)
    {
        var transaction = await RequiredAsync(_transactions.GetByIdAsync(transactionId, cancellationToken), "Transaction", transactionId);
        var branch = await RequiredAsync(_branches.GetByIdAsync(transaction.BranchId, cancellationToken), "Branch", transaction.BranchId);
        var staff = await RequiredAsync(_staff.GetByIdAsync(transaction.StaffId, cancellationToken), "Staff", transaction.StaffId);
        await StagePaymentAsync(transaction, staff, branch, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid> CheckoutAsync(CheckoutCommand command, CancellationToken cancellationToken = default)
    {
        if (command.Lines.Count is < 1 or > 100)
            throw new ArgumentException("A checkout needs between 1 and 100 lines.");
        var branch = await RequiredAsync(_branches.GetByIdAsync(command.BranchId, cancellationToken), "Branch", command.BranchId);
        var counter = await RequiredAsync(_counters.GetByIdAsync(command.CounterId, cancellationToken), "Counter", command.CounterId);
        var staff = await RequiredAsync(_staff.GetByIdAsync(command.StaffId, cancellationToken), "Staff", command.StaffId);
        Transaction transaction;
        try
        {
            transaction = Transaction.Start(branch, counter, staff, command.OccurredAt);
        }
        catch (InvalidOperationException ex)
        {
            throw new ApexPost.Application.Exceptions.DomainRuleException(ex.Message, ex);
        }

        foreach (var line in command.Lines)
        {
            var product = await RequiredAsync(_products.GetByIdAsync(line.ProductId, cancellationToken), "Product", line.ProductId);
            transaction.AddProduct(product, new Quantity(line.Quantity));
        }

        await _transactions.AddAsync(transaction, cancellationToken);
        await StagePaymentAsync(transaction, staff, branch, cancellationToken);
        // Header, NEW lines, stock decrements and payment are all first persisted by this one commit.
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return transaction.Id;
    }

    private async Task StagePaymentAsync(Transaction transaction, Staff staff, Branch branch, CancellationToken cancellationToken)
    {
        if (transaction.Lines.Count == 0 || transaction.Total.Amount <= 0)
            throw new ArgumentException("Add a positive-value line before paying.");
        var requirements = transaction.Lines.Where(l => l.Type == TransactionLineType.RetailProduct && l.ProductId.HasValue).GroupBy(l => l.ProductId!.Value).Select(g => (Id: g.Key, Quantity: checked(g.Sum(l => l.Quantity.Value)))).OrderBy(x => x.Id).ToArray();
        var stock = new List<(BranchStock Item, int Quantity)>();
        foreach (var item in requirements)
        {
            var record = await _stock.GetByBranchAndProductAsync(transaction.BranchId, item.Id, cancellationToken) ?? throw new ArgumentException("Product has no branch stock.");
            if (record.Quantity.Value < item.Quantity)
                throw new ArgumentException("Insufficient stock. Refresh the sale and try again.");
            stock.Add((record, item.Quantity));
        }

        try
        {
            transaction.Pay(staff, branch);
        }
        catch (InvalidOperationException ex)
        {
            throw new ApexPost.Application.Exceptions.DomainRuleException(ex.Message, ex);
        }

        foreach (var item in stock)
        {
            item.Item.Decrease(item.Quantity);
            await _stock.SaveAsync(item.Item, cancellationToken);
        }

        await _transactions.SaveAsync(transaction, cancellationToken);
        // Stable payment key makes a repeated payment insert a unique conflict, not a double charge.
        await _payments.AddAsync(new Payment(transaction.Id, transaction.Id, transaction.Total, DateTimeOffset.UtcNow), cancellationToken);
    }

    public async Task VoidAsync(Guid transactionId, Guid managerId, string reason, CancellationToken cancellationToken = default)
    {
        var transaction = await RequiredAsync(_transactions.GetByIdAsync(transactionId, cancellationToken), "Transaction", transactionId);
        var manager = await RequiredAsync(_staff.GetByIdAsync(managerId, cancellationToken), "Staff", managerId);
        try
        {
            transaction.Void(manager, reason);
        }
        catch (InvalidOperationException ex)
        {
            throw new ApexPost.Application.Exceptions.DomainRuleException(ex.Message, ex);
        }

        await _transactions.SaveAsync(transaction, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task RefundAsync(Guid transactionId, Guid managerId, string reason, CancellationToken cancellationToken = default)
    {
        var transaction = await RequiredAsync(_transactions.GetByIdAsync(transactionId, cancellationToken), "Transaction", transactionId);
        var manager = await RequiredAsync(_staff.GetByIdAsync(managerId, cancellationToken), "Staff", managerId);
        try
        {
            transaction.Refund(manager, reason);
        }
        catch (InvalidOperationException ex)
        {
            throw new ApexPost.Application.Exceptions.DomainRuleException(ex.Message, ex);
        }

        await _transactions.SaveAsync(transaction, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static async Task<T> RequiredAsync<T>(Task<T?> task, string type, Guid id)
        where T : class => await task ?? throw new KeyNotFoundException($"{type} '{id}' was not found.");
}
