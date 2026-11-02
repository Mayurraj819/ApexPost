using System.ComponentModel.DataAnnotations;

namespace ApexPost.Web.Models;
public sealed class StockDeltaRequest
{
    public Guid ProductId { get; set; }

    [Range(-1000000, 1000000)]
    public int Delta { get; set; }

    [Required, StringLength(500)]
    public string Reason { get; set; } = "";
}

public sealed class PriceRevisionRequest
{
    [Range(typeof(decimal), "0.01", "10")]
    public decimal Factor { get; set; }
}
