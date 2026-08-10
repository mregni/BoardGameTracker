using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Core.Rag.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/rag")]
[Authorize]
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
