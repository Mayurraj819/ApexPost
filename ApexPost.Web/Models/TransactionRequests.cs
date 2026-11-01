using System.ComponentModel.DataAnnotations;

namespace ApexPost.Web.Models;
public sealed class StartTransactionRequest
{
    public Guid CounterId { get; set; }
}

public sealed class AddProductRequest
{
    public Guid ProductId { get; set; }

    [Range(1, 1000000)]
    public int Quantity { get; set; }
}

public sealed class PaymentRequest
{
    public bool Confirm { get; set; } = true;
}

public sealed class CorrectionRequest
{
    [Required, StringLength(500, MinimumLength = 1)]
    public string Reason { get; set; } = "";
}
