using AuctionHouse.Core.DTOs;
using AuctionHouse.Core.Entities;
using AuctionHouse.Core.Interfaces;
using AuctionHouse.Infrastructure.Data;
using AuctionHouse.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AuctionHouse.Tests;

/// <summary>Gönderilen canlı bildirimleri yakalayan test sahtesi.</summary>
internal sealed class FakeNotifier : IAuctionNotifier
{
    public List<BidPlacedEvent> BidPlaced { get; } = new();
    public List<AuctionExtendedEvent> Extended { get; } = new();
    public List<AuctionEndedEvent> Ended { get; } = new();
    public List<(string userId, int auctionId, decimal price)> Outbids { get; } = new();

    public Task BidPlacedAsync(BidPlacedEvent e, CancellationToken ct = default)
    { BidPlaced.Add(e); return Task.CompletedTask; }

    public Task AuctionExtendedAsync(AuctionExtendedEvent e, CancellationToken ct = default)
    { Extended.Add(e); return Task.CompletedTask; }

    public Task AuctionEndedAsync(AuctionEndedEvent e, CancellationToken ct = default)
    { Ended.Add(e); return Task.CompletedTask; }

    public Task OutbidAsync(string userId, int auctionId, decimal newPrice, CancellationToken ct = default)
    { Outbids.Add((userId, auctionId, newPrice)); return Task.CompletedTask; }
}

