using ApexPost.Domain.Enums;
using ApexPost.Domain.ValueObjects;

namespace ApexPost.Application.Contracts;
public sealed record AddShipmentCommand(Guid TransactionId, Guid BranchId, Guid ZoneId, Weight Weight, ShippingServiceLevel ServiceLevel, string Recipient, string Street, string City, string PostalCode);
public sealed record ShippingTariff(Money BaseRate, decimal PerKilogram);
