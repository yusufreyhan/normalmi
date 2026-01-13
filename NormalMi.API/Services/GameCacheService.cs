using NormalMi.API.Models;

namespace NormalMi.API.Services;

public class GameCacheService
{
    private Dictionary<string, List<GameDeal>> _cachedDeals = new(); // gameId -> deals
    private Dictionary<string, DateTime> _cacheTimestamps = new(); // gameId -> timestamp
    private readonly object _lock = new();
    private const int CacheHours = 6; // 6 saat cache

    public List<GameDeal>? GetCachedDeals(string gameId)
    {
        lock (_lock)
        {
            if (_cachedDeals.TryGetValue(gameId, out var deals) && 
                _cacheTimestamps.TryGetValue(gameId, out var timestamp))
            {
                // Cache geçerli mi kontrol et (6 saat)
                if ((DateTime.Now - timestamp).TotalHours < CacheHours)
                {
                    return deals.ToList(); // Kopya döndür
                }
                else
                {
                    // Cache süresi dolmuş, temizle
                    _cachedDeals.Remove(gameId);
                    _cacheTimestamps.Remove(gameId);
                }
            }
            return null;
        }
    }

    public void UpdateCache(string gameId, List<GameDeal> deals)
    {
        lock (_lock)
        {
            _cachedDeals[gameId] = deals;
            _cacheTimestamps[gameId] = DateTime.Now;
            Console.WriteLine($"Game cache güncellendi: {gameId}, {deals.Count} deal, Tarih: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        }
    }
}

