namespace ApexPost.Domain.ValueObjects;

public sealed record BranchCode
{
    public BranchCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Branch code is required.", nameof(value));
        }

        Value = value.Trim().ToUpperInvariant();
    }

    public string Value { get; }

    public override string ToString() => Value;
}
