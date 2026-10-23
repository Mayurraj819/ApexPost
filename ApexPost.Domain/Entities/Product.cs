using ApexPost.Domain.Common;
using ApexPost.Domain.Enums;
using ApexPost.Domain.ValueObjects;

namespace ApexPost.Domain.Entities;

public sealed class Product : Entity
{
    public Product(Guid id, Sku sku, string name, Money unitPrice, ProductCategory category = ProductCategory.OfficeSupplies, Weight? weight = null) : base(id)
    {
        Sku = sku ?? throw new ArgumentNullException(nameof(sku));
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Product name is required.", nameof(name)) : name.Trim();
        UnitPrice = unitPrice;
        Category = category;
        Weight = weight;
    }

    public Sku Sku { get; }
    public string Name { get; }
    public Money UnitPrice { get; }
    public ProductCategory Category { get; }
    public Weight? Weight { get; }
}
