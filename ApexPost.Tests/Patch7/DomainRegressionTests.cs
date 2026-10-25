using ApexPost.Domain.Entities;
using ApexPost.Domain.Enums;
using ApexPost.Domain.ValueObjects;
using ApexPost.Application.Services;
using ApexPost.Application.Contracts;

namespace ApexPost.Tests.Patch7;

public sealed class DomainRegressionTests
{
    private static readonly DateTimeOffset BusinessTime = new(2026, 10, 20, 10, 0, 0, TimeSpan.Zero);
    private static (Branch Branch, Counter Counter, Staff Clerk, Staff Manager) Actors()
    {
        var branch = new Branch(Guid.NewGuid(), new BranchCode("PUNE"), "Pune", "Main road");
        var counter = new Counter(Guid.NewGuid(), branch.Id, 1, "Counter 1");
        var clerk = new Staff(Guid.NewGuid(), branch.Id, "EMP1", "Clerk", new EmailAddress("clerk@example.com"));
        var manager = new Staff(Guid.NewGuid(), branch.Id, "EMP2", "Manager", new EmailAddress("manager@example.com"), StaffRole.BranchManager);
        return (branch, counter, clerk, manager);
    }
    [Fact] public void Start_Rejects_CrossBranch_Counter()
    {
        var a = Actors();
        Assert.Throws<InvalidOperationException>(() => Transaction.Start(a.Branch, new Counter(Guid.NewGuid(), Guid.NewGuid(), 2, "Other"), a.Clerk, BusinessTime));
    }
    [Fact] public void Start_Rejects_Inactive_Staff()
    {
        var a = Actors(); a.Clerk.Deactivate();
        Assert.Throws<InvalidOperationException>(() => Transaction.Start(a.Branch, a.Counter, a.Clerk, BusinessTime));
    }
    [Fact] public void Start_Rejects_Closed_Branch()
    {
        var a = Actors(); a.Branch.Close();
        Assert.Throws<InvalidOperationException>(() => Transaction.Start(a.Branch, a.Counter, a.Clerk, BusinessTime));
    }
    [Fact] public void Start_Rejects_Outside_Hours()
    {
        var a = Actors();
        Assert.Throws<InvalidOperationException>(() => Transaction.Start(a.Branch, a.Counter, a.Clerk, new DateTimeOffset(2026,10,20,1,0,0,TimeSpan.Zero)));
    }
    [Fact] public void Retail_Line_Computes_Total()
    {
        var a = Actors(); var t = Transaction.Start(a.Branch,a.Counter,a.Clerk,BusinessTime);
        t.AddProduct(new Product(Guid.NewGuid(),new Sku("P001"),"Envelope",new Money(12.50m)),new Quantity(3));
        Assert.Equal(37.50m,t.Total.Amount);
    }
    [Fact] public void Paid_Transaction_Rejects_New_Lines()
    {
        var a = Actors(); var t = Transaction.Start(a.Branch,a.Counter,a.Clerk,BusinessTime);
        t.AddService("POST",TransactionLineType.PostalService,new Money(50)); t.Pay(a.Clerk,a.Branch);
        Assert.Throws<InvalidOperationException>(() => t.AddService("SECOND",TransactionLineType.PostalService,new Money(20)));
    }
    [Fact] public void Void_Requires_Manager()
    {
        var a = Actors(); var t = Transaction.Start(a.Branch,a.Counter,a.Clerk,BusinessTime);
        Assert.Throws<InvalidOperationException>(() => t.Void(a.Clerk,"Correction"));
    }
    [Fact] public void Void_Requires_Reason()
    {
        var a = Actors(); var t = Transaction.Start(a.Branch,a.Counter,a.Clerk,BusinessTime);
        Assert.Throws<ArgumentException>(() => t.Void(a.Manager," "));
    }
    [Fact] public void Refund_Requires_Paid_Status()
    {
        var a = Actors(); var t = Transaction.Start(a.Branch,a.Counter,a.Clerk,BusinessTime);
        Assert.Throws<InvalidOperationException>(() => t.Refund(a.Manager,"Customer request"));
    }
    [Fact] public void Refund_Records_Reason()
    {
        var a = Actors(); var t = Transaction.Start(a.Branch,a.Counter,a.Clerk,BusinessTime);
        t.AddService("POST",TransactionLineType.PostalService,new Money(50)); t.Pay(a.Clerk,a.Branch);
        t.Refund(a.Manager,"  Customer request  ");
        Assert.Equal(TransactionStatus.Refunded,t.Status); Assert.Equal("Customer request",t.StatusReason);
    }
    [Fact] public void Stock_Prevents_Negative_Quantity()
    {
        var s = new BranchStock(Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),new Quantity(2));
        Assert.Throws<InvalidOperationException>(() => s.Decrease(3)); Assert.Equal(2,s.Quantity.Value);
    }
    [Theory][InlineData(0)][InlineData(-1)] public void Stock_Rejects_Nonpositive_Adjustments(int n)
    {
        var s = new BranchStock(Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),new Quantity(2));
        Assert.Throws<ArgumentOutOfRangeException>(() => s.Increase(n));
        Assert.Throws<ArgumentOutOfRangeException>(() => s.Decrease(n));
    }
    [Theory][InlineData(ShippingServiceLevel.Standard,70)][InlineData(ShippingServiceLevel.Express,120)][InlineData(ShippingServiceLevel.Registered,95)]
    public async Task Shipping_OneKilogram_Rate(ShippingServiceLevel level,int expected)
    {
        var result = await new ShippingPricingService().CalculateAsync(new ShippingQuoteRequest(new Weight(1000),level,"Z1"));
        Assert.Equal(expected,result.Amount.Amount);
    }
    [Fact] public async Task Shipping_Rejects_Overweight()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => new ShippingPricingService().CalculateAsync(new ShippingQuoteRequest(new Weight(30001),ShippingServiceLevel.Standard,"Z1")));
    }
    [Fact] public async Task Shipping_Rejects_Blank_Zone()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => new ShippingPricingService().CalculateAsync(new ShippingQuoteRequest(new Weight(1000),ShippingServiceLevel.Standard," ")));
    }
    [Fact] public void Identity_Number_Is_Masked()
    {
        var doc = IdentityDocument.Create("Passport","ABCD12345678");
        Assert.Equal("***5678",doc.MaskedNumber); Assert.DoesNotContain("ABCD12345678",doc.MaskedNumber);
    }
}
