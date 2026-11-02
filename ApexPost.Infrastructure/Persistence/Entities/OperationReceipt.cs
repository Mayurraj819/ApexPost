namespace ApexPost.Infrastructure.Persistence.Entities;

// Written atomically with a retried SQL command, so an uncertain COMMIT can be verified.
public sealed class OperationReceipt
{
    public Guid Id { get; set; }
}
