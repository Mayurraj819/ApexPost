using ApexPost.Application.Delivery;
using ApexPost.Application.Abstractions.Persistence;
using ApexPost.Domain.Delivery;
using ApexPost.Infrastructure.Persistence;
using ApexPost.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ApexPost.Infrastructure.Delivery;
public static class DeliveryRegistration
{
    public static IServiceCollection AddShipmentApiServices(this IServiceCollection services) => services.AddScoped<IShipmentApiService, ShipmentApiService>();
}

public sealed class ShipmentApiService(ApexPostDbContext db, IUnitOfWork unitOfWork) : IShipmentApiService
{
    private Task<TrackedShipment?> Find(string tracking, CancellationToken ct) => db.Set<TrackedShipment>().SingleOrDefaultAsync(s => s.TrackingNumber == tracking, ct);
    private async Task<TrackedShipment> Required(string tracking, CancellationToken ct) => await Find(tracking, ct) ?? throw new ShipmentNotFoundException(tracking);
    private static ShipmentView View(TrackedShipment s, ScanView? latest = null) => new(s.TrackingNumber, s.WeightKg, s.RecipientName, s.RecipientPhone, s.Address, s.City, s.Status, latest);
    private static ScanView Scan(CourierScanRecord s, string tracking) => new(s.Id, tracking, s.ScanType, s.CourierId, s.Location, s.ScannedAtUtc, s.Notes);
    public async Task<ShipmentPage> ListAsync(int page, int pageSize, DeliveryStatus? status, string? city, CancellationToken ct)
    {
        if (page < 1 || pageSize is < 1 or > 50 || (long)(page - 1) * pageSize > int.MaxValue)
            throw new ArgumentException("Invalid pagination.");
        var query = db.Set<TrackedShipment>().AsNoTracking().Where(s => (status == null || s.Status == status) && (city == null || s.City == city));
        var total = await query.CountAsync(ct);
        // A fixed pair of data queries, never a per-shipment latest-scan query.
        var rows = await query.OrderBy(s => s.TrackingNumber).Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(ct);
        var ids = rows.Select(s => s.Id).ToArray();
        var latest = await db.Set<CourierScanRecord>().AsNoTracking().Where(s => ids.Contains(s.ShipmentId)).GroupBy(s => s.ShipmentId).Select(g => g.OrderByDescending(s => s.ScannedAtUtc).ThenByDescending(s => s.Id).First()).ToArrayAsync(ct);
        return new(rows.Select(s => View(s, latest.FirstOrDefault(l => l.ShipmentId == s.Id)is { } scan ? Scan(scan, s.TrackingNumber) : null)).ToArray(), page, pageSize, total);
    }

    public async Task<ShipmentView> GetAsync(string tracking, CancellationToken ct)
    {
        var s = await db.Set<TrackedShipment>().AsNoTracking().SingleOrDefaultAsync(s => s.TrackingNumber == tracking, ct) ?? throw new ShipmentNotFoundException(tracking);
        var latest = await db.Set<CourierScanRecord>().AsNoTracking().Where(x => x.ShipmentId == s.Id).OrderByDescending(x => x.ScannedAtUtc).ThenByDescending(x => x.Id).FirstOrDefaultAsync(ct);
        return View(s, latest is null ? null : Scan(latest, tracking));
    }

    public async Task<ShipmentView> CreateAsync(string tracking, ShipmentDetails d, CancellationToken ct)
    {
        var s = new TrackedShipment(tracking, d.WeightKg, d.RecipientName, d.RecipientPhone, d.Address, d.City);
        db.Add(s);
        await unitOfWork.SaveChangesAsync(ct);
        return View(s);
    }

    public async Task<ShipmentView> ReplaceAsync(string tracking, ShipmentDetails d, CancellationToken ct)
    {
        var s = await Required(tracking, ct);
        s.ReplaceDetails(d.WeightKg, d.RecipientName, d.RecipientPhone, d.Address, d.City);
        await unitOfWork.SaveChangesAsync(ct);
        return await GetAsync(tracking, ct);
    }

