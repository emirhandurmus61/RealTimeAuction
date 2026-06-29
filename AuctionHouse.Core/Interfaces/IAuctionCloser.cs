namespace AuctionHouse.Core.Interfaces;

/// <summary>
/// Süresi dolmuş aktif açık artırmaları kapatır: Status = Ended yapar,
/// kazananı belirler ve ilgili gruba AuctionEnded yayınlar.
/// BackgroundService tarafından periyodik çağrılır; ayrıca test edilebilir.
/// </summary>
public interface IAuctionCloser
{
    /// <summary>Kapatılan açık artırma sayısını döndürür.</summary>
    Task<int> CloseExpiredAuctionsAsync(CancellationToken ct = default);
}
