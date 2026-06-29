using AuctionHouse.Core.Constants;
using AuctionHouse.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AuctionHouse.Infrastructure.Data;

/// <summary>
/// Uygulama başlangıcında veritabanını migrate eder ve temel/örnek verileri ekler:
/// roller, bir örnek satıcı, kategoriler ve birkaç aktif açık artırma.
/// </summary>
public static class DbSeeder
{
    public const string SampleSellerEmail = "seller@auction.local";
    private const string SampleSellerPassword = "Seller123!";

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;

        var db = sp.GetRequiredService<AuctionDbContext>();
        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();

        await db.Database.MigrateAsync();

        await SeedRolesAsync(roleManager);
        var seller = await SeedSampleSellerAsync(userManager);
        await SeedCatalogAsync(db, seller);
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    private static async Task<ApplicationUser> SeedSampleSellerAsync(UserManager<ApplicationUser> userManager)
    {
        var seller = await userManager.FindByEmailAsync(SampleSellerEmail);
        if (seller is not null)
            return seller;

        seller = new ApplicationUser
        {
            UserName = SampleSellerEmail,
            Email = SampleSellerEmail,
            EmailConfirmed = true,
            DisplayName = "Örnek Satıcı"
        };

        var result = await userManager.CreateAsync(seller, SampleSellerPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Örnek satıcı oluşturulamadı: {errors}");
        }

        await userManager.AddToRoleAsync(seller, Roles.Seller);
        return seller;
    }

    private static async Task SeedCatalogAsync(AuctionDbContext db, ApplicationUser seller)
    {
        if (await db.Auctions.AnyAsync())
            return;

        var electronics = new Category { Name = "Elektronik" };
        var art = new Category { Name = "Sanat & Koleksiyon" };
        var collectibles = new Category { Name = "Antika" };
        db.Categories.AddRange(electronics, art, collectibles);

        var now = DateTime.UtcNow;

        var auctions = new[]
        {
            NewAuction("Vintage Leica M3 Fotoğraf Makinesi", 500m, seller.Id, electronics,
                start: now.AddMinutes(-30), end: now.AddHours(2), increment: 25m),

            NewAuction("İmzalı Modern Sanat Tablosu", 1200m, seller.Id, art,
                start: now.AddMinutes(-10), end: now.AddHours(6), increment: 50m),

            NewAuction("1960 Model Mekanik Kol Saati", 300m, seller.Id, collectibles,
                start: now.AddMinutes(-5), end: now.AddMinutes(20), increment: 10m),
        };

        db.Auctions.AddRange(auctions);
        await db.SaveChangesAsync();
    }

    private static Auction NewAuction(
        string title, decimal startPrice, string sellerId, Category category,
        DateTime start, DateTime end, decimal increment)
    {
        return new Auction
        {
            Title = title,
            Description = $"{title} — örnek seed açık artırması.",
            StartPrice = startPrice,
            CurrentPrice = startPrice,
            MinIncrement = increment,
            StartTime = start,
            EndTime = end,
            Status = AuctionStatus.Active,
            SellerId = sellerId,
            Category = category,
            RowVersion = Guid.NewGuid().ToByteArray()
        };
    }
}
