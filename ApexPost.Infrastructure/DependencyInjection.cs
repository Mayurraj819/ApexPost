using System.Text.Json;
using ApexPost.Application.Abstractions.Persistence;
using ApexPost.Application.Abstractions.Services;
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
        services.AddSingleton<IJsonStore<Transaction>>(_ => new FileJsonStore<Transaction>(Path.Combine(dataDirectory, "transactions.json"), serializerOptions));
        services.AddSingleton<IAuditStore>(_ => new JsonAuditStore(dataDirectory));

        services.AddSingleton<IBranchRepository, JsonBranchRepository>();
        services.AddSingleton<IProductRepository, JsonProductRepository>();
        services.AddSingleton<ITransactionRepository, JsonTransactionRepository>();
        services.AddSingleton<IBranchService, BranchService>();
        services.AddSingleton<IProductService, ProductService>();
        services.AddSingleton<ITransactionService, TransactionService>();

        return services;
    }
}
