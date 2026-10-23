namespace ApexPost.Domain.ValueObjects;

public sealed record Sku
{
    public Sku(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("SKU is required.", nameof(value));
        }

        Value = value.Trim().ToUpperInvariant();
    }

    public string Value { get; }

    public override string ToString() => Value;
}
