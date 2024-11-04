using AutoMapper;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Models.Bgg;
using BoardGameTracker.Common.ViewModels;
using BoardGameTracker.Common.ViewModels.Results;
using BoardGameTracker.Core.Games.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/game")]
public class GameController
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

    [HttpDelete]
    [Route("{id:int}")]
    public async Task<IActionResult> DeleteGameById(int id)
    {
        await _gameService.Delete(id);
        return new OkObjectResult(new DeletionResultViewModel(ResultState.Success));
    }

    [HttpGet]
    [Route("{id:int}/stats")]
    public async Task<IActionResult> GetGameStats(int id)
    {
        var stats = await _gameService.GetStats(id);

        var statsViewModel = _mapper.Map<GameStatisticsViewModel>(stats);
        return new OkObjectResult(statsViewModel);
    }
    
    [HttpGet]
    [Route("{id:int}/top")]
    public async Task<IActionResult> GetTopPlayers(int id)
    {
        var topPlayers = await _gameService.GetTopPlayers(id);
        
        var playersViewModel = _mapper.Map<IList<TopPlayerViewModel>>(topPlayers);
        return new OkObjectResult(playersViewModel);
    }

    [HttpPost("bgg")]
    public async Task<IActionResult> SearchOnBgg([FromBody] BggSearch search)
    {
        var existingGame = await _gameService.GetGameByBggId(search.BggId);
        if (existingGame != null)
        {
            var existingGameViewModel = _mapper.Map<GameViewModel>(existingGame);
            return new OkObjectResult(existingGameViewModel);
        }

        var game = await _gameService.SearchAndCreateGame(search.BggId);
        if (game == null)
        {
            return new BadRequestResult();
        }

        var dbGame = await _gameService.ProcessBggGameData(game, search);
        var result = _mapper.Map<GameViewModel>(dbGame);
        return new OkObjectResult(result);
    }

    [HttpGet("{id:int}/chart/sessionsbyday")]
    public async Task<IActionResult> PlayByDayChart(int id)
    {
        var data = await _gameService.GetPlayByDayChart(id);
        var dataViewModel = _mapper.Map<IList<PlayByDayChartViewModel>>(data);
        return new OkObjectResult(dataViewModel);
    }
    
    [HttpGet("{id:int}/chart/playercounts")]
    public async Task<IActionResult> PlayerCounts(int id)
    {
        var data = await _gameService.GetPlayerCountChart(id);
        var dataViewModel = _mapper.Map<IList<PlayerCountChartViewModel>>(data);
        return new OkObjectResult(dataViewModel);
    }
        
    [HttpGet("{id:int}/chart/playerscoring")]
    public async Task<IActionResult> PlayerScoring(int id)
    {
        var game = await _gameService.GetGameById(id);
        if (game is {HasScoring: false})
        {
            return new BadRequestResult();
        }
        
        var data = await _gameService.GetPlayerScoringChart(id);
        var dataViewModel = _mapper.Map<IList<PlayerScoringChartViewModel>>(data);
        return new OkObjectResult(dataViewModel);
    }
    
    [HttpGet("{id:int}/chart/scorerank")]
    public async Task<IActionResult> ScoringRanked(int id)
    {
        var data = await _gameService.GetScoringRankedChart(id);
        var dataViewModel = _mapper.Map<IList<ScoreRankChartViewModel>>(data);
        return new OkObjectResult(dataViewModel);
    }
}