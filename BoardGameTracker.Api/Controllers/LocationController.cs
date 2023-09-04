using AutoMapper;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.ViewModels;
using BoardGameTracker.Core.Locations.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/location")]
public class LocationController
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

        var resultViewModel = new ListResultViewModel<LocationViewModel>(mappedGames);
        return new OkObjectResult(resultViewModel);
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateLocation([FromBody] LocationViewModel? viewModel)
    {
        if (viewModel == null)
        {
            var failedViewModel = new CreationResultViewModel<LocationViewModel>(CreationResultType.Failed, null, "No data provided");
            return new OkObjectResult(failedViewModel);
        }

        var location = _mapper.Map<Location>(viewModel);
        try
        {
            await _locationService.Create(location);
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            var failedViewModel = new CreationResultViewModel<LocationViewModel>(CreationResultType.Failed, null, "Creation failed because of backend error, check logs for details");
            return new OkObjectResult(failedViewModel);
        }
        
        var resultViewModel = new CreationResultViewModel<LocationViewModel>(CreationResultType.Success, null);
        return new OkObjectResult(resultViewModel);
    }
    
    [HttpPut]
    public async Task<IActionResult> UpdateLocation([FromBody] LocationViewModel? viewModel)
    {
        if (viewModel is not {Id: { }})
        {
            var failedViewModel = new CreationResultViewModel<LocationViewModel>(CreationResultType.Failed, null, "No data provided");
            return new OkObjectResult(failedViewModel);
        }

        var location = _mapper.Map<Location>(viewModel);
        try
        {
            await _locationService.Update(location);
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            var failedViewModel = new CreationResultViewModel<LocationViewModel>(CreationResultType.Failed, null, "Update failed because of backend error, check logs for details");
            return new OkObjectResult(failedViewModel);
        }
        
        var resultViewModel = new CreationResultViewModel<LocationViewModel>(CreationResultType.Success, null);
        return new OkObjectResult(resultViewModel);
    }
    
    [HttpDelete]
    [Route("{id:int}")]
    public async Task<IActionResult> DeleteLocation(int id)
    {
        await _locationService.Delete(id);
        return new OkObjectResult(new CreationResultViewModel<string>(CreationResultType.Success, null));
    }
}