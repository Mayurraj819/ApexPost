using ApexPost.Domain.Entities;
using ApexPost.Domain.Enums;
using ApexPost.Domain.ValueObjects;

namespace ApexPost.Tests.Domain;

public sealed class BusinessRuleTests
{
    [Fact]
    public void BranchStock_CannotBeDecreasedBelowZero()
    {
        var stock = new BranchStock(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new Quantity(2));

        Assert.Throws<InvalidOperationException>(() => stock.Decrease(3));
        Assert.Equal(2, stock.Quantity.Value);
    }

    [Fact]
    public void InactiveStaff_CannotStartTransaction()
    {
        var branch = CreateBranch();
        var counter = new Counter(Guid.NewGuid(), branch.Id, 1, "Counter 1");
        var staff = CreateStaff(branch.Id);
        staff.Deactivate();

        Assert.Throws<InvalidOperationException>(() => Transaction.Start(branch, counter, staff, At(9)));
    }

    [Fact]
    public void StaffCannotStartTransactionAtAnotherBranch()
    {
        var branch = CreateBranch();
        var otherBranch = CreateBranch();
        var counter = new Counter(Guid.NewGuid(), branch.Id, 1, "Counter 1");
        var staff = CreateStaff(otherBranch.Id);

        Assert.Throws<InvalidOperationException>(() => Transaction.Start(branch, counter, staff, At(9)));
    }

    [Fact]
    public void ClosedBranch_CannotStartTransaction()
    {
        var branch = CreateBranch();
        branch.Close();
        var counter = new Counter(Guid.NewGuid(), branch.Id, 1, "Counter 1");
        var staff = CreateStaff(branch.Id);

        Assert.Throws<InvalidOperationException>(() => Transaction.Start(branch, counter, staff, At(9)));
    }

    [Fact]
    public void TransactionCanContainRetailPostalAndFinancialLines()
    {
        var branch = CreateBranch();
        var counter = new Counter(Guid.NewGuid(), branch.Id, 1, "Counter 1");
        var staff = CreateStaff(branch.Id);
        var product = new Product(Guid.NewGuid(), new Sku("BOX-1"), "Box", new Money(10));
        var transaction = Transaction.Start(branch, counter, staff, At(9));

        transaction.AddProduct(product, new Quantity(1));
        transaction.AddService("POST-STD", TransactionLineType.PostalService, new Money(25), ShippingServiceLevel.Standard);
        transaction.AddService("BILL-PAY", TransactionLineType.FinancialService, new Money(5));

        Assert.Equal(3, transaction.Lines.Count);
        Assert.Equal(40m, transaction.Total.Amount);
    }

    [Fact]
    public void VoidRequiresActiveBranchManagerAndReason()
    {
        var branch = CreateBranch();
        var counter = new Counter(Guid.NewGuid(), branch.Id, 1, "Counter 1");
        var clerk = CreateStaff(branch.Id);
        var manager = CreateStaff(branch.Id, StaffRole.BranchManager);
        var transaction = Transaction.Start(branch, counter, clerk, At(9));

        Assert.Throws<InvalidOperationException>(() => transaction.Void(clerk, "mistake"));
        Assert.Throws<ArgumentException>(() => transaction.Void(manager, " "));

        transaction.Void(manager, "Duplicate sale");

        Assert.Equal(TransactionStatus.Voided, transaction.Status);
        Assert.Equal("Duplicate sale", transaction.StatusReason);
    }

    [Fact]
    public void RefundRequiresManagerAndPaidTransaction()
    {
        var branch = CreateBranch();
        var counter = new Counter(Guid.NewGuid(), branch.Id, 1, "Counter 1");
        var clerk = CreateStaff(branch.Id);
        var manager = CreateStaff(branch.Id, StaffRole.BranchManager);
        var transaction = Transaction.Start(branch, counter, clerk, At(9));

        transaction.Pay(clerk, branch);
        transaction.Refund(manager, "Customer return");

        Assert.Equal(TransactionStatus.Refunded, transaction.Status);
    }

    [Fact]
    public void IdentityDocument_DoesNotExposeFullNumber()
    {
        var document = IdentityDocument.Create("Passport", "P123456789");

        Assert.Equal("***6789", document.MaskedNumber);
        Assert.DoesNotContain("P123456789", document.MaskedNumber);
        Assert.DoesNotContain("P123456789", document.Fingerprint);
    }

    private static Branch CreateBranch() =>
        new(Guid.NewGuid(), new BranchCode("BR-01"), "Main", "Address", new OpeningHours(new TimeOnly(8, 0), new TimeOnly(17, 0)));

    private static Staff CreateStaff(Guid branchId, StaffRole role = StaffRole.CounterClerk) =>
        new(Guid.NewGuid(), branchId, "EMP-01", "Staff Member", new EmailAddress("staff@example.com"), role);

    private static DateTimeOffset At(int hour) => new(2026, 1, 1, hour, 0, 0, TimeSpan.Zero);
}
