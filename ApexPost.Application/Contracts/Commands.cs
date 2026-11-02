using ApexPost.Domain.Enums;

namespace ApexPost.Application.Contracts;
public sealed record SaveProductCommand(Guid? Id, string Sku, string Name, decimal Price, ProductCategory Category);
public sealed record SetStockCommand(Guid BranchId, Guid ProductId, int Quantity);
