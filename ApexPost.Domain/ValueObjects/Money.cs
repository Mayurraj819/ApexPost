namespace ApexPost.Domain.ValueObjects;

public readonly record struct Money
{
    public Money(decimal amount, string currency = "PHP")
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Money cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Currency is required.", nameof(currency));
        }

        Amount = decimal.Round(amount, 2, MidpointRounding.ToEven);
        Currency = currency.Trim().ToUpperInvariant();
    }

    public decimal Amount { get; }
    public string Currency { get; }
}
