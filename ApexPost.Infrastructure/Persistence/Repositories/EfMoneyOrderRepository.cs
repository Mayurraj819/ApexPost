using ApexPost.Application.Abstractions.Persistence;
using ApexPost.Application.Contracts;
using ApexPost.Domain.Entities;
using ApexPost.Domain.ValueObjects;
using ApexPost.Infrastructure.Persistence.Entities;
using ApexPost.Infrastructure.Persistence.Mapping;
using Microsoft.EntityFrameworkCore;

namespace ApexPost.Infrastructure.Persistence.Repositories;
public sealed class EfMoneyOrderRepository(ApexPostDbContext db) : IMoneyOrderRepository
{
    public async Task AddAsync(MoneyOrderResult result, CancellationToken cancellationToken = default)
    {
        var verification = new IdentityVerification(result.Reference, IdentityDocument.RestoreMasked(result.DocumentType, result.MaskedNumber, result.IdentityFingerprint), false, "FingerprintRecorded");
        await db.Set<IdentityVerificationRecord>().AddAsync(IdentityVerificationMapper.ToRecord(verification), cancellationToken);
        await db.Set<MoneyOrderRecord>().AddAsync(new() { Id = result.Reference, BranchId = result.BranchId, StaffId = result.StaffId, Amount = result.Amount, PayeeName = result.PayeeName, IdentityVerificationId = verification.Id, OccurredAtUtc = result.CreatedAt }, cancellationToken);
    }

    private IQueryable<MoneyOrderResult> Query(Guid? branch = null, Guid? reference = null) =>
        from m in db.Set<MoneyOrderRecord>().AsNoTracking().Where(m => (branch == null || m.BranchId == branch) && (reference == null || m.Id == reference))join v in db.Set<IdentityVerificationRecord>().AsNoTracking()on m.IdentityVerificationId equals v.Id
        select new MoneyOrderResult(m.Id, m.BranchId, m.StaffId, m.Amount, m.PayeeName, v.Fingerprint, m.OccurredAtUtc)
        {
            MaskedNumber = v.MaskedNumber,
            DocumentType = v.DocumentType
        };
    public Task<MoneyOrderResult?> GetByReferenceAsync(Guid reference, CancellationToken cancellationToken = default) => Query(reference: reference).SingleOrDefaultAsync(cancellationToken);
    public async Task<IReadOnlyList<MoneyOrderResult>> ListByBranchAsync(Guid branchId, CancellationToken cancellationToken = default) => await Query(branch: branchId).ToArrayAsync(cancellationToken);
}
