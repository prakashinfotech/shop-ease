using EBayClone.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace EBayClone.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Listing> Listings => Set<Listing>();
    public DbSet<ListingView> ListingViews => Set<ListingView>();
    public DbSet<ListingImage> ListingImages => Set<ListingImage>();
    public DbSet<ListingAttributeValue> ListingAttributeValues => Set<ListingAttributeValue>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<CategoryAttribute> CategoryAttributes => Set<CategoryAttribute>();
    public DbSet<AttributeOption> AttributeOptions => Set<AttributeOption>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<BusinessProfile> BusinessProfiles => Set<BusinessProfile>();
    public DbSet<UserDocument> UserDocuments => Set<UserDocument>();
    public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();
    public DbSet<ListingVersion> ListingVersions => Set<ListingVersion>();
    public DbSet<ListingApprovalLog> ListingApprovalLogs => Set<ListingApprovalLog>();
    public DbSet<Bid> Bids => Set<Bid>();
    public DbSet<AuctionResult> AuctionResults => Set<AuctionResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Modified))
        {
            if (entry.Entity is Domain.Common.BaseEntity entity)
                entity.UpdatedAt = DateTime.UtcNow;
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
