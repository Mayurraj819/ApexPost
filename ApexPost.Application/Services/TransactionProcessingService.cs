using ApexPost.Application.Abstractions.Persistence;
using ApexPost.Application.Abstractions.Services;
using ApexPost.Application.Contracts;
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

    public TransactionProcessingService(IBranchRepository branches, ICounterRepository counters, IStaffRepository staff, IProductRepository products, ITransactionRepository transactions)
    {
        _branches = branches;
        _counters = counters;
        _staff = staff;
        _products = products;
        _transactions = transactions;
    }

    public async Task<Transaction> StartAsync(Guid branchId, Guid counterId, Guid staffId, DateTimeOffset occurredAt, CancellationToken cancellationToken = default)
    {
        var branch = await RequiredAsync(_branches.GetByIdAsync(branchId, cancellationToken), "Branch", branchId);
        var counter = await RequiredAsync(_counters.GetByIdAsync(counterId, cancellationToken), "Counter", counterId);
        var staff = await RequiredAsync(_staff.GetByIdAsync(staffId, cancellationToken), "Staff", staffId);
        var transaction = Transaction.Start(branch, counter, staff, occurredAt);
        await _transactions.AddAsync(transaction, cancellationToken);
        return transaction;
    }

    public async Task AddProductAsync(Guid transactionId, Guid productId, Quantity quantity, CancellationToken cancellationToken = default)
    {
        var transaction = await RequiredAsync(_transactions.GetByIdAsync(transactionId, cancellationToken), "Transaction", transactionId);
        var product = await RequiredAsync(_products.GetByIdAsync(productId, cancellationToken), "Product", productId);
        transaction.AddProduct(product, quantity);
        await _transactions.SaveAsync(transaction, cancellationToken);
    }

    public async Task AddServiceAsync(Guid transactionId, string serviceCode, TransactionLineType serviceType, Money price, ShippingServiceLevel? shippingLevel = null, CancellationToken cancellationToken = default)
    {
        var transaction = await RequiredAsync(_transactions.GetByIdAsync(transactionId, cancellationToken), "Transaction", transactionId);
        transaction.AddService(serviceCode, serviceType, price, shippingLevel);
        await _transactions.SaveAsync(transaction, cancellationToken);
    }

    public async Task PayAsync(Guid transactionId, CancellationToken cancellationToken = default)
    {
        var transaction = await RequiredAsync(_transactions.GetByIdAsync(transactionId, cancellationToken), "Transaction", transactionId);
        var branch = await RequiredAsync(_branches.GetByIdAsync(transaction.BranchId, cancellationToken), "Branch", transaction.BranchId);
        var staff = await RequiredAsync(_staff.GetByIdAsync(transaction.StaffId, cancellationToken), "Staff", transaction.StaffId);
        transaction.Pay(staff, branch);
        await _transactions.SaveAsync(transaction, cancellationToken);
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
