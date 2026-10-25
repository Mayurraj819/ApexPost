namespace ApexPost.Domain.Events;

public record StockLowEvent(Guid BranchId, Guid ProductId, int CurrentQuantity, int ReorderLevel);
public record HighValueMoneyOrderEvent(Guid BranchId, Guid StaffId, decimal Amount, string Recipient, string? MaskedDocumentNumber);
public record TransactionVoidedEvent(Guid BranchId, Guid TransactionId, string Reason, Guid StaffId);
public record TransactionRefundedEvent(Guid BranchId, Guid TransactionId, string Reason, Guid StaffId);

public interface IDomainEventObserver
{
    Task NotifyAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default) where TEvent : class;
}