using EBayClone.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EBayClone.Infrastructure.Data.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.OrderNumber).IsRequired().HasMaxLength(50);
        builder.Property(o => o.TotalAmount).HasPrecision(18, 2);
        builder.Property(o => o.PaymentMethod).IsRequired().HasMaxLength(50);
        builder.Property(o => o.PaymentReference).HasMaxLength(100);
        builder.Property(o => o.UpiId).HasMaxLength(100);
        builder.Property(o => o.ShippingAddress).HasMaxLength(500);
        builder.Property(o => o.Carrier).HasMaxLength(100);
        builder.Property(o => o.TrackingNumber).HasMaxLength(100);
        builder.Property(o => o.Notes).HasMaxLength(1000);

        builder.HasIndex(o => o.OrderNumber).IsUnique().HasDatabaseName("UQ_Orders_OrderNumber");
        builder.HasIndex(o => o.BuyerId).HasDatabaseName("IX_Orders_BuyerId");

        builder.HasQueryFilter(o => !o.IsDeleted);

        builder.HasMany(o => o.Items)
            .WithOne(i => i.Order)
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.UnitPrice).HasPrecision(18, 2);
        builder.Property(i => i.ListingTitle).IsRequired().HasMaxLength(80);
        builder.HasQueryFilter(i => !i.IsDeleted);
    }
}
