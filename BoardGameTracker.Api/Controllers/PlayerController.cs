using System.ComponentModel.DataAnnotations;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Common;
using BoardGameTracker.Core.Players.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/player")]
[Authorize]
public class PlayerController : ControllerBase
{
    private readonly IPlayerService _playerService;

    public PlayerController(IPlayerService playerService)
    {
        _playerService = playerService;
    }

    [HttpGet]
    [ProducesResponseType<List<PlayerDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPlayers()
    {
        var players = await _playerService.GetList();
        return Ok(players.ToListDto());
    }

    [HttpPost]
    [Authorize(Roles = Constants.AuthRoles.UserOrAdmin)]
    [ProducesResponseType<PlayerDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreatePlayer([FromBody] CreatePlayerCommand command)
    {
        var player = await _playerService.Create(command);
        return CreatedAtAction(nameof(GetPlayerById), new { id = player.Id }, player.ToDto());
    }

    [HttpPut]
    [Authorize(Roles = Constants.AuthRoles.UserOrAdmin)]
    [ProducesResponseType<PlayerDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdatePlayer([FromBody] UpdatePlayerCommand command)
    {
        var player = await _playerService.Update(command);
        return Ok(player.ToDto());
    }

    [HttpGet]
    [Route("{id:int}")]
    [ProducesResponseType<PlayerDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPlayerById(int id)
    {
        var player = await _playerService.Get(id);
        if (player == null)
        {
            return NotFound();
        }

        return Ok(player.ToDto());
    }

    [HttpDelete]
    [Route("{id:int}")]
    [Authorize(Roles = Constants.AuthRoles.UserOrAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeletePlayerById(int id)
    {
        await _playerService.Delete(id);
        return NoContent();
    }

    [HttpGet]
    [Route("{id:int}/statistics")]
    [ProducesResponseType<PlayerStatistics>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPlayerStats(int id)
    {
        var stats = await _playerService.GetStats(id);
        return Ok(stats);
    }

    [HttpGet]
    [Route("{id:int}/sessions")]
    [ProducesResponseType<List<SessionDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPlayerSessionsById(int id, [FromQuery, Range(1, int.MaxValue)] int? count)
    {
        var sessions = await _playerService.GetSessions(id, count);
        return Ok(sessions.ToListDto());
    }
}
