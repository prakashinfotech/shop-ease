using EBayClone.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EBayClone.Infrastructure.Data.Configurations;

public class ListingAttributeValueConfiguration : IEntityTypeConfiguration<ListingAttributeValue>
{
    public void Configure(EntityTypeBuilder<ListingAttributeValue> builder)
    {
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Value).IsRequired().HasMaxLength(2000);

        builder.HasIndex(v => new { v.ListingId, v.CategoryAttributeId }).IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_ListingAttributeValues_ListingId_AttributeId");

        builder.HasQueryFilter(v => !v.IsDeleted);

        builder.HasOne(v => v.Listing)
            .WithMany(l => l.AttributeValues)
            .HasForeignKey(v => v.ListingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(v => v.CategoryAttribute)
            .WithMany(a => a.ListingValues)
            .HasForeignKey(v => v.CategoryAttributeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
