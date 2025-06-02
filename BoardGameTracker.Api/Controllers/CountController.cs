using BoardGameTracker.Common.ViewModels;
using BoardGameTracker.Core.Games.Interfaces;
using BoardGameTracker.Core.Locations.Interfaces;
using BoardGameTracker.Core.Players.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/count")]
public class CountController
{
    private readonly IGameService _gameService;
    private readonly IPlayerService _playerService;
    private readonly ILocationService _locationService;

    public CountController(ILocationService locationService, IPlayerService playerService, IGameService gameService)
    {
        _locationService = locationService;
        _playerService = playerService;
        _gameService = gameService;
    }

    [HttpGet]
    public async Task<IActionResult> GetMenuCounts()
    {
        var counts = new KeyValuePairViewModel<string,int>[]
        {
            new("games", await _gameService.CountAsync()),
            new("players", await _playerService.CountAsync()),
            new("locations", await _locationService.CountAsync()),
        };

        return new OkObjectResult(counts);
    }
}