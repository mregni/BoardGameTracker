using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Common;
using BoardGameTracker.Core.Locations.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/location")]
[Authorize]
public class LocationController : ControllerBase
{
    private readonly ILocationService _locationService;

    public LocationController(ILocationService locationService)
    {
        _locationService = locationService;
    }

    [HttpGet]
    [ProducesResponseType<List<LocationDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLocations()
    {
        var locations = await _locationService.GetLocations();
        return Ok(locations);
    }

    [HttpPost]
    [Authorize(Roles = Constants.AuthRoles.UserOrAdmin)]
    [ProducesResponseType<LocationDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateLocation([FromBody] CreateLocationCommand command)
    {
        var location = await _locationService.Create(command);
        return Created((string?)null, location.ToDto());
    }

    [HttpPut]
    [Authorize(Roles = Constants.AuthRoles.UserOrAdmin)]
    [ProducesResponseType<LocationDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateLocation([FromBody] UpdateLocationCommand command)
    {
        var location = await _locationService.Update(command);
        return Ok(location.ToDto());
    }

    [HttpDelete]
    [Route("{id:int}")]
    [Authorize(Roles = Constants.AuthRoles.UserOrAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteLocation(int id)
    {
        var location = await _locationService.GetByIdAsync(id);
        if (location == null)
        {
            return NotFound();
        }

        await _locationService.Delete(id);
        return NoContent();
    }
}