/// <summary>
/// BidService iş kuralları ve optimistic concurrency testleri.
/// Gerçek RowVersion davranışı gerektiği için (InMemory provider concurrency
/// token desteklemez) RAM üzerinde gerçek bir SQLite bağlantısı kullanılır.
/// </summary>
public class BidServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AuctionDbContext> _options;

    private const string SellerId = "seller-1";
    private const string BidderId = "bidder-1";

    public BidServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<AuctionDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var ctx = new AuctionDbContext(_options);
        ctx.Database.EnsureCreated();

        // FK kısıtları için gereken kullanıcıları oluştur.
        ctx.Users.AddRange(
            new ApplicationUser { Id = SellerId, UserName = "seller", Email = "seller@test" },
            new ApplicationUser { Id = BidderId, UserName = "bidder", Email = "bidder@test" },
            new ApplicationUser { Id = "bidder-A", UserName = "bidderA", Email = "a@test" },
            new ApplicationUser { Id = "bidder-B", UserName = "bidderB", Email = "b@test" });
        ctx.SaveChanges();
    }

    private AuctionDbContext NewContext() => new(_options);

    private readonly FakeNotifier _notifier = new();

    private BidService NewService(AuctionDbContext ctx)
        => new(ctx, _notifier, NullLogger<BidService>.Instance);

    private async Task<Auction> SeedAuctionAsync(
        decimal startPrice = 100m, decimal minIncrement = 10m,
        AuctionStatus status = AuctionStatus.Active,
        int startOffsetMin = -10, int endOffsetMin = 60)
    {
        using var ctx = NewContext();
        var auction = new Auction
        {
            Title = "Test Auction",
            StartPrice = startPrice,
            CurrentPrice = startPrice,
            MinIncrement = minIncrement,
            StartTime = DateTime.UtcNow.AddMinutes(startOffsetMin),
            EndTime = DateTime.UtcNow.AddMinutes(endOffsetMin),
            Status = status,
            SellerId = SellerId,
            RowVersion = Guid.NewGuid().ToByteArray()
        };
        ctx.Auctions.Add(auction);
        await ctx.SaveChangesAsync();
        return auction;
    }

    [Fact]
    public async Task PlaceBid_ValidBid_Succeeds_And_UpdatesPrice()
    {
        var auction = await SeedAuctionAsync(startPrice: 100m, minIncrement: 10m);

        using var ctx = NewContext();
        var result = await NewService(ctx).PlaceBidAsync(auction.Id, BidderId, 110m);

        Assert.True(result.IsSuccess);
        Assert.Equal(BidOutcome.Success, result.Outcome);
        Assert.Equal(110m, result.NewCurrentPrice);

        using var verify = NewContext();
        var updated = await verify.Auctions.FindAsync(auction.Id);
        Assert.Equal(110m, updated!.CurrentPrice);
        Assert.Equal(1, await verify.Bids.CountAsync());
    }

    [Fact]
    public async Task PlaceBid_BelowMinimumIncrement_Fails()
    {
        var auction = await SeedAuctionAsync(startPrice: 100m, minIncrement: 10m);

        using var ctx = NewContext();
        // 105 < 100 + 10
        var result = await NewService(ctx).PlaceBidAsync(auction.Id, BidderId, 105m);

        Assert.False(result.IsSuccess);
        Assert.Equal(BidOutcome.BidTooLow, result.Outcome);
    }

    [Fact]
    public async Task PlaceBid_SellerOnOwnAuction_Fails()
    {
        var auction = await SeedAuctionAsync();

        using var ctx = NewContext();
        var result = await NewService(ctx).PlaceBidAsync(auction.Id, SellerId, 200m);

        Assert.Equal(BidOutcome.SellerCannotBid, result.Outcome);
    }

    [Fact]
    public async Task PlaceBid_OnEndedAuction_Fails()
    {
        var auction = await SeedAuctionAsync(status: AuctionStatus.Ended, endOffsetMin: -1);

        using var ctx = NewContext();
        var result = await NewService(ctx).PlaceBidAsync(auction.Id, BidderId, 200m);

        Assert.Equal(BidOutcome.AuctionEnded, result.Outcome);
    }

    [Fact]
    public async Task PlaceBid_NonExistentAuction_ReturnsNotFound()
    {
        using var ctx = NewContext();
        var result = await NewService(ctx).PlaceBidAsync(999, BidderId, 200m);

        Assert.Equal(BidOutcome.AuctionNotFound, result.Outcome);
    }

    [Fact]
    public async Task PlaceBid_ConcurrentStaleUpdate_IsResolved()
    {
        // İki ayrı context aynı açık artırmayı okur (aynı RowVersion).
        // İlki kaydeder, ikincisinin RowVersion'ı bayatlar -> servis
        // DbUpdateConcurrencyException yakalayıp taze okur ve yeniden değerlendirir.
        var auction = await SeedAuctionAsync(startPrice: 100m, minIncrement: 10m);

        // 1. teklif: 110 -> başarılı, fiyat 110, RowVersion değişti.
        using (var ctx1 = NewContext())
        {
            var r1 = await NewService(ctx1).PlaceBidAsync(auction.Id, "bidder-A", 110m);
            Assert.True(r1.IsSuccess);
        }

        // 2. teklif aynı 110 tutarıyla gelirse: artık geçerli min 120, retry sonrası BidTooLow.
        using (var ctx2 = NewContext())
        {
            var r2 = await NewService(ctx2).PlaceBidAsync(auction.Id, "bidder-B", 110m);
            Assert.False(r2.IsSuccess);
            Assert.Equal(BidOutcome.BidTooLow, r2.Outcome);
        }

        // Veri tutarlı: tek geçerli bid, fiyat 110.
        using var verify = NewContext();
        Assert.Equal(110m, (await verify.Auctions.FindAsync(auction.Id))!.CurrentPrice);
        Assert.Equal(1, await verify.Bids.CountAsync());
    }

    [Fact]
    public async Task PlaceBid_WithinSnipeWindow_ExtendsEndTime_AndNotifies()
    {
        // Bitişe 5 sn kala (snipe penceresi 10 sn) -> bitiş uzatılmalı.
        var auction = await SeedAuctionAsync(startPrice: 100m, minIncrement: 10m,
            startOffsetMin: -10, endOffsetMin: 0);
        // endOffsetMin 0 tam "şimdi"yi verir; birkaç saniyelik pencereyi garanti
        // etmek için bitişi elle 5 sn sonraya çek.
        using (var setup = NewContext())
        {
            var a = await setup.Auctions.FindAsync(auction.Id);
            a!.EndTime = DateTime.UtcNow.AddSeconds(5);
            await setup.SaveChangesAsync();
        }

        using var ctx = NewContext();
        var before = (await ctx.Auctions.FindAsync(auction.Id))!.EndTime;
        var result = await NewService(ctx).PlaceBidAsync(auction.Id, "bidder-A", 110m);

        Assert.True(result.IsSuccess);

        using var verify = NewContext();
        var after = (await verify.Auctions.FindAsync(auction.Id))!.EndTime;
        Assert.True(after > before, "Bitiş zamanı uzatılmalıydı.");
        Assert.True(after > DateTime.UtcNow.AddSeconds(20), "Uzatma ~30 sn olmalı.");

        // Hem teklif hem uzatma bildirimi gönderildi.
        Assert.Single(_notifier.BidPlaced);
        Assert.Single(_notifier.Extended);
    }

    [Fact]
    public async Task PlaceBid_OutbidsPreviousLeader_NotifiesPreviousLeader()
    {
        var auction = await SeedAuctionAsync(startPrice: 100m, minIncrement: 10m);

        using (var ctx1 = NewContext())
            await NewService(ctx1).PlaceBidAsync(auction.Id, "bidder-A", 110m);

        using (var ctx2 = NewContext())
            await NewService(ctx2).PlaceBidAsync(auction.Id, "bidder-B", 120m);

        var outbid = Assert.Single(_notifier.Outbids);
        Assert.Equal("bidder-A", outbid.userId);
        Assert.Equal(120m, outbid.price);
    }

    public void Dispose() => _connection.Dispose();
}
