using AutoMapper;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.ViewModels;
using BoardGameTracker.Common.ViewModels.Results;
using BoardGameTracker.Core.Locations.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/location")]
public class LocationController : ControllerBase
{
    private readonly ILocationService _locationService;
    private readonly IMapper _mapper;
    private readonly ILogger<LocationController> _logger;

    public LocationController(ILocationService locationService, IMapper mapper, ILogger<LocationController> logger)
    {
        _locationService = locationService;
        _mapper = mapper;
        _logger = logger;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetLocations()
    {
        var games = await _locationService.GetLocations();
        var mappedGames = _mapper.Map<IList<LocationViewModel>>(games);

        return ListResultViewModel<LocationViewModel>.CreateResult(mappedGames);
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateLocation([FromBody] LocationViewModel? viewModel)
    {
        if (viewModel == null)
        {
            //TODO: move to translation file
            var failedViewModel = new FailResultViewModel("No data provided");
            return new BadRequestObjectResult(failedViewModel);
        }
        
        try
        {
            var location = _mapper.Map<Location>(viewModel);
            location = await _locationService.Create(location);
            
            var result = _mapper.Map<LocationViewModel>(location);
            return ResultViewModel<LocationViewModel>.CreateCreatedResult(result);
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            //TODO: move to translation file
            var failViewModel = new FailResultViewModel("Creation failed because of backend error, check logs for details");
            return StatusCode(500, failViewModel);
        }
    }
    
    [HttpPut]
    public async Task<IActionResult> UpdateLocation([FromBody] LocationViewModel? viewModel)
    {
        if (viewModel is null)
        {
            //TODO: move to translation file
            var failedViewModel = new FailResultViewModel("No data provided");
            return new OkObjectResult(failedViewModel);
        }

        try
        {
            var location = _mapper.Map<Location>(viewModel);
            await _locationService.Update(location);
            
            return ResultViewModel<LocationViewModel>.CreateUpdatedResult(viewModel);
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            //TODO: move to translation file
            var failViewModel = new FailResultViewModel("Update failed because of backend error, check logs for details");
            return StatusCode(500, failViewModel);
        }
    }
    
    [HttpDelete]
    [Route("{id:int}")]
    public async Task<IActionResult> DeleteLocation(int id)
    {
        await _locationService.Delete(id);
        return new OkObjectResult(new DeletionResultViewModel(ResultState.Success));
    }
}