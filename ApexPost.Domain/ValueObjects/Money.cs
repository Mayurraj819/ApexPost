using System.Text.Json.Serialization;
namespace ApexPost.Domain.ValueObjects;

public readonly record struct Money
{
    [JsonConstructor]
    public Money(decimal amount, string? currency = "PHP")
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                "Money cannot be negative.");
        }

        Currency = string.IsNullOrWhiteSpace(currency)
            ? "PHP"
            : currency.Trim().ToUpperInvariant();

        Amount = decimal.Round(
            amount,
            2,
            MidpointRounding.ToEven);
    }

    public decimal Amount { get; }
    public string Currency { get; }
}
