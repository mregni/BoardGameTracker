using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Core.Sessions.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/session")]
public class SessionController : ControllerBase
{
    private readonly ISessionService _sessionService;
    private readonly ILogger<SessionController> _logger;

    public SessionController(ISessionService sessionService, ILogger<SessionController> logger)
    {
        _sessionService = sessionService;
        _logger = logger;
    }

    [HttpGet]
    [Route("{id:int}")]
    public async Task<IActionResult> GetSession(int id)
    {
        var session = await _sessionService.Get(id);
        if (session == null)
        {
            return NotFound();
        }

        return Ok(session.ToDto());
    }

    [HttpPost]
    public async Task<IActionResult> CreateSession([FromBody] CreateSessionCommand? command)
    {
        if (command == null)
        {
            return BadRequest();
        }

        try
        {
            var session = await _sessionService.CreateFromCommand(command);
            return Ok(session.ToDto());
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error creating session");
            return StatusCode(500);
        }
    }

    [HttpPut]
    public async Task<IActionResult> UpdateSession([FromBody] UpdateSessionCommand? command)
    {
        if (command == null || command.Id == 0)
        {
            return BadRequest();
        }

        try
        {
            var result = await _sessionService.UpdateFromCommand(command);
            return Ok(result.ToDto());
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error updating session");
            return StatusCode(500);
        }
    }

    [HttpDelete]
    [Route("{id:int}")]
    public async Task<IActionResult> DeleteSession(int id)
    {
        await _sessionService.Delete(id);
        return Ok(new { success = true });
    }
}
