using ApexPost.Domain.Common;
using ApexPost.Domain.Enums;
using ApexPost.Domain.ValueObjects;

namespace ApexPost.Domain.Entities;
public sealed class Product : Entity
{
    private Product()
    {
        Sku = null !;
        Name = null !;
    }

    public Product(Guid id, Sku sku, string name, Money unitPrice, ProductCategory category = ProductCategory.OfficeSupplies, Weight? weight = null) : base(id)
    {
        Sku = sku ?? throw new ArgumentNullException(nameof(sku));
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Product name is required.", nameof(name)) : name.Trim();
        UnitPrice = unitPrice;
        Category = category;
        Weight = weight;
    }

    public Sku Sku { get; private set; }
    public string Name { get; private set; }
    public Money UnitPrice { get; private set; }
    public ProductCategory Category { get; private set; }
    public Weight? Weight { get; }

    public void Revise(Sku sku, string name, Money price, ProductCategory category)
    {
        if (string.IsNullOrWhiteSpace(name) || price.Amount <= 0 || !Enum.IsDefined(category))
            throw new ArgumentException("Valid name, category and positive price are required.");
        Sku = sku;
        Name = name.Trim();
        UnitPrice = price;
        Category = category;
    }
}
