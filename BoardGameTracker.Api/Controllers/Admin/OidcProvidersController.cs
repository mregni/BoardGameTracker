using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs.Auth;
using BoardGameTracker.Common.Entities.Auth;
using BoardGameTracker.Core.Datastore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BoardGameTracker.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/oidc-providers")]
[Authorize(Roles = Constants.AuthRoles.Admin)]
public class OidcProvidersController : ControllerBase
{
    private readonly MainDbContext _context;

    public OidcProvidersController(MainDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetProviders()
    {
        var providers = await _context.OidcProviders
            .Select(p => new OidcProviderDto(
                p.Id, p.Name, p.DisplayName, p.Enabled,
                p.Authority, p.ClientId, p.Scopes,
                p.AutoProvisionUsers, p.IconUrl, p.ButtonColor))
            .ToListAsync();

        return Ok(providers);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetProvider(int id)
    {
        var provider = await _context.OidcProviders.FindAsync(id);
        if (provider == null)
        {
            return NotFound();
        }

        return Ok(new OidcProviderDto(
            provider.Id, provider.Name, provider.DisplayName, provider.Enabled,
            provider.Authority, provider.ClientId, provider.Scopes,
            provider.AutoProvisionUsers, provider.IconUrl, provider.ButtonColor));
    }

    [HttpPost]
    public async Task<IActionResult> CreateProvider([FromBody] CreateOidcProviderRequest request)
    {
        var existing = await _context.OidcProviders.FirstOrDefaultAsync(p => p.Name == request.Name);
        if (existing != null)
        {
            return BadRequest(new { message = "Provider with this name already exists" });
        }

        var provider = new OidcProvider(request.Name, request.DisplayName, request.Authority, request.ClientId);
        provider.Update(
            request.DisplayName, request.Authority, request.ClientId, request.ClientSecret,
            true, request.Scopes, request.AutoProvisionUsers,
            request.AuthorizationEndpoint, request.TokenEndpoint, request.UserInfoEndpoint,
            request.UsernameClaimType, request.EmailClaimType, request.DisplayNameClaimType,
            request.IconUrl, request.ButtonColor);

        _context.OidcProviders.Add(provider);
        await _context.SaveChangesAsync();

        return Ok(new OidcProviderDto(
            provider.Id, provider.Name, provider.DisplayName, provider.Enabled,
            provider.Authority, provider.ClientId, provider.Scopes,
            provider.AutoProvisionUsers, provider.IconUrl, provider.ButtonColor));
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProvider([FromBody] UpdateOidcProviderRequest request)
    {
        var provider = await _context.OidcProviders.FindAsync(request.Id);
        if (provider == null)
        {
            return NotFound();
        }

        provider.Update(
            request.DisplayName, request.Authority, request.ClientId, request.ClientSecret,
            request.Enabled, request.Scopes, request.AutoProvisionUsers,
            request.AuthorizationEndpoint, request.TokenEndpoint, request.UserInfoEndpoint,
            request.UsernameClaimType, request.EmailClaimType, request.DisplayNameClaimType,
            request.IconUrl, request.ButtonColor);

        await _context.SaveChangesAsync();

        return Ok(new OidcProviderDto(
            provider.Id, provider.Name, provider.DisplayName, provider.Enabled,
            provider.Authority, provider.ClientId, provider.Scopes,
            provider.AutoProvisionUsers, provider.IconUrl, provider.ButtonColor));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteProvider(int id)
    {
        var provider = await _context.OidcProviders.FindAsync(id);
        if (provider == null)
        {
            return NotFound();
        }

        _context.OidcProviders.Remove(provider);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
