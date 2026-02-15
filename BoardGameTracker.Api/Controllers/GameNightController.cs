using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Core.GameNights.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/gamenight")]
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
    public async Task<IActionResult> Create([FromBody] CreateGameNightCommand? command)
    {
        if (command == null)
        {
            return BadRequest();
        }

        var gameNight = await _gameNightService.Create(command);
        return Ok(gameNight.ToDto());
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateGameNightCommand? command)
    {
        if (command == null)
        {
            return BadRequest();
        }

        var gameNight = await _gameNightService.Update(command);
        return Ok(gameNight.ToDto());
    }

    [HttpDelete]
    [Route("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _gameNightService.Delete(id);
        return Ok(new { success = true });
    }

    [HttpPut]
    [Route("rsvp")]
    public async Task<IActionResult> UpdateRsvp([FromBody] UpdateRsvpCommand? command)
    {
        if (command == null)
        {
            return BadRequest();
        }

        var rsvp = await _gameNightService.UpdateRsvp(command);
        return Ok(rsvp.ToDto());
    }
}
