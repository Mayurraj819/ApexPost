using ApexPost.Domain.ValueObjects;

namespace ApexPost.Tests.PatchFinal;

public sealed class FinalMoneyOrderSafetyTests
{
    [Fact]
    public void Identity_Masks_Full_Document_Number()
    {
        var doc = IdentityDocument.Create("Passport", "ABC123456789");
        Assert.Equal("***6789", doc.MaskedNumber);
        Assert.DoesNotContain("ABC123456789", doc.MaskedNumber);
    }

    [Fact]
    public void Stock_Cannot_Become_Negative()
    {
        var stock = new ApexPost.Domain.Entities.BranchStock(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new Quantity(1));
        Assert.Throws<InvalidOperationException>(() => stock.Decrease(2));
    }
}
