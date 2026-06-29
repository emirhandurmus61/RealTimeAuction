using AuctionHouse.Core.DTOs;
using AuctionHouse.Core.Entities;
using AuctionHouse.Core.Interfaces;
using AuctionHouse.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuctionHouse.Infrastructure.Services;

/// <summary>
/// Süresi dolmuş aktif açık artırmaları kapatır. Kazanan = en yüksek teklif
/// sahibi (WinnerId teklif sırasında zaten güncellenir). Kapanışı gruba yayınlar.
/// </summary>
public class AuctionCloser : IAuctionCloser
{
    private readonly AuctionDbContext _db;
    private readonly IAuctionNotifier _notifier;
    private readonly ILogger<AuctionCloser> _logger;

    public AuctionCloser(AuctionDbContext db, IAuctionNotifier notifier, ILogger<AuctionCloser> logger)
    {
        _db = db;
        _notifier = notifier;
        _logger = logger;
    }

    public async Task<int> CloseExpiredAuctionsAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        var expired = await _db.Auctions
            .Where(a => a.Status == AuctionStatus.Active && a.EndTime <= now)
            .ToListAsync(ct);

        if (expired.Count == 0)
            return 0;

        foreach (var auction in expired)
        {
            auction.Status = AuctionStatus.Ended;
            auction.RowVersion = Guid.NewGuid().ToByteArray();
        }

        await _db.SaveChangesAsync(ct);

        // Kazanan adlarını çöz ve kapanışı yayınla.
        foreach (var auction in expired)
        {
            string? winnerName = null;
            if (!string.IsNullOrEmpty(auction.WinnerId))
            {
                winnerName = await _db.Users
                    .Where(u => u.Id == auction.WinnerId)
                    .Select(u => u.DisplayName ?? u.Email)
                    .FirstOrDefaultAsync(ct);
            }

            _logger.LogInformation(
                "Açık artırma kapatıldı: {AuctionId} '{Title}', son fiyat {Price}, kazanan {Winner}",
                auction.Id, auction.Title, auction.CurrentPrice, winnerName ?? "(teklif yok)");

            try
            {
                await _notifier.AuctionEndedAsync(
                    new AuctionEndedEvent(auction.Id, auction.CurrentPrice, winnerName), ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AuctionEnded bildirimi gönderilemedi (auction {AuctionId}).", auction.Id);
            }
        }

        return expired.Count;
    }
}
