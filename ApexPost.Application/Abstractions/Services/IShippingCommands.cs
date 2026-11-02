using ApexPost.Application.Contracts;

namespace ApexPost.Application.Abstractions.Services;
public interface IShippingCommands
{
    Task AddAsync(AddShipmentCommand command, CancellationToken ct);
}
