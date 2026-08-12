using EBayClone.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EBayClone.Infrastructure.Data.Configurations;

public class ListingViewConfiguration : IEntityTypeConfiguration<ListingView>
{
    public void Configure(EntityTypeBuilder<ListingView> builder)
    {
        builder.HasKey(v => v.Id);

        builder.HasIndex(v => new { v.UserId, v.ListingId })
            .IsUnique()
            .HasDatabaseName("IX_ListingViews_UserId_ListingId");

        builder.HasIndex(v => new { v.UserId, v.LastViewedAt })
            .HasDatabaseName("IX_ListingViews_UserId_LastViewedAt");

        builder.HasOne(v => v.User)
            .WithMany(u => u.ListingViews)
            .HasForeignKey(v => v.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(v => v.Listing)
            .WithMany(l => l.Views)
            .HasForeignKey(v => v.ListingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(v => !v.IsDeleted);
    }
}
