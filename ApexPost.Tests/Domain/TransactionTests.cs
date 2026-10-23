using ApexPost.Domain.Entities;
using ApexPost.Domain.ValueObjects;

namespace ApexPost.Tests.Domain;

public sealed class TransactionTests
{
    [Fact]
    public void Transaction_CalculatesTotalFromLines()
    {
        var transaction = new Transaction(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        transaction.AddLine(new TransactionLine(Guid.NewGuid(), new Quantity(2), new Money(12.50m)));
        transaction.AddLine(new TransactionLine(Guid.NewGuid(), new Quantity(1), new Money(5m)));

        Assert.Equal(30m, transaction.Total.Amount);
        Assert.Equal("PHP", transaction.Total.Currency);
    }

    [Fact]
    public void TransactionLine_RejectsZeroQuantity()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TransactionLine(Guid.NewGuid(), new Quantity(0), new Money(10m)));
    }
}
