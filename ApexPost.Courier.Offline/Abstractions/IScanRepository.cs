using ApexPost.Courier.Offline.Models;

namespace ApexPost.Courier.Offline.Abstractions;

public interface IScanRepository
{
    ShipmentScan Create(ShipmentScan scan);

    ShipmentScan? GetById(string id);

    void Update(ShipmentScan scan);

    bool Delete(string id);

    IReadOnlyList<ShipmentScan> ListAll();

    IReadOnlyList<ShipmentScan> FindByTrackingNumber(string trackingNumber);

    IReadOnlyList<ShipmentScan> ListPending();

    IReadOnlyDictionary<ScanType, long> CountByScanType(string courierId);
}