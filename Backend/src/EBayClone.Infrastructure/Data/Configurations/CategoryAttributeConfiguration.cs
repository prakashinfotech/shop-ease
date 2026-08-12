using EBayClone.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EBayClone.Infrastructure.Data.Configurations;

public class CategoryAttributeConfiguration : IEntityTypeConfiguration<CategoryAttribute>
{
    public void Configure(EntityTypeBuilder<CategoryAttribute> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Name).IsRequired().HasMaxLength(80);
        builder.Property(a => a.DisplayName).IsRequired().HasMaxLength(120);
        builder.Property(a => a.Description).HasMaxLength(500);
        builder.Property(a => a.Placeholder).HasMaxLength(160);
        builder.Property(a => a.Unit).HasMaxLength(30);
        builder.Property(a => a.RegexPattern).HasMaxLength(500);
        builder.Property(a => a.MinValue).HasPrecision(18, 2);
        builder.Property(a => a.MaxValue).HasPrecision(18, 2);
        builder.Property(a => a.ConditionValue).HasMaxLength(250);

        builder.HasIndex(a => new { a.CategoryId, a.Name }).IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_CategoryAttributes_CategoryId_Name");

        builder.HasQueryFilter(a => !a.IsDeleted);

        builder.HasOne(a => a.Category)
            .WithMany(c => c.Attributes)
            .HasForeignKey(a => a.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.ConditionAttribute)
            .WithMany()
            .HasForeignKey(a => a.ConditionAttributeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(a => a.Options)
            .WithOne(o => o.CategoryAttribute)
            .HasForeignKey(o => o.CategoryAttributeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
