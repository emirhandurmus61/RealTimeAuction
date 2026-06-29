using AuctionHouse.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace AuctionHouse.Hubs;

public static class DependencyInjection
{
    /// <summary>
    /// SignalR'ı, presence tracker'ı ve IAuctionNotifier implementasyonunu kaydeder.
    /// Hub endpoint'i ayrıca app.MapHub&lt;AuctionHub&gt;("/hubs/auction") ile bağlanmalıdır.
    /// </summary>
    public static IServiceCollection AddAuctionRealtime(this IServiceCollection services)
    {
        services.AddSignalR();
        services.AddSingleton<AuctionPresenceTracker>();
        services.AddScoped<IAuctionNotifier, SignalRAuctionNotifier>();
        return services;
    }
}
