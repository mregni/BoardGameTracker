using System.Net;
using AutoMapper;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.ViewModels;
using BoardGameTracker.Common.ViewModels.Results;
using BoardGameTracker.Core.Players.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/player")]
public class PlayerController : ControllerBase
{
    private readonly IPlayerService _playerService;
    private readonly IMapper _mapper;
    private readonly ILogger<PlayerController> _logger;

    public PlayerController(IPlayerService playerService, IMapper mapper, ILogger<PlayerController> logger)
    {
        _playerService = playerService;
        _mapper = mapper;
        _logger = logger;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetPlayers()
    {
        var games = await _playerService.GetList();
        var mappedGames = _mapper.Map<IList<PlayerViewModel>>(games);

        return ListResultViewModel<PlayerViewModel>.CreateResult(mappedGames);
    }

    [HttpPost]
    public async Task<IActionResult> CreatePlayer([FromBody] PlayerCreationViewModel? playerCreationViewModel)
    {
        if (playerCreationViewModel == null)
        {
            //TODO: Add to translation file
            var failedViewModel = new FailResultViewModel("No data provided");
            return new OkObjectResult(failedViewModel);
        }

        try
        {
            var player = _mapper.Map<Player>(playerCreationViewModel);
            player = await _playerService.Create(player);

            var playerViewModel = _mapper.Map<PlayerViewModel>(player);
            return ResultViewModel<PlayerViewModel>.CreateCreatedResult(playerViewModel);
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            //TODO: Add to translation file
            var failedViewModel = new FailResultViewModel("Creation failed because of backend error, check logs for details");
            return StatusCode(500, failedViewModel);
        }    
    }
    
    [HttpPut]
    public async Task<IActionResult> UpdatePlayer([FromBody] PlayerViewModel? viewModel)
    {
        if (viewModel is not {Id: { }})
        {
            //TODO: Add to translation file
            var failedViewModel = new FailResultViewModel("No data provided");
            return new OkObjectResult(failedViewModel);
        }
        
        try
        {
            var player = _mapper.Map<Player>(viewModel);
            player = await _playerService.Update(player);
            var result = _mapper.Map<PlayerViewModel>(player);
            return ResultViewModel<PlayerViewModel>.CreateUpdatedResult(result);
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            //TODO: Add to translation file
            var failedViewModel = new FailResultViewModel("Update failed because of backend error, check logs for details");
            return StatusCode(500, failedViewModel);
        }
    }

    
    [HttpGet]
    [Route("{id:int}")]
    public async Task<IActionResult> GetPlayerById(int id)
    {
        var player = await _playerService.Get(id);
        if (player == null)
        {
            return new NotFoundObjectResult(new FailResultViewModel("player.notifications.not-found"));
        }

        var viewModel = _mapper.Map<PlayerViewModel>(player);
        return ResultViewModel<PlayerViewModel>.CreateFoundResult(viewModel);
    }
     
    [HttpDelete]
    [Route("{id:int}")]
    public async Task<IActionResult> DeleteGameById(int id)
    {
        await _playerService.Delete(id);
        return new OkObjectResult(new DeletionResultViewModel(ResultState.Success));
    }
    
    [HttpGet]
    [Route("{id:int}/plays")]
    public async Task<IActionResult> GetGamePlays(int id, [FromQuery] int skip, [FromQuery] int take)
    {
        var plays = await _playerService.GetPlays(id);

        var playViewModel = _mapper.Map<IList<PlayViewModel>>(plays);
        return ResultViewModel<IList<PlayViewModel>>.CreateFoundResult(playViewModel); 
    }
    
    [HttpGet]
    [Route("{id:int}/stats")]
    public async Task<IActionResult> GetGameStats(int id)
    {
        var stats = await _playerService.GetStats(id);

        var statsViewModel = _mapper.Map<PlayerStatisticsViewModel>(stats);
        return ResultViewModel<PlayerStatisticsViewModel>.CreateFoundResult(statsViewModel); 
    }
}