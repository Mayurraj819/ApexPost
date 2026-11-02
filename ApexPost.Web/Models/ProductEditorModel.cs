using System.ComponentModel.DataAnnotations;
using ApexPost.Domain.Enums;

namespace ApexPost.Web.Models;
public sealed class ProductEditorModel
{
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
    public Guid Id { get; set; }

    [Required, StringLength(40)]
    public string Sku { get; set; } = "";

    [Required, StringLength(120)]
    public string Name { get; set; } = "";

    [Range(typeof(decimal), "0.01", "99999999")]
    public decimal Price { get; set; }

    [EnumDataType(typeof(ProductCategory))]
    public ProductCategory Category { get; set; }
}
