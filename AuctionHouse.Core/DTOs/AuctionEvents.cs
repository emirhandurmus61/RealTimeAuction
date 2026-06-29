namespace AuctionHouse.Core.DTOs;

/// <summary>Bir teklif kabul edildiğinde gruba yayılan canlı olay.</summary>
public record BidPlacedEvent(
    int AuctionId,
    decimal NewPrice,
    string BidderName,
    DateTime Timestamp,
    DateTime EndTime);

/// <summary>Anti-sniping ile bitiş süresi uzatıldığında yayılan olay.</summary>
public record AuctionExtendedEvent(int AuctionId, DateTime NewEndTime);

/// <summary>Açık artırma kapandığında yayılan olay.</summary>
public record AuctionEndedEvent(int AuctionId, decimal FinalPrice, string? WinnerName);

/// <summary>İzleyici sayısı değiştiğinde yayılan presence olayı.</summary>
public record ViewerCountChangedEvent(int AuctionId, int ViewerCount);
