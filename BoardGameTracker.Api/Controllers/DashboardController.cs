using AutoMapper;
using BoardGameTracker.Common.Models.Dashboard;
using BoardGameTracker.Common.ViewModels.Dashboard;
using BoardGameTracker.Common.ViewModels.Results;
using BoardGameTracker.Core.Dashboard.Interfaces;
using BoardGameTracker.Core.Games.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController
{
    private readonly IMapper _mapper;
    private readonly IDashboardService _dashboardService;

    public DashboardController(IMapper mapper, IDashboardService dashboardService)
    {
        _mapper = mapper;
        _dashboardService = dashboardService;
    }
    
    [HttpGet("statistics")]
    public async Task<IActionResult> GetDashboardStatistics()
    {
        var statistics = await _dashboardService.GetStatistics();
        return ResultViewModel<DashbardStatistics>.CreateFoundResult(statistics);
    }
    
    [HttpGet("charts")]
    public async Task<IActionResult> GetDashboardCharts()
    {
        var charts = await _dashboardService.GetCharts();

        var mappedResult = _mapper.Map<DashboardChartsViewModel>(charts);
        return ResultViewModel<DashboardChartsViewModel>.CreateFoundResult(mappedResult);
    }
}