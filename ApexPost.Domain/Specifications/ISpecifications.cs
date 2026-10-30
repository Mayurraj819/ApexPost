using System.Linq.Expressions;

namespace ApexPost.Domain.Specifications;

public interface ISpecification<T>
{
    Expression<Func<T, bool>> Criteria { get; }
    bool IsSatisfiedBy(T entity);
}

public abstract class BaseSpecification<T> : ISpecification<T>
{
    public abstract Expression<Func<T, bool>> Criteria { get; }

    public virtual bool IsSatisfiedBy(T entity)
    {
        var predicate = Criteria.Compile();
        return predicate(entity);
    }
}