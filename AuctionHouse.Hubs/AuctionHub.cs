using System.Collections.Concurrent;
using AuctionHouse.Core.DTOs;
using Microsoft.AspNetCore.SignalR;

namespace AuctionHouse.Hubs;

/// <summary>
/// Açık artırma canlı yayın hub'ı. Her açık artırma bir SignalR grubudur
/// ("auction-{id}"). İstemciler JoinAuction ile gruba katılır; teklif/uzatma/
/// kapanış olayları ve izleyici sayısı bu gruba yayınlanır.
/// </summary>
public class AuctionHub : Hub
{
    public const string ViewerCountChanged = nameof(ViewerCountChanged);

    private readonly AuctionPresenceTracker _presence;

    // connectionId -> katıldığı auctionId (disconnect temizliği için).
    private static readonly ConcurrentDictionary<string, int> ConnectionAuction = new();

    public AuctionHub(AuctionPresenceTracker presence)
    {
        _presence = presence;
    }

    public static string GroupName(int auctionId) => $"auction-{auctionId}";

    public async Task JoinAuction(int auctionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(auctionId));
        ConnectionAuction[Context.ConnectionId] = auctionId;

        var count = _presence.Join(auctionId, Context.ConnectionId);
        await BroadcastViewerCount(auctionId, count);
    }

    public async Task LeaveAuction(int auctionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(auctionId));
        ConnectionAuction.TryRemove(Context.ConnectionId, out _);

        var count = _presence.Leave(auctionId, Context.ConnectionId);
        await BroadcastViewerCount(auctionId, count);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (ConnectionAuction.TryRemove(Context.ConnectionId, out var auctionId))
        {
            var count = _presence.Leave(auctionId, Context.ConnectionId);
            await BroadcastViewerCount(auctionId, count);
        }

        await base.OnDisconnectedAsync(exception);
    }

    private Task BroadcastViewerCount(int auctionId, int count)
        => Clients.Group(GroupName(auctionId))
            .SendAsync(ViewerCountChanged, new ViewerCountChangedEvent(auctionId, count));
}
