namespace ApexPost.Domain.ValueObjects;

public static class MoneyExtensions
{
    // C# 14 extension members syntax
    extension(Money money)
    {
        public Money ApplyTax(decimal taxRate)
        {
            var taxAmount = Math.Round(money.Amount * taxRate, 2, MidpointRounding.AwayFromZero);
            return new Money(taxAmount, money.Currency);
        }

        public Money RoundToCurrencyStandard()
        {
            return new Money(Math.Round(money.Amount, 2, MidpointRounding.ToEven), money.Currency);
        }

        public bool Exceeds(decimal threshold) => money.Amount > threshold;
    }
}