using EBayClone.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EBayClone.Infrastructure.Data.Configurations;

public class BusinessProfileConfiguration : IEntityTypeConfiguration<BusinessProfile>
{
    public void Configure(EntityTypeBuilder<BusinessProfile> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.CompanyName).IsRequired().HasMaxLength(200);
        builder.Property(b => b.GstNumber).IsRequired().HasMaxLength(20);
        builder.Property(b => b.PanNumber).IsRequired().HasMaxLength(15);
        builder.Property(b => b.BusinessAddress).HasMaxLength(500);
        builder.Property(b => b.BusinessPhone).HasMaxLength(20);
        builder.Property(b => b.BusinessEmail).HasMaxLength(255);
        builder.Property(b => b.BusinessWebsite).HasMaxLength(255);
        builder.Property(b => b.RejectionReason).HasMaxLength(1000);

        builder.HasIndex(b => b.UserId).IsUnique().HasDatabaseName("UQ_BusinessProfiles_UserId");
        builder.HasIndex(b => b.GstNumber).IsUnique().HasDatabaseName("UQ_BusinessProfiles_GstNumber");
        builder.HasIndex(b => b.PanNumber).IsUnique().HasDatabaseName("UQ_BusinessProfiles_PanNumber");

        builder.HasQueryFilter(b => !b.IsDeleted);

        builder.HasMany(b => b.Documents)
            .WithOne(d => d.BusinessProfile)
            .HasForeignKey(d => d.BusinessProfileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
