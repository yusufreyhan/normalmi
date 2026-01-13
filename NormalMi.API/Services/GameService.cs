using System.Text.Json;
using NormalMi.API.Models;

namespace NormalMi.API.Services;

public class GameService : IGameService
{
    private readonly HttpClient _httpClient;
    private readonly GameCacheService? _cacheService;
    private const string CheapSharkBaseUrl = "https://www.cheapshark.com/api/1.0";

    public GameService(HttpClient httpClient, GameCacheService cacheService)
    {
        _httpClient = httpClient;
        _cacheService = cacheService;
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "NormalMi/1.0");
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<List<Game>> SearchGamesAsync(string query)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<Game>();

            var url = $"{CheapSharkBaseUrl}/games?title={Uri.EscapeDataString(query)}&limit=20";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"CheapShark API error: {response.StatusCode}");
                return new List<Game>();
            }

            var json = await response.Content.ReadAsStringAsync();
            var games = JsonSerializer.Deserialize<List<CheapSharkGameResponse>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<CheapSharkGameResponse>();

            return games.Select(g => new Game
            {
                GameId = g.GameId ?? "",
                Title = g.External ?? "",
                SteamAppId = g.SteamAppId ?? "",
                SalePrice = ParsePrice(g.Cheapest),
                NormalPrice = ParsePrice(g.Cheapest),
                IsOnSale = !string.IsNullOrEmpty(g.Cheapest) && g.Cheapest != "0.00",
                Thumb = g.Thumb ?? ""
            }).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Game search error: {ex.Message}");
            return new List<Game>();
        }
    }

    public async Task<List<GameDeal>> GetGameDealsAsync(string gameId)
    {
        // Önce cache'i kontrol et
        if (_cacheService != null)
        {
            var cached = _cacheService.GetCachedDeals(gameId);
            if (cached != null)
            {
                Console.WriteLine($"Game deals cache'den döndürülüyor: {gameId}, {cached.Count} deal");
                return cached;
            }
        }

        try
        {
            // CheapShark API'den game deals çek
            var url = $"{CheapSharkBaseUrl}/games?id={Uri.EscapeDataString(gameId)}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"CheapShark deals API error: {response.StatusCode}");
                return new List<GameDeal>();
            }

            var json = await response.Content.ReadAsStringAsync();
            var gameData = JsonSerializer.Deserialize<CheapSharkGameDealsResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (gameData?.Deals == null || !gameData.Deals.Any())
                return new List<GameDeal>();

            var deals = new List<GameDeal>();
            
            // Store bilgilerini al
            var storesUrl = $"{CheapSharkBaseUrl}/stores";
            var storesResponse = await _httpClient.GetAsync(storesUrl);
            var storesJson = await storesResponse.Content.ReadAsStringAsync();
            var stores = JsonSerializer.Deserialize<List<CheapSharkStoreResponse>>(storesJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<CheapSharkStoreResponse>();
            
            var storeDict = stores.ToDictionary(s => s.StoreId ?? "", s => s.StoreName ?? "");

            foreach (var deal in gameData.Deals)
            {
                var dealId = deal.DealId ?? "";
                var price = ParsePrice(deal.Price);
                var retailPrice = ParsePrice(deal.RetailPrice);
                var savings = ParsePrice(deal.Savings);
                var storeId = deal.StoreId ?? "";

                deals.Add(new GameDeal
                {
                    DealId = dealId,
                    StoreId = storeId,
                    StoreName = storeDict.TryGetValue(storeId, out var storeName) ? storeName : "Unknown Store",
                    Price = price,
                    RetailPrice = retailPrice,
                    Savings = savings,
                    BuyUrl = $"https://www.cheapshark.com/redirect?dealID={dealId}"
                });
            }

            // Cache'e kaydet
            if (_cacheService != null && deals.Any())
            {
                _cacheService.UpdateCache(gameId, deals);
            }

            return deals;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Get game deals error: {ex.Message}");
            return new List<GameDeal>();
        }
    }

    public async Task<GamePriceComparison> ComparePriceAsync(string gameId, decimal userPrice)
    {
        try
        {
            // Game bilgilerini ve deals'ları al
            var deals = await GetGameDealsAsync(gameId);
            
            if (!deals.Any())
            {
                return new GamePriceComparison
                {
                    UserPrice = userPrice,
                    AveragePrice = 0,
                    LowestPrice = 0,
                    Result = "Bilinmiyor",
                    Difference = 0,
                    DifferencePercentage = 0
                };
            }

            // Game info için search yap
            Game? game = null;
            try
            {
                var url = $"{CheapSharkBaseUrl}/games?id={Uri.EscapeDataString(gameId)}";
                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var gameData = JsonSerializer.Deserialize<CheapSharkGameDealsResponse>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    
                    if (gameData?.Info != null)
                    {
                        game = new Game
                        {
                            GameId = gameId,
                            Title = gameData.Info.Title ?? "",
                            Thumb = gameData.Info.Thumb ?? ""
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Get game info error: {ex.Message}");
            }

            // Ortalama fiyat hesapla
            var averagePrice = deals.Average(d => d.Price);
            var lowestDeal = deals.OrderBy(d => d.Price).First();
            
            // Karşılaştırma yap (%10 tolerans)
            var difference = userPrice - averagePrice;
            var differencePercentage = averagePrice > 0 ? (difference / averagePrice) * 100 : 0;

            string result;
            if (differencePercentage <= -10)
                result = "Ucuz";
            else if (differencePercentage >= 10)
                result = "Pahalı";
            else
                result = "Normal";

            return new GamePriceComparison
            {
                UserPrice = userPrice,
                AveragePrice = averagePrice,
                LowestPrice = lowestDeal.Price,
                Result = result,
                Difference = difference,
                DifferencePercentage = differencePercentage,
                CheapestDeal = lowestDeal,
                GameInfo = game
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Compare price error: {ex.Message}");
            return new GamePriceComparison
            {
                UserPrice = userPrice,
                AveragePrice = 0,
                LowestPrice = 0,
                Result = "Bilinmiyor",
                Difference = 0,
                DifferencePercentage = 0
            };
        }
    }

    private decimal ParsePrice(string? priceText)
    {
        if (string.IsNullOrEmpty(priceText))
            return 0;

        if (decimal.TryParse(priceText, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var price))
        {
            return price;
        }
        return 0;
    }

    // CheapShark API Response Models
    private class CheapSharkGameResponse
    {
        public string? GameId { get; set; }
        public string? SteamAppId { get; set; }
        public string? External { get; set; }
        public string? Cheapest { get; set; }
        public string? Thumb { get; set; }
    }

    private class CheapSharkGameDealsResponse
    {
        public CheapSharkGameInfo? Info { get; set; }
        public List<CheapSharkDealResponse>? Deals { get; set; }
    }

    private class CheapSharkGameInfo
    {
        public string? Title { get; set; }
        public string? Thumb { get; set; }
    }

    private class CheapSharkDealResponse
    {
        public string? StoreId { get; set; }
        public string? DealId { get; set; }
        public string? Price { get; set; }
        public string? RetailPrice { get; set; }
        public string? Savings { get; set; }
    }

    private class CheapSharkStoreResponse
    {
        public string? StoreId { get; set; }
        public string? StoreName { get; set; }
    }
}

