using System.Security.Claims;
using System.Text;
using BoardGameTracker.Common.Entities.Auth;
using BoardGameTracker.Core.Auth.Interfaces;
using BoardGameTracker.Core.Datastore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace BoardGameTracker.Core.Auth;

public class TokenService : ITokenService
{
    private static readonly JsonWebTokenHandler TokenHandler = new();

    private readonly JwtOptions _options;
    private readonly MainDbContext _context;

    public TokenService(IOptions<JwtOptions> options, MainDbContext context)
    {
        _options = options.Value;
        _context = context;
    }

    public string GenerateAccessToken(ApplicationUser user, IList<string> roles)
    {
        if (string.IsNullOrWhiteSpace(_options.Secret))
        {
            throw new InvalidOperationException("JWT Secret is not configured");
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret)); // NOSONAR - secret loaded from configuration, not hardcoded
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

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_options.AccessTokenExpiryMinutes),
            SigningCredentials = credentials,
        };

        return TokenHandler.CreateToken(descriptor);
    }

    public async Task<RefreshToken> GenerateRefreshTokenAsync(string userId)
    {
        var refreshToken = RefreshToken.Create(userId, _options.RefreshTokenExpiryDays);

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        return refreshToken;
    }

    public async Task<RefreshToken> RotateRefreshTokenAsync(RefreshToken current)
    {
        var replacement = RefreshToken.Create(current.UserId, _options.RefreshTokenExpiryDays);
        current.Revoke("Replaced by new token", replacement.Token);

        _context.RefreshTokens.Add(replacement);
        _context.RefreshTokens.Update(current);
        await _context.SaveChangesAsync();

        return replacement;
    }

    public async Task<RefreshToken?> GetRefreshTokenAsync(string token)
    {
        var hash = RefreshToken.Hash(token);
        return await _context.RefreshTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Token == hash);
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
        return DateTime.UtcNow.AddMinutes(_options.AccessTokenExpiryMinutes);
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
