using System.Security.Claims;
using AuctionHouse.Core.DTOs;
using AuctionHouse.Core.Interfaces;
using AuctionHouse.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuctionHouse.Web.Controllers;

public class AuctionsController : Controller
{
    private readonly AuctionDbContext _db;
    private readonly IBidService _bids;

    public AuctionsController(AuctionDbContext db, IBidService bids)
    {
        _db = db;
        _bids = bids;
    }

    // GET: /Auctions
    public async Task<IActionResult> Index()
    {
        var auctions = await _db.Auctions
            .Include(a => a.Category)
            .Include(a => a.Seller)
            .OrderBy(a => a.EndTime)
            .AsNoTracking()
            .ToListAsync();

        return View(auctions);
    }

    // GET: /Auctions/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var auction = await _db.Auctions
            .Include(a => a.Category)
            .Include(a => a.Seller)
            .Include(a => a.Bids.OrderByDescending(b => b.Amount))
                .ThenInclude(b => b.Bidder)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id);

        if (auction is null)
            return NotFound();

        return View(auction);
    }

    // POST: /Auctions/PlaceBid  (AJAX, giriş gerekli)
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceBid(int id, decimal amount, CancellationToken ct)
    {
        var bidderId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _bids.PlaceBidAsync(id, bidderId, amount, ct);

        return Json(new { success = result.IsSuccess, message = result.Message, newPrice = result.NewCurrentPrice });
    }
}
