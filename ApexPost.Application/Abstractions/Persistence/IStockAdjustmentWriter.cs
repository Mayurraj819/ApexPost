namespace ApexPost.Application.Abstractions.Persistence;
public interface IStockAdjustmentWriter
{
    Task StageAsync(Guid branchId, Guid productId, int delta, string reason, string actor, CancellationToken ct);
}
