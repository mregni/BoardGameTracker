using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BoardGameTracker.Common.Entities.Auth;
using BoardGameTracker.Core.Auth.Interfaces;
using BoardGameTracker.Core.Datastore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace BoardGameTracker.Core.Auth;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly MainDbContext _context;

    public TokenService(IConfiguration configuration, MainDbContext context)
    {
        _configuration = configuration;
        _context = context;
    }

    public string GenerateAccessToken(ApplicationUser user, IList<string> roles)
    {
        var secret = _configuration["Jwt:Secret"]
                     ?? throw new InvalidOperationException("JWT Secret is not configured");
        var issuer = _configuration["Jwt:Issuer"] ?? "boardgametracker-api";
        var audience = _configuration["Jwt:Audience"] ?? "boardgametracker-client";
        var expiryMinutes = int.Parse(_configuration["Jwt:AccessTokenExpiryMinutes"] ?? "15");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        if (user.DisplayName != null)
        {
            claims.Add(new Claim("display_name", user.DisplayName));
        }

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<RefreshToken> GenerateRefreshTokenAsync(string userId)
    {
        var expiryDays = int.Parse(_configuration["Jwt:RefreshTokenExpiryDays"] ?? "7");
        var refreshToken = RefreshToken.Create(userId, expiryDays);

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        return refreshToken;
    }

    public async Task<RefreshToken?> GetRefreshTokenAsync(string token)
    {
        return await _context.RefreshTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Token == token);
    }

    public async Task RevokeRefreshTokenAsync(RefreshToken token, string? reason = null, string? replacedByToken = null)
    {
        token.Revoke(reason, replacedByToken);
        _context.RefreshTokens.Update(token);
        await _context.SaveChangesAsync();
    }

    public async Task RevokeAllUserTokensAsync(string userId, string? reason = null)
    {
        var tokens = await _context.RefreshTokens
            .Where(x => x.UserId == userId && x.RevokedAt == null)
            .ToListAsync();

        foreach (var token in tokens)
        {
            token.Revoke(reason);
        }

        await _context.SaveChangesAsync();
    }

    public DateTime GetAccessTokenExpiry()
    {
        var expiryMinutes = int.Parse(_configuration["Jwt:AccessTokenExpiryMinutes"] ?? "15");
        return DateTime.UtcNow.AddMinutes(expiryMinutes);
    }

    public async Task CleanupExpiredTokensAsync()
    {
        var cutoff = DateTime.UtcNow.AddDays(-30);
        var expiredTokens = await _context.RefreshTokens
            .Where(x => x.ExpiresAt < cutoff || (x.RevokedAt != null && x.RevokedAt < cutoff))
            .ToListAsync();

        if (expiredTokens.Count > 0)
        {
            _context.RefreshTokens.RemoveRange(expiredTokens);
            await _context.SaveChangesAsync();
        }
    }
}
