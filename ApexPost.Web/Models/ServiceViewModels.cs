using System.ComponentModel.DataAnnotations;
using ApexPost.Domain.Enums;

namespace ApexPost.Web.Models;

//public class ShippingLabelViewModel
//{
//    [Required]
//    public string RecipientName { get; set; } = string.Empty;
//    [Required]
//    public string DestinationAddress { get; set; } = string.Empty;
//    [Required]
//    public string DestinationZone { get; set; } = "Zone-1";
//    [Range(0.01, 30.0)]
//    public decimal WeightKg { get; set; }
//    public ShippingServiceLevel ServiceLevel { get; set; } = ShippingServiceLevel.Standard;
//    public decimal CalculatedQuote { get; set; }
//    public string TrackingNumber { get; set; } = string.Empty;
//}

public class MoneyOrderFormViewModel
{
    [Required]
    public string SenderName { get; set; } = string.Empty;
    [Required]
    public string RecipientName { get; set; } = string.Empty;
    [Range(1, 5000)]
    public decimal Amount { get; set; }
    public decimal Fee => Amount > 500 ? 15.00m : 5.00m;

    // Identity Verification (Triggered if Amount >= 1000)
    public bool RequiresIdVerification => Amount >= 1000m;
    public string? IdDocumentType { get; set; }
    public string? FullIdNumber { get; set; }
}

public class StockAdjustmentViewModel
{
    [Required]
    public string Sku { get; set; } = string.Empty;
    [Required]
    public Guid BranchId { get; set; }
    [Range(-500, 500)]
    public int AdjustmentQuantity { get; set; }
    [Required]
    public string Reason { get; set; } = string.Empty;
}