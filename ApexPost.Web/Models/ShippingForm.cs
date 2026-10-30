using System.ComponentModel.DataAnnotations;
using ApexPost.Domain.Enums;

namespace ApexPost.Web.Models;

public sealed class ShippingForm
{
    [Required] public Guid TransactionId { get; set; }
    [Range(typeof(decimal), "0.001", "30000")] public decimal WeightGrams { get; set; }
    [Required, StringLength(40)] public string DestinationZone { get; set; } = string.Empty;
    [EnumDataType(typeof(ShippingServiceLevel))]
    public ShippingServiceLevel ServiceLevel { get; set; } = ShippingServiceLevel.Standard;
}
