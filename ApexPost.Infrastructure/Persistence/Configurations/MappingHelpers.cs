using ApexPost.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexPost.Infrastructure.Persistence.Configurations;
internal static class MappingHelpers
{
    public static void Key<T>(EntityTypeBuilder<T> b, string table)
        where T : class
    {
        b.ToTable(table);
        b.HasKey("Id");
        b.Property<Guid>("Id").ValueGeneratedNever();
    }

    public static void Money(ComplexPropertyBuilder<Money> b, string prefix)
    {
        b.Property(x => x.Amount).HasColumnName(prefix + "Amount").HasPrecision(18, 2);
        b.Property(x => x.Currency).HasColumnName(prefix + "Currency").HasMaxLength(3).IsUnicode(false);
    }

    public static void Check<T>(EntityTypeBuilder<T> b, string name, string sql)
        where T : class => b.ToTable(t => t.HasCheckConstraint(name, sql));
}
