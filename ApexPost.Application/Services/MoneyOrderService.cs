using ApexPost.Application.Abstractions.Persistence;
using ApexPost.Application.Abstractions.Services;
using ApexPost.Application.Contracts;

namespace ApexPost.Application.Services;
public sealed class MoneyOrderService : IMoneyOrderService
{
    private readonly IBranchRepository _branches;
    private readonly IStaffRepository _staff;
    private readonly IMoneyOrderRepository _moneyOrders;
    private readonly IUnitOfWork _unitOfWork;
    public MoneyOrderService(IBranchRepository branches, IStaffRepository staff, IMoneyOrderRepository moneyOrders, IUnitOfWork unitOfWork)
    {
        _branches = branches;
        _staff = staff;
        _moneyOrders = moneyOrders;
        _unitOfWork = unitOfWork;
    }

    public async Task<MoneyOrderResult> CreateAsync(MoneyOrderRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Amount.Amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(request), "Money order amount must be positive.");
        if (string.IsNullOrWhiteSpace(request.PayeeName))
            throw new ArgumentException("Payee name is required.", nameof(request));
        ArgumentNullException.ThrowIfNull(request.IdentityDocument);
        var branch = await _branches.GetByIdAsync(request.BranchId, cancellationToken) ?? throw new KeyNotFoundException($"Branch '{request.BranchId}' was not found.");
        var staff = await _staff.GetByIdAsync(request.StaffId, cancellationToken) ?? throw new KeyNotFoundException($"Staff '{request.StaffId}' was not found.");
        var occurredAt = request.OccurredAt ?? DateTimeOffset.UtcNow;
        if (!staff.CanProcessAt(branch.Id) || !branch.CanAcceptTransactions(occurredAt))
            throw new ApexPost.Application.Exceptions.DomainRuleException("Staff and branch are not authorized for this money order.");
        // Current contract only carries a self-reported document fingerprint.
        // Do not call this an independently verified identity.
        // Block high-value issuance until an authoritative verifier is integrated.
        if (request.Amount.Amount >= 10000m)
            throw new ApexPost.Application.Exceptions.DomainRuleException("High-value money orders require independent identity verification. " + "Issuance is disabled until a trusted identity provider is configured.");
        var result = new MoneyOrderResult(Guid.NewGuid(), branch.Id, staff.Id, request.Amount, request.PayeeName.Trim(), request.IdentityDocument.Fingerprint, occurredAt)
        {
            MaskedNumber = request.IdentityDocument.MaskedNumber,
            DocumentType = request.IdentityDocument.DocumentType
        };
        await _moneyOrders.AddAsync(result, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return result;
    }
}
