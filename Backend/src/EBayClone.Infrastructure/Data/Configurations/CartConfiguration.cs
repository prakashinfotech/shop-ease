using EBayClone.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EBayClone.Infrastructure.Data.Configurations;

public class CartConfiguration : IEntityTypeConfiguration<Cart>
{
    public void Configure(EntityTypeBuilder<Cart> builder)
    {
        builder.HasKey(c => c.Id);
        builder.HasQueryFilter(c => !c.IsDeleted);

        builder.HasMany(c => c.Items)
            .WithOne(i => i.Cart)
            .HasForeignKey(i => i.CartId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.HasKey(i => i.Id);
        builder.HasQueryFilter(i => !i.IsDeleted);
    }
}

public class ListingImageConfiguration : IEntityTypeConfiguration<ListingImage>
{
    public void Configure(EntityTypeBuilder<ListingImage> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Url).IsRequired().HasMaxLength(500);
        builder.Property(i => i.AltText).HasMaxLength(200);
        builder.HasQueryFilter(i => !i.IsDeleted);
    }
}

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Token).IsRequired().HasMaxLength(500);
        builder.Property(r => r.CreatedByIp).HasMaxLength(50);
        builder.Property(r => r.ReplacedByToken).HasMaxLength(500);
        builder.HasIndex(r => r.Token).HasDatabaseName("IX_RefreshTokens_Token");
        builder.HasQueryFilter(r => !r.IsDeleted);
    }
}
