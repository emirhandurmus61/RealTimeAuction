using System.Security.Claims;
using AuctionHouse.Core.Constants;
using AuctionHouse.Core.DTOs;
using AuctionHouse.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuctionHouse.Api.Controllers;

[ApiController]
[Route("api/auctions")]
public class AuctionsController : ControllerBase
{
    private readonly IAuctionService _auctions;
    private readonly IBidService _bids;

    public AuctionsController(IAuctionService auctions, IBidService bids)
    {
        _auctions = auctions;
        _bids = bids;
    }

    // GET /api/auctions
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AuctionSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => Ok(await _auctions.GetAllAsync(ct));

    // GET /api/auctions/5
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AuctionDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var auction = await _auctions.GetByIdAsync(id, ct);
        return auction is null ? NotFound() : Ok(auction);
    }

    // POST /api/auctions  (yalnızca Seller/Admin)
    [HttpPost]
    [Authorize(Roles = $"{Roles.Seller},{Roles.Admin}")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateAuctionRequest request, CancellationToken ct)
    {
        if (request.EndTime <= request.StartTime)
            return BadRequest("Bitiş zamanı başlangıçtan sonra olmalı.");
        if (request.StartPrice < 0)
            return BadRequest("Açılış fiyatı negatif olamaz.");

        var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var id = await _auctions.CreateAsync(request, sellerId, ct);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    // POST /api/auctions/5/bids
    [HttpPost("{id:int}/bids")]
    [Authorize]
    [ProducesResponseType(typeof(BidResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PlaceBid(int id, PlaceBidRequest request, CancellationToken ct)
    {
        var bidderId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _bids.PlaceBidAsync(id, bidderId, request.Amount, ct);

        return result.Outcome switch
        {
            BidOutcome.Success => Ok(result),
            BidOutcome.AuctionNotFound => NotFound(result),
            BidOutcome.Conflict => Conflict(result),
            _ => BadRequest(result)
        };
    }
}
