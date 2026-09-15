using System.ComponentModel.DataAnnotations;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Common.Models.Bgg;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Common;
using BoardGameTracker.Core.Games.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/game")]
[Authorize]
public class GameController : ControllerBase
{
    private readonly IGameService _gameService;
    private readonly IGameStatisticsService  _gameStatisticsService;
    private readonly IBggImportService _bggImportService;
    private readonly IGameChartService _gameChartService;
    private readonly IShameService _shameService;

    public GameController(
        IGameService gameService,
        IGameStatisticsService gameStatisticsService,
        IBggImportService bggImportService,
        IGameChartService gameChartService,
        IShameService shameService)
    {
        _gameService = gameService;
        _gameStatisticsService = gameStatisticsService;
        _bggImportService = bggImportService;
        _gameChartService = gameChartService;
        _shameService = shameService;
    }

    [HttpGet]
    [ProducesResponseType<List<GameDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGames()
    {
        var games = await _gameService.GetGames();
        return Ok(games.ToListDto());
    }

    [HttpPost]
    [Route("")]
    [Authorize(Roles = Constants.AuthRoles.UserOrAdmin)]
    [ProducesResponseType<GameDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateGame([FromBody] CreateGameCommand command)
    {
        var game = await _gameService.CreateGameFromCommand(command);
        return CreatedAtAction(nameof(GetGameById), new { id = game.Id }, game.ToDto());
    }

    [HttpPut]
    [Route("")]
    [Authorize(Roles = Constants.AuthRoles.UserOrAdmin)]
    [ProducesResponseType<GameDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateGame([FromBody] UpdateGameCommand command)
    {
        var game = await _gameService.UpdateGame(command);
        return Ok(game.ToDto());
    }

    [HttpDelete]
    [Route("{id:int}")]
    [Authorize(Roles = Constants.AuthRoles.UserOrAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteGameById(int id)
    {
        await _gameService.Delete(id);
        return NoContent();
    }

    [HttpGet]
    [Route("{id:int}")]
    [ProducesResponseType<GameDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGameById(int id)
    {
        var game = await _gameService.GetGameById(id);
        if (game == null)
        {
            return NotFound();
        }

        return Ok(game.ToDto());
    }

    [HttpPost("bgg/search")]
    [Authorize(Roles = Constants.AuthRoles.UserOrAdmin)]
    [ProducesResponseType<GameDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchOnBgg([FromBody] BggSearch search)
    {
        var game = await _bggImportService.ImportGameFromBgg(search);
        if (game == null)
        {
            return NotFound();
        }

        return Ok(game.ToDto());
    }

    [HttpGet("bgg/import")]
    [Authorize(Roles = Constants.AuthRoles.UserOrAdmin)]
    [ProducesResponseType<IList<BggImportGame>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ImportBgg([FromQuery] string username, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return BadRequest();
        }

        var games = await _bggImportService.ImportBggCollection(username.Trim(), cancellationToken);
        return Ok(games);
    }

    [HttpPost("bgg/import")]
    [Authorize(Roles = Constants.AuthRoles.UserOrAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ImportBggGames([FromBody] ImportBggGamesCommand command)
    {
        await _bggImportService.ImportList(command.Games);
        return NoContent();
    }

    [HttpGet]
    [Route("{id:int}/price")]
    [EnableRateLimiting("changedetection")]
    [ProducesResponseType<GamePriceDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGamePrice(int id, [FromQuery] bool refresh, CancellationToken cancellationToken)
    {
        var price = await _gameService.GetGamePriceAsync(id, refresh, cancellationToken);
        if (price == null)
        {
            return NotFound();
        }

        return Ok(price);
    }

    [HttpGet]
    [Route("prices/tracked")]
    [EnableRateLimiting("changedetection")]
    [ProducesResponseType<List<GamePriceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTrackedPrices([FromQuery] bool refresh, CancellationToken cancellationToken)
    {
        var prices = await _gameService.GetTrackedPricesAsync(refresh, cancellationToken);
        return Ok(prices);
    }

    [HttpPost]
    [Route("{id:int}/watch")]
    [Authorize(Roles = Constants.AuthRoles.UserOrAdmin)]
    [EnableRateLimiting("changedetection")]
    [ProducesResponseType<GameDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateWatch(int id, [FromBody] CreateWatchCommand command, CancellationToken cancellationToken)
    {
        var game = await _gameService.CreateWatchForGame(id, command.Url, cancellationToken);
        return Ok(game.ToDto());
    }

    [HttpGet]
    [Route("{id:int}/sessions")]
    [ProducesResponseType<List<SessionDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGameSessionsById(int id, [FromQuery, Range(1, int.MaxValue)] int? count)
    {
        var sessions = await _gameService.GetSessionsForGame(id, count);
        return Ok(sessions.ToListDto());
    }

    [HttpGet]
    [Route("{id:int}/expansions")]
    [ProducesResponseType<ExpansionData[]>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGameExpansions(int id)
    {
        var expansions = await _gameService.SearchExpansionsForGame(id);
        return Ok(expansions);
    }

    [HttpPost]
    [Route("{id:int}/expansions")]
    [Authorize(Roles = Constants.AuthRoles.UserOrAdmin)]
    [ProducesResponseType<List<ExpansionDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateGameExpansions(int id, [FromBody] UpdateGameExpansionsCommand command)
    {
        var expansions = await _gameService.UpdateGameExpansions(id, command.ExpansionBggIds);
        return Ok(expansions.ToListDto());
    }

    [HttpPost]
    [Route("{id:int}/expansions/manual")]
    [Authorize(Roles = Constants.AuthRoles.UserOrAdmin)]
    [ProducesResponseType<ExpansionDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> AddManualExpansion(int id, [FromBody] CreateExpansionCommand command)
    {
        var expansion = await _gameService.AddManualExpansion(id, command.Title);
        return Created((string?)null, expansion.ToDto());
    }

    [HttpDelete]
    [Route("{id:int}/expansion/{expansionId:int}")]
    [Authorize(Roles = Constants.AuthRoles.UserOrAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteGameExpansions(int id, int expansionId)
    {
        await _gameService.DeleteExpansion(id, expansionId);
        return NoContent();
    }

    [HttpGet]
    [Route("{id:int}/statistics")]
    [ProducesResponseType<GameStatisticsResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGameStatistics(int id, CancellationToken cancellationToken)
    {
        var stats = await _gameStatisticsService.CalculateStatisticsAsync(id, cancellationToken);
        var topPlayers = await _gameChartService.GetTopPlayers(id, cancellationToken);
        var playByDayChart = await _gameChartService.GetPlayByDayChart(id, cancellationToken);
        var playerCountChart = await _gameChartService.GetPlayerCountChart(id, cancellationToken);
        var playerScoringChart = await _gameChartService.GetPlayerScoringChart(id, cancellationToken);
        var scoringRankChart = await _gameChartService.GetScoringRankedChart(id, stats.AverageScore, cancellationToken);

        return Ok(new GameStatisticsResponse
        {
            GameStats = stats,
            TopPlayers = topPlayers,
            PlayByDayChart = playByDayChart,
            PlayerCountChart = playerCountChart,
            PlayerScoringChart = playerScoringChart,
            ScoreRankChart = scoringRankChart
        });
    }

    [HttpGet]
    [Route("shames")]
    [ProducesResponseType<List<ShameDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetShameGames()
    {
        var games = await _shameService.GetShameGames();
        return Ok(games.ToListDto());
    }

    [HttpGet]
    [Route("shames/statistics")]
    [ProducesResponseType<ShameStatisticsDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetShameStatistics()
    {
        var statistics = await _shameService.GetShameStatistics();
        return Ok(statistics.ToDto());
    }
}
