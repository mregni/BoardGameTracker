using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Common;
using BoardGameTracker.Core.Sessions.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/session")]
[Authorize]
public class SessionController : ControllerBase
{
    private readonly ISessionService _sessionService;

    public SessionController(ISessionService sessionService)
    {
        _sessionService = sessionService;
    }

    [HttpGet]
    [Route("{id:int}")]
    [ProducesResponseType<SessionDto>(StatusCodes.Status200OK)]
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
    [Authorize(Roles = Constants.AuthRoles.UserOrAdmin)]
    [ProducesResponseType<SessionDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateSession([FromBody] CreateSessionCommand command)
    {
        var session = await _sessionService.CreateFromCommand(command);
        return CreatedAtAction(nameof(GetSession), new { id = session.Id }, session.ToDto());
    }

    [HttpPut]
    [Authorize(Roles = Constants.AuthRoles.UserOrAdmin)]
    [ProducesResponseType<SessionDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateSession([FromBody] UpdateSessionCommand command)
    {
        var result = await _sessionService.UpdateFromCommand(command);
        return Ok(result.ToDto());
    }

    [HttpDelete]
    [Route("{id:int}")]
    [Authorize(Roles = Constants.AuthRoles.UserOrAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteSession(int id)
    {
        var session = await _sessionService.Get(id);
        if (session == null)
        {
            return NotFound();
        }

        await _sessionService.Delete(id);
        return NoContent();
    }
}
