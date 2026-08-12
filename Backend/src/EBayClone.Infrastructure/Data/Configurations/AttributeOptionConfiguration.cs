using EBayClone.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EBayClone.Infrastructure.Data.Configurations;

public class AttributeOptionConfiguration : IEntityTypeConfiguration<AttributeOption>
{
    public void Configure(EntityTypeBuilder<AttributeOption> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Value).IsRequired().HasMaxLength(120);
        builder.Property(o => o.Label).IsRequired().HasMaxLength(160);

        builder.HasIndex(o => new { o.CategoryAttributeId, o.Value }).IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_AttributeOptions_AttributeId_Value");

        builder.HasQueryFilter(o => !o.IsDeleted);
    }
}
