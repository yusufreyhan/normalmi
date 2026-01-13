using Microsoft.AspNetCore.Mvc;
using NormalMi.API.Models;
using NormalMi.API.Services;

namespace NormalMi.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GameController : ControllerBase
{
    private readonly IGameService _gameService;

    public GameController(IGameService gameService)
    {
        _gameService = gameService;
    }

    [HttpGet("search")]
    public async Task<ActionResult<List<Game>>> SearchGames([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest(new { message = "Arama sorgusu boş olamaz" });
        }

        var games = await _gameService.SearchGamesAsync(query);
        return Ok(games);
    }

    [HttpGet("{gameId}/compare")]
    public async Task<ActionResult<GamePriceComparison>> ComparePrice(
        [FromRoute] string gameId,
        [FromQuery] decimal userPrice)
    {
        if (userPrice <= 0)
        {
            return BadRequest(new { message = "Fiyat 0'dan büyük olmalıdır" });
        }

        var comparison = await _gameService.ComparePriceAsync(gameId, userPrice);
        return Ok(comparison);
    }

    [HttpGet("{gameId}/deals")]
    public async Task<ActionResult<List<GameDeal>>> GetGameDeals([FromRoute] string gameId)
    {
        var deals = await _gameService.GetGameDealsAsync(gameId);
        return Ok(deals);
    }
}

