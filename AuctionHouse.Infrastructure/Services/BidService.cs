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

    // Anti-sniping: bitişe bu süreden az kala gelen teklif, bitişi uzatır.
    private static readonly TimeSpan SnipeWindow = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan SnipeExtension = TimeSpan.FromSeconds(30);

    private readonly AuctionDbContext _db;
    private readonly IAuctionNotifier _notifier;
    private readonly ILogger<BidService> _logger;

    public BidService(AuctionDbContext db, IAuctionNotifier notifier, ILogger<BidService> logger)
    {
        _db = db;
        _notifier = notifier;
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
            var previousLeaderId = auction!.WinnerId; // outbid bildirimi için (güncellemeden önce).

            auction.CurrentPrice = amount;
            auction.WinnerId = bidderId; // şimdilik en yüksek teklif sahibi

            // Anti-sniping: son saniyelerde gelen teklif bitişi uzatır.
            var now = DateTime.UtcNow;
            var extended = false;
            if (auction.EndTime - now <= SnipeWindow)
            {
                auction.EndTime = now + SnipeExtension;
                extended = true;
            }

            // Concurrency token'ı elle ilerlet (SQLite otomatik üretmez).
            auction.RowVersion = Guid.NewGuid().ToByteArray();

            var bid = new Bid
            {
                AuctionId = auctionId,
                BidderId = bidderId,
                Amount = amount,
                Timestamp = now
            };
            _db.Bids.Add(bid);

            try
            {
                await _db.SaveChangesAsync(ct);

                await NotifyAsync(auction, bid, bidderId, previousLeaderId, extended, ct);

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

    /// <summary>
    /// Teklif kabul edildikten sonra canlı bildirimleri gönderir. Bildirim
    /// hatası teklifin başarısını etkilemez (yalnızca loglanır).
    /// </summary>
    private async Task NotifyAsync(
        Auction auction, Bid bid, string bidderId, string? previousLeaderId,
        bool extended, CancellationToken ct)
    {
        try
        {
            // İzleyicilere okunabilir bir ad göster (DisplayName > Email > "Anonim").
            var bidderName = await _db.Users
                .Where(u => u.Id == bidderId)
                .Select(u => u.DisplayName ?? u.Email)
                .FirstOrDefaultAsync(ct) ?? "Anonim";

            await _notifier.BidPlacedAsync(new BidPlacedEvent(
                auction.Id, auction.CurrentPrice, bidderName, bid.Timestamp, auction.EndTime), ct);

            if (extended)
                await _notifier.AuctionExtendedAsync(
                    new AuctionExtendedEvent(auction.Id, auction.EndTime), ct);

            // Önceki lider varsa ve farklı bir kullanıcıysa "teklifin geçildi" bildir.
            if (!string.IsNullOrEmpty(previousLeaderId) && previousLeaderId != bidderId)
                await _notifier.OutbidAsync(previousLeaderId, auction.Id, auction.CurrentPrice, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Teklif sonrası canlı bildirim gönderilemedi (auction {AuctionId}).", auction.Id);
        }
    }

    private void Detach(object entity)
    {
        var entry = _db.Entry(entity);
        if (entry.State != EntityState.Detached)
            entry.State = EntityState.Detached;
    }
}
