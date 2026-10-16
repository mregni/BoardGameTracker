using System.ComponentModel.DataAnnotations;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Models;
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
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetLeaderboard(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery, Range(1, int.MaxValue)] int? gameId,
        [FromQuery, Range(1, 1000)] int minSessions = LeaderboardFilter.DefaultMinSessions,
        [FromQuery] LeaderboardRankBy rankBy = LeaderboardRankBy.Wins,
        CancellationToken cancellationToken = default)
    {
        if (from.HasValue && to.HasValue && from.Value > to.Value)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "The start date must be on or before the end date.",
            });
        }

        var filter = new LeaderboardFilter(from, to, gameId, minSessions, rankBy);
        return Ok(await _leaderboardService.GetLeaderboardAsync(filter, cancellationToken));
    }
}
