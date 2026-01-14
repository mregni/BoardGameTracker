using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Core.Badges.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/badge")]
public class BadgeController : ControllerBase
{
    private readonly IBadgeService _badgeService;

    public BadgeController(IBadgeService badgeService)
    {
        _badgeService = badgeService;
    }

    [HttpGet]
    public async Task<IActionResult> GetBadges()
    {
        var badges = await _badgeService.GetAllBadgesAsync();
        return Ok(badges.ToListDto());
    }
}
