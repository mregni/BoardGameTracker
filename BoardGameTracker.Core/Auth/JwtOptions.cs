using System.ComponentModel.DataAnnotations;

namespace BoardGameTracker.Core.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Secret { get; set; } = string.Empty;

    [Required]
    public string Issuer { get; set; } = "boardgametracker-api";

    [Required]
    public string Audience { get; set; } = "boardgametracker-client";

    [Range(1, 1440)]
    public int AccessTokenExpiryMinutes { get; set; } = 60;

    [Range(1, 365)]
    public int RefreshTokenExpiryDays { get; set; } = 7;
}
