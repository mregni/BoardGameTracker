using BoardGameTracker.Common;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Common.Models.Compare;
using BoardGameTracker.Core.Compares.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/compare")]
[Authorize]
public class CompareController : ControllerBase
{
    private readonly ICompareService _compareService;

    public CompareController(ICompareService compareService)
    {
        _compareService = compareService;
    }

    [HttpGet]
    [Route("{playerOneId:int}/{playerTwoId:int}")]
    [ProducesResponseType<CompareResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPlayerComparison(int playerOneId, int playerTwoId)
    {
        if (playerOneId == playerTwoId)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = Constants.Errors.CompareSamePlayer
            });
        }

        var result = await _compareService.GetPlayerComparison(playerOneId, playerTwoId);
        return Ok(result);
    }

    [HttpGet("default-pair")]
    [ProducesResponseType<ComparePairDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> GetDefaultPair()
    {
        var pair = await _compareService.GetDefaultPair();
        return pair == null ? NoContent() : Ok(pair);
    }
}
