using AuctionHouse.Core.Entities;

namespace AuctionHouse.Core.DTOs;

/// <summary>Liste görünümü için özet açık artırma bilgisi.</summary>
public record AuctionSummaryDto(
    int Id,
    string Title,
    string? CategoryName,
    decimal CurrentPrice,
    decimal MinIncrement,
    DateTime EndTime,
    AuctionStatus Status);

/// <summary>Detay görünümü — teklif geçmişiyle birlikte.</summary>
public record AuctionDetailDto(
    int Id,
    string Title,
    string? Description,
    string? CategoryName,
    decimal StartPrice,
    decimal CurrentPrice,
    decimal MinIncrement,
    DateTime StartTime,
    DateTime EndTime,
    AuctionStatus Status,
    string SellerId,
    string? SellerName,
    IReadOnlyList<BidDto> Bids);

/// <summary>Tek bir teklif.</summary>
public record BidDto(
    int Id,
    decimal Amount,
    DateTime Timestamp,
    string BidderId,
    string? BidderName);

/// <summary>Yeni açık artırma oluşturma isteği (satıcı).</summary>
public record CreateAuctionRequest(
    string Title,
    string? Description,
    decimal StartPrice,
    decimal MinIncrement,
    DateTime StartTime,
    DateTime EndTime,
    int? CategoryId,
    IReadOnlyList<string>? ImageUrls = null);

/// <summary>Teklif verme isteği.</summary>
public record PlaceBidRequest(decimal Amount);
