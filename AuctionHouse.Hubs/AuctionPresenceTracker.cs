using System.Collections.Concurrent;

namespace AuctionHouse.Hubs;

/// <summary>
/// Her açık artırma grubundaki canlı izleyici (bağlantı) sayısını izler.
/// Singleton olarak kaydedilir; thread-safe.
/// </summary>
public class AuctionPresenceTracker
{
    // auctionId -> (connectionId kümesi)
    private readonly ConcurrentDictionary<int, HashSet<string>> _viewers = new();

    /// <summary>Bağlantıyı gruba ekler, güncel izleyici sayısını döndürür.</summary>
    public int Join(int auctionId, string connectionId)
    {
        var set = _viewers.GetOrAdd(auctionId, _ => new HashSet<string>());
        lock (set)
        {
            set.Add(connectionId);
            return set.Count;
        }
    }

    /// <summary>Bağlantıyı gruptan çıkarır, kalan izleyici sayısını döndürür.</summary>
    public int Leave(int auctionId, string connectionId)
    {
        if (!_viewers.TryGetValue(auctionId, out var set))
            return 0;

        lock (set)
        {
            set.Remove(connectionId);
            return set.Count;
        }
    }

    public int Count(int auctionId)
        => _viewers.TryGetValue(auctionId, out var set) ? set.Count : 0;
}
