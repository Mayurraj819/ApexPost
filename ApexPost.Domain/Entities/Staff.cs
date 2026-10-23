using ApexPost.Domain.Common;
using ApexPost.Domain.Enums;
using ApexPost.Domain.ValueObjects;

namespace ApexPost.Domain.Entities;

public sealed class Staff : Entity
{
    public Staff(Guid id, Guid branchId, string employeeNumber, string fullName, EmailAddress email, StaffRole role = StaffRole.CounterClerk, IdentityDocument? identityDocument = null) : base(id)
    {
        if (branchId == Guid.Empty)
        {
            throw new ArgumentException("Branch ID cannot be empty.", nameof(branchId));
        }

        BranchId = branchId;
        EmployeeNumber = RequireText(employeeNumber, nameof(employeeNumber));
        FullName = RequireText(fullName, nameof(fullName));
        Email = email ?? throw new ArgumentNullException(nameof(email));
        Role = role;
        IdentityDocument = identityDocument;
        IsActive = true;
    }

    public Guid BranchId { get; }
    public string EmployeeNumber { get; }
    public string FullName { get; }
    public EmailAddress Email { get; }
    public StaffRole Role { get; }
    public IdentityDocument? IdentityDocument { get; }
    public bool IsActive { get; private set; }

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;

    public bool CanProcessAt(Guid branchId) => IsActive && BranchId == branchId;

    public bool CanApproveCorrections(Guid branchId) => CanProcessAt(branchId) && Role == StaffRole.BranchManager;

    private static string RequireText(string value, string parameterName) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Value is required.", parameterName) : value.Trim();
}
