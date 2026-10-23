namespace ApexPost.Domain.ValueObjects;

public sealed record EmailAddress
{
    public EmailAddress(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !value.Contains('@', StringComparison.Ordinal))
        {
            throw new ArgumentException("A valid email address is required.", nameof(value));
        }

        Value = value.Trim().ToLowerInvariant();
    }

    public string Value { get; }

    public override string ToString() => Value;
}
