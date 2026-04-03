using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Common.Models.Bgg;
using BoardGameTracker.Core.Games.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    public async Task<IActionResult> GetGames()
    {
        var games = await _gameService.GetGames();
        return Ok(games.ToListDto());
    }

    [HttpPost]
    [Route("")]
    [Authorize(Roles = Constants.AuthRoles.UserOrAdmin)]
    public async Task<IActionResult> CreateGame([FromBody] CreateGameCommand command)
    {
        var game = await _gameService.CreateGameFromCommand(command);
        return Ok(game.ToDto());
    }

    [HttpPut]
    [Route("")]
    [Authorize(Roles = Constants.AuthRoles.UserOrAdmin)]
    public async Task<IActionResult> UpdateGame([FromBody] UpdateGameCommand command)
    {
        var game = await _gameService.UpdateGame(command);
        return Ok(game.ToDto());
    }

    [HttpDelete]
    [Route("{id:int}")]
    [Authorize(Roles = Constants.AuthRoles.UserOrAdmin)]
    public async Task<IActionResult> DeleteGameById(int id)
    {
        await _gameService.Delete(id);
        return NoContent();
    }

    [HttpGet]
    [Route("{id:int}")]
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
    public async Task<IActionResult> SearchOnBgg([FromBody] BggSearch search)
    {
        var game = await _bggImportService.ImportGameFromBgg(search);
        if (game == null)
        {
            return BadRequest();
        }

        return Ok(game.ToDto());
    }

    [HttpGet("bgg/import")]
    public async Task<IActionResult> ImportBgg([FromQuery] string username)
    {
        var result = await _bggImportService.ImportBggCollection(username);
        return Ok(result);
    }

    [HttpPost("bgg/import")]
    [Authorize(Roles = Constants.AuthRoles.UserOrAdmin)]
    public async Task<IActionResult> ImportBggGames([FromBody] ImportBggGamesCommand command)
    {
        await _bggImportService.ImportList(command.Games);
        return NoContent();
    }

    [HttpGet]
    [Route("{id:int}/sessions")]
    public async Task<IActionResult> GetGameSessionsById(int id, [FromQuery] int? count)
    {
        var sessions = await _gameService.GetSessionsForGame(id, count);
        return Ok(sessions.ToListDto());
    }

    [HttpGet]
    [Route("{id:int}/expansions")]
    public async Task<IActionResult> GetGameExpansions(int id)
    {
        var expansions = await _gameService.SearchExpansionsForGame(id);
        return Ok(expansions);
    }

    [HttpPost]
    [Route("{id:int}/expansions")]
    [Authorize(Roles = Constants.AuthRoles.UserOrAdmin)]
    public async Task<IActionResult> UpdateGameExpansions(int id, [FromBody] UpdateGameExpansionsCommand command)
    {
        var expansions = await _gameService.UpdateGameExpansions(id, command.ExpansionBggIds);
        return Ok(expansions.ToListDto());
    }

    [HttpDelete]
    [Route("{id:int}/expansion/{expansionId:int}")]
    [Authorize(Roles = Constants.AuthRoles.UserOrAdmin)]
    public async Task<IActionResult> DeleteGameExpansions(int id, int expansionId)
    {
        await _gameService.DeleteExpansion(id, expansionId);
        return NoContent();
    }

    [HttpGet]
    [Route("{id:int}/statistics")]
    public async Task<IActionResult> GetGameStatistics(int id)
    {
        var stats = await _gameStatisticsService.CalculateStatisticsAsync(id);
        var topPlayers = await _gameChartService.GetTopPlayers(id);
        var playByDayChart = await _gameChartService.GetPlayByDayChart(id);
        var playerCountChart = await _gameChartService.GetPlayerCountChart(id);
        var playerScoringChart = await _gameChartService.GetPlayerScoringChart(id);
        var scoringRankChart = await _gameChartService.GetScoringRankedChart(id);

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
    public async Task<IActionResult> GetShameGames()
    {
        var games = await _shameService.GetShameGames();
        return Ok(games.ToListDto());
    }

    [HttpGet]
    [Route("shames/statistics")]
    public async Task<IActionResult> GetShameStatistics()
    {
        var statistics = await _shameService.GetShameStatistics();
        return Ok(statistics.ToDto());
    }
}
