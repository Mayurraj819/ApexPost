using System.ComponentModel.DataAnnotations;
namespace ApexPost.Web.Models;
public sealed class StockEditorModel
{
    [Required] public Guid ProductId { get; set; }
    [Range(0, 1000000)] public int Quantity { get; set; }
}
