using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Core.GameNights.Interfaces;
using BoardGameTracker.Core.Games.Interfaces;
using BoardGameTracker.Core.Loans.Interfaces;
using BoardGameTracker.Core.Locations.Interfaces;
using BoardGameTracker.Core.Players.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/count")]
[Authorize]
public class CountController : ControllerBase
{
    private readonly IGameService _gameService;
    private readonly IPlayerService _playerService;
    private readonly ILocationService _locationService;
    private readonly ILoanService _loanService;
    private readonly IGameNightService _gameNightService;
    private readonly IShameService _shameService;

    public CountController(
        ILocationService locationService,
        IPlayerService playerService,
        IGameService gameService,
        ILoanService loanService,
        IGameNightService gameNightService,
        IShameService shameService)
    {
        _locationService = locationService;
        _playerService = playerService;
        _gameService = gameService;
        _loanService = loanService;
        _gameNightService = gameNightService;
        _shameService = shameService;
    }

    [HttpGet]
    [ProducesResponseType<KeyValuePairDto<int>[]>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMenuCounts(CancellationToken cancellationToken)
    {
        var counts = new KeyValuePairDto<int>[]
        {
            new("games", await _gameService.CountAsync(cancellationToken)),
            new("players", await _playerService.CountAsync(cancellationToken)),
            new("locations", await _locationService.CountAsync(cancellationToken)),
            new("shames", await _shameService.CountShelfOfShameGames(cancellationToken)),
            new("loans", await _loanService.CountActiveLoans(cancellationToken)),
            new("game-nights", await _gameNightService.CountFutureGameNights(cancellationToken)),
        };

        return Ok(counts);
    }
}
