using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Core.Storage.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/storage")]
[Authorize]
public class StorageController : ControllerBase
{
    private readonly IStorageService _storageService;

    public StorageController(IStorageService storageService)
    {
        _storageService = storageService;
    }

    [HttpGet]
    [ProducesResponseType<List<StorageLocationDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStorageLocations()
    {
        var storageLocations = await _storageService.GetAll();
        return Ok(storageLocations.ToListDto());
    }

    [HttpPost]
    [Authorize(Roles = Constants.AuthRoles.UserOrAdmin)]
    [ProducesResponseType<StorageLocationDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateStorageLocation([FromBody] CreateStorageLocationCommand command)
    {
        var storageLocation = await _storageService.Create(command);
        return Created((string?)null, storageLocation.ToDto());
    }

    [HttpPut]
    [Route("{id:int}")]
    [Authorize(Roles = Constants.AuthRoles.UserOrAdmin)]
    [ProducesResponseType<StorageLocationDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateStorageLocation(int id, [FromBody] UpdateStorageLocationCommand command)
    {
        var storageLocation = await _storageService.Update(id, command);
        return Ok(storageLocation.ToDto());
    }

    [HttpPut]
    [Route("{id:int}/move")]
    [Authorize(Roles = Constants.AuthRoles.UserOrAdmin)]
    [ProducesResponseType<StorageLocationDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> MoveStorageLocation(int id, [FromBody] MoveStorageLocationCommand command)
    {
        var storageLocation = await _storageService.Move(id, command);
        return Ok(storageLocation.ToDto());
    }

    [HttpDelete]
    [Route("{id:int}")]
    [Authorize(Roles = Constants.AuthRoles.UserOrAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteStorageLocation(int id)
    {
        await _storageService.Delete(id);
        return NoContent();
    }

    [HttpPut]
    [Route("assign")]
    [Authorize(Roles = Constants.AuthRoles.UserOrAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Assign([FromBody] AssignStorageCommand command)
    {
        await _storageService.Assign(command);
        return NoContent();
    }

    [HttpPut]
    [Route("arrange")]
    [Authorize(Roles = Constants.AuthRoles.UserOrAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Arrange([FromBody] ArrangeStorageCommand command)
    {
        await _storageService.Arrange(command);
        return NoContent();
    }

    [HttpPut]
    [Route("orientation")]
    [Authorize(Roles = Constants.AuthRoles.UserOrAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateBoxOrientation([FromBody] UpdateBoxOrientationCommand command)
    {
        await _storageService.UpdateBoxOrientation(command);
        return NoContent();
    }
}
