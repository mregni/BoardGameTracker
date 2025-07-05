using AutoMapper;
using BoardGameTracker.Common.ViewModels;
using BoardGameTracker.Core.Compares.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/compare")]
public class CompareController
{
    private readonly ICompareService _compareService;
    private readonly IMapper _mapper;

    public CompareController(ICompareService compareService, IMapper mapper)
    {
        _compareService = compareService;
        _mapper = mapper;
    }

    [HttpGet]
    [Route("{playerOne:int}/{playerTwo:int}")]
    public async Task<IActionResult> GetPlayerById(int playerOne, int playerTwo)
    {
        var result = await _compareService.GetPlayerComparisation(playerOne, playerTwo);

        var viewModel = _mapper.Map<CompareResultViewModel>(result);
        return new OkObjectResult(viewModel);
    }
}