using System.Security.Claims;
using AuctionHouse.Core.Constants;
using AuctionHouse.Core.DTOs;
using AuctionHouse.Core.Interfaces;
using AuctionHouse.Infrastructure.Data;
using AuctionHouse.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AuctionHouse.Web.Controllers;

public class AuctionsController : Controller
{
    private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
    private const long MaxImageBytes = 5 * 1024 * 1024; // 5 MB
    private const int MaxImagesPerAuction = 8;

    private readonly AuctionDbContext _db;
    private readonly IBidService _bids;
    private readonly IAuctionService _auctions;
    private readonly IWebHostEnvironment _env;

    public AuctionsController(AuctionDbContext db, IBidService bids, IAuctionService auctions, IWebHostEnvironment env)
    {
        _db = db;
        _bids = bids;
        _auctions = auctions;
        _env = env;
    }

    // GET: /Auctions
    public async Task<IActionResult> Index()
    {
        var auctions = await _db.Auctions
            .Include(a => a.Category)
            .Include(a => a.Seller)
            .Include(a => a.Images.OrderBy(i => i.SortOrder))
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
            .Include(a => a.Winner)
            .Include(a => a.Images.OrderBy(i => i.SortOrder))
            .Include(a => a.Bids.OrderByDescending(b => b.Amount))
                .ThenInclude(b => b.Bidder)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id);

        if (auction is null)
            return NotFound();

        return View(auction);
    }

    // GET: /Auctions/Create  (sadece Seller)
    [Authorize(Roles = Roles.Seller)]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        var model = new CreateAuctionViewModel
        {
            StartTime = DateTime.Now,
            EndTime = DateTime.Now.AddDays(1),
            MinIncrement = 10m
        };
        await PopulateCategoriesAsync(model, ct);
        return View(model);
    }

    // POST: /Auctions/Create
    [HttpPost]
    [Authorize(Roles = Roles.Seller)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateAuctionViewModel model, CancellationToken ct)
    {
        if (model.EndTime <= model.StartTime)
            ModelState.AddModelError(nameof(model.EndTime), "Bitiş zamanı, başlangıçtan sonra olmalıdır.");

        if (model.EndTime <= DateTime.Now)
            ModelState.AddModelError(nameof(model.EndTime), "Bitiş zamanı gelecekte olmalıdır.");

        ValidateImages(model.Images);

        if (!ModelState.IsValid)
        {
            await PopulateCategoriesAsync(model, ct);
            return View(model);
        }

        var imageUrls = await SaveImagesAsync(model.Images, ct);

        var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var request = new CreateAuctionRequest(
            Title: model.Title.Trim(),
            Description: string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
            StartPrice: model.StartPrice,
            MinIncrement: model.MinIncrement,
            // Form yerel saat alır; sunucu UTC tutar.
            StartTime: model.StartTime.ToUniversalTime(),
            EndTime: model.EndTime.ToUniversalTime(),
            CategoryId: model.CategoryId,
            ImageUrls: imageUrls);

        var id = await _auctions.CreateAsync(request, sellerId, ct);
        TempData["Created"] = "Açık artırma başarıyla yayınlandı.";
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>Yüklenen görselleri tür/boyut/adet açısından doğrular, hataları ModelState'e ekler.</summary>
    private void ValidateImages(List<IFormFile>? images)
    {
        if (images is null || images.Count == 0)
            return;

        if (images.Count > MaxImagesPerAuction)
            ModelState.AddModelError(nameof(CreateAuctionViewModel.Images),
                $"En fazla {MaxImagesPerAuction} fotoğraf yükleyebilirsiniz.");

        foreach (var file in images)
        {
            if (file.Length == 0)
                continue;

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedImageExtensions.Contains(ext))
                ModelState.AddModelError(nameof(CreateAuctionViewModel.Images),
                    $"'{file.FileName}' desteklenmeyen bir dosya türü. İzin verilenler: {string.Join(", ", AllowedImageExtensions)}");

            if (file.Length > MaxImageBytes)
                ModelState.AddModelError(nameof(CreateAuctionViewModel.Images),
                    $"'{file.FileName}' çok büyük (en fazla {MaxImageBytes / (1024 * 1024)} MB).");
        }
    }

    /// <summary>Doğrulanmış görselleri wwwroot/uploads altına benzersiz adlarla kaydeder, göreli yolları döndürür.</summary>
    private async Task<List<string>> SaveImagesAsync(List<IFormFile>? images, CancellationToken ct)
    {
        var urls = new List<string>();
        if (images is null || images.Count == 0)
            return urls;

        var uploadsRoot = Path.Combine(_env.WebRootPath, "uploads");
        Directory.CreateDirectory(uploadsRoot);

        foreach (var file in images.Where(f => f.Length > 0))
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(uploadsRoot, fileName);

            await using var stream = System.IO.File.Create(fullPath);
            await file.CopyToAsync(stream, ct);

            urls.Add($"/uploads/{fileName}");
        }

        return urls;
    }

    private async Task PopulateCategoriesAsync(CreateAuctionViewModel model, CancellationToken ct)
    {
        var categories = await _db.Categories
            .OrderBy(c => c.Name)
            .AsNoTracking()
            .ToListAsync(ct);

        model.Categories = categories
            .Select(c => new SelectListItem(c.Name, c.Id.ToString()))
            .ToList();
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
