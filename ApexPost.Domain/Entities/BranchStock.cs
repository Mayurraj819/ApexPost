using ApexPost.Domain.Common;
using ApexPost.Domain.ValueObjects;

namespace ApexPost.Domain.Entities;
public sealed class BranchStock : Entity
{
    private BranchStock()
    {
    }

    public BranchStock(Guid id, Guid branchId, Guid productId, Quantity quantity) : base(id)
    {
        if (branchId == Guid.Empty)
        {
            throw new ArgumentException("Branch ID cannot be empty.", nameof(branchId));
        }

        if (productId == Guid.Empty)
        {
            throw new ArgumentException("Product ID cannot be empty.", nameof(productId));
        }

        BranchId = branchId;
        ProductId = productId;
        _quantity = quantity.Value;
    }

    public Guid BranchId { get; }
    public Guid ProductId { get; }

    private int _quantity;
    public Quantity Quantity => new(_quantity);

    public void Increase(int amount)
    {
        ValidatePositive(amount);
        _quantity = checked(_quantity + amount);
    }

    public void Decrease(int amount)
    {
        ValidatePositive(amount);
        if (amount > Quantity.Value)
        {
            throw new InvalidOperationException("Branch stock cannot become negative.");
        }

        _quantity -= amount;
    }

    private static void ValidatePositive(int amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Stock adjustment must be positive.");
        }
    }
}
