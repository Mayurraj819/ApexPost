using ApexPost.Application.Contracts;
using ApexPost.Application.Services;
using ApexPost.Domain.Entities;
using ApexPost.Domain.Enums;
using ApexPost.Domain.ValueObjects;
using ApexPost.Tests.Application;

namespace ApexPost.Tests;

public sealed class DomainApplicationTests
{
    [Fact]
    public void StockCannotBeReducedBelowZero()
    {
        var stock = new BranchStock(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new Quantity(1));

        Assert.Throws<InvalidOperationException>(() => stock.Decrease(2));
    }

    [Fact]
    public async Task IdentityVerificationRejectsDifferentFingerprint()
    {
        var first = IdentityDocument.Create("Passport", "P123456789");
        var second = IdentityDocument.Create("Passport", "P987654321");
        var service = new IdentityVerificationService();

        var result = await service.VerifyAsync(new IdentityVerificationRequest(first, second.Fingerprint));

        Assert.False(result.IsVerified);
        Assert.Equal("***6789", result.MaskedNumber);
    }

    [Fact]
    public async Task ShippingPricingRejectsParcelAboveMaximumWeight()
    {
        var service = new ShippingPricingService();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.CalculateAsync(
            new ShippingQuoteRequest(new Weight(Weight.MaxParcelGrams + 1), ShippingServiceLevel.Standard, "ZONE-1")));
    }

    [Fact]
    public async Task ShippingPricingCalculatesWeightTieredRate()
    {
        var service = new ShippingPricingService();

        var quote = await service.CalculateAsync(
            new ShippingQuoteRequest(new Weight(2_001), ShippingServiceLevel.Registered, "ZONE-1"));

        Assert.Equal(145m, quote.Amount.Amount);
        Assert.Equal("PHP", quote.Amount.Currency);
    }
}
