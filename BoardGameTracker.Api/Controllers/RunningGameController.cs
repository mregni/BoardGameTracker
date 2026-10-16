using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Core.GameNights.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/gamenight")]
[Authorize(Roles = Constants.AuthRoles.UserOrAdmin)]
public class RunningGameController : ControllerBase
{
    private readonly IRunningGameService _runningGameService;

    public RunningGameController(IRunningGameService runningGameService)
    {
        _runningGameService = runningGameService;
    }

    [HttpPost]
    [Route("{gameNightId:int}/running-games")]
    [ProducesResponseType<RunningGameDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Start(int gameNightId, [FromBody] StartRunningGameCommand command)
    {
        var runningGame = await _runningGameService.Start(gameNightId, command);
        return Created((string?)null, runningGame.ToDto());
    }

    [HttpPost]
    [Route("running-games/{id:int}/finish")]
    [ProducesResponseType<SessionDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Finish(int id, [FromBody] FinishRunningGameCommand command)
    {
        var session = await _runningGameService.Finish(id, command);
        return Ok(session.ToDto());
    }

    [HttpDelete]
    [Route("running-games/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Discard(int id)
    {
        var runningGame = await _runningGameService.GetById(id);
        if (runningGame == null)
        {
            return NotFound();
        }

        await _runningGameService.Delete(id);
        return NoContent();
    }
}
