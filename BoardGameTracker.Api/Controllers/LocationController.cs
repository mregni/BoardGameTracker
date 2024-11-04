using AutoMapper;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.ViewModels;
using BoardGameTracker.Common.ViewModels.Location;
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

        return new ObjectResult(mappedGames);
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateLocation([FromBody] CreateLocationViewModel? viewModel)
    {
        if (viewModel == null)
        {
            return new BadRequestResult();
        }
        
        try
        {
            var location = new Location
            {
                Name = viewModel.Name
            };
            location = await _locationService.Create(location);
            
            var result = _mapper.Map<LocationViewModel>(location);
            return new OkObjectResult(result);
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            return StatusCode(500);
        }
    }
    
    [HttpPut]
    public async Task<IActionResult> UpdateLocation([FromBody] LocationViewModel? viewModel)
    {
        if (viewModel is null)
        {
            return new BadRequestResult();
        }

        try
        {
            var location = _mapper.Map<Location>(viewModel);
            await _locationService.Update(location);
            
            return new OkObjectResult(viewModel);
        }
        catch (Exception e)
        {
            return StatusCode(500);
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