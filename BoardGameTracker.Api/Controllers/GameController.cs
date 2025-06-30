using AutoMapper;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Common.Models.Bgg;
using BoardGameTracker.Common.ViewModels;
using BoardGameTracker.Common.ViewModels.Results;
using BoardGameTracker.Core.Games.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/game")]
public class GameController : ControllerBase
{
    private readonly IGameService _gameService;
    private readonly IMapper _mapper;

    public GameController(IGameService gameService, IMapper mapper)
    {
        _gameService = gameService;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<IActionResult> GetGames()
    {
        var games = await _gameService.GetGames();
        var mappedGames = _mapper.Map<IList<GameViewModel>>(games);

        return new OkObjectResult(mappedGames);
    }

    [HttpPost]
    [Route("")]
    public async Task<IActionResult> CreateGame([FromBody] CreateGameViewModel? gameViewModel)
    {
        if (gameViewModel == null)
        {
            return new BadRequestResult();
        }

        var game = _mapper.Map<Game>(gameViewModel);
        game = await _gameService.CreateGame(game);
        return new OkObjectResult(_mapper.Map<GameViewModel>(game));
    }

    [HttpPut]
    [Route("")]
    public async Task<IActionResult> UpdateGame([FromBody] GameViewModel? gameViewModel)
    {
        if (gameViewModel == null)
        {
            return new BadRequestResult();
        }

        try
        {
            var game = _mapper.Map<Game>(gameViewModel);
            game = await _gameService.UpdateGame(game);

            return new OkObjectResult(_mapper.Map<GameViewModel>(game));
        }
        catch (Exception e)
        {
            return StatusCode(500);
        }
    }
    
    [HttpDelete]
    [Route("{id:int}")]
    public async Task<IActionResult> DeleteGameById(int id)
    {
        await _gameService.Delete(id);
        return new OkObjectResult(new DeletionResultViewModel(ResultState.Success));
    }


    [HttpGet]
    [Route("{id:int}")]
    public async Task<IActionResult> GetGameById(int id)
    {
        var game = await _gameService.GetGameById(id);
        if (game == null)
        {
            return new NotFoundResult();
        }

        var viewModel = _mapper.Map<GameViewModel>(game);
        return new OkObjectResult(viewModel);
    }
    
    [HttpPost("bgg/search")]
    public async Task<IActionResult> SearchOnBgg([FromBody] BggSearch search)
    {
        var existingGame = await _gameService.GetGameByBggId(search.BggId);
        if (existingGame != null)
        {
            var existingGameViewModel = _mapper.Map<GameViewModel>(existingGame);
            return new OkObjectResult(existingGameViewModel);
        }

        var game = await _gameService.SearchGame(search.BggId);
        if (game == null)
        {
            return new BadRequestResult();
        }

        var dbGame = await _gameService.ProcessBggGameData(game, search);
        var result = _mapper.Map<GameViewModel>(dbGame);
        return new OkObjectResult(result);
    }
    
    [HttpGet("bgg/import")]
    public async Task<IActionResult> ImportBgg([FromQuery] string username)
    {
        var result = await _gameService.ImportBggCollection(username); 
        return new OkObjectResult(result);
    }
    
    [HttpPost("bgg/import")]
    public async Task<IActionResult> ImportBggGames([FromBody] ImportGameListViewModal gameImport)
    {
        try
        {
            var gamesToImport = _mapper.Map<IList<ImportGame>>(gameImport.Games);
            await _gameService.ImportList(gamesToImport);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        
        return new OkObjectResult(true);
    }

    [HttpGet]
    [Route("{id:int}/sessions")]
    public async Task<IActionResult> GetGameSessionsById(int id)
    {
        var sessions = await _gameService.GetSessionsForGame(id);

        var viewModel = _mapper.Map<IList<SessionViewModel>>(sessions);
        return new OkObjectResult(viewModel);
    }
    
    [HttpGet]
    [Route("{id:int}/expansions")]
    public async Task<IActionResult> GetGameExpansions(int id)
    {
        var expansions = await _gameService.SearchExpansionsForGame(id);

        var expansionsViewModel = _mapper.Map<IList<BggLinkViewModel>>(expansions);
        return new OkObjectResult(expansionsViewModel);
    }
    
    [HttpPost]
    [Route("{id:int}/expansions")]
    public async Task<IActionResult> UpdateGameExpansions(int id, [FromBody] GameExpansionUpdateViewModel model)
    {
        var expansions = await _gameService.UpdateGameExpansions(id, model.ExpansionBggIds);
        
        var viewModel = _mapper.Map<IList<Expansion>>(expansions);
        return new OkObjectResult(viewModel);
    }
    
    [HttpGet]
    [Route("{id:int}/statistics")]
    public async Task<IActionResult> GetGameStatistics(int id)
    {
        var stats = await _gameService.GetStats(id);
        var topPlayers = await _gameService.GetTopPlayers(id);
        var playByDayChart = await _gameService.GetPlayByDayChart(id);
        var playerCountChart = await _gameService.GetPlayerCountChart(id);
        var playerScoringChart = await _gameService.GetPlayerScoringChart(id);
        var scoringRankChart = await _gameService.GetScoringRankedChart(id);

        var result = new GameStatisticsViewModel
        {
            GameStats = _mapper.Map<GameStatsViewModel>(stats),
            TopPlayers = _mapper.Map<IList<TopPlayerViewModel>>(topPlayers),
            PlayByDayChart = _mapper.Map<IList<PlayByDayChartViewModel>>(playByDayChart),
            PlayerCountChart = _mapper.Map<IList<PlayerCountChartViewModel>>(playerCountChart),
            PlayerScoringChart = _mapper.Map<IList<PlayerScoringChartViewModel>>(playerScoringChart),
            ScoreRankChart = _mapper.Map<IList<ScoreRankChartViewModel>>(scoringRankChart),
        };
        
        return new OkObjectResult(result);
    }
}