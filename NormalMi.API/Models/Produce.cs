namespace NormalMi.API.Models;

public class Produce
{
    public string ProductName { get; set; } = string.Empty;
    public string ProductType { get; set; } = string.Empty;
    public string ProductCategory { get; set; } = string.Empty; // Geleneksel, İyi Tarım, Organik
    public decimal AveragePrice { get; set; }
    public decimal Volume { get; set; }
    public string Unit { get; set; } = string.Empty;
}

public class ProducePriceComparison
{
    public decimal UserPrice { get; set; }
    public decimal MarketPrice { get; set; }
    public string Result { get; set; } = string.Empty; // "Ucuz", "Normal", "Pahalı"
    public decimal Difference { get; set; }
    public decimal DifferencePercentage { get; set; }
    public Produce? ProduceInfo { get; set; }
}

