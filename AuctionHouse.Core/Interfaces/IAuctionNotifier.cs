using AuctionHouse.Core.DTOs;

namespace AuctionHouse.Core.Interfaces;

/// <summary>
/// Açık artırma olaylarını ilgili izleyicilere canlı olarak ileten soyutlama.
/// Implementasyonu SignalR (Hubs katmanı) sağlar; Core/Infrastructure SignalR'a
/// doğrudan bağımlı olmaz.
/// </summary>
public interface IAuctionNotifier
{
    Task BidPlacedAsync(BidPlacedEvent e, CancellationToken ct = default);

    Task AuctionExtendedAsync(AuctionExtendedEvent e, CancellationToken ct = default);

    Task AuctionEndedAsync(AuctionEndedEvent e, CancellationToken ct = default);

    /// <summary>Önceki en yüksek teklif sahibine "teklifin geçildi" bildirimi (targeted).</summary>
    Task OutbidAsync(string userId, int auctionId, decimal newPrice, CancellationToken ct = default);
}
