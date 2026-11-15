using System.Globalization;
using ApexPost.Domain.Delivery;

namespace ApexPost.UnitTests;

public sealed class ShipmentBoundaryTests
{
    [Theory]
    [InlineData("APX-00000000")]
    [InlineData("APX-99999999")]
    public void Constructor_TrackingDigitBoundaries_PreservesTrackingAndStartsCreated(string tracking)
    {
        // Arrange: all other fields are valid fictional values.
        // Act
        var shipment = Create(tracking);

        // Assert
        Assert.Equal(tracking, shipment.TrackingNumber);
        Assert.Equal(DeliveryStatus.Created, shipment.Status);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("apx-12345678")]
    [InlineData("APX-123456789")]
    [InlineData("APX-12345678\n")]
    [InlineData("APX-１２３４５６７８")]
    public void Constructor_InvalidTrackingFormat_ThrowsArgumentException(string? tracking)
    {
        // Arrange: preserve the raw invalid input; do not trim/normalize it in the test.
        // Act
        var error = Assert.Throws<ArgumentException>(() => Create(tracking!));

        // Assert
        Assert.Contains("Tracking number", error.Message);
    }

    [Theory]
    [InlineData("0.1")]
    [InlineData("70")]
    public void Constructor_WeightAtInclusiveLimits_PreservesExactWeight(string weightText)
    {
        // Arrange
        var weight = decimal.Parse(weightText, CultureInfo.InvariantCulture);

        // Act
        var shipment = Create(weight: weight);

        // Assert
        Assert.Equal(weight, shipment.WeightKg);
        Assert.Equal(DeliveryStatus.Created, shipment.Status);
    }

    [Theory]
    [InlineData("0.099999")]
    [InlineData("70.000001")]
    public void ReplaceDetails_WeightJustOutsideLimits_ThrowsAndPreservesAllDetails(string weightText)
    {
        // Arrange
        var shipment = Create();
        var weight = decimal.Parse(weightText, CultureInfo.InvariantCulture);
        var before = (shipment.TrackingNumber, shipment.WeightKg, shipment.RecipientName,
            shipment.RecipientPhone, shipment.Address, shipment.City, shipment.Status);

        // Act
        var error = Assert.Throws<ArgumentException>(() => shipment.ReplaceDetails(
            weight, "Changed Fictional Recipient", "+919876543211", "99 Fictional Road", "Mumbai"));

        // Assert
        Assert.Contains("Weight", error.Message);
        Assert.Equal(before, (shipment.TrackingNumber, shipment.WeightKg, shipment.RecipientName,
            shipment.RecipientPhone, shipment.Address, shipment.City, shipment.Status));
    }

    private static TrackedShipment Create(string tracking = "APX-12345678", decimal weight = 1.25m) =>
        new(tracking, weight, "Fictional Recipient", "+919876543210", "10 Fictional Road", "Pune");
}
