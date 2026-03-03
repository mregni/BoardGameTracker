using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Core.Updates.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/update")]
[Authorize]
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
