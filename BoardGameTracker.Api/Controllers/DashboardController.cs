using BoardGameTracker.Core.Dashboard.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetDashboardStatistics()
    {
        var statistics = await _dashboardService.GetStatistics();
        return new OkObjectResult(statistics);
    }
}
