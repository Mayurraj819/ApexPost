using System.Text.RegularExpressions;

namespace ApexPost.Domain.Delivery;
public enum DeliveryStatus
{
    Created,
    PickedUp,
    InTransit,
    OutForDelivery,
    Delivered,
    Failed
}

public enum CourierScanType
{
    Pickup,
    InTransit,
    Delivered,
    Failed
}

// Operational delivery tracking, separate from the counter sale/pricing aggregate.
public sealed class TrackedShipment
{
    public Guid Id { get; private set; }
    public string TrackingNumber { get; private set; } = "";
    public decimal WeightKg { get; private set; }
    public string RecipientName { get; private set; } = "";
    public string RecipientPhone { get; private set; } = "";
    public string Address { get; private set; } = "";
    public string City { get; private set; } = "";
    public DeliveryStatus Status { get; private set; }

    private TrackedShipment()
    {
    }

    public TrackedShipment(string trackingNumber, decimal weightKg, string name, string phone, string address, string city)
    {
        if (!Regex.IsMatch(trackingNumber ?? "", @"\AAPX-[0-9]{8}\z"))
            throw new ArgumentException("Tracking number must be APX- followed by eight digits.");
        Id = Guid.NewGuid();
        TrackingNumber = trackingNumber!;
        ReplaceDetails(weightKg, name, phone, address, city);
    }

    public void ReplaceDetails(decimal weightKg, string name, string phone, string address, string city)
    {
        if (weightKg < 0.1m || weightKg > 70m)
            throw new ArgumentException("Weight must be between 0.1 and 70 kg.");
        foreach (var value in new[]
        {
            name,
            phone,
            address,
            city
        }

        )
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (!Regex.IsMatch(phone, @"\A\+?[1-9][0-9]{6,14}\z"))
            throw new ArgumentException("Phone format is invalid.");
        if (name.Length > 120 || phone.Length > 25 || address.Length > 300 || city.Length > 120)
            throw new ArgumentException("Recipient details exceed their maximum length.");
        WeightKg = weightKg;
        RecipientName = name;
        RecipientPhone = phone;
        Address = address;
        City = city;
    }

    public void ChangeStatus(DeliveryStatus next)
    {
        var allowed = (Status, next) switch
        {
            (DeliveryStatus.Created, DeliveryStatus.PickedUp) => true,
            (DeliveryStatus.PickedUp, DeliveryStatus.InTransit) => true,
            (DeliveryStatus.InTransit, DeliveryStatus.OutForDelivery) => true,
            (DeliveryStatus.OutForDelivery, DeliveryStatus.Delivered or DeliveryStatus.Failed) => true,
            _ => false
        };
        if (!allowed)
            throw new DeliveryConflictException($"Cannot change shipment status from {Status} to {next}.");
        Status = next;
    }

    public void EnsureCanDelete()
    {
        if (Status != DeliveryStatus.Created)
            throw new DeliveryConflictException("Only a Created shipment can be deleted.");
    }
}

public sealed class DeliveryConflictException(string message) : Exception(message);
