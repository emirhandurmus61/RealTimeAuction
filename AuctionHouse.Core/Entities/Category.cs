namespace AuctionHouse.Core.Entities;

/// <summary>Açık artırma kategorisi (Elektronik, Sanat, vb.).</summary>
public class Category
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public ICollection<Auction> Auctions { get; set; } = new List<Auction>();
}
