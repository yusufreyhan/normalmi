using Microsoft.AspNetCore.HttpOverrides;
using NormalMi.API.Services;
using OfficeOpenXml;

// EPPlus 8+ için lisans ayarı (global)
// EPPlus 8'de LicenseContext deprecated, ExcelPackage.License kullanılmalı
ExcelPackage.License.SetNonCommercialPersonal("NormalMi");

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

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
        policy.SetIsOriginAllowed(_ => true)
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

app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

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
