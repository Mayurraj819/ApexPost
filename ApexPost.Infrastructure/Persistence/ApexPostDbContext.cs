using ApexPost.Application.Abstractions.Services;
using ApexPost.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;

namespace ApexPost.Infrastructure.Persistence;
public sealed class ApexPostDbContext(DbContextOptions<ApexPostDbContext> options, ICurrentActor? actor = null) : DbContext(options)
{
    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        builder.Properties<ApexPost.Domain.ValueObjects.Sku>().HaveConversion<SkuConverter>();
        builder.Properties<ApexPost.Domain.ValueObjects.BranchCode>().HaveConversion<BranchCodeConverter>();
        builder.Properties<ApexPost.Domain.ValueObjects.EmailAddress>().HaveConversion<EmailConverter>();
        builder.Properties<ApexPost.Domain.ValueObjects.Weight>().HaveConversion<WeightConverter>();
        builder.Properties<DateTimeOffset>().HaveConversion<UtcDateTimeOffsetConverter>().HaveColumnType("datetime2");
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(typeof(ApexPostDbContext).Assembly);
        // Audit fields are shadow state: clients cannot over-post them into domain entities.
        foreach (var type in builder.Model.GetEntityTypes().Where(t => !t.IsOwned()).ToArray())
        {
            var entity = builder.Entity(type.ClrType);
            entity.Property<DateTime>("CreatedAtUtc").HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property<string>("CreatedBy").IsRequired().HasMaxLength(100).HasDefaultValue("system");
            entity.Property<DateTime?>("ModifiedAtUtc");
        }
    }

    internal bool IsReplayableSqlOperation { get; set; }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        SaveChangesAsync(true, cancellationToken);

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        if (IsReplayableSqlOperation)
            throw new InvalidOperationException("Tracked SaveChanges is not supported inside the replayable SQL callback. Use the tracked-write transaction pattern documented in ADR 0015.");
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries().Where(e => !e.Metadata.IsOwned()))
        {
            if (entry.State == EntityState.Added)
            {
                entry.Property("CreatedAtUtc").CurrentValue = now;
                entry.Property("CreatedBy").CurrentValue = actor?.Id ?? "system";
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Property("ModifiedAtUtc").CurrentValue = now;
                entry.Property("CreatedAtUtc").IsModified = false;
                entry.Property("CreatedBy").IsModified = false;
            }
        }

        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
}
