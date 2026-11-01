using ApexPost.Application.Abstractions.Persistence;
using ApexPost.Application.Contracts;
using ApexPost.Domain.Enums;
using ApexPost.Domain.ValueObjects;
using ApexPost.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApexPost.Infrastructure.Persistence.Repositories;
public sealed class EfShipmentStore(ApexPostDbContext db) : IShipmentStore
{
    public async Task<ShippingTariff?> GetRateAsync(Guid zoneId, ShippingServiceLevel level, CancellationToken ct)
    {
        var rate = await db.Set<ShippingRateRecord>().AsNoTracking().SingleOrDefaultAsync(r => r.ZoneId == zoneId && r.ServiceLevel == level, ct);
        return rate is null ? null : new ShippingTariff(rate.BaseRate, rate.PerKilogram);
    }

    public async Task AddAsync(AddShipmentCommand command, Money charge, CancellationToken ct)
    {
        await db.Set<ShipmentRecord>().AddAsync(new() { Id = Guid.NewGuid(), TransactionId = command.TransactionId, ZoneId = command.ZoneId, Weight = command.Weight, ServiceLevel = command.ServiceLevel, Charge = charge, RecipientName = command.Recipient, Address = new() { Street = command.Street, City = command.City, PostalCode = command.PostalCode } }, ct);
    }
}
