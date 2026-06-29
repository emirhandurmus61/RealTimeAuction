using AuctionHouse.Core.DTOs;
using AuctionHouse.Core.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace AuctionHouse.Hubs;

/// <summary>
/// IAuctionNotifier'ın SignalR implementasyonu. Olayları ilgili açık artırma
/// grubuna (ve gerektiğinde tek bir kullanıcıya) yayınlar.
/// </summary>
public class SignalRAuctionNotifier : IAuctionNotifier
{
    // İstemci tarafındaki event adları.
    public const string BidPlaced = nameof(BidPlaced);
    public const string AuctionExtended = nameof(AuctionExtended);
    public const string AuctionEnded = nameof(AuctionEnded);
    public const string Outbid = nameof(Outbid);

    private readonly IHubContext<AuctionHub> _hub;

    public SignalRAuctionNotifier(IHubContext<AuctionHub> hub)
    {
        _hub = hub;
    }

    public Task BidPlacedAsync(BidPlacedEvent e, CancellationToken ct = default)
        => _hub.Clients.Group(AuctionHub.GroupName(e.AuctionId)).SendAsync(BidPlaced, e, ct);

    public Task AuctionExtendedAsync(AuctionExtendedEvent e, CancellationToken ct = default)
        => _hub.Clients.Group(AuctionHub.GroupName(e.AuctionId)).SendAsync(AuctionExtended, e, ct);

    public Task AuctionEndedAsync(AuctionEndedEvent e, CancellationToken ct = default)
        => _hub.Clients.Group(AuctionHub.GroupName(e.AuctionId)).SendAsync(AuctionEnded, e, ct);

    public Task OutbidAsync(string userId, int auctionId, decimal newPrice, CancellationToken ct = default)
        => _hub.Clients.User(userId).SendAsync(Outbid, new { auctionId, newPrice }, ct);
}
