namespace AuctionHouse.Core.Entities;

/// <summary>Bir açık artırmanın yaşam döngüsü durumu.</summary>
public enum AuctionStatus
{
    /// <summary>Başlangıç zamanı henüz gelmedi.</summary>
    Scheduled = 0,

    /// <summary>Teklif kabul ediyor.</summary>
    Active = 1,

    /// <summary>Süresi doldu, kapandı (kazanan belirlendi).</summary>
    Ended = 2,

    /// <summary>Satıcı/Admin tarafından iptal edildi.</summary>
    Cancelled = 3
}
