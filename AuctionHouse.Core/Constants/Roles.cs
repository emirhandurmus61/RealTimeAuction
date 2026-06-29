namespace AuctionHouse.Core.Constants;

/// <summary>Uygulama rolleri. Tek kaynak — string'leri elle yazmak yerine bunları kullan.</summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string Seller = "Seller";
    public const string Bidder = "Bidder";

    public static readonly string[] All = { Admin, Seller, Bidder };
}
