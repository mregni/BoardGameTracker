using AutoMapper;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.ViewModels;
using BoardGameTracker.Core.Players.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/player")]
public class PlayerController
{
    private readonly IPlayerService _playerService;
    private readonly IMapper _mapper;

    public PlayerController(IPlayerService playerService, IMapper mapper)
    {
        _playerService = playerService;
        _mapper = mapper;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetPlayers()
    {
        var games = await _playerService.GetPlayers();
        var mappedGames = _mapper.Map<IList<PlayerViewModel>>(games);

        var resultViewModel = new ListResultViewModel<PlayerViewModel>(mappedGames);
        return new OkObjectResult(resultViewModel);
    }

    [HttpPost]
    public async Task<IActionResult> CreatePlayer([FromBody] PlayerCreationViewModel? playerViewModel)
    {
        if (playerViewModel == null)
        {
            var failedViewModel = new CreationResultViewModel<PlayerCreationViewModel>(CreationResultType.Failed, null, "No data provided");
            return new OkObjectResult(failedViewModel);
        }

        var player = _mapper.Map<Player>(playerViewModel);
        await _playerService.CreatePlayer(player);
        
        var resultViewModel = new CreationResultViewModel<PlayerViewModel>(CreationResultType.Success, null);
        return new OkObjectResult(resultViewModel);
    }
    
    [HttpGet]
    [Route("{id:int}")]
    public async Task<IActionResult> GetGameById(int id)
    {
        var player = await _playerService.GetPlayer(id);
        if (player == null)
        {
            return new OkObjectResult(SearchResultViewModel<PlayerViewModel>.CreateSearchResult(null));
        }

        var viewModel = _mapper.Map<PlayerViewModel>(player);
        return new OkObjectResult(SearchResultViewModel<PlayerViewModel>.CreateSearchResult(viewModel));
    }
     
    [HttpDelete]
    [Route("{id:int}")]
    public async Task<IActionResult> DeleteGameById(int id)
    {
        await _playerService.Delete(id);
        return new OkObjectResult(new CreationResultViewModel<string>(CreationResultType.Success, null));
    }
}