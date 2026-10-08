using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Core.Badges.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/badge")]
[Authorize]
public class BadgeController : ControllerBase
{
    private readonly IBadgeService _badgeService;

    public BadgeController(IBadgeService badgeService)
    {
        _badgeService = badgeService;
    }

    [HttpGet]
    [ProducesResponseType<List<BadgeDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBadges()
    {
        var badges = await _badgeService.GetAllBadgesAsync();
        return Ok(badges.ToListDto());
    }
}
