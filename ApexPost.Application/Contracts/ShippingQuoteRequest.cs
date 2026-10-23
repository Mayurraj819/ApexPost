using ApexPost.Domain.Enums;
using ApexPost.Domain.ValueObjects;

namespace ApexPost.Application.Contracts;

public sealed record ShippingQuoteRequest(Weight Weight, ShippingServiceLevel ServiceLevel, string DestinationZone);

public sealed record ShippingQuote(Money Amount, ShippingServiceLevel ServiceLevel, Weight Weight, string DestinationZone);
