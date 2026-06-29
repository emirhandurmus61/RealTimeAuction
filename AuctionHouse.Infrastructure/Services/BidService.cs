using AuctionHouse.Core.DTOs;
using AuctionHouse.Core.Entities;
using AuctionHouse.Core.Interfaces;
using AuctionHouse.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuctionHouse.Infrastructure.Services;

/// <summary>
/// Teklif iş mantığı. Eşzamanlı tekliflerin doğru yönetilmesi için
/// optimistic concurrency (RowVersion) + sınırlı retry kullanır.
/// </summary>
public class BidService : IBidService
{
    private const int MaxRetries = 3;

    private readonly AuctionDbContext _db;
    private readonly ILogger<BidService> _logger;

    public BidService(AuctionDbContext db, ILogger<BidService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<BidResult> PlaceBidAsync(int auctionId, string bidderId, decimal amount, CancellationToken ct = default)
    {
        for (var attempt = 1; attempt <= MaxRetries; attempt++)
        {
            // Her denemede taze oku — RowVersion dahil güncel durumu al.
            var auction = await _db.Auctions.FirstOrDefaultAsync(a => a.Id == auctionId, ct);

            var validation = Validate(auction, bidderId, amount);
            if (validation is not null)
                return validation;

            // auction null değil (Validate kontrol etti).
            auction!.CurrentPrice = amount;
            auction.WinnerId = bidderId; // şimdilik en yüksek teklif sahibi
            // Concurrency token'ı elle ilerlet (SQLite otomatik üretmez).
            auction.RowVersion = Guid.NewGuid().ToByteArray();

            var bid = new Bid
            {
                AuctionId = auctionId,
                BidderId = bidderId,
                Amount = amount,
                Timestamp = DateTime.UtcNow
            };
            _db.Bids.Add(bid);

            try
            {
                await _db.SaveChangesAsync(ct);

                var dto = new BidDto(bid.Id, bid.Amount, bid.Timestamp, bidderId, null);
                return BidResult.Success(dto, amount);
            }
            catch (DbUpdateConcurrencyException)
            {
                // Başka bir teklif arada araya girdi. Entity'leri ayır ve yeniden dene.
                _logger.LogWarning(
                    "Teklif çakışması: auction {AuctionId}, deneme {Attempt}/{Max}",
                    auctionId, attempt, MaxRetries);

                Detach(auction);
                Detach(bid);

                if (attempt == MaxRetries)
                    return BidResult.Fail(BidOutcome.Conflict,
                        "Eşzamanlı teklif yoğunluğu nedeniyle teklif işlenemedi, lütfen tekrar deneyin.");
            }
        }

        return BidResult.Fail(BidOutcome.Conflict, "Teklif işlenemedi.");
    }

    /// <summary>İş kurallarını kontrol eder. Sorun varsa hatalı sonuç, yoksa null döner.</summary>
    private static BidResult? Validate(Auction? auction, string bidderId, decimal amount)
    {
        if (auction is null)
            return BidResult.Fail(BidOutcome.AuctionNotFound, "Açık artırma bulunamadı.");

        if (auction.Status == AuctionStatus.Ended || DateTime.UtcNow >= auction.EndTime)
            return BidResult.Fail(BidOutcome.AuctionEnded, "Açık artırma sona erdi.");

        if (auction.Status != AuctionStatus.Active || DateTime.UtcNow < auction.StartTime)
            return BidResult.Fail(BidOutcome.AuctionNotActive, "Açık artırma henüz aktif değil.");

        if (auction.SellerId == bidderId)
            return BidResult.Fail(BidOutcome.SellerCannotBid, "Kendi açık artırmanıza teklif veremezsiniz.");

        var minimumValid = auction.CurrentPrice + auction.MinIncrement;
        if (amount < minimumValid)
            return BidResult.Fail(BidOutcome.BidTooLow,
                $"Teklif en az {minimumValid:N2} olmalı (güncel {auction.CurrentPrice:N2} + min artış {auction.MinIncrement:N2}).");

        return null;
    }

    private void Detach(object entity)
    {
        var entry = _db.Entry(entity);
        if (entry.State != EntityState.Detached)
            entry.State = EntityState.Detached;
    }
}
