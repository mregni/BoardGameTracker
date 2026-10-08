using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Core.Dashboard.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("statistics")]
    [ProducesResponseType<DashboardStatisticsDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboardStatistics(CancellationToken cancellationToken)
    {
        var statistics = await _dashboardService.GetStatistics(cancellationToken);
        return Ok(statistics);
    }
}
