using AutoMapper;
using BoardGameTracker.Common.Models.Dashboard;
using BoardGameTracker.Common.ViewModels.Dashboard;
using BoardGameTracker.Common.ViewModels.Results;
using BoardGameTracker.Core.Dashboard.Interfaces;
using BoardGameTracker.Core.Games.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BoardGameTracker.Api.Controllers;


/*
 * Last played game
 * Most played game
 * Longest game session (incl game name)
 * total games sold (only display IF NOT NULL)
 * Highest rated game
 * Most commen game categery played
 * Most common game mechanic played
 * Most played location
 * Player with most sessions
 *
 * PIE
 * Games by category
 * Most played games
 * 
 * BAR
 * Games by year published (for last 20 years for example)
 */
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
        return new OkObjectResult(statistics);
    }
    
    [HttpGet("charts")]
    public async Task<IActionResult> GetDashboardCharts()
    {
        var charts = await _dashboardService.GetCharts();

        var mappedResult = _mapper.Map<DashboardChartsViewModel>(charts);
        return new OkObjectResult(mappedResult);
    }
}