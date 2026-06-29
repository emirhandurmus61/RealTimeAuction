using System.ComponentModel.DataAnnotations;

namespace AuctionHouse.Core.Entities;

/// <summary>Bir açık artırma kaydı.</summary>
public class Auction
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    /// <summary>Açılış fiyatı (başlangıç teklif tabanı).</summary>
    public decimal StartPrice { get; set; }

    /// <summary>Şu anki en yüksek teklif tutarı (teklif yoksa StartPrice).</summary>
    public decimal CurrentPrice { get; set; }

    /// <summary>Her teklifte uygulanacak minimum artış adımı.</summary>
    public decimal MinIncrement { get; set; } = 1m;

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public AuctionStatus Status { get; set; } = AuctionStatus.Scheduled;

    /// <summary>
    /// Optimistic concurrency token. Eşzamanlı tekliflerde çakışmayı
    /// DbUpdateConcurrencyException ile yakalamak için kullanılır.
    /// SQLite RowVersion'ı otomatik üretmediği için değer uygulama tarafında
    /// (her güncellemede yeni bir GUID) atanır; konfigürasyon için
    /// AuctionConfiguration'a bakınız.
    /// </summary>
    public byte[] RowVersion { get; set; } = Guid.NewGuid().ToByteArray();

    // --- İlişkiler ---

    public string SellerId { get; set; } = string.Empty;
    public ApplicationUser? Seller { get; set; }

    public int? CategoryId { get; set; }
    public Category? Category { get; set; }

    /// <summary>Kazanan teklifi veren kullanıcı (kapanınca atanır).</summary>
    public string? WinnerId { get; set; }
    public ApplicationUser? Winner { get; set; }

    public ICollection<Bid> Bids { get; set; } = new List<Bid>();
}
