using EBayClone.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EBayClone.Infrastructure.Data.Configurations;

public class ListingVersionConfiguration : IEntityTypeConfiguration<ListingVersion>
{
    public void Configure(EntityTypeBuilder<ListingVersion> builder)
    {
        builder.ToTable("ListingVersions");
        builder.HasKey(v => v.Id);

        builder.Property(v => v.SnapshotJson).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(v => v.RejectionReason).HasMaxLength(2000);
        builder.Property(v => v.VersionNumber).IsRequired();
        builder.Property(v => v.Status).IsRequired();
        builder.Property(v => v.IsPendingUpdate).IsRequired();
        builder.Property(v => v.SubmittedAt).IsRequired();

        builder.HasOne(v => v.Listing)
            .WithMany(l => l.Versions)
            .HasForeignKey(v => v.ListingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(v => v.ReviewedByAdmin)
            .WithMany()
            .HasForeignKey(v => v.ReviewedByAdminId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasQueryFilter(v => !v.IsDeleted);
        builder.HasIndex(v => new { v.ListingId, v.Status });
    }
}
