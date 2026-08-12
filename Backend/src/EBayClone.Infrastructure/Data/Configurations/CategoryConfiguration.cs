using EBayClone.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EBayClone.Infrastructure.Data.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Description).HasMaxLength(500);
        builder.Property(c => c.ImageUrl).HasMaxLength(500);

        builder.HasQueryFilter(c => !c.IsDeleted);

        builder.HasMany(c => c.SubCategories)
            .WithOne(c => c.ParentCategory)
            .HasForeignKey(c => c.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.Listings)
            .WithOne(l => l.Category)
            .HasForeignKey(l => l.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        // Seed categories
        var categories = new[]
        {
            new Category { Id = Guid.Parse("10000000-0000-0000-0000-000000000001"), Name = "Electronics", SortOrder = 1, CreatedAt = new DateTime(2024,1,1,0,0,0,DateTimeKind.Utc), UpdatedAt = new DateTime(2024,1,1,0,0,0,DateTimeKind.Utc) },
            new Category { Id = Guid.Parse("10000000-0000-0000-0000-000000000002"), Name = "Fashion", SortOrder = 2, CreatedAt = new DateTime(2024,1,1,0,0,0,DateTimeKind.Utc), UpdatedAt = new DateTime(2024,1,1,0,0,0,DateTimeKind.Utc) },
            new Category { Id = Guid.Parse("10000000-0000-0000-0000-000000000003"), Name = "Home & Garden", SortOrder = 3, CreatedAt = new DateTime(2024,1,1,0,0,0,DateTimeKind.Utc), UpdatedAt = new DateTime(2024,1,1,0,0,0,DateTimeKind.Utc) },
            new Category { Id = Guid.Parse("10000000-0000-0000-0000-000000000004"), Name = "Sports", SortOrder = 4, CreatedAt = new DateTime(2024,1,1,0,0,0,DateTimeKind.Utc), UpdatedAt = new DateTime(2024,1,1,0,0,0,DateTimeKind.Utc) },
            new Category { Id = Guid.Parse("10000000-0000-0000-0000-000000000005"), Name = "Vehicles", SortOrder = 5, CreatedAt = new DateTime(2024,1,1,0,0,0,DateTimeKind.Utc), UpdatedAt = new DateTime(2024,1,1,0,0,0,DateTimeKind.Utc) },
            new Category { Id = Guid.Parse("10000000-0000-0000-0000-000000000006"), Name = "Toys", SortOrder = 6, CreatedAt = new DateTime(2024,1,1,0,0,0,DateTimeKind.Utc), UpdatedAt = new DateTime(2024,1,1,0,0,0,DateTimeKind.Utc) },
            new Category { Id = Guid.Parse("10000000-0000-0000-0000-000000000007"), Name = "Books", SortOrder = 7, CreatedAt = new DateTime(2024,1,1,0,0,0,DateTimeKind.Utc), UpdatedAt = new DateTime(2024,1,1,0,0,0,DateTimeKind.Utc) },
            new Category { Id = Guid.Parse("10000000-0000-0000-0000-000000000008"), Name = "Collectibles", SortOrder = 8, CreatedAt = new DateTime(2024,1,1,0,0,0,DateTimeKind.Utc), UpdatedAt = new DateTime(2024,1,1,0,0,0,DateTimeKind.Utc) },
        };
        builder.HasData(categories);
    }
}