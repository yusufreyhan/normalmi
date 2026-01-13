using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NormalMi.API.Services;

public class HalBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<HalBackgroundService> _logger;

    public HalBackgroundService(IServiceProvider serviceProvider, ILogger<HalBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // İlk çalıştırmada hemen veri çek
        await RefreshDataAsync();

        // Her gün saat 04:00'te güncelle
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.Now;
            var nextRun = now.Date.AddDays(1).AddHours(4); // Yarın saat 04:00
            
            // Eğer şu an 04:00'ten önceyse, bugün 04:00'te çalıştır
            if (now.Hour < 4)
            {
                nextRun = now.Date.AddHours(4);
            }

            var delay = nextRun - now;
            _logger.LogInformation($"Sonraki güncelleme: {nextRun:yyyy-MM-dd HH:mm:ss} ({delay.TotalHours:F1} saat sonra)");

            await Task.Delay(delay, stoppingToken);

            if (!stoppingToken.IsCancellationRequested)
            {
                await RefreshDataAsync();
            }
        }
    }

    private async Task RefreshDataAsync()
    {
        try
        {
            _logger.LogInformation("HAL verileri güncelleniyor...");
            
            using var scope = _serviceProvider.CreateScope();
            // Cache servisini doğrudan al (singleton, aynı instance)
            var cacheService = scope.ServiceProvider.GetRequiredService<ProduceCacheService>();
            var halService = scope.ServiceProvider.GetRequiredService<IHalService>();
            
            // Veriyi çek ve cache'i güncelle
            var produces = await halService.RefreshProduceListAsync();
            
            // Cache'in güncellendiğini doğrula
            var cachedCount = cacheService.GetCachedProduces().Count;
            var lastUpdate = cacheService.GetLastUpdateTime();
            _logger.LogInformation($"HAL verileri güncellendi: {produces.Count} ürün, Cache'de: {cachedCount} ürün, Son güncelleme: {lastUpdate:yyyy-MM-dd HH:mm:ss}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HAL verileri güncellenirken hata oluştu");
        }
    }
}

