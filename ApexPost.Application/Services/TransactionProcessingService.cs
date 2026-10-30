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
    private static readonly SemaphoreSlim PaymentGate = new(1, 1);

    public TransactionProcessingService(IBranchRepository branches, ICounterRepository counters,
        IStaffRepository staff, IProductRepository products, ITransactionRepository transactions,
        IBranchStockRepository stock)
    {
        _branches = branches;
        _counters = counters;
        _staff = staff;
        _products = products;
        _transactions = transactions;
        _stock = stock;
    }

    public async Task<Transaction> StartAsync(Guid branchId, Guid counterId, Guid staffId,
        DateTimeOffset occurredAt, CancellationToken cancellationToken = default)
    {
        var branch = await RequiredAsync(_branches.GetByIdAsync(branchId, cancellationToken), "Branch", branchId);
        var counter = await RequiredAsync(_counters.GetByIdAsync(counterId, cancellationToken), "Counter", counterId);
        var staff = await RequiredAsync(_staff.GetByIdAsync(staffId, cancellationToken), "Staff", staffId);
        var transaction = Transaction.Start(branch, counter, staff, occurredAt);
        await _transactions.AddAsync(transaction, cancellationToken);
        return transaction;
    }

    public async Task AddProductAsync(Guid transactionId, Guid productId, Quantity quantity,
        CancellationToken cancellationToken = default)
    {
        var transaction = await RequiredAsync(_transactions.GetByIdAsync(transactionId, cancellationToken), "Transaction", transactionId);
        var product = await RequiredAsync(_products.GetByIdAsync(productId, cancellationToken), "Product", productId);
        transaction.AddProduct(product, quantity);
        await _transactions.SaveAsync(transaction, cancellationToken);
    }

    public async Task AddServiceAsync(Guid transactionId, string serviceCode, TransactionLineType serviceType,
        Money price, ShippingServiceLevel? shippingLevel = null, CancellationToken cancellationToken = default)
    {
        var transaction = await RequiredAsync(_transactions.GetByIdAsync(transactionId, cancellationToken), "Transaction", transactionId);
        transaction.AddService(serviceCode, serviceType, price, shippingLevel);
        await _transactions.SaveAsync(transaction, cancellationToken);
    }

    public async Task PayAsync(Guid transactionId, CancellationToken cancellationToken = default)
    {
        await PaymentGate.WaitAsync(cancellationToken);
        try
        {
            var transaction = await RequiredAsync(_transactions.GetByIdAsync(transactionId, cancellationToken), "Transaction", transactionId);
            var branch = await RequiredAsync(_branches.GetByIdAsync(transaction.BranchId, cancellationToken), "Branch", transaction.BranchId);
            var staff = await RequiredAsync(_staff.GetByIdAsync(transaction.StaffId, cancellationToken), "Staff", transaction.StaffId);
            // Validate authorization and state before writing any stock records.
            transaction.Pay(staff, branch);
            var requirements = transaction.Lines
                .Where(line => line.Type == TransactionLineType.RetailProduct && line.ProductId.HasValue)
                .GroupBy(line => line.ProductId!.Value)
                .Select(group => (ProductId: group.Key, Quantity: checked(group.Sum(line => line.Quantity.Value))))
                .ToArray();
            var changes = new List<(BranchStock Stock, int Quantity)>();
            foreach (var requirement in requirements)
            {
                var item = await _stock.GetByBranchAndProductAsync(transaction.BranchId, requirement.ProductId, cancellationToken)
                    ?? throw new InvalidOperationException($"No stock record exists for product {requirement.ProductId}.");
                if (item.Quantity.Value < requirement.Quantity)
                    throw new InvalidOperationException($"Insufficient stock for product {requirement.ProductId}.");
                changes.Add((item, requirement.Quantity));
            }
            // JSON stores do not support multi-file ACID transactions. This prevents insufficient
            // stock and concurrent payments in this process, but is NOT crash-safe/atomic.
            var saved = new List<(BranchStock Stock, int Quantity)>();
            try
            {
                foreach (var change in changes)
                {
                    change.Stock.Decrease(change.Quantity);
                    await _stock.SaveAsync(change.Stock, cancellationToken);
                    saved.Add(change);
                }
                await _transactions.SaveAsync(transaction, cancellationToken);
            }
            catch
            {
                // Best-effort rollback; a process crash or rollback failure still needs reconciliation.
                foreach (var change in saved.AsEnumerable().Reverse())
                {
                    change.Stock.Increase(change.Quantity);
                    await _stock.SaveAsync(change.Stock, CancellationToken.None);
                }
                throw;
            }
        }
        finally
        {
            PaymentGate.Release();
        }
    }

    public async Task VoidAsync(Guid transactionId, Guid managerId, string reason, CancellationToken cancellationToken = default)
    {
        var transaction = await RequiredAsync(_transactions.GetByIdAsync(transactionId, cancellationToken), "Transaction", transactionId);
        var manager = await RequiredAsync(_staff.GetByIdAsync(managerId, cancellationToken), "Staff", managerId);
        transaction.Void(manager, reason);
        await _transactions.SaveAsync(transaction, cancellationToken);
    }

    public async Task RefundAsync(Guid transactionId, Guid managerId, string reason, CancellationToken cancellationToken = default)
    {
        var transaction = await RequiredAsync(_transactions.GetByIdAsync(transactionId, cancellationToken), "Transaction", transactionId);
        var manager = await RequiredAsync(_staff.GetByIdAsync(managerId, cancellationToken), "Staff", managerId);
        transaction.Refund(manager, reason);
        await _transactions.SaveAsync(transaction, cancellationToken);
    }

    private static async Task<T> RequiredAsync<T>(Task<T?> task, string type, Guid id) where T : class =>
        await task ?? throw new KeyNotFoundException($"{type} '{id}' was not found.");
}
