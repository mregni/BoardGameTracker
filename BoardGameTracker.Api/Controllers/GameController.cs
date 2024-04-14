using AutoMapper;
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

        return ListResultViewModel<GameViewModel>.CreateResult(mappedGames);
    }

    [HttpGet]
    [Route("{id:int}")]
    public async Task<IActionResult> GetGameById(int id)
    {
        var game = await _gameService.GetGameById(id);
        if (game == null)
        {
            return new NotFoundObjectResult(new FailResultViewModel("game.notifications.not-found"));
        }
        
        var viewModel = _mapper.Map<GameViewModel>(game);
        return ResultViewModel<GameViewModel>.CreateFoundResult(viewModel);
    }

    [HttpDelete]
    [Route("{id:int}")]
    public async Task<IActionResult> DeleteGameById(int id)
    {
        await _gameService.Delete(id);
        return new OkObjectResult(new DeletionResultViewModel(ResultState.Success));
    }

    [HttpGet]
    [Route("{id:int}/plays")]
    public async Task<IActionResult> GetGamePlays(int id, [FromQuery] int? skip, [FromQuery] int? take)
    {
        skip ??= 0;
        take ??= null;
        
        var plays = await _gameService.GetPlays(id, skip.Value, take);
        var flags = await _gameService.GetPlayFlags(id);

        var playViewModel = _mapper.Map<IList<PlayViewModel>>(plays);
        foreach (var flag in flags
                     .Where(x => playViewModel.Any(y => y.Id == x.Value)))
        {
            var viewModel = playViewModel.Single(x => x.Id == flag.Value);
            viewModel.PlayFlags ??= [];
            viewModel.PlayFlags.Add(flag.Key);
        }
        
        var totalCount = await _gameService.GetTotalPlayCount(id);
        return ListResultViewModel<PlayViewModel>.CreateResult(playViewModel, totalCount);
    }

    [HttpGet]
    [Route("{id:int}/stats")]
    public async Task<IActionResult> GetGameStats(int id)
    {
        var stats = await _gameService.GetStats(id);

        var statsViewModel = _mapper.Map<GameStatisticsViewModel>(stats);
        return ResultViewModel<GameStatisticsViewModel>.CreateFoundResult(statsViewModel);
    }
    
    [HttpGet]
    [Route("{id:int}/top")]
    public async Task<IActionResult> GetTopPlayers(int id)
    {
        var topPlayers = await _gameService.GetTopPlayers(id);
        
        var playersViewModel = _mapper.Map<IList<TopPlayerViewModel>>(topPlayers);
        return ResultViewModel<IList<TopPlayerViewModel>>.CreateFoundResult(playersViewModel);
    }

    [HttpPost("bgg")]
    public async Task<IActionResult> SearchOnBgg([FromBody] BggSearch search)
    {
        var existingGame = await _gameService.GetGameByBggId(search.BggId);
        if (existingGame != null)
        {
            var existingGameViewModel = _mapper.Map<GameViewModel>(existingGame);
            return ResultViewModel<GameViewModel>.CreateDuplicateResult(existingGameViewModel);
        }

        var game = await _gameService.SearchAndCreateGame(search.BggId);
        if (game == null)
        {
            var failedViewModel = new FailResultViewModel("error.bgg.id-not-found");
            return new BadRequestObjectResult(failedViewModel);
        }

        var dbGame = await _gameService.ProcessBggGameData(game, search);
        var result = _mapper.Map<GameViewModel>(dbGame);
        return ResultViewModel<GameViewModel>.CreateCreatedResult(result);
    }
}