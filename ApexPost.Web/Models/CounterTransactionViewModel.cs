using System.ComponentModel.DataAnnotations;
using ApexPost.Domain.Entities;
using ApexPost.Domain.Enums;

namespace ApexPost.Web.Models;

public class NewTransactionViewModel
{
    public Guid BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string CounterNumber { get; set; } = "C-01";
    public List<BasketItemViewModel> Items { get; set; } = new();

    public decimal SubTotal => Items.Sum(i => i.Total);
    public decimal TaxTotal => Items.Sum(i => i.Tax);
    public decimal GrandTotal => SubTotal + TaxTotal;

    [Required]
    public string PaymentMethod { get; set; } = "Cash";

    // For Adding Products
    public string? SelectedSku { get; set; }
    public int QuantityToAdd { get; set; } = 1;
    public List<Product> AvailableProducts { get; set; } = new();
}

public class BasketItemViewModel
{
    public string Sku { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TransactionLineType LineType { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal TaxRate { get; set; }

    public decimal Total => UnitPrice * Quantity;
    public decimal Tax => Math.Round(Total * TaxRate, 2);
}

public class VoidOrRefundViewModel
{
    [Required]
    public Guid TransactionId { get; set; }

    [Required]
    [MinLength(5, ErrorMessage = "A valid business justification is required.")]
    public string Reason { get; set; } = string.Empty;

    public bool IsRefund { get; set; }
}