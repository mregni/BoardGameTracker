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

        return new OkObjectResult(mappedGames);
    }

    [HttpPost]
    public async Task<IActionResult> CreatePlayer([FromBody] PlayerCreationViewModel? playerCreationViewModel)
    {
        if (playerCreationViewModel == null)
        {
            return new BadRequestResult();
        }

        try
        {
            var player = _mapper.Map<Player>(playerCreationViewModel);
            player = await _playerService.Create(player);

            var playerViewModel = _mapper.Map<PlayerViewModel>(player);
            return new OkObjectResult(playerViewModel);
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            return StatusCode(500);
        }    
    }
    
    [HttpPut]
    public async Task<IActionResult> UpdatePlayer([FromBody] PlayerViewModel? viewModel)
    {
        if (viewModel is null)
        {
            return new BadRequestResult();
        }
        
        try
        {
            var player = _mapper.Map<Player>(viewModel);
            player = await _playerService.Update(player);
            var result = _mapper.Map<PlayerViewModel>(player);
            return new OkObjectResult(result);
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            return StatusCode(500);
        }
    }

    
    [HttpGet]
    [Route("{id:int}")]
    public async Task<IActionResult> GetPlayerById(int id)
    {
        var player = await _playerService.Get(id);
        if (player == null)
        {
            return new NotFoundResult();
        }

        var viewModel = _mapper.Map<PlayerViewModel>(player);
        return new OkObjectResult(viewModel);
    }
     
    [HttpDelete]
    [Route("{id:int}")]
    public async Task<IActionResult> DeleteGameById(int id)
    {
        await _playerService.Delete(id);
        return new OkObjectResult(new DeletionResultViewModel(ResultState.Success));
    }
    
    [HttpGet]
    [Route("{id:int}/stats")]
    public async Task<IActionResult> GetGameStats(int id)
    {
        var stats = await _playerService.GetStats(id);

        var statsViewModel = _mapper.Map<PlayerStatisticsViewModel>(stats);
        return new OkObjectResult(statsViewModel);
    }
    
    [HttpGet]
    [Route("{id:int}/sessions")]
    public async Task<IActionResult> GetGameSessionsById(int id)
    {
        var sessions = await _playerService.GetSessions(id);

        var viewModel = _mapper.Map<IList<SessionViewModel>>(sessions);
        return new OkObjectResult(viewModel);
    }
}