using ApexPost.Domain.Common;
using ApexPost.Domain.Enums;
using ApexPost.Domain.ValueObjects;

namespace ApexPost.Domain.Entities;

public sealed class Transaction : Entity
{
    private readonly List<TransactionLine> _lines = [];

    public Transaction(Guid id, Guid branchId, Guid counterId, Guid staffId, DateTimeOffset occurredAt) : base(id)
    {
        ValidateId(branchId, nameof(branchId));
        ValidateId(counterId, nameof(counterId));
        ValidateId(staffId, nameof(staffId));

        BranchId = branchId;
        CounterId = counterId;
        StaffId = staffId;
        OccurredAt = occurredAt;
        Status = TransactionStatus.Open;
    }

    public Guid BranchId { get; }
    public Guid CounterId { get; }
    public Guid StaffId { get; }
    public DateTimeOffset OccurredAt { get; }
    public TransactionStatus Status { get; private set; }
    public string? StatusReason { get; private set; }
    public IReadOnlyCollection<TransactionLine> Lines => _lines.AsReadOnly();
    public Money Total => _lines.Count == 0
        ? new Money(0, "PHP")
        : new Money(_lines.Sum(line => line.Total.Amount), _lines[0].UnitPrice.Currency);

    public static Transaction Start(Branch branch, Counter counter, Staff staff, DateTimeOffset occurredAt)
    {
        ArgumentNullException.ThrowIfNull(branch);
        ArgumentNullException.ThrowIfNull(counter);
        ArgumentNullException.ThrowIfNull(staff);

        if (counter.BranchId != branch.Id)
        {
            throw new InvalidOperationException("Counter does not belong to the selected branch.");
        }

        if (!staff.CanProcessAt(branch.Id))
        {
            throw new InvalidOperationException("Inactive or cross-branch staff cannot process transactions.");
        }

        if (!branch.CanAcceptTransactions(occurredAt))
        {
            throw new InvalidOperationException("Closed branches cannot accept transactions.");
        }

        return new Transaction(Guid.NewGuid(), branch.Id, counter.Id, staff.Id, occurredAt);
    }

    public void AddLine(TransactionLine line)
    {
        ArgumentNullException.ThrowIfNull(line);
        EnsureOpen();
        _lines.Add(line);
    }

    public void AddProduct(Product product, Quantity quantity)
    {
        ArgumentNullException.ThrowIfNull(product);
        AddLine(TransactionLine.ForProduct(product, quantity));
    }

    public void AddService(string serviceCode, TransactionLineType serviceType, Money price, ShippingServiceLevel? shippingLevel = null)
    {
        if (serviceType is TransactionLineType.RetailProduct)
        {
            throw new ArgumentException("Service line type must be postal or financial.", nameof(serviceType));
        }

        AddLine(TransactionLine.ForService(serviceCode, serviceType, price, shippingLevel));
    }

    public void Pay(Staff staff, Branch branch)
    {
        EnsureAuthorizedProcessor(staff, branch);
        EnsureStatus(TransactionStatus.Open);
        Status = TransactionStatus.Paid;
    }

    public void Void(Staff manager, string reason)
    {
        EnsureManager(manager);
        EnsureReason(reason);
        EnsureStatus(TransactionStatus.Open, TransactionStatus.Paid);
        Status = TransactionStatus.Voided;
        StatusReason = reason.Trim();
    }

    public void Refund(Staff manager, string reason)
    {
        EnsureManager(manager);
        EnsureReason(reason);
        EnsureStatus(TransactionStatus.Paid);
        Status = TransactionStatus.Refunded;
        StatusReason = reason.Trim();
    }

    private void EnsureAuthorizedProcessor(Staff staff, Branch branch)
    {
        ArgumentNullException.ThrowIfNull(staff);
        ArgumentNullException.ThrowIfNull(branch);
        if (!staff.CanProcessAt(BranchId) || !branch.CanAcceptTransactions(OccurredAt))
        {
            throw new InvalidOperationException("Staff and branch are not authorized for this transaction.");
        }
    }

    private void EnsureManager(Staff manager)
    {
        ArgumentNullException.ThrowIfNull(manager);
        if (!manager.CanApproveCorrections(BranchId))
        {
            throw new InvalidOperationException("Only an active Branch Manager at the transaction branch may approve corrections.");
        }
    }

    private void EnsureOpen() => EnsureStatus(TransactionStatus.Open);

    private void EnsureStatus(params TransactionStatus[] allowed)
    {
        if (!allowed.Contains(Status))
        {
            throw new InvalidOperationException($"Transaction status '{Status}' does not allow this operation.");
        }
    }

    private static void EnsureReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("A reason is required.", nameof(reason));
        }
    }

    private static void ValidateId(Guid value, string name)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException($"{name} cannot be empty.", name);
        }
    }
}

public sealed record TransactionLine
{
    public TransactionLine(Guid productId, Quantity quantity, Money unitPrice)
        : this(TransactionLineType.RetailProduct, productId, productId.ToString("N"), quantity, unitPrice, null)
    {
    }

    private TransactionLine(TransactionLineType type, Guid? productId, string code, Quantity quantity, Money unitPrice, ShippingServiceLevel? shippingLevel)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Line code is required.", nameof(code));
        }

        if (quantity.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Transaction quantity must be positive.");
        }

        if (type == TransactionLineType.RetailProduct && productId is null)
        {
            throw new ArgumentException("Retail product lines require a product ID.", nameof(productId));
        }

        Type = type;
        ProductId = productId;
        Code = code.Trim();
        Quantity = quantity;
        UnitPrice = unitPrice;
        ShippingLevel = shippingLevel;
    }

    public TransactionLineType Type { get; }
    public Guid? ProductId { get; }
    public string Code { get; }
    public Quantity Quantity { get; }
    public Money UnitPrice { get; }
    public ShippingServiceLevel? ShippingLevel { get; }
    public Money Total => new(UnitPrice.Amount * Quantity.Value, UnitPrice.Currency);

    public static TransactionLine ForProduct(Product product, Quantity quantity)
    {
        ArgumentNullException.ThrowIfNull(product);
        return new TransactionLine(TransactionLineType.RetailProduct, product.Id, product.Sku.Value, quantity, product.UnitPrice, null);
    }

    public static TransactionLine ForService(string serviceCode, TransactionLineType serviceType, Money price, ShippingServiceLevel? shippingLevel = null) =>
        new(serviceType is TransactionLineType.RetailProduct ? throw new ArgumentException("Service type cannot be retail product.", nameof(serviceType)) : serviceType, null, serviceCode, new Quantity(1), price, shippingLevel);
}
