using ApexPost.Domain.Common;
using ApexPost.Domain.ValueObjects;

namespace ApexPost.Domain.Entities;
public sealed class Payment : Entity
{
    private Payment()
    {
    }

    public Payment(Guid id, Guid transactionId, Money amount, DateTimeOffset paidAtUtc) : base(id)
    {
        if (transactionId == Guid.Empty || amount.Amount <= 0)
            throw new ArgumentException("A transaction and positive payment are required.");
        TransactionId = transactionId;
        Amount = amount;
        PaidAtUtc = paidAtUtc.ToUniversalTime();
    }

    public Guid TransactionId { get; private set; }
    public Money Amount { get; private set; }
    public DateTimeOffset PaidAtUtc { get; private set; }
}
