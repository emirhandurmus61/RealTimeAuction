using AuctionHouse.Core.DTOs;
using AuctionHouse.Core.Entities;
using AuctionHouse.Core.Interfaces;
using AuctionHouse.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AuctionHouse.Infrastructure.Services;

public class AuctionService : IAuctionService
{
    private readonly AuctionDbContext _db;

    public AuctionService(AuctionDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<AuctionSummaryDto>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Auctions
            .AsNoTracking()
            .OrderBy(a => a.EndTime)
            .Select(a => new AuctionSummaryDto(
                a.Id, a.Title, a.Category != null ? a.Category.Name : null,
                a.CurrentPrice, a.MinIncrement, a.EndTime, a.Status))
            .ToListAsync(ct);
    }

    public async Task<AuctionDetailDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _db.Auctions
            .AsNoTracking()
            .Where(a => a.Id == id)
            .Select(a => new AuctionDetailDto(
                a.Id, a.Title, a.Description,
                a.Category != null ? a.Category.Name : null,
                a.StartPrice, a.CurrentPrice, a.MinIncrement,
                a.StartTime, a.EndTime, a.Status,
                a.SellerId, a.Seller != null ? a.Seller.DisplayName : null,
                a.Bids
                    .OrderByDescending(b => b.Amount)
                    .Select(b => new BidDto(b.Id, b.Amount, b.Timestamp, b.BidderId,
                        b.Bidder != null ? b.Bidder.DisplayName : null))
                    .ToList()))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<int> CreateAsync(CreateAuctionRequest request, string sellerId, CancellationToken ct = default)
    {
        var auction = new Auction
        {
            Title = request.Title,
            Description = request.Description,
            StartPrice = request.StartPrice,
            CurrentPrice = request.StartPrice,
            MinIncrement = request.MinIncrement <= 0 ? 1m : request.MinIncrement,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            CategoryId = request.CategoryId,
            SellerId = sellerId,
            Status = request.StartTime <= DateTime.UtcNow ? AuctionStatus.Active : AuctionStatus.Scheduled,
            RowVersion = Guid.NewGuid().ToByteArray()
        };

        if (request.ImageUrls is { Count: > 0 })
        {
            var order = 0;
            foreach (var url in request.ImageUrls)
            {
                auction.Images.Add(new AuctionImage { Url = url, SortOrder = order++ });
            }
        }

        _db.Auctions.Add(auction);
        await _db.SaveChangesAsync(ct);
        return auction.Id;
    }

    public async Task<IReadOnlyList<AuctionSummaryDto>> GetBidByUserAsync(string userId, CancellationToken ct = default)
    {
        return await _db.Auctions
            .AsNoTracking()
            .Where(a => a.Bids.Any(b => b.BidderId == userId))
            .OrderByDescending(a => a.Bids.Where(b => b.BidderId == userId).Max(b => b.Timestamp))
            .Select(a => new AuctionSummaryDto(
                a.Id, a.Title, a.Category != null ? a.Category.Name : null,
                a.CurrentPrice, a.MinIncrement, a.EndTime, a.Status))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<AuctionSummaryDto>> GetWonByUserAsync(string userId, CancellationToken ct = default)
    {
        return await _db.Auctions
            .AsNoTracking()
            .Where(a => a.Status == AuctionStatus.Ended && a.WinnerId == userId)
            .OrderByDescending(a => a.EndTime)
            .Select(a => new AuctionSummaryDto(
                a.Id, a.Title, a.Category != null ? a.Category.Name : null,
                a.CurrentPrice, a.MinIncrement, a.EndTime, a.Status))
            .ToListAsync(ct);
    }
}
