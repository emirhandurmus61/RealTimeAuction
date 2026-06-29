using AuctionHouse.Core.DTOs;

namespace AuctionHouse.Core.Interfaces;

public interface IBidService
{
    /// <summary>
    /// Bir açık artırmaya teklif verir. İş kurallarını uygular
    /// (aktiflik, süre, minimum artış, kendi artırmana teklif verememe)
    /// ve optimistic concurrency (RowVersion) ile eşzamanlı çakışmaları yönetir.
    /// </summary>
    Task<BidResult> PlaceBidAsync(int auctionId, string bidderId, decimal amount, CancellationToken ct = default);
}
