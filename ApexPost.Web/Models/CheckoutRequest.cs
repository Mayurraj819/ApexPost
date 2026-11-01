using System.ComponentModel.DataAnnotations;

namespace ApexPost.Web.Models;
public sealed class CheckoutRequest
{
    public Guid CounterId { get; set; }

    [MinLength(1), MaxLength(100)]
    public List<CheckoutLineRequest> Lines { get; set; } = [];
}

public sealed class CheckoutLineRequest
{
    public Guid ProductId { get; set; }

    [Range(1, 1000000)]
    public int Quantity { get; set; }
}
