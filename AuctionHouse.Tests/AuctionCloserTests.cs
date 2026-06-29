using AuctionHouse.Core.Entities;
using AuctionHouse.Infrastructure.Data;
using AuctionHouse.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AuctionHouse.Tests;

public class AuctionCloserTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AuctionDbContext> _options;
    private readonly FakeNotifier _notifier = new();

    public AuctionCloserTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<AuctionDbContext>().UseSqlite(_connection).Options;

        using var ctx = new AuctionDbContext(_options);
        ctx.Database.EnsureCreated();
        ctx.Users.AddRange(
            new ApplicationUser { Id = "seller", UserName = "seller", Email = "s@test" },
            new ApplicationUser { Id = "winner", UserName = "winner", Email = "winner@test", DisplayName = "Kazanan" });
        ctx.SaveChanges();
    }

    private AuctionDbContext NewContext() => new(_options);
    private AuctionCloser NewCloser(AuctionDbContext ctx) => new(ctx, _notifier, NullLogger<AuctionCloser>.Instance);

    private async Task<int> AddAuctionAsync(AuctionStatus status, int endOffsetMin, string? winnerId)
    {
        using var ctx = NewContext();
        var a = new Auction
        {
            Title = "A", StartPrice = 100m, CurrentPrice = 150m, MinIncrement = 10m,
            StartTime = DateTime.UtcNow.AddHours(-1),
            EndTime = DateTime.UtcNow.AddMinutes(endOffsetMin),
            Status = status, SellerId = "seller", WinnerId = winnerId,
            RowVersion = Guid.NewGuid().ToByteArray()
        };
        ctx.Auctions.Add(a);
        await ctx.SaveChangesAsync();
        return a.Id;
    }

    [Fact]
    public async Task CloseExpired_EndsExpiredActiveAuctions_AndNotifies()
    {
        var expiredId = await AddAuctionAsync(AuctionStatus.Active, endOffsetMin: -1, winnerId: "winner");
        var activeId = await AddAuctionAsync(AuctionStatus.Active, endOffsetMin: 60, winnerId: null);

        using var ctx = NewContext();
        var closed = await NewCloser(ctx).CloseExpiredAuctionsAsync();

        Assert.Equal(1, closed);

        using var verify = NewContext();
        Assert.Equal(AuctionStatus.Ended, (await verify.Auctions.FindAsync(expiredId))!.Status);
        Assert.Equal(AuctionStatus.Active, (await verify.Auctions.FindAsync(activeId))!.Status);

        var ev = Assert.Single(_notifier.Ended);
        Assert.Equal(expiredId, ev.AuctionId);
        Assert.Equal("Kazanan", ev.WinnerName);
    }

    [Fact]
    public async Task CloseExpired_NoExpired_ReturnsZero_AndNoNotification()
    {
        await AddAuctionAsync(AuctionStatus.Active, endOffsetMin: 60, winnerId: null);

        using var ctx = NewContext();
        var closed = await NewCloser(ctx).CloseExpiredAuctionsAsync();

        Assert.Equal(0, closed);
        Assert.Empty(_notifier.Ended);
    }

    public void Dispose() => _connection.Dispose();
}
