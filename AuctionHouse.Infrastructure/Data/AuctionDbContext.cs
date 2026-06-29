using AuctionHouse.Core.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AuctionHouse.Infrastructure.Data;

/// <summary>
/// Uygulamanın EF Core DbContext'i. Identity tablolarını (ApplicationUser üzerinden)
/// ve domain tablolarını (Auction, Bid, Category) tek bir veritabanında birleştirir.
/// </summary>
public class AuctionDbContext : IdentityDbContext<ApplicationUser>
{
    public AuctionDbContext(DbContextOptions<AuctionDbContext> options)
        : base(options)
    {
    }

    public DbSet<Auction> Auctions => Set<Auction>();
    public DbSet<Bid> Bids => Set<Bid>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<AuctionImage> AuctionImages => Set<AuctionImage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Tüm IEntityTypeConfiguration sınıflarını bu assembly'den uygula.
        builder.ApplyConfigurationsFromAssembly(typeof(AuctionDbContext).Assembly);
    }
}
