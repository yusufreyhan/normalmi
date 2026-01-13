using NormalMi.API.Models;

namespace NormalMi.API.Services;

public interface IHalService
{
    Task<List<Produce>> GetProduceListAsync();
    Task<List<Produce>> RefreshProduceListAsync(); // Cache'i bypass ederek direkt çek
    Task<Produce?> GetProducePriceAsync(string productName, string? productType = null);
    Task<ProducePriceComparison> ComparePriceAsync(string productName, decimal userPrice, string? productType = null);
}

