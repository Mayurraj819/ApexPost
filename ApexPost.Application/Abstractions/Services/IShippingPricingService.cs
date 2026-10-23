using ApexPost.Application.Contracts;

namespace ApexPost.Application.Abstractions.Services;

public interface IShippingPricingService
{
    Task<ShippingQuote> CalculateAsync(ShippingQuoteRequest request, CancellationToken cancellationToken = default);
}
