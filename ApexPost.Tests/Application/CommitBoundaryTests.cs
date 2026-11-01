using ApexPost.Application.Abstractions.Persistence;
using ApexPost.Application.Services;
using ApexPost.Domain.Entities;
using ApexPost.Domain.Enums;
using ApexPost.Domain.ValueObjects;

namespace ApexPost.Tests.Application;
public sealed class SpyUnitOfWork : IUnitOfWork
{
    public int Commits { get; private set; }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Commits++;
        return Task.CompletedTask;
    }

    public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default) => operation(cancellationToken);
}

internal sealed class FakePayments : IPaymentRepository
{
    public List<Payment> Items { get; } = [];

    public Task AddAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        Items.Add(payment);
        return Task.CompletedTask;
    }
}

[Trait("Category", "Unit")]
public sealed class CommitBoundaryTests
{
    [Fact]
    public async Task Inventory_success_commits_once()
    {
        var products = new InMemoryProductRepository();
        var stock = new InMemoryBranchStockRepository();
        var uow = new SpyUnitOfWork();
        var product = new Product(Guid.NewGuid(), new Sku("TEST"), "Test", new Money(10));
        await products.AddAsync(product);
        var result = await new InventoryService(stock, products, uow).AdjustAsync(Guid.NewGuid(), product.Id, 3);
        Assert.Equal(3, result.Quantity.Value);
        Assert.Equal(1, uow.Commits);
    }

    [Fact]
    public async Task Inventory_validation_failure_never_commits()
    {
        var uow = new SpyUnitOfWork();
        var service = new InventoryService(new InMemoryBranchStockRepository(), new InMemoryProductRepository(), uow);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.AdjustAsync(Guid.NewGuid(), Guid.NewGuid(), 0));
        Assert.Equal(0, uow.Commits);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(0, false)]
    public async Task Checkout_commits_stock_sale_payment_once_or_not_at_all(int quantity, bool succeeds)
    {
        var branch = new Branch(Guid.NewGuid(), new BranchCode("TEST"), "Test", "Address");
        var counter = new Counter(Guid.NewGuid(), branch.Id, 1, "Counter");
        var clerk = new Staff(Guid.NewGuid(), branch.Id, "E1", "Clerk", new EmailAddress("test@example.com"));
        var product = new Product(Guid.NewGuid(), new Sku("TEST"), "Test", new Money(10));
        var sale = Transaction.Start(branch, counter, clerk, new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero));
        sale.AddProduct(product, new Quantity(1));
        var branches = new InMemoryBranchRepository();
        await branches.AddAsync(branch);
        var counters = new InMemoryCounterRepository();
        await counters.AddAsync(counter);
        var staff = new InMemoryStaffRepository();
        await staff.AddAsync(clerk);
        var products = new InMemoryProductRepository();
        await products.AddAsync(product);
        var transactions = new InMemoryTransactionRepository();
        await transactions.AddAsync(sale);
        var stock = new InMemoryBranchStockRepository();
        await stock.AddAsync(new BranchStock(Guid.NewGuid(), branch.Id, product.Id, new Quantity(quantity)));
        var uow = new SpyUnitOfWork();
        var payments = new FakePayments();
        var service = new TransactionProcessingService(branches, counters, staff, products, transactions, stock, uow, payments);
        if (succeeds)
        {
            await service.PayAsync(sale.Id);
            Assert.Equal(1, uow.Commits);
            Assert.Single(payments.Items);
            Assert.Equal(TransactionStatus.Paid, sale.Status);
        }
        else
        {
            await Assert.ThrowsAsync<ArgumentException>(() => service.PayAsync(sale.Id));
            Assert.Equal(0, uow.Commits);
            Assert.Empty(payments.Items);
            Assert.Equal(TransactionStatus.Open, sale.Status);
        }
    }
}
