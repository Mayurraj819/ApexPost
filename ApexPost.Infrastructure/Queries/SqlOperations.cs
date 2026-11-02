using ApexPost.Application.Abstractions.Persistence;
using ApexPost.Application.Abstractions.Services;
using ApexPost.Application.Contracts;
using ApexPost.Domain.Entities;
using ApexPost.Domain.ValueObjects;
using ApexPost.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ApexPost.Infrastructure.Queries;
public sealed class SqlOperations(ApexPostDbContext db, IUnitOfWork unitOfWork) : ISqlOperations
{
    public async Task<IReadOnlyList<DailySalesProcedureRow>> DailySalesAsync(string branchCode, DateOnly day, CancellationToken ct)
    {
        return await db.Database.SqlQuery<DailySalesProcedureRow>($"EXEC dbo.usp_DailyBranchSales @BranchCode={branchCode}, @Date={day}").ToArrayAsync(ct);
    }

    public async Task<IReadOnlyList<LowStockProcedureRow>> LowStockAsync(string? branchCode, int threshold, CancellationToken ct)
    {
        return await db.Database.SqlQuery<LowStockProcedureRow>($"EXEC dbo.usp_LowStockReport @BranchCode={branchCode}, @Threshold={threshold}").ToArrayAsync(ct);
    }

    public async Task AdjustStockAsync(string branchCode, string sku, int delta, string reason, string changedBy, CancellationToken ct)
    {
        if (delta == 0 || string.IsNullOrWhiteSpace(reason) || string.IsNullOrWhiteSpace(changedBy))
            throw new ArgumentException("Nonzero delta, reason and actor required.");
        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            try
            {
                await db.Database.ExecuteSqlAsync($"EXEC dbo.usp_AdjustStock @BranchCode={branchCode}, @Sku={sku}, @Delta={delta}, @Reason={reason}, @ChangedBy={changedBy}", token);
            }
            catch (SqlException ex)when (ex.Number is >= 50001 and <= 50007)
            {
                throw new ArgumentException(ex.Message);
            }
        }, ct);
        db.ChangeTracker.Clear();
    }

    public async Task<int> RevisePricesAsync(decimal factor, string changedBy, CancellationToken ct)
    {
        if (factor <= 0 || factor > 10 || string.IsNullOrWhiteSpace(changedBy))
            throw new ArgumentException("A positive factor up to 10 and actor are required.");
        // Fixed reviewed SQL shape: ExecuteUpdate is set based, and does not invoke SaveChanges/interceptors.
        var count = 0;
        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            count = await db.Set<Product>().ExecuteUpdateAsync(setters => setters.SetProperty(p => p.UnitPrice.Amount, p => decimal.Round(p.UnitPrice.Amount * factor, 2)).SetProperty(p => EF.Property<DateTime?>(p, "ModifiedAtUtc"), DateTime.UtcNow), token);
        }, ct);
        db.ChangeTracker.Clear();
        return count;
    }
}
