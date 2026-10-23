using ApexPost.Application.Abstractions.Persistence;
using ApexPost.Application.Abstractions.Services;
using ApexPost.Domain.Entities;
using ApexPost.Domain.ValueObjects;

namespace ApexPost.Application.Services;

public sealed class InventoryService : IInventoryService
{
    private readonly IBranchStockRepository _stock;
    private readonly IProductRepository _products;

    public InventoryService(IBranchStockRepository stock, IProductRepository products)
    {
        _stock = stock;
        _products = products;
    }

    public Task<BranchStock?> GetAsync(Guid branchId, Guid productId, CancellationToken cancellationToken = default) =>
        _stock.GetByBranchAndProductAsync(branchId, productId, cancellationToken);

    public async Task<IReadOnlyList<BranchStock>> ListByBranchAsync(Guid branchId, CancellationToken cancellationToken = default)
    {
        if (branchId == Guid.Empty)
        {
            throw new ArgumentException("Branch ID cannot be empty.", nameof(branchId));
        }

        return (await _stock.ListAsync(cancellationToken)).Where(item => item.BranchId == branchId).ToArray();
    }

    public async Task<BranchStock> AdjustAsync(Guid branchId, Guid productId, int delta, CancellationToken cancellationToken = default)
    {
        ValidateId(branchId, nameof(branchId));
        ValidateId(productId, nameof(productId));
        if (delta == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(delta), "Inventory adjustment cannot be zero.");
        }

        if (await _products.GetByIdAsync(productId, cancellationToken) is null)
        {
            throw new KeyNotFoundException($"Product '{productId}' was not found.");
        }

        var stock = await _stock.GetByBranchAndProductAsync(branchId, productId, cancellationToken);
        if (stock is null)
        {
            if (delta < 0)
            {
                throw new InvalidOperationException("Cannot decrease inventory that has not been initialized.");
            }

            stock = new BranchStock(Guid.NewGuid(), branchId, productId, new Quantity(0));
            stock.Increase(delta);
            await _stock.AddAsync(stock, cancellationToken);
            return stock;
        }

        if (delta > 0)
        {
            stock.Increase(delta);
        }
        else
        {
            stock.Decrease(Math.Abs(delta));
        }

        await _stock.SaveAsync(stock, cancellationToken);
        return stock;
    }

    private static void ValidateId(Guid value, string name)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException($"{name} cannot be empty.", name);
        }
    }
}
