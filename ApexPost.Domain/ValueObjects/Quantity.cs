namespace ApexPost.Domain.ValueObjects;

public readonly record struct Quantity
{
    public Quantity(int value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Quantity cannot be negative.");
        }

        Value = value;
    }

    public int Value { get; }

    public static implicit operator int(Quantity quantity) => quantity.Value;
}
