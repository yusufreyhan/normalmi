using NormalMi.API.Models;

namespace NormalMi.API.Services;

public interface IGameService
{
    Task<List<Game>> SearchGamesAsync(string query);
    Task<GamePriceComparison> ComparePriceAsync(string gameId, decimal userPrice);
    Task<List<GameDeal>> GetGameDealsAsync(string gameId);
}

