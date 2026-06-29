using AuctionHouse.Core.Entities;
using AuctionHouse.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuctionHouse.Web.Controllers;

public class AuctionsController : Controller
{
    private readonly AuctionDbContext _db;

    public AuctionsController(AuctionDbContext db)
    {
        _db = db;
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
}
