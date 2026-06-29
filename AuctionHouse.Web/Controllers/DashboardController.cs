using System.Security.Claims;
using AuctionHouse.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuctionHouse.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IAuctionService _auctions;

    public DashboardController(IAuctionService auctions)
    {
        _auctions = auctions;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // GET /Dashboard  (tekliflerim)
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["Title"] = "Tekliflerim";
        var items = await _auctions.GetBidByUserAsync(UserId, ct);
        return View(items);
    }

    // GET /Dashboard/Won  (kazandıklarım)
    public async Task<IActionResult> Won(CancellationToken ct)
    {
        ViewData["Title"] = "Kazandıklarım";
        var items = await _auctions.GetWonByUserAsync(UserId, ct);
        return View(items);
    }
}
