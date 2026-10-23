using ApexPost.Domain.Entities;
using ApexPost.Domain.ValueObjects;

namespace ApexPost.Tests.Domain;

public sealed class EntityTests
{
    [Fact]
    public void Branch_StoresValidatedIdentityAndDetails()
    {
        var branch = new Branch(Guid.NewGuid(), new BranchCode("manila"), "  Main Branch ", " 1 Main Street ");

        Assert.Equal("MANILA", branch.Code.Value);
        Assert.Equal("Main Branch", branch.Name);
        Assert.Equal("1 Main Street", branch.Address);
    }

    [Fact]
    public void Counter_RejectsNonPositiveNumber()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Counter(Guid.NewGuid(), Guid.NewGuid(), 0, "Front counter"));
    }
}
