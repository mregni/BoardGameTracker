using AutoMapper;
using BoardGameTracker.Common.ViewModels;
using BoardGameTracker.Core.Locations.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/location")]
public class LocationController
{
    private readonly ILocationService _locationService;
    private readonly IMapper _mapper;

    public LocationController(ILocationService locationService, IMapper mapper)
    {
        _locationService = locationService;
        _mapper = mapper;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetLocations()
    {
        var games = await _locationService.GetLocations();
        var mappedGames = _mapper.Map<IList<LocationViewModel>>(games);

        var resultViewModel = new ListResultViewModel<LocationViewModel>(mappedGames);
        return new OkObjectResult(resultViewModel);
    }
}