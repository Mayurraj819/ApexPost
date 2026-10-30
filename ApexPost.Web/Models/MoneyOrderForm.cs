using System.ComponentModel.DataAnnotations;

namespace ApexPost.Web.Models;

public sealed class MoneyOrderForm
{
    [Range(typeof(decimal), "0.01", "100000000")] public decimal Amount { get; set; }
    [Required, StringLength(120)] public string PayeeName { get; set; } = string.Empty;
    [Required, StringLength(40)] public string DocumentType { get; set; } = string.Empty;
    [Required, StringLength(120)] public string DocumentNumber { get; set; } = string.Empty;
    [Required, StringLength(120)] public string ConfirmDocumentNumber { get; set; } = string.Empty;
}
