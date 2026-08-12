using EBayClone.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EBayClone.Infrastructure.Data.Configurations;

public class BidConfiguration : IEntityTypeConfiguration<Bid>
{
    public void Configure(EntityTypeBuilder<Bid> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Amount).HasPrecision(18, 2).IsRequired();
        builder.HasIndex(b => b.ListingId).HasDatabaseName("IX_Bids_ListingId");
        builder.HasIndex(b => b.BidderId).HasDatabaseName("IX_Bids_BidderId");
        builder.HasQueryFilter(b => !b.IsDeleted);

        builder.HasOne(b => b.Listing)
            .WithMany(l => l.Bids)
            .HasForeignKey(b => b.ListingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(b => b.Bidder)
            .WithMany()
            .HasForeignKey(b => b.BidderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
