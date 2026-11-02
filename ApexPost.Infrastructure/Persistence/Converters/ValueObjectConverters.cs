using ApexPost.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ApexPost.Infrastructure.Persistence.Converters;
public sealed class EmailConverter() : ValueConverter<EmailAddress, string>(v => v.Value, v => new EmailAddress(v));
public sealed class SkuConverter() : ValueConverter<Sku, string>(v => v.Value, v => new Sku(v));
public sealed class BranchCodeConverter() : ValueConverter<BranchCode, string>(v => v.Value, v => new BranchCode(v));
public sealed class WeightConverter() : ValueConverter<Weight, decimal>(v => v.Grams, v => new Weight(v));
public sealed class QuantityConverter() : ValueConverter<Quantity, int>(v => v.Value, v => new Quantity(v));
// Shipping tariffs are explicitly PHP; other Money fields preserve Amount AND Currency as complex values.
public sealed class PhpMoneyConverter() : ValueConverter<Money, decimal>(v => ToAmount(v), v => new Money(v, "PHP"))
{
    private static decimal ToAmount(Money value) => value.Currency == "PHP" ? value.Amount : throw new ArgumentException("Shipping tariff currency must be PHP.");
}

public sealed class UtcDateTimeOffsetConverter() : ValueConverter<DateTimeOffset, DateTime>(v => v.UtcDateTime, v => new DateTimeOffset(DateTime.SpecifyKind(v, DateTimeKind.Utc)));
