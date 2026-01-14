using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Players.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/player")]
public class PlayerController : ControllerBase
{
    private readonly IPlayerService _playerService;
    private readonly ILogger<PlayerController> _logger;

    public PlayerController(IPlayerService playerService, ILogger<PlayerController> logger)
    {
        _playerService = playerService;
        _logger = logger;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetPlayers()
    {
        var players = await _playerService.GetList();
        return Ok(players.ToListDto());
    }

    [HttpPost]
    public async Task<IActionResult> CreatePlayer([FromBody] CreatePlayerCommand? command)
    {
        if (command == null)
        {
            return BadRequest();
        }

        try
        {
            var player = new Player(command.Name, command.Image);
            player = await _playerService.Create(player);
            return Ok(player.ToDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in CreatePlayer");
            return StatusCode(500, new { error = "An unexpected error occurred. Please try again later." });
        }
    }

    [HttpPut]
    public async Task<IActionResult> UpdatePlayer([FromBody] UpdatePlayerCommand? command)
    {
        if (command is null)
        {
            return BadRequest();
        }

        try
        {
            var player = await _playerService.Update(command);
            if (player == null)
            {
                return BadRequest();
            }
            
            return Ok(player.ToDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in UpdatePlayer");
            return StatusCode(500, new { error = "An unexpected error occurred. Please try again later." });
        }
    }

    [HttpGet]
    [Route("{id:int}")]
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
    public async Task<IActionResult> DeleteGameById(int id)
    {
        await _playerService.Delete(id);
        return Ok(new { success = true });
    }

    [HttpGet]
    [Route("{id:int}/statistics")]
    public async Task<IActionResult> GetGameStats(int id)
    {
        var stats = await _playerService.GetStats(id);
        return Ok(stats);
    }

    [HttpGet]
    [Route("{id:int}/sessions")]
    public async Task<IActionResult> GetGameSessionsById(int id, [FromQuery] int? count)
    {
        var sessions = await _playerService.GetSessions(id, count);
        return Ok(sessions.ToListDto());
    }
}