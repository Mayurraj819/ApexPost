using System.Text.Json.Serialization;
namespace ApexPost.Domain.ValueObjects;

public readonly record struct Weight
{
    public const decimal MaxParcelGrams = 30_000m;

    [JsonConstructor]
    public Weight(decimal grams)
    {
        if (grams < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(grams), "Weight cannot be negative.");
        }

        Grams = decimal.Round(grams, 3, MidpointRounding.ToEven);
    }

    public decimal Grams { get; }

    public bool IsWithinParcelLimit => Grams <= MaxParcelGrams;
}
