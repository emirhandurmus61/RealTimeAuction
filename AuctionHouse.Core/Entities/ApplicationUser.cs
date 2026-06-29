using Microsoft.AspNetCore.Identity;

namespace AuctionHouse.Core.Entities;

/// <summary>
/// Uygulama kullanıcısı. ASP.NET Core Identity'nin IdentityUser'ını genişletir.
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }

    /// <summary>Kullanıcının satıcı olarak açtığı açık artırmalar.</summary>
    public ICollection<Auction> Auctions { get; set; } = new List<Auction>();

    /// <summary>Kullanıcının verdiği teklifler.</summary>
    public ICollection<Bid> Bids { get; set; } = new List<Bid>();
}
