using ApexPost.Domain.Delivery;

namespace ApexPost.Api.Tests;
public sealed class WorkflowTests
{
    [Theory]
    [InlineData(DeliveryStatus.Created, DeliveryStatus.PickedUp)]
    [InlineData(DeliveryStatus.PickedUp, DeliveryStatus.InTransit)]
    [InlineData(DeliveryStatus.InTransit, DeliveryStatus.OutForDelivery)]
    [InlineData(DeliveryStatus.OutForDelivery, DeliveryStatus.Delivered)]
    [InlineData(DeliveryStatus.OutForDelivery, DeliveryStatus.Failed)]
    public void Only_declared_transitions_are_allowed(DeliveryStatus initial, DeliveryStatus next)
    {
        var shipment = New();
        foreach (var status in new[]
        {
            DeliveryStatus.PickedUp,
            DeliveryStatus.InTransit,
            DeliveryStatus.OutForDelivery
        }

        )
        {
            if (shipment.Status == initial)
                break;
            shipment.ChangeStatus(status);
        }

        shipment.ChangeStatus(next);
        Assert.Equal(next, shipment.Status);
    }

    [Theory]
    [InlineData(DeliveryStatus.Created)]
    [InlineData(DeliveryStatus.InTransit)]
    [InlineData(DeliveryStatus.Delivered)]
    [InlineData(DeliveryStatus.Failed)]
    public void Created_cannot_skip_workflow_or_repeat_itself(DeliveryStatus status) => Assert.Throws<DeliveryConflictException>(() => New().ChangeStatus(status));
    [Fact]
    public void Only_created_can_be_deleted()
    {
        var s = New();
        s.EnsureCanDelete();
        s.ChangeStatus(DeliveryStatus.PickedUp);
        Assert.Throws<DeliveryConflictException>(s.EnsureCanDelete);
    }

    [Theory]
    [InlineData("APX-123", 1)]
    [InlineData("APX-12345678", 0)]
    [InlineData("APX-12345678", 71)]
    public void Domain_also_validates_api_inputs(string tracking, int weight) => Assert.Throws<ArgumentException>(() => new TrackedShipment(tracking, weight, "Fictional", "+919876543210", "Demo street", "Pune"));
    internal static TrackedShipment New() => new("APX-12345678", 1, "Fictional", "+919876543210", "Demo street", "Pune");
}
