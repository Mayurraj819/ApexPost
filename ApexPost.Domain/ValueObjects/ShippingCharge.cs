namespace ApexPost.Domain.ValueObjects;
public static class ShippingCharge
{
    public static Money Calculate(Weight weight, Money baseRate, decimal perKilogram)
    {
        if (!weight.IsWithinParcelLimit || weight.Grams < 0 || perKilogram < 0)
            throw new ArgumentException("Invalid shipping weight or tariff.");
        return new Money(baseRate.Amount + decimal.Ceiling(weight.Grams / 1000m) * perKilogram, baseRate.Currency);
    }
}
