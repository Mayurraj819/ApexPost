using ApexPost.Application.Abstractions.Persistence;
using ApexPost.Application.Abstractions.Services;
using ApexPost.Application.Services;
using ApexPost.Infrastructure.Persistence;
using ApexPost.Infrastructure.Persistence.Repositories;
using ApexPost.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ApexPost.Infrastructure;
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<ApexPostDbContext>(o => o.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null)));
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IBranchRepository, EfBranchRepository>();
        services.AddScoped<ICounterRepository, EfCounterRepository>();
        services.AddScoped<IStaffRepository, EfStaffRepository>();
        services.AddScoped<IProductRepository, EfProductRepository>();
        services.AddScoped<IBranchStockRepository, EfStockRepository>();
        services.AddScoped<ITransactionRepository, EfTransactionRepository>();
        services.AddScoped<IMoneyOrderRepository, EfMoneyOrderRepository>();
        services.AddScoped<IPaymentRepository, EfPaymentRepository>();
        services.AddScoped<IAuditStore, EfAuditStore>();
        services.AddScoped<IBranchService, BranchService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<ITransactionProcessingService, TransactionProcessingService>();
        services.AddScoped<IMoneyOrderService, MoneyOrderService>();
        services.AddScoped<IStaffManagementService, StaffManagementService>();
        services.AddScoped<IBranchManagementService, BranchManagementService>();
        services.AddScoped<IShippingPricingService, ShippingPricingService>();
        services.AddScoped<IIdentityVerificationService, IdentityVerificationService>();
        services.AddScoped<IPlatformQueries, ApexPost.Infrastructure.Queries.PlatformQueries>();
        services.AddScoped<ICatalogCommands, CatalogCommands>();
        services.AddScoped<IShipmentStore, EfShipmentStore>();
        services.AddScoped<IShippingCommands, ShippingCommands>();
        services.AddScoped<ISqlOperations, ApexPost.Infrastructure.Queries.SqlOperations>();
        services.AddScoped<IStockAdjustmentWriter, EfStockAdjustmentWriter>();
        return services;
    }
}
