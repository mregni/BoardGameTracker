using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Integrations.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/settings/integrations")]
[Authorize(Roles = Constants.AuthRoles.Admin)]
[EnableRateLimiting("integrations")]
public class IntegrationsController : ControllerBase
{
    private readonly IIntegrationStatusService _integrationStatusService;

    public IntegrationsController(IIntegrationStatusService integrationStatusService)
    {
        _integrationStatusService = integrationStatusService;
    }

    [HttpGet("status")]
    [ProducesResponseType<List<IntegrationStatusDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
    {
        var statuses = await _integrationStatusService.GetAllAsync(cancellationToken);
        return Ok(statuses);
    }

    [HttpPost("{integration}/test")]
    [ProducesResponseType<IntegrationStatusDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Test(string integration, CancellationToken cancellationToken)
    {
        IntegrationKind? kind = integration.ToLowerInvariant() switch
        {
            "bgg" => IntegrationKind.Bgg,
            "changedetection" => IntegrationKind.ChangeDetection,
            _ => null
        };

        if (kind == null)
        {
            return NotFound();
        }

        var status = await _integrationStatusService.TestAsync(kind.Value, cancellationToken);
        return Ok(status);
    }
}
