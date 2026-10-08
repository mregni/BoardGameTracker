using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Core.Leaderboard.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/leaderboard")]
[Authorize]
public class LeaderboardController : ControllerBase
{
    private readonly ILeaderboardService _leaderboardService;

    public LeaderboardController(ILeaderboardService leaderboardService)
    {
        _leaderboardService = leaderboardService;
    }

    [HttpGet]
    [ProducesResponseType<LeaderboardDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLeaderboard(CancellationToken cancellationToken)
    {
        return Ok(await _leaderboardService.GetLeaderboardAsync(cancellationToken));
    }
}
