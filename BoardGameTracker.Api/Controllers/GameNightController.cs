using Ardalis.GuardClauses;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Common;
using BoardGameTracker.Core.GameNights.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

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
    [ProducesResponseType<List<GameNightDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGameNights()
    {
        var gameNights = await _gameNightService.GetGameNights();
        return Ok(gameNights.ToListDto());
    }

    [HttpGet]
    [Route("{id:int}")]
    [ProducesResponseType<GameNightDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGameNight(int id)
    {
        var gameNight = await _gameNightService.GetDetail(id);
        if (gameNight == null)
        {
            return NotFound();
        }

        return Ok(gameNight.ToDetailDto());
    }

    [HttpPost]
    [Route("{id:int}/end")]
    [Authorize(Roles = Constants.AuthRoles.UserOrAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> End(int id)
    {
        var gameNight = await _gameNightService.End(id);
        return gameNight == null ? NotFound() : NoContent();
    }

    [HttpPost]
    [Route("{id:int}/reopen")]
    [Authorize(Roles = Constants.AuthRoles.UserOrAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reopen(int id)
    {
        var gameNight = await _gameNightService.Reopen(id);
        return gameNight == null ? NotFound() : NoContent();
    }

    [HttpPut]
    [Route("{id:int}/attendance")]
    [Authorize(Roles = Constants.AuthRoles.UserOrAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAttendance(int id, [FromBody] UpdateAttendanceCommand command)
    {
        var gameNight = await _gameNightService.UpdateAttendance(id, command);
        return gameNight == null ? NotFound() : NoContent();
    }

    [HttpPost]
    [Authorize(Roles = Constants.AuthRoles.UserOrAdmin)]
    [ProducesResponseType<GameNightDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateGameNightCommand command)
    {
        var gameNight = await _gameNightService.Create(command);
        return Created((string?)null, gameNight.ToDto());
    }

    [HttpPut]
    [Authorize(Roles = Constants.AuthRoles.UserOrAdmin)]
    [ProducesResponseType<GameNightDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Update([FromBody] UpdateGameNightCommand command)
    {
        var gameNight = await _gameNightService.Update(command);
        return Ok(gameNight.ToDto());
    }

    [HttpPut]
    [Route("{id:int}/suggestions/{gameId:int}")]
    [Authorize(Roles = Constants.AuthRoles.UserOrAdmin)]
    [ProducesResponseType<GameNightDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddSuggestedGame(int id, int gameId)
    {
        var gameNight = await _gameNightService.AddSuggestedGame(new AddSuggestedGameCommand { GameNightId = id, GameId = gameId });
        return Ok(gameNight.ToDto());
    }

    [HttpDelete]
    [Route("{id:int}/suggestions/{gameId:int}")]
    [Authorize(Roles = Constants.AuthRoles.UserOrAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveSuggestedGame(int id, int gameId)
    {
        await _gameNightService.RemoveSuggestedGame(new RemoveSuggestedGameCommand { GameNightId = id, GameId = gameId });
        return NoContent();
    }

    [HttpPost]
    [Route("{id:int}/send-invites")]
    [Authorize(Roles = Constants.AuthRoles.UserOrAdmin)]
    [ProducesResponseType<SendInvitesResultDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> SendInvites(int id, CancellationToken cancellationToken)
    {
        var result = await _gameNightService.SendInvitesAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpDelete]
    [Route("{id:int}")]
    [Authorize(Roles = Constants.AuthRoles.UserOrAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
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
    [Authorize(Roles = Constants.AuthRoles.UserOrAdmin)]
    [ProducesResponseType<GameNightRsvpDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateRsvp([FromBody] UpdateRsvpCommand command)
    {
        GuardRsvpLookup(command);

        var rsvp = await _gameNightService.UpdateRsvp(command);
        return Ok(rsvp.ToDto());
    }

    [HttpPut]
    [Route("link/{linkId:guid}/rsvp")]
    [AllowAnonymous]
    [EnableRateLimiting("gamenight-link")]
    [ProducesResponseType<GameNightRsvpUpdateDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateRsvpByLink(Guid linkId, [FromBody] UpdateRsvpCommand command)
    {
        GuardRsvpLookup(command);

        var result = await _gameNightService.UpdateRsvpByLink(linkId, command, User?.Identity?.IsAuthenticated == true);
        return Ok(result.Rsvp.ToUpdateDto(result.HostNotified));
    }

    private static void GuardRsvpLookup(UpdateRsvpCommand command)
    {
        if (command.Id == null)
        {
            Guard.Against.Null(command.GameNightId);
            Guard.Against.Null(command.PlayerId);
        }
    }

    [HttpGet]
    [Route("link/{linkId:guid}")]
    [AllowAnonymous]
    [EnableRateLimiting("gamenight-link")]
    [ProducesResponseType<GameNightDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByLink(Guid linkId)
    {
        var rsvp = await _gameNightService.GetByLinkId(linkId, User?.Identity?.IsAuthenticated == true);
        if (rsvp == null)
        {
            return NotFound();
        }

        return Ok(rsvp.ToPublicDto());
    }
}
