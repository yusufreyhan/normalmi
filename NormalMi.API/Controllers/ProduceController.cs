using Microsoft.AspNetCore.Mvc;
using NormalMi.API.Models;
using NormalMi.API.Services;

namespace NormalMi.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProduceController : ControllerBase
{
    private readonly IHalService _halService;

    public ProduceController(IHalService halService)
    {
        _halService = halService;
    }

    [HttpGet("list")]
    public async Task<ActionResult> GetProduceList()
    {
        var produces = await _halService.GetProduceListAsync();
        
        // Cache boşsa "Veriler hazırlanıyor" mesajı döndür
        if (produces == null || !produces.Any())
        {
            return StatusCode(202, new 
            { 
                message = "Veriler hazırlanıyor, lütfen birkaç saniye sonra tekrar deneyin.",
                loading = true,
                data = new List<Produce>()
            });
        }
        
        return Ok(produces);
    }

    [HttpGet("price")]
    public async Task<ActionResult<Produce>> GetProducePrice([FromQuery] string product, [FromQuery] string? type = null)
    {
        var produce = await _halService.GetProducePriceAsync(product, type);
        if (produce == null)
            return NotFound(new { message = "Ürün bulunamadı" });

        return Ok(produce);
    }

    [HttpGet("compare")]
    public async Task<ActionResult<ProducePriceComparison>> ComparePrice(
        [FromQuery] string product, 
        [FromQuery] decimal price, 
        [FromQuery] string? type = null)
    {
        var comparison = await _halService.ComparePriceAsync(product, price, type);
        return Ok(comparison);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult> RefreshProduceList()
    {
        try
        {
            var produces = await _halService.RefreshProduceListAsync();
            return Ok(new { message = "Veriler güncellendi", count = produces.Count });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Veri güncellenirken hata oluştu", error = ex.Message });
        }
    }
}

