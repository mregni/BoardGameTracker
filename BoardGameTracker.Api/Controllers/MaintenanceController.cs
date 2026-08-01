using BoardGameTracker.Api.Infrastructure;
using BoardGameTracker.Common;
using BoardGameTracker.Core.Maintenance.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/maintenance")]
[Authorize(Roles = Constants.AuthRoles.Admin)]
[ServiceFilter(typeof(AuthDisabledFilter))]
public class MaintenanceController : ControllerBase
{
    private readonly IResetService _resetService;

    public MaintenanceController(IResetService resetService)
    {
        _resetService = resetService;
    }

    [HttpPost("reset")]
    public async Task<IActionResult> Reset()
    {
        await _resetService.ResetDataAsync();
        return NoContent();
    }

    [HttpPost("factory-reset")]
    public async Task<IActionResult> FactoryReset()
    {
        await _resetService.FactoryResetAsync();
        return NoContent();
    }
}