    public async Task<ShipmentView> ChangeStatusAsync(string tracking, DeliveryStatus status, CancellationToken ct)
    {
        var s = await Required(tracking, ct);
        s.ChangeStatus(status);
        await unitOfWork.SaveChangesAsync(ct);
        return await GetAsync(tracking, ct);
    }

    public async Task DeleteAsync(string tracking, CancellationToken ct)
    {
        var s = await Required(tracking, ct);
        s.EnsureCanDelete();
        db.Remove(s);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ScanView>> ScansAsync(string tracking, CancellationToken ct)
    {
        var shipment = await db.Set<TrackedShipment>().AsNoTracking().SingleOrDefaultAsync(s => s.TrackingNumber == tracking, ct) ?? throw new ShipmentNotFoundException(tracking);
        return (await db.Set<CourierScanRecord>().AsNoTracking().Where(s => s.ShipmentId == shipment.Id).OrderBy(s => s.ScannedAtUtc).ThenBy(s => s.Id).ToArrayAsync(ct)).Select(s => Scan(s, tracking)).ToArray();
    }

    public async Task<ScanUploadResult> UploadAsync(string tracking, IReadOnlyList<ScanInput> scans, CancellationToken ct)
    {
        if (scans.Count is < 1 or > 100)
            throw new ArgumentException("Supply between 1 and 100 scans.");
        if (db.ChangeTracker.HasChanges())
            throw new InvalidOperationException("Upload must start without pending writes.");
        var receipt = Guid.NewGuid();
        ScanUploadResult? response = null;
        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteInTransactionAsync(async token =>
        {
            // Rebuild tracked state on a rolled-back retry; the callback has no network side effects.
            db.ChangeTracker.Clear();
            var shipment = await db.Set<TrackedShipment>().FromSql($"SELECT * FROM dbo.TrackedShipments WITH (UPDLOCK,HOLDLOCK) WHERE TrackingNumber = {tracking}").SingleOrDefaultAsync(token) ?? throw new ShipmentNotFoundException(tracking);
            var ids = scans.Select(s => s.Id).Distinct().ToArray();
            var known = await db.Set<CourierScanRecord>().Where(s => ids.Contains(s.Id)).ToDictionaryAsync(s => s.Id, token);
            var accepted = new List<string>();
            var duplicates = new List<string>();
            var rejected = new List<ScanRejection>();
            foreach (var input in scans)
            {
                if (input.TrackingNumber != tracking)
                {
                    rejected.Add(new(input.Id, "Tracking number does not match the route."));
                    continue;
                }

                if (input.ScannedAtUtc > DateTimeOffset.UtcNow)
                {
                    rejected.Add(new(input.Id, "Scan time cannot be in the future."));
                    continue;
                }

                if (known.TryGetValue(input.Id, out var old))
                {
                    if (old.ShipmentId == shipment.Id && old.ScanType == input.ScanType && old.CourierId == input.CourierId && old.Location == input.Location && old.ScannedAtUtc == input.ScannedAtUtc && old.Notes == input.Notes)
                        duplicates.Add(input.Id);
                    else
                        rejected.Add(new(input.Id, "Scan ID already exists with different content or shipment."));
                    continue;
                }

                var row = new CourierScanRecord
                {
                    Id = input.Id,
                    ShipmentId = shipment.Id,
                    ScanType = input.ScanType,
                    CourierId = input.CourierId,
                    Location = input.Location,
                    ScannedAtUtc = input.ScannedAtUtc.ToUniversalTime(),
                    Notes = input.Notes
                };
                db.Add(row);
                known.Add(row.Id, row);
                accepted.Add(row.Id);
            }

            db.Add(new OperationReceipt { Id = receipt });
            await db.SaveChangesAsync(false, token);
            response = new(accepted.Count, duplicates.Count, rejected.Count, accepted, duplicates, rejected);
        }, token => db.Set<OperationReceipt>().AsNoTracking().AnyAsync(r => r.Id == receipt, token), ct);
        db.ChangeTracker.AcceptAllChanges();
        return response ?? throw new InvalidOperationException("Upload completed without a result.");
    }
}
