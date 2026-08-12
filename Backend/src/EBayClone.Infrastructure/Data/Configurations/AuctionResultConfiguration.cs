using EBayClone.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EBayClone.Infrastructure.Data.Configurations;

public class AuctionResultConfiguration : IEntityTypeConfiguration<AuctionResult>
{
    public void Configure(EntityTypeBuilder<AuctionResult> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.WinningAmount).HasPrecision(18, 2);
        builder.HasIndex(a => a.ListingId).IsUnique().HasDatabaseName("IX_AuctionResults_ListingId");
        builder.HasQueryFilter(a => !a.IsDeleted);

        builder.HasOne(a => a.Listing)
            .WithOne(l => l.AuctionResult)
            .HasForeignKey<AuctionResult>(a => a.ListingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Winner)
            .WithMany()
            .HasForeignKey(a => a.WinnerId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
