using ApexPost.Application.Abstractions.Persistence;
using ApexPost.Domain.Entities;
using ApexPost.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApexPost.Infrastructure.Persistence;
// Independent audit context: never flush a business context after a failed command.
public sealed class EfAuditStore(DbContextOptions<ApexPostDbContext> options) : IAuditStore
{
    public async Task AppendAsync(object value, CancellationToken cancellationToken = default)
    {
        if (value is not AuditEntry e)
            throw new ArgumentException("An audit entry is required.", nameof(value));
        await using var db = new ApexPostDbContext(options);
        db.Set<AuditRecord>().Add(new() { Id = Guid.NewGuid(), OccurredAtUtc = e.OccurredAt, Controller = e.Controller, Action = e.Action, StaffId = e.StaffId, Role = e.Role, BranchId = e.BranchId, CorrelationId = e.CorrelationId, StatusCode = e.StatusCode, ElapsedMilliseconds = e.DurationMs, SafeRoutes = e.RouteValues, Outcome = e.Result, ExceptionType = e.Details });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<object>> ReadAsync(CancellationToken cancellationToken = default)
    {
        await using var db = new ApexPostDbContext(options);
        var rows = await db.Set<AuditRecord>().AsNoTracking().OrderByDescending(x => x.OccurredAtUtc).Take(1000).ToArrayAsync(cancellationToken);
        return rows.Cast<object>().ToArray();
    }
}
