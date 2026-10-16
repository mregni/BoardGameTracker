using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.GameNights.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/gamenight/statistics")]
[Authorize]
public class GameNightStatisticsController : ControllerBase
{
    private readonly IGameNightStatisticsService _statisticsService;

    public GameNightStatisticsController(IGameNightStatisticsService statisticsService)
    {
        _statisticsService = statisticsService;
    }

    [HttpGet]
    [ProducesResponseType<GameNightStatisticsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStatistics([FromQuery] GameNightStatisticsPeriod period = GameNightStatisticsPeriod.AllTime, CancellationToken cancellationToken = default)
    {
        var statistics = await _statisticsService.GetStatistics(period, cancellationToken);
        return Ok(statistics);
    }
}
