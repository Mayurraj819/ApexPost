using ApexPost.Domain.Entities;

namespace ApexPost.Application.Abstractions.Persistence;
public interface IPaymentRepository
{
    Task AddAsync(Payment payment, CancellationToken cancellationToken = default);
}
