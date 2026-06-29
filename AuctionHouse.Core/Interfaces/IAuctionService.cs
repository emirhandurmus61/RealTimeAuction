using AuctionHouse.Core.DTOs;

namespace AuctionHouse.Core.Interfaces;

public interface IAuctionService
{
    Task<IReadOnlyList<AuctionSummaryDto>> GetAllAsync(CancellationToken ct = default);

    Task<AuctionDetailDto?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>Yeni açık artırma oluşturur ve oluşturulan kaydın id'sini döndürür.</summary>
    Task<int> CreateAsync(CreateAuctionRequest request, string sellerId, CancellationToken ct = default);
}
