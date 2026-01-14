using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Locations.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/location")]
public class LocationController : ControllerBase
{
    private readonly ILocationService _locationService;
    private readonly ILogger<LocationController> _logger;

    public LocationController(ILocationService locationService, ILogger<LocationController> logger)
    {
        _locationService = locationService;
        _logger = logger;
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

        try
        {
            var location = new Location(command.Name);
            location = await _locationService.Create(location);
            return Ok(location.ToDto());
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error creating location");
            return StatusCode(500, new { error = "An unexpected error occurred. Please try again later." });
        }
    }

    [HttpPut]
    public async Task<IActionResult> UpdateLocation([FromBody] UpdateLocationCommand? command)
    {
        if (command is null)
        {
            return BadRequest();
        }

        try
        {
            var location = new Location(command.Name) { Id = command.Id };
            await _locationService.Update(location);
            return Ok(location.ToDto());
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error updating location");
            return StatusCode(500);
        }
    }

    [HttpDelete]
    [Route("{id:int}")]
    public async Task<IActionResult> DeleteLocation(int id)
    {
        await _locationService.Delete(id);
        return Ok(new { success = true });
    }
}
