using ApexPost.Application.Contracts;
using ApexPost.Domain.ValueObjects;

namespace ApexPost.Application.Abstractions.Persistence;
public interface IShipmentStore
{
    Task<ShippingTariff?> GetRateAsync(Guid zoneId, ApexPost.Domain.Enums.ShippingServiceLevel level, CancellationToken ct);
    Task AddAsync(AddShipmentCommand command, Money charge, CancellationToken ct);
}
