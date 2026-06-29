namespace AuctionHouse.Core.Entities;

/// <summary>Bir açık artırmaya verilen tek bir teklif.</summary>
public class Bid
{
    public int Id { get; set; }

    public decimal Amount { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // --- İlişkiler ---

    public int AuctionId { get; set; }
    public Auction? Auction { get; set; }

    public string BidderId { get; set; } = string.Empty;
    public ApplicationUser? Bidder { get; set; }
}
