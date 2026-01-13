namespace NormalMi.API.Models;

public class Game
{
    public string GameId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string SteamAppId { get; set; } = string.Empty;
    public decimal SalePrice { get; set; }
    public decimal NormalPrice { get; set; }
    public bool IsOnSale { get; set; }
    public string MetacriticScore { get; set; } = string.Empty;
    public string SteamRatingText { get; set; } = string.Empty;
    public int SteamRatingPercent { get; set; }
    public string SteamRatingCount { get; set; } = string.Empty;
    public string StoreId { get; set; } = string.Empty;
    public string Thumb { get; set; } = string.Empty; // Image URL
}

public class GameDeal
{
    public string StoreId { get; set; } = string.Empty;
    public string DealId { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal RetailPrice { get; set; }
    public decimal Savings { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public string BuyUrl { get; set; } = string.Empty; // CheapShark redirect URL
}

public class GamePriceComparison
{
    public decimal UserPrice { get; set; }
    public decimal AveragePrice { get; set; }
    public decimal LowestPrice { get; set; }
    public string Result { get; set; } = string.Empty; // "Ucuz", "Normal", "Pahalı"
    public decimal Difference { get; set; }
    public decimal DifferencePercentage { get; set; }
    public GameDeal? CheapestDeal { get; set; } // En ucuz mağaza bilgisi
    public Game? GameInfo { get; set; }
}

