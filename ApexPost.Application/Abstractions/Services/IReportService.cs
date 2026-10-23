using ApexPost.Application.Contracts;

namespace ApexPost.Application.Abstractions.Services;

public interface IReportService
{
    Task<TransactionReport> GetTransactionReportAsync(Guid branchId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);
}
