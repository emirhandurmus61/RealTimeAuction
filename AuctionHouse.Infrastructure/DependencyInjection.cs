using AuctionHouse.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AuctionHouse.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// EF Core DbContext'i (SQLite) DI'a kaydeder. Identity ve servisler ayrıca
    /// ilgili katmanlarda kaydedilir.
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' bulunamadı.");

        services.AddDbContext<AuctionDbContext>(options =>
            options.UseSqlite(connectionString));

        return services;
    }
}
