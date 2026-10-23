using ApexPost.Application.Abstractions.Services;
using ApexPost.Application.Contracts;
using ApexPost.Application.Services;
using ApexPost.Domain.Entities;
using ApexPost.Domain.Enums;
using ApexPost.Domain.ValueObjects;

namespace ApexPost.Tests.Application;

public sealed class ApplicationServiceTests
{
    [Fact]
    public async Task InventoryService_InitializesAndAdjustsStock()
    {
        var products = new InMemoryProductRepository();
        var stock = new InMemoryBranchStockRepository();
        var product = Product();
        await products.AddAsync(product);
        var service = new InventoryService(stock, products);

        var initialized = await service.AdjustAsync(Guid.NewGuid(), product.Id, 5);
        var adjusted = await service.AdjustAsync(initialized.BranchId, product.Id, -2);

        Assert.Equal(3, adjusted.Quantity.Value);
    }

    [Fact]
    public async Task TransactionProcessingService_UsesDomainAuthorizationAndPersistsChanges()
    {
        var branch = Branch();
        var counter = new Counter(Guid.NewGuid(), branch.Id, 1, "Counter");
        var staff = Staff(branch.Id);
        var product = Product();
        var branches = new InMemoryBranchRepository();
        var counters = new InMemoryCounterRepository();
        var staffRepository = new InMemoryStaffRepository();
        var products = new InMemoryProductRepository();
        var transactions = new InMemoryTransactionRepository();
        await branches.AddAsync(branch);
        await counters.AddAsync(counter);
        await staffRepository.AddAsync(staff);
        await products.AddAsync(product);
        var service = new TransactionProcessingService(branches, counters, staffRepository, products, transactions);

        var transaction = await service.StartAsync(branch.Id, counter.Id, staff.Id, At(9));
        await service.AddProductAsync(transaction.Id, product.Id, new Quantity(2));
        await service.PayAsync(transaction.Id);

        var stored = await transactions.GetByIdAsync(transaction.Id);
        Assert.NotNull(stored);
        Assert.Equal(TransactionStatus.Paid, stored!.Status);
        Assert.Equal(2, stored.Lines.Single().Quantity.Value);
    }

    [Fact]
    public async Task ShippingPricingService_UsesServiceLevelAndWeight()
    {
        IShippingPricingService service = new ShippingPricingService();

        var quote = await service.CalculateAsync(new ShippingQuoteRequest(new Weight(1500), ShippingServiceLevel.Express, "ZONE-1"));

        Assert.Equal(155m, quote.Amount.Amount);
    }

    [Fact]
    public async Task IdentityVerificationService_ReturnsMaskedVerificationResult()
    {
        var document = IdentityDocument.Create("Passport", "P123456789");
        IIdentityVerificationService service = new IdentityVerificationService();

        var result = await service.VerifyAsync(new IdentityVerificationRequest(document, document.Fingerprint));

        Assert.True(result.IsVerified);
        Assert.Equal("***6789", result.MaskedNumber);
    }

    [Fact]
    public async Task ReportService_SummarizesTransactionsByBranchAndDateRange()
    {
        var branch = Branch();
        var counter = new Counter(Guid.NewGuid(), branch.Id, 1, "Counter");
        var staff = Staff(branch.Id);
        var first = Transaction.Start(branch, counter, staff, At(9));
        first.Pay(staff, branch);
        var second = Transaction.Start(branch, counter, staff, At(10));
        second.Void(Staff(branch.Id, StaffRole.BranchManager), "Duplicate");
        var repository = new InMemoryTransactionRepository();
        await repository.AddAsync(first);
        await repository.AddAsync(second);
        var service = new ReportService(repository);

        var report = await service.GetTransactionReportAsync(branch.Id, At(8), At(17));

        Assert.Equal(2, report.TransactionCount);
        Assert.Equal(1, report.PaidCount);
        Assert.Equal(1, report.VoidedCount);
    }

    [Fact]
    public async Task BranchManagementService_ClosesAndReopensBranch()
    {
        var branch = Branch();
        var repository = new InMemoryBranchRepository();
        await repository.AddAsync(branch);
        var service = new BranchManagementService(repository);

        await service.CloseAsync(branch.Id);
        Assert.False((await service.GetAsync(branch.Id))!.IsActive);

        await service.ReopenAsync(branch.Id);
        Assert.True((await service.GetAsync(branch.Id))!.IsActive);
    }

    [Fact]
    public async Task StaffManagementService_DeactivatesAndActivatesStaff()
    {
        var staff = Staff(Guid.NewGuid());
        var repository = new InMemoryStaffRepository();
        await repository.AddAsync(staff);
        var service = new StaffManagementService(repository);

        await service.DeactivateAsync(staff.Id);
        Assert.False((await service.GetAsync(staff.Id))!.IsActive);

        await service.ActivateAsync(staff.Id);
        Assert.True((await service.GetAsync(staff.Id))!.IsActive);
    }

    [Fact]
    public async Task MoneyOrderService_PersistsAuthorizedMoneyOrderWithoutFullIdentityNumber()
    {
        var branch = Branch();
        var staff = Staff(branch.Id);
        var branches = new InMemoryBranchRepository();
        var staffRepository = new InMemoryStaffRepository();
        var moneyOrders = new InMemoryMoneyOrderRepository();
        await branches.AddAsync(branch);
        await staffRepository.AddAsync(staff);
        var service = new MoneyOrderService(branches, staffRepository, moneyOrders);
        var document = IdentityDocument.Create("Passport", "P123456789");

        var result = await service.CreateAsync(new MoneyOrderRequest(branch.Id, staff.Id, new Money(500), "Payee", document, At(9)));
        var stored = await moneyOrders.GetByReferenceAsync(result.Reference);

        Assert.NotNull(stored);
        Assert.Equal(document.Fingerprint, stored!.IdentityFingerprint);
        Assert.DoesNotContain("P123456789", stored.IdentityFingerprint);
    }

    private static Branch Branch() =>
        new(Guid.NewGuid(), new BranchCode("BR-01"), "Main", "Address", new OpeningHours(new TimeOnly(8, 0), new TimeOnly(17, 0)));

    private static Staff Staff(Guid branchId, StaffRole role = StaffRole.CounterClerk) =>
        new(Guid.NewGuid(), branchId, Guid.NewGuid().ToString("N"), "Staff", new EmailAddress($"{Guid.NewGuid():N}@example.com"), role);

    private static Product Product() =>
        new(Guid.NewGuid(), new Sku("BOX-1"), "Box", new Money(10));

    private static DateTimeOffset At(int hour) => new(2026, 1, 1, hour, 0, 0, TimeSpan.Zero);
}
