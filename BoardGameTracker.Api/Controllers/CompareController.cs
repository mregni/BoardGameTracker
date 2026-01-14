using BoardGameTracker.Core.Compares.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/compare")]
public class CompareController : ControllerBase
{
    private readonly ICompareService _compareService;

    public CompareController(ICompareService compareService)
    {
        _compareService = compareService;
    }

    [HttpGet]
    [Route("{playerOne:int}/{playerTwo:int}")]
    public async Task<IActionResult> GetPlayerComparison(int playerOne, int playerTwo)
    {
        var result = await _compareService.GetPlayerComparisation(playerOne, playerTwo);
        return Ok(result);
    }
}
