using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Core.Sessions.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/session")]
public class SessionController : ControllerBase
{
    private readonly ISessionService _sessionService;

    public SessionController(ISessionService sessionService)
    {
        _sessionService = sessionService;
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

        return Ok(SessionDtoExtensions.ToDto(session));
    }

    [HttpPost]
    public async Task<IActionResult> CreateSession([FromBody] CreateSessionCommand? command)
    {
        if (command == null)
        {
            return BadRequest();
        }

        var session = await _sessionService.CreateFromCommand(command);
        return Ok(SessionDtoExtensions.ToDto(session));
    }

    [HttpPut]
    public async Task<IActionResult> UpdateSession([FromBody] UpdateSessionCommand? command)
    {
        if (command == null || command.Id == 0)
        {
            return BadRequest();
        }

        var result = await _sessionService.UpdateFromCommand(command);
        return Ok(SessionDtoExtensions.ToDto(result));
    }

    [HttpDelete]
    [Route("{id:int}")]
    public async Task<IActionResult> DeleteSession(int id)
    {
        await _sessionService.Delete(id);
        return Ok(new { success = true });
    }
}
