using Ardalis.GuardClauses;
using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Core.GameNights.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/gamenight")]
[Authorize]
public class GameNightController : ControllerBase
{
    private readonly IGameNightService _gameNightService;

    public GameNightController(IGameNightService gameNightService)
    {
        _gameNightService = gameNightService;
    }

    [HttpGet]
    public async Task<IActionResult> GetGameNights([FromQuery] bool past = false)
    {
        var gameNights = await _gameNightService.GetGameNights(past);
        return Ok(gameNights.ToListDto());
    }

    [HttpPost]
    [Authorize(Roles = Constants.AuthRoles.UserOrAdmin)]
    public async Task<IActionResult> Create([FromBody] CreateGameNightCommand command)
    {
        var gameNight = await _gameNightService.Create(command);
        return Ok(gameNight.ToDto());
    }

    [HttpPut]
    [Authorize(Roles = Constants.AuthRoles.UserOrAdmin)]
    public async Task<IActionResult> Update([FromBody] UpdateGameNightCommand command)
    {
        var gameNight = await _gameNightService.Update(command);
        return Ok(gameNight.ToDto());
    }

    [HttpDelete]
    [Route("{id:int}")]
    [Authorize(Roles = Constants.AuthRoles.UserOrAdmin)]
    public async Task<IActionResult> Delete(int id)
    {
        var gameNight = await _gameNightService.GetById(id);
        if (gameNight == null)
        {
            return NotFound();
        }

        await _gameNightService.Delete(id);
        return NoContent();
    }

    [HttpPut]
    [Route("rsvp")]
    [AllowAnonymous]
    public async Task<IActionResult> UpdateRsvp([FromBody] UpdateRsvpCommand command)
    {
        if (command.Id == null)
        {
            Guard.Against.Null(command.GameNightId);
            Guard.Against.Null(command.PlayerId);
        }
        
        var rsvp = await _gameNightService.UpdateRsvp(command);
        return Ok(rsvp.ToDto());
    }

    [HttpGet]
    [Route("link/{linkId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByLink(Guid linkId)
    {
        var rsvp = await _gameNightService.GetByLinkId(linkId);
        if (rsvp == null)
        {
            return NotFound();
        }

        return Ok(rsvp.ToDto());
    }
}
