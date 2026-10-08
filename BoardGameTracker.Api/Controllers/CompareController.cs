using BoardGameTracker.Common.Models;
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
    public async Task<IActionResult> GetPlayerComparison(int playerOneId, int playerTwoId)
    {
        var result = await _compareService.GetPlayerComparison(playerOneId, playerTwoId);
        return Ok(result);
    }
}
