using ApexPost.Domain.ValueObjects;

namespace ApexPost.Tests.Domain;

public sealed class ValueObjectTests
{
    [Fact]
    public void BranchCode_NormalizesValue()
    {
        var code = new BranchCode("  cebu-01 ");

        Assert.Equal("CEBU-01", code.Value);
    }

    [Fact]
    public void Money_RejectsNegativeAmount()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Money(-0.01m));
    }

    [Fact]
    public void Quantity_RejectsNegativeValue()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Quantity(-1));
    }
}
