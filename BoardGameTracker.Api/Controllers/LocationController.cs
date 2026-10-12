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

    [HttpGet]
    [Route("{id:int}")]
    [ProducesResponseType<LocationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLocationById(int id)
    {
        var location = await _locationService.GetDetails(id);
        if (location == null)
        {
            return NotFound();
        }

        return Ok(location);
    }

    [HttpGet]
    [Route("{id:int}/statistics")]
    [ProducesResponseType<LocationStatisticsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLocationStatistics(int id)
    {
        if (!await _locationService.ExistsAsync(id))
        {
            return NotFound();
        }

        var statistics = await _locationService.GetStatistics(id);
        return Ok(statistics);
    }

    [HttpGet]
    [Route("{id:int}/sessions")]
    [ProducesResponseType<List<SessionDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLocationSessions(int id)
    {
        var sessions = await _locationService.GetSessions(id);
        return Ok(sessions.ToListDto());
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
