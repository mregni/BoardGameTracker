using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Core.Updates.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/update")]
public class UpdateController : ControllerBase
{
    private readonly IUpdateService _updateService;

    public UpdateController(IUpdateService updateService)
    {
        _updateService = updateService;
    }

    [HttpPost("check")]
    public async Task<IActionResult> CheckNow()
    {
        await _updateService.CheckForUpdatesAsync();
        var status = await _updateService.GetVersionInfoAsync();
        return Ok(status.ToDto());
    }
}
