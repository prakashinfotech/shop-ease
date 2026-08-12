using EBayClone.Domain.Entities;
using EBayClone.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EBayClone.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.FirstName).IsRequired().HasMaxLength(50);
        builder.Property(u => u.LastName).IsRequired().HasMaxLength(50);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(255);
        builder.Property(u => u.PasswordHash).IsRequired();
        builder.Property(u => u.PhoneNumber).HasMaxLength(20);
        builder.Property(u => u.AvatarUrl).HasMaxLength(500);
        builder.Property(u => u.EmailVerificationToken).HasMaxLength(200);
        builder.Property(u => u.PasswordResetToken).HasMaxLength(200);

        builder.HasIndex(u => u.Email).IsUnique().HasDatabaseName("UQ_Users_Email");

        builder.HasQueryFilter(u => !u.IsDeleted);

        builder.HasMany(u => u.RefreshTokens)
            .WithOne(rt => rt.User)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.Listings)
            .WithOne(l => l.Seller)
            .HasForeignKey(l => l.SellerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.BuyerOrders)
            .WithOne(o => o.Buyer)
            .HasForeignKey(o => o.BuyerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(u => u.Cart)
            .WithOne(c => c.User)
            .HasForeignKey<Cart>(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(u => u.BusinessProfile)
            .WithOne(b => b.User)
            .HasForeignKey<BusinessProfile>(b => b.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Seed admin user
        builder.HasData(new User
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            FirstName = "Admin",
            LastName = "User",
            Email = "admin@shopease.com",
            PasswordHash = "$2a$11$l3lJC3jsNR5QSFrF2rnpG.seNn4lfnarYcu1ph79C.gVsyp69mkYa",
            AccountType = AccountType.Business,
            Role = UserRole.Admin,
            IsEmailVerified = true,
            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        });
    }
}
