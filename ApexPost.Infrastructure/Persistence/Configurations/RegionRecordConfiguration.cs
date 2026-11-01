using ApexPost.Domain.Entities;
using ApexPost.Infrastructure.Persistence.Entities;
using ApexPost.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexPost.Infrastructure.Persistence.Configurations;
public sealed class RegionRecordConfiguration : IEntityTypeConfiguration<RegionRecord>
{
    public void Configure(EntityTypeBuilder<RegionRecord> b)
    {
        MappingHelpers.Key(b, "Regions");
        b.Property(x => x.Code).HasMaxLength(40).IsUnicode(false);
        b.HasIndex(x => x.Code).IsUnique();
        b.Property(x => x.Name).HasMaxLength(120);
    }
}
