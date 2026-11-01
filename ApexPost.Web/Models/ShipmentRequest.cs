using System.ComponentModel.DataAnnotations;
using ApexPost.Domain.Enums;

namespace ApexPost.Web.Models;
public sealed class ShipmentRequest
{
    public Guid TransactionId { get; set; }
    public Guid ZoneId { get; set; }

    [Range(typeof(decimal), "0.001", "30000")]
    public decimal WeightGrams { get; set; }

    [EnumDataType(typeof(ShippingServiceLevel))]
    public ShippingServiceLevel ServiceLevel { get; set; }

    [Required, StringLength(120)]
    public string Recipient { get; set; } = "";

    [Required, StringLength(200)]
    public string Street { get; set; } = "";

    [Required, StringLength(100)]
    public string City { get; set; } = "";

    [StringLength(20)]
    public string PostalCode { get; set; } = "";
}
