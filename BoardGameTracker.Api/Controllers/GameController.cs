using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Common.Models.Bgg;
using BoardGameTracker.Core.Games.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BoardGameTracker.Api.Controllers;

[Route("api/game")]
public class GameController : BaseApiController
{
    private readonly IGameService _gameService;
    private readonly IGameStatisticsService  _gameStatisticsService;

    public GameController(IGameService gameService, IGameStatisticsService gameStatisticsService)
    {
        _gameService = gameService;
        _gameStatisticsService = gameStatisticsService;
    }

    [HttpGet]
    public async Task<IActionResult> GetGames()
    {
        var games = await _gameService.GetGames();
        return Ok(games.ToListDto());
    }

    [HttpPost]
    [Route("")]
    public async Task<IActionResult> CreateGame([FromBody] CreateGameCommand? command)
    {
        if (command == null)
        {
            return BadRequest();
        }

        var game = await _gameService.CreateGameFromCommand(command);
        return Ok(game.ToDto());
    }

    [HttpPut]
    [Route("")]
    public async Task<IActionResult> UpdateGame([FromBody] GameDto? dto)
    {
        if (dto == null)
        {
            return BadRequest();
        }

        try
        {
            var game = dto.ToEntity();
            game = await _gameService.UpdateGame(game);
            return Ok(game.ToDto());
        }
        catch (Exception)
        {
            return StatusCode(500, new { error = "An unexpected error occurred. Please try again later." });
        }
    }

    [HttpDelete]
    [Route("{id:int}")]
    public async Task<IActionResult> DeleteGameById(int id)
    {
        await _gameService.Delete(id);
        return Ok(new { success = true });
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
    public async Task<IActionResult> SearchOnBgg([FromBody] BggSearch search)
    {
        var existingGame = await _gameService.GetGameByBggId(search.BggId);
        if (existingGame != null)
        {
            return Ok(existingGame.ToDto());
        }

        var game = await _gameService.SearchGame(search.BggId);
        if (game == null)
        {
            return BadRequest();
        }

        var dbGame = await _gameService.SearchOnBgg(game, search);
        return Ok(dbGame.ToDto());
    }

    [HttpGet("bgg/import")]
    public async Task<IActionResult> ImportBgg([FromQuery] string username)
    {
        var result = await _gameService.ImportBggCollection(username);
        return Ok(result);
    }

    [HttpPost("bgg/import")]
    public async Task<IActionResult> ImportBggGames([FromBody] ImportBggGamesCommand command)
    {
        try
        {
            await _gameService.ImportList(command.Games);
            return Ok(new { success = true });
        }
        catch (Exception e)
        {
            return StatusCode(500, new { error = e.Message });
        }
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
    public async Task<IActionResult> UpdateGameExpansions(int id, [FromBody] UpdateGameExpansionsCommand command)
    {
        var expansions = await _gameService.UpdateGameExpansions(id, command.ExpansionBggIds);
        return Ok(expansions.ToListDto());
    }

    [HttpDelete]
    [Route("{id:int}/expansion/{expansionId:int}")]
    public async Task<IActionResult> DeleteGameExpansions(int id, int expansionId)
    {
        await _gameService.DeleteExpansion(id, expansionId);
        return Ok(new { success = true });
    }

    [HttpGet]
    [Route("{id:int}/statistics")]
    public async Task<IActionResult> GetGameStatistics(int id)
    {
        var stats = await _gameStatisticsService.CalculateStatisticsAsync(id);
        var topPlayers = await _gameService.GetTopPlayers(id);
        var playByDayChart = await _gameService.GetPlayByDayChart(id);
        var playerCountChart = await _gameService.GetPlayerCountChart(id);
        var playerScoringChart = await _gameService.GetPlayerScoringChart(id);
        var scoringRankChart = await _gameService.GetScoringRankedChart(id);

        return Ok(new
        {
            gameStats = stats,
            topPlayers,
            playByDayChart,
            playerCountChart,
            playerScoringChart,
            scoreRankChart = scoringRankChart
        });
    }

    [HttpGet]
    [Route("shelf-of-shame")]
    public async Task<IActionResult> GetShelfOfShame()
    {
        var games = await _gameService.GetShelfOfShameGames();
        return Ok(games.ToListDto());
    }
}
