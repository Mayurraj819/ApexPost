using ApexPost.Domain.Delivery;

namespace ApexPost.UnitTests;

public sealed class DeliveredShipmentTests
{
    [Fact]
    public void ReplaceDetails_WhenDelivered_ThrowsAndPreservesDetails()
    {
        // Arrange
        var shipment = new TrackedShipment(
            "APX-12345678",
            1.25m,
            "Original Recipient",
            "+919876543210",
            "12 Demo Street",
            "Pune");

        shipment.ChangeStatus(DeliveryStatus.PickedUp);
        shipment.ChangeStatus(DeliveryStatus.InTransit);
        shipment.ChangeStatus(DeliveryStatus.OutForDelivery);
        shipment.ChangeStatus(DeliveryStatus.Delivered);

        // Act
        Action replaceDetails = () => shipment.ReplaceDetails(
            2m,
            "Changed Recipient",
            "+919876543211",
            "99 Other Street",
            "Mumbai");

        // Assert
        Assert.Throws<DeliveryConflictException>(replaceDetails);
        Assert.Equal(1.25m, shipment.WeightKg);
        Assert.Equal("Original Recipient", shipment.RecipientName);
        Assert.Equal("+919876543210", shipment.RecipientPhone);
        Assert.Equal("12 Demo Street", shipment.Address);
        Assert.Equal("Pune", shipment.City);
        Assert.Equal(DeliveryStatus.Delivered, shipment.Status);
    }
}