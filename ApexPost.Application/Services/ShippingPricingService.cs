using ApexPost.Application.Abstractions.Services;
using ApexPost.Application.Contracts;
using ApexPost.Domain.Enums;
using ApexPost.Domain.ValueObjects;

namespace ApexPost.Application.Services;
public sealed class ShippingPricingService : IShippingPricingService
{
    public async Task<ShippingQuote> CalculateAsync(ShippingQuoteRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(request.DestinationZone))
        {
            throw new ArgumentException("Destination zone is required.", nameof(request));
        }

        if (!request.Weight.IsWithinParcelLimit)
        {
            throw new ArgumentOutOfRangeException(nameof(request), $"Parcel weight cannot exceed {Weight.MaxParcelGrams} grams.");
        }

        var(baseRate, perKilogram) = request.ServiceLevel switch
        {
            ShippingServiceLevel.Standard => (50m, 20m),
            ShippingServiceLevel.Express => (85m, 35m),
            ShippingServiceLevel.Registered => (70m, 25m),
            _ => throw new ArgumentOutOfRangeException(nameof(request), "Unsupported shipping service level.")};
        var amount = ShippingCharge.Calculate(request.Weight, new Money(baseRate), perKilogram);
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();
        return new ShippingQuote(amount, request.ServiceLevel, request.Weight, request.DestinationZone.Trim());
    }
}
