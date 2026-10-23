using ApexPost.Application.Contracts;

namespace ApexPost.Application.Abstractions.Services;

public interface IMoneyOrderService
{
    Task<MoneyOrderResult> CreateAsync(MoneyOrderRequest request, CancellationToken cancellationToken = default);
}
