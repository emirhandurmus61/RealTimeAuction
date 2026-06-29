namespace AuctionHouse.Core.DTOs;

/// <summary>Bir teklif denemesinin sonuç durumu.</summary>
public enum BidOutcome
{
    Success = 0,
    AuctionNotFound = 1,
    AuctionNotActive = 2,
    AuctionEnded = 3,
    BidTooLow = 4,
    SellerCannotBid = 5,
    /// <summary>Eşzamanlı teklif çakışması — istemci yeniden denemeli.</summary>
    Conflict = 6
}

/// <summary>
/// Teklif iş mantığının sonucu. Exception fırlatmak yerine durum + mesaj döndürür;
/// böylece API katmanı uygun HTTP koduna haritalayabilir.
/// </summary>
public record BidResult(BidOutcome Outcome, string Message, BidDto? Bid = null, decimal? NewCurrentPrice = null)
{
    public bool IsSuccess => Outcome == BidOutcome.Success;

    public static BidResult Success(BidDto bid, decimal newPrice) =>
        new(BidOutcome.Success, "Teklif kabul edildi.", bid, newPrice);

    public static BidResult Fail(BidOutcome outcome, string message) =>
        new(outcome, message);
}
