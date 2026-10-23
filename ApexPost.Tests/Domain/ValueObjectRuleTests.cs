using ApexPost.Domain.ValueObjects;

namespace ApexPost.Tests.Domain;

public sealed class ValueObjectRuleTests
{
    [Fact]
    public void OpeningHours_ExcludesClosingTime()
    {
        var hours = new OpeningHours(new TimeOnly(8, 0), new TimeOnly(17, 0));

        Assert.True(hours.Contains(new TimeOnly(8, 0)));
        Assert.False(hours.Contains(new TimeOnly(17, 0)));
    }

    [Fact]
    public void Weight_CannotBeNegative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Weight(-1));
    }
}
