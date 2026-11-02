using ApexPost.Domain.Entities;
using ApexPost.Domain.Enums;
using ApexPost.Infrastructure;
using ApexPost.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ApexPost.Tests.Persistence;

[Trait("Category", "Unit")]
public sealed class ReadinessContractTests
{
    [Fact]
    public void Migration_snapshot_matches_the_runtime_model()
    {
        using var db = ModelContractTests.Context();
        Assert.False(db.Database.HasPendingModelChanges());
    }

    [Fact]
    public void Production_registration_enables_transient_retries()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure("Server=localhost;Database=ModelOnly;Integrated Security=true;TrustServerCertificate=true");
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApexPostDbContext>();
        Assert.True(db.Database.CreateExecutionStrategy().RetriesOnFailure);
    }

    [Theory]
    [InlineData(TransactionStatus.Open)]
    [InlineData(TransactionStatus.Paid)]
    [InlineData(TransactionStatus.Voided)]
    [InlineData(TransactionStatus.Refunded)]
    public void Status_codes_roundtrip_as_varchar(TransactionStatus status)
    {
        using var db = ModelContractTests.Context();
        var property = db.Model.FindEntityType(typeof(Transaction))!.FindProperty(nameof(Transaction.Status))!;
        Assert.Equal("varchar(24)", property.GetColumnType());
        var converter = property.GetTypeMapping().Converter!;
        Assert.Equal(status.ToString(), converter.ConvertToProvider(status));
        Assert.Equal(status, converter.ConvertFromProvider(status.ToString()));
    }

    [Theory]
    [InlineData(TransactionLineType.RetailProduct)]
    [InlineData(TransactionLineType.PostalService)]
    [InlineData(TransactionLineType.FinancialService)]
    [InlineData(TransactionLineType.ParcelShipping)]
    public void All_line_codes_roundtrip_as_varchar(TransactionLineType type)
    {
        using var db = ModelContractTests.Context();
        var property = db.Model.FindEntityType(typeof(TransactionLine))!.FindProperty(nameof(TransactionLine.Type))!;
        Assert.Equal("varchar(24)", property.GetColumnType());
        var converter = property.GetTypeMapping().Converter!;
        Assert.Equal(type.ToString(), converter.ConvertToProvider(type));
        Assert.Equal(type, converter.ConvertFromProvider(type.ToString()));
    }

    [Fact]
    public async Task Retry_wrapper_rejects_pending_tracked_writes_before_connecting()
    {
        await using var db = ModelContractTests.Context();
        db.Add(new Product(Guid.NewGuid(), new("GUARD"), "Guard", new(1)));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new EfUnitOfWork(db).ExecuteInTransactionAsync(_ => Task.CompletedTask));
    }
}
