using NormalMi.API.Models;

namespace NormalMi.API.Services;

public class ProduceCacheService
{
    private List<Produce> _cachedProduces = new();
    private DateTime _lastUpdate = DateTime.MinValue;
    private readonly object _lock = new();

    public List<Produce> GetCachedProduces()
    {
        lock (_lock)
        {
            return _cachedProduces.ToList(); // Kopya döndür
        }
    }

    public void UpdateCache(List<Produce> produces)
    {
        lock (_lock)
        {
            // Eski verileri tamamen silip yerine yeni verileri ekle
            // Bu satır eski _cachedProduces listesini tamamen yeni liste ile değiştirir
            _cachedProduces = produces;
            _lastUpdate = DateTime.Now;
            Console.WriteLine($"Cache güncellendi: {produces.Count} ürün (eski veriler silindi, yeni veriler eklendi), Tarih: {_lastUpdate:yyyy-MM-dd HH:mm:ss}");
        }
    }

    public DateTime GetLastUpdateTime()
    {
        lock (_lock)
        {
            return _lastUpdate;
        }
    }

    public bool IsCacheValid()
    {
        lock (_lock)
        {
            // Cache 24 saatten eskiyse geçersiz
            return _lastUpdate != DateTime.MinValue && 
                   (DateTime.Now - _lastUpdate).TotalHours < 24;
        }
    }
}

