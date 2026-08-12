using EBayClone.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EBayClone.Infrastructure.Data.Configurations;

public class ListingApprovalLogConfiguration : IEntityTypeConfiguration<ListingApprovalLog>
{
    public void Configure(EntityTypeBuilder<ListingApprovalLog> builder)
    {
        builder.ToTable("ListingApprovalLogs");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Action).IsRequired();
        builder.Property(l => l.Notes).HasMaxLength(2000);

        builder.HasOne(l => l.Listing)
            .WithMany(li => li.ApprovalLogs)
            .HasForeignKey(l => l.ListingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Version)
            .WithMany()
            .HasForeignKey(l => l.VersionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(l => l.Admin)
            .WithMany()
            .HasForeignKey(l => l.AdminId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(l => !l.IsDeleted);
        builder.HasIndex(l => l.ListingId);
    }
}
