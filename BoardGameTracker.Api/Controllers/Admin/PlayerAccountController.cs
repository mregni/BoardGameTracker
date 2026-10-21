using BoardGameTracker.Api.Infrastructure;
using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.DTOs.Auth;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Core.Auth.Interfaces;
using BoardGameTracker.Core.Players.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BoardGameTracker.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/players")]
[Authorize(Roles = Constants.AuthRoles.Admin)]
[ServiceFilter(typeof(AuthDisabledFilter))]
public class PlayerAccountController : ControllerBase
{
    private readonly IUserAdminService _userAdminService;
    private readonly IPlayerService _playerService;

    public PlayerAccountController(IUserAdminService userAdminService, IPlayerService playerService)
    {
        _userAdminService = userAdminService;
        _playerService = playerService;
    }

    [HttpPut("{id:int}/account")]
    [ProducesResponseType<PlayerDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LinkAccount(int id, [FromBody] LinkPlayerAccountRequest request)
    {
        await _userAdminService.LinkPlayerAccountAsync(id, request.UserId);
        var player = await _playerService.Get(id) ?? throw new EntityNotFoundException(nameof(Player), id);
        return Ok(player.ToDto(await _playerService.GetLinkedAccountAsync(id)));
    }
}
