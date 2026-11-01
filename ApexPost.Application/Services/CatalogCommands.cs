using ApexPost.Application.Abstractions.Persistence;
using ApexPost.Application.Abstractions.Services;
using ApexPost.Application.Contracts;
using ApexPost.Domain.Entities;
using ApexPost.Domain.ValueObjects;

namespace ApexPost.Application.Services;
public sealed class CatalogCommands(IProductRepository products, IBranchStockRepository stock, ITransactionRepository transactions, IUnitOfWork unitOfWork, IStockAdjustmentWriter adjustments, ICurrentActor actor) : ICatalogCommands
{
    public async Task SaveAsync(SaveProductCommand command, CancellationToken ct)
    {
        if (command.Price <= 0 || !Enum.IsDefined(command.Category))
            throw new ArgumentException("Positive price and valid category required.");
        var sku = new Sku(command.Sku);
        // Database UNIQUE is authoritative for concurrent requests.
        if (await products.SkuExistsAsync(sku, command.Id, ct))
            throw new ArgumentException("SKU already exists.");
        if (command.Id is Guid id)
        {
            var existing = await products.GetByIdAsync(id, ct) ?? throw new KeyNotFoundException("Product not found.");
            existing.Revise(sku, command.Name, new Money(command.Price, existing.UnitPrice.Currency), command.Category);
            await products.SaveAsync(existing, ct);
        }
        else
            await products.AddAsync(new Product(Guid.NewGuid(), sku, command.Name, new Money(command.Price), command.Category), ct);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        _ = await products.GetByIdAsync(id, ct) ?? throw new KeyNotFoundException("Product not found.");
        if (await transactions.HasProductHistoryAsync(id, ct) || await products.HasStockAdjustmentHistoryAsync(id, ct))
            throw new ArgumentException("Product has transaction or stock-adjustment history.");
        var records = (await stock.ListByProductAsync(id, ct)).ToArray();
        if (records.Any(s => s.Quantity.Value > 0))
            throw new ArgumentException("Product still has stock.");
        foreach (var s in records)
            await stock.DeleteAsync(s.Id, ct);
        await products.DeleteAsync(id, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task SetStockAsync(SetStockCommand command, CancellationToken ct)
    {
        if (command.Quantity < 0 || command.Quantity > 1000000)
            throw new ArgumentException("Quantity must be between zero and 1000000.");
        _ = await products.GetByIdAsync(command.ProductId, ct) ?? throw new KeyNotFoundException("Product not found.");
        var item = await stock.GetByBranchAndProductAsync(command.BranchId, command.ProductId, ct);
        var change = command.Quantity - (item?.Quantity.Value ?? 0);
        if (item is null)
            await stock.AddAsync(new BranchStock(Guid.NewGuid(), command.BranchId, command.ProductId, new Quantity(command.Quantity)), ct);
        else
        {
            var delta = command.Quantity - item.Quantity.Value;
            if (delta > 0)
                item.Increase(delta);
            else if (delta < 0)
                item.Decrease(-delta);
            await stock.SaveAsync(item, ct);
        }

        await adjustments.StageAsync(command.BranchId, command.ProductId, change, "Manual stock set", actor.Id, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task RemoveEmptyStockAsync(Guid branchId, Guid productId, CancellationToken ct)
    {
        var record = await stock.GetByBranchAndProductAsync(branchId, productId, ct) ?? throw new KeyNotFoundException("Stock record not found.");
        if (record.Quantity.Value != 0)
            throw new ArgumentException("Only zero-stock records can be removed.");
        await stock.DeleteAsync(record.Id, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }
}
