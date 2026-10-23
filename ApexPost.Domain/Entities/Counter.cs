using ApexPost.Domain.Common;

namespace ApexPost.Domain.Entities;

public sealed class Counter : Entity
{
    public Counter(Guid id, Guid branchId, int number, string name) : base(id)
    {
        if (branchId == Guid.Empty)
        {
            throw new ArgumentException("Branch ID cannot be empty.", nameof(branchId));
        }

        if (number <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(number), "Counter number must be positive.");
        }

        BranchId = branchId;
        Number = number;
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Counter name is required.", nameof(name)) : name.Trim();
    }

    public Guid BranchId { get; }
    public int Number { get; }
    public string Name { get; }
}
