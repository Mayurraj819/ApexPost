using ApexPost.Application.Abstractions.Persistence;
using ApexPost.Application.Abstractions.Services;
using ApexPost.Application.Contracts;
using ApexPost.Domain.Enums;
using ApexPost.Domain.ValueObjects;

namespace ApexPost.Application.Services;

public sealed class ReportService : IReportService
{
    private readonly ITransactionRepository _transactions;

    public ReportService(ITransactionRepository transactions) => _transactions = transactions;

    public async Task<TransactionReport> GetTransactionReportAsync(Guid branchId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
    {
        if (branchId == Guid.Empty)
        {
            throw new ArgumentException("Branch ID cannot be empty.", nameof(branchId));
        }

        if (from > to)
        {
            throw new ArgumentException("Report start must be before report end.", nameof(from));
        }

        var transactions = (await _transactions.ListAsync(cancellationToken))
            .Where(item => item.BranchId == branchId && item.OccurredAt >= from && item.OccurredAt <= to)
            .ToArray();
        var paid = transactions.Where(item => item.Status == TransactionStatus.Paid).ToArray();
        var currency = paid.Select(item => item.Total.Currency).FirstOrDefault() ?? "PHP";
        var total = new Money(paid.Sum(item => item.Total.Amount), currency);

        return new TransactionReport(
            branchId,
            from,
            to,
            transactions.Length,
            paid.Length,
            transactions.Count(item => item.Status == TransactionStatus.Voided),
            transactions.Count(item => item.Status == TransactionStatus.Refunded),
            total);
    }
}
