using System.Text.Json;
using ApexPost.Application.Abstractions.Persistence;
using ApexPost.Application.Abstractions.Services;
using ApexPost.Application.Contracts;
using ApexPost.Application.Services;
using ApexPost.Domain.Entities;
using ApexPost.Infrastructure.Persistence;
using ApexPost.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ApexPost.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string dataDirectory)
    {
        if (string.IsNullOrWhiteSpace(dataDirectory))
        {
            throw new ArgumentException("A data directory is required.", nameof(dataDirectory));
        }

        var serializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        };

        services.AddSingleton<IJsonStore<Branch>>(_ => new FileJsonStore<Branch>(Path.Combine(dataDirectory, "branches.json"), serializerOptions));
        services.AddSingleton<IJsonStore<Product>>(_ => new FileJsonStore<Product>(Path.Combine(dataDirectory, "products.json"), serializerOptions));
        services.AddSingleton<IJsonStore<Staff>>(_ => new FileJsonStore<Staff>(Path.Combine(dataDirectory, "staff.json"), serializerOptions));
        services.AddSingleton<IJsonStore<Counter>>(_ => new FileJsonStore<Counter>(Path.Combine(dataDirectory, "counters.json"), serializerOptions));
        services.AddSingleton<IJsonStore<BranchStock>>(_ => new FileJsonStore<BranchStock>(Path.Combine(dataDirectory, "branch-stock.json"), serializerOptions));
        services.AddSingleton<IJsonStore<Transaction>>(_ => new FileJsonStore<Transaction>(Path.Combine(dataDirectory, "transactions.json"), serializerOptions));
        services.AddSingleton<IJsonStore<MoneyOrderResult>>(_ => new FileJsonStore<MoneyOrderResult>(Path.Combine(dataDirectory, "money-orders.json"), serializerOptions));
        services.AddSingleton<IAuditStore>(_ => new JsonAuditStore(dataDirectory));

        services.AddSingleton<IBranchRepository, JsonBranchRepository>();
        services.AddSingleton<IProductRepository, JsonProductRepository>();
        services.AddSingleton<IStaffRepository, JsonStaffRepository>();
        services.AddSingleton<ICounterRepository, JsonCounterRepository>();
        services.AddSingleton<IBranchStockRepository, JsonBranchStockRepository>();
        services.AddSingleton<ITransactionRepository, JsonTransactionRepository>();
        services.AddSingleton<IMoneyOrderRepository, JsonMoneyOrderRepository>();

        services.AddSingleton<IBranchService, BranchService>();
        services.AddSingleton<IProductService, ProductService>();
        services.AddSingleton<ITransactionService, TransactionService>();
        services.AddSingleton<IInventoryService, InventoryService>();
        services.AddSingleton<IReportService, ReportService>();
        services.AddSingleton<ITransactionProcessingService, TransactionProcessingService>();
        services.AddSingleton<IMoneyOrderService, MoneyOrderService>();
        services.AddSingleton<IStaffManagementService, StaffManagementService>();
        services.AddSingleton<IBranchManagementService, BranchManagementService>();
        services.AddSingleton<IShippingPricingService, ShippingPricingService>();
        services.AddSingleton<IIdentityVerificationService, IdentityVerificationService>();

        return services;
    }
}
