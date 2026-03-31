using BoardGameTracker.Api.Infrastructure;
using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs.Auth;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Core.Auth.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BoardGameTracker.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/oidc-providers")]
[Authorize(Roles = Constants.AuthRoles.Admin)]
[ServiceFilter(typeof(AuthDisabledFilter))]
public class OidcProvidersController : ControllerBase
{
    private readonly IOidcProviderService _service;

    public OidcProvidersController(IOidcProviderService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetProviders()
    {
        var providers = await _service.GetAllAsync();
        return Ok(providers.Select(p => p.ToListDto()));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetProvider(int id)
    {
        var provider = await _service.GetByIdAsync(id);
        return Ok(provider.ToDto());
    }

    [HttpPost]
    public async Task<IActionResult> CreateProvider([FromBody] CreateOidcProviderRequest request)
    {
        var provider = await _service.CreateAsync(
            request.Name, request.DisplayName, request.Authority, request.ClientId,
            request.ClientSecret, request.Scopes, request.AutoProvisionUsers,
            request.AuthorizationEndpoint, request.TokenEndpoint, request.UserInfoEndpoint,
            request.UsernameClaimType, request.EmailClaimType, request.DisplayNameClaimType,
            request.RolesClaimType, request.AdminGroupValue,
            request.IconUrl, request.ButtonColor);

        return Ok(provider.ToDto());
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateProvider(int id, [FromBody] UpdateOidcProviderRequest request)
    {
        var provider = await _service.UpdateAsync(
            id, request.DisplayName, request.Authority, request.ClientId,
            request.ClientSecret, request.Enabled, request.Scopes, request.AutoProvisionUsers,
            request.AuthorizationEndpoint, request.TokenEndpoint, request.UserInfoEndpoint,
            request.UsernameClaimType, request.EmailClaimType, request.DisplayNameClaimType,
            request.RolesClaimType, request.AdminGroupValue,
            request.IconUrl, request.ButtonColor);

        return Ok(provider.ToDto());
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteProvider(int id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}
