using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Core.Rag.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/rag")]
[Authorize(Roles = Constants.AuthRoles.UserOrAdmin)]
[EnableRateLimiting("rag")]
public class RagController : ControllerBase
{
    private readonly IRagService _ragService;
    private readonly IEnvironmentProvider _environmentProvider;

    public RagController(IRagService ragService, IEnvironmentProvider environmentProvider)
    {
        _ragService = ragService;
        _environmentProvider = environmentProvider;
    }

    [HttpPost]
    [Route("game/{gameId:int}/ask")]
    [ProducesResponseType<RagAnswerDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Ask(int gameId, [FromBody] AskRagCommand command, CancellationToken cancellationToken)
    {
        if (!_environmentProvider.RagEnabled)
        {
            return NotFound();
        }

        var answer = await _ragService.AskAsync(gameId, command.Question, command.ManualId, cancellationToken);
        return Ok(answer);
    }
}
