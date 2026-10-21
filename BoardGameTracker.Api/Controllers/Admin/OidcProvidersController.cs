using BoardGameTracker.Api.Infrastructure;
using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs.Auth;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Core.Auth;
using BoardGameTracker.Core.Auth.Interfaces;
using BoardGameTracker.Core.Email.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BoardGameTracker.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/oidc-providers")]
[Authorize(Roles = Constants.AuthRoles.Admin)]
[ServiceFilter(typeof(AuthDisabledFilter))]
public class OidcProvidersController : ControllerBase
{
    private readonly IOidcProviderService _service;
    private readonly IOidcService _oidcService;
    private readonly IPublicUrlBuilder _publicUrlBuilder;

    public OidcProvidersController(IOidcProviderService service, IOidcService oidcService, IPublicUrlBuilder publicUrlBuilder)
    {
        _service = service;
        _oidcService = oidcService;
        _publicUrlBuilder = publicUrlBuilder;
    }

    [HttpGet("setup")]
    [ProducesResponseType<OidcSetupDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSetup()
    {
        var (baseUrl, configured) = await PublicBaseUrl.ResolveAsync(_publicUrlBuilder, Request);
        return Ok(new OidcSetupDto(
            baseUrl,
            configured,
            OidcService.CallbackUri(baseUrl),
            OidcService.LinkCallbackUri(baseUrl),
            OidcLogoutUrl.PostLogoutRedirectUri(baseUrl),
            !OidcTransportPolicy.IsAllowed(baseUrl)));
    }

    [HttpPost("test-discovery")]
    [ProducesResponseType<OidcDiscoveryResultDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> TestDiscovery([FromBody] TestOidcDiscoveryRequest request)
    {
        return Ok(await _oidcService.TestDiscoveryAsync(request.Authority));
    }

    [HttpGet]
    [ProducesResponseType<IEnumerable<OidcProviderListDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProviders()
    {
        var providers = await _service.GetAllAsync();
        return Ok(providers.Select(p => p.ToListDto()));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<OidcProviderDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProvider(int id)
    {
        var provider = await _service.GetByIdAsync(id);
        return Ok(provider.ToDto());
    }

    [HttpPost]
    [ProducesResponseType<OidcProviderDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateProvider([FromBody] CreateOidcProviderRequest request)
    {
        await EnsureEnableAllowedAsync(request.Enabled);
        var provider = await _service.CreateAsync(
            request.DisplayName, request.Authority, request.ClientId,
            request.ClientSecret, request.Scopes, request.AutoProvisionUsers,
            request.AuthorizationEndpoint, request.TokenEndpoint, request.UserInfoEndpoint,
            request.UsernameClaimType, request.EmailClaimType, request.DisplayNameClaimType,
            request.RolesClaimType, request.AdminGroupValue,
            request.IconUrl, request.ButtonColor, request.DefaultRole, request.Enabled, request.PictureClaimType);

        return CreatedAtAction(nameof(GetProvider), new { id = provider.Id }, provider.ToDto());
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<OidcProviderDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateProvider(int id, [FromBody] UpdateOidcProviderRequest request)
    {
        await EnsureEnableAllowedAsync(request.Enabled);
        var provider = await _service.UpdateAsync(
            id, request.DisplayName, request.Authority, request.ClientId,
            request.ClientSecret, request.Enabled, request.Scopes, request.AutoProvisionUsers,
            request.AuthorizationEndpoint, request.TokenEndpoint, request.UserInfoEndpoint,
            request.UsernameClaimType, request.EmailClaimType, request.DisplayNameClaimType,
            request.RolesClaimType, request.AdminGroupValue,
            request.IconUrl, request.ButtonColor, request.DefaultRole, request.PictureClaimType);

        return Ok(provider.ToDto());
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteProvider(int id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }

    private async Task EnsureEnableAllowedAsync(bool enabled)
    {
        if (enabled && !OidcTransportPolicy.IsAllowed((await PublicBaseUrl.ResolveAsync(_publicUrlBuilder, Request)).BaseUrl))
        {
            throw new ValidationException(Constants.Errors.OidcHttpsRequired);
        }
    }
}
