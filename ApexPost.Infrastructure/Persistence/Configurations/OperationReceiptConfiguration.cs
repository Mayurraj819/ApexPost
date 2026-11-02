using ApexPost.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexPost.Infrastructure.Persistence.Configurations;

public sealed class OperationReceiptConfiguration : IEntityTypeConfiguration<OperationReceipt>
{
    public void Configure(EntityTypeBuilder<OperationReceipt> builder) =>
        MappingHelpers.Key(builder, "OperationReceipts");
}
