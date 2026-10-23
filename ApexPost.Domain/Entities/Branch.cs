using ApexPost.Domain.Common;
using ApexPost.Domain.ValueObjects;

namespace ApexPost.Domain.Entities;

public sealed class Branch : Entity
{
    public Branch(Guid id, BranchCode code, string name, string address, OpeningHours? openingHours = null) : base(id)
    {
        Code = code ?? throw new ArgumentNullException(nameof(code));
        Name = RequireText(name, nameof(name));
        Address = RequireText(address, nameof(address));
        OpeningHours = openingHours ?? new OpeningHours(new TimeOnly(8, 0), new TimeOnly(17, 0));
        IsActive = true;
    }

    public BranchCode Code { get; }
    public string Name { get; private set; }
    public string Address { get; private set; }
    public OpeningHours OpeningHours { get; }
    public bool IsActive { get; private set; }

    public void Close() => IsActive = false;

    public void Reopen() => IsActive = true;

    public bool CanAcceptTransactions(DateTimeOffset at) => IsActive && OpeningHours.Contains(new TimeOnly(at.Hour, at.Minute, at.Second));

    private static string RequireText(string value, string parameterName) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Value is required.", parameterName) : value.Trim();
}
