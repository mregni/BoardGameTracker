using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Core.Sessions.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/session/suggestions")]
[Authorize]
[Tags("Session")]
public class SessionSuggestionsController : ControllerBase
{
    private readonly ISessionSuggestionService _sessionSuggestionService;

    public SessionSuggestionsController(ISessionSuggestionService sessionSuggestionService)
    {
        _sessionSuggestionService = sessionSuggestionService;
    }

    [HttpGet]
    [ProducesResponseType<SessionSuggestionsDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSuggestions(CancellationToken cancellationToken)
    {
        var suggestions = await _sessionSuggestionService.GetSuggestions(cancellationToken);
        return Ok(suggestions);
    }
}
