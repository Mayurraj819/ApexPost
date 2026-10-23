using ApexPost.Application.Contracts;
using ApexPost.Domain.Entities;
using ApexPost.Domain.Enums;
using ApexPost.Domain.ValueObjects;

namespace ApexPost.Application.Abstractions.Services;

public interface ITransactionProcessingService
{
    Task<Transaction> StartAsync(Guid branchId, Guid counterId, Guid staffId, DateTimeOffset occurredAt, CancellationToken cancellationToken = default);
    Task AddProductAsync(Guid transactionId, Guid productId, Quantity quantity, CancellationToken cancellationToken = default);
    Task AddServiceAsync(Guid transactionId, string serviceCode, TransactionLineType serviceType, Money price, ShippingServiceLevel? shippingLevel = null, CancellationToken cancellationToken = default);
    Task PayAsync(Guid transactionId, CancellationToken cancellationToken = default);
    Task VoidAsync(Guid transactionId, Guid managerId, string reason, CancellationToken cancellationToken = default);
    Task RefundAsync(Guid transactionId, Guid managerId, string reason, CancellationToken cancellationToken = default);
}
