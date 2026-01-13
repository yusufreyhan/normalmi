using NormalMi.API.Services;
using OfficeOpenXml;

// EPPlus 8+ için lisans ayarı (global)
// EPPlus 8'de LicenseContext deprecated, ExcelPackage.License kullanılmalı
ExcelPackage.License.SetNonCommercialPersonal("NormalMi");

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // JSON property isimlerini camelCase'e çevir
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS ayarları (Frontend için)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5500", "http://127.0.0.1:5500", "http://localhost:3000", 
                          "https://localhost:7160", "http://localhost:5036")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// HttpClient ve servisler
builder.Services.AddSingleton<ProduceCacheService>();
builder.Services.AddSingleton<GameCacheService>();
builder.Services.AddHttpClient<IHalService, HalService>();
builder.Services.AddHttpClient<IGameService, GameService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();

// Background service - Günlük veri güncelleme
builder.Services.AddHostedService<HalBackgroundService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");

// Static files (wwwroot) - Default files (index.html) önce olmalı
app.UseDefaultFiles();
app.UseStaticFiles();

// HTML route'ları için middleware - /meyve-sebze -> /meyve-sebze.html
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value;
    if (path != null && !path.Contains(".") && !path.StartsWith("/api") && path != "/")
    {
        var htmlPath = path + ".html";
        var filePath = Path.Combine(app.Environment.WebRootPath ?? "", htmlPath.TrimStart('/'));
        if (File.Exists(filePath))
        {
            context.Request.Path = htmlPath;
        }
    }
    await next();
});

app.UseAuthorization();
app.MapControllers();

// Fallback routing - HTML sayfaları için
app.MapFallbackToFile("index.html");

app.Run();
