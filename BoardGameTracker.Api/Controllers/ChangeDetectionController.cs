using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Models.ChangeDetection;
using BoardGameTracker.Core.ChangeDetection.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/changedetection")]
[Authorize]
[EnableRateLimiting("changedetection")]
public class ChangeDetectionController : ControllerBase
{
    private readonly IChangeDetectionClient _changeDetectionClient;

    public ChangeDetectionController(IChangeDetectionClient changeDetectionClient)
    {
        _changeDetectionClient = changeDetectionClient;
    }

    [HttpGet("test")]
    [Authorize(Roles = Constants.AuthRoles.Admin)]
    public async Task<IActionResult> TestConnection(CancellationToken cancellationToken)
    {
        var (ok, version) = await _changeDetectionClient.TestConnectionAsync(cancellationToken);
        return Ok(new ChangeDetectionConnectionTestDto { Ok = ok, Version = version });
    }

    [HttpGet("watch/{watchId}")]
    public async Task<IActionResult> GetWatch(string watchId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(watchId, out _))
        {
            return BadRequest();
        }

        var (status, info) = await _changeDetectionClient.GetWatchInfoAsync(watchId, cancellationToken);
        return status switch
        {
            ChangeDetectionStatus.Ok when info != null => Ok(info),
            ChangeDetectionStatus.WatchNotFound => NotFound(),
            ChangeDetectionStatus.NotConfigured or ChangeDetectionStatus.Misconfigured =>
                StatusCode(StatusCodes.Status503ServiceUnavailable),
            _ => StatusCode(StatusCodes.Status502BadGateway)
        };
    }
}
