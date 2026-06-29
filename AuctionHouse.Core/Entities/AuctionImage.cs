using System.ComponentModel.DataAnnotations;

namespace AuctionHouse.Core.Entities;

/// <summary>Bir açık artırma ilanına ait ürün görseli.</summary>
public class AuctionImage
{
    public int Id { get; set; }

    /// <summary>wwwroot köküne göreli yol, örn. "/uploads/abc123.jpg".</summary>
    [Required]
    [MaxLength(400)]
    public string Url { get; set; } = string.Empty;

    /// <summary>Galeri sıralaması (0 = kapak görseli).</summary>
    public int SortOrder { get; set; }

    // --- İlişki ---
    public int AuctionId { get; set; }
    public Auction? Auction { get; set; }
}
