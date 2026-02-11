using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Locations.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/location")]
public class LocationController : ControllerBase
{
    private readonly ILocationService _locationService;

    public LocationController(ILocationService locationService)
    {
        _locationService = locationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetLocations()
    {
        var locations = await _locationService.GetLocations();
        return Ok(locations.ToListDto());
    }

    [HttpPost]
    public async Task<IActionResult> CreateLocation([FromBody] CreateLocationCommand? command)
    {
        if (command == null)
        {
            return BadRequest();
        }

        var location = new Location(command.Name);
        location = await _locationService.Create(location);
        return Ok(location.ToDto());
    }

    [HttpPut]
    public async Task<IActionResult> UpdateLocation([FromBody] UpdateLocationCommand? command)
    {
        if (command is null)
        {
            return BadRequest();
        }

        var location = new Location(command.Name) { Id = command.Id };
        await _locationService.Update(location);
        return Ok(location.ToDto());
    }

    [HttpDelete]
    [Route("{id:int}")]
    public async Task<IActionResult> DeleteLocation(int id)
    {
        await _locationService.Delete(id);
        return Ok(new { success = true });
    }
}
