using System.Globalization;
using System.Security.Cryptography;
using BoardGameTracker.Core.Auth.Interfaces;
using BoardGameTracker.Core.Common;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace BoardGameTracker.Core.Auth;

public class ProfileImageTicketService : IProfileImageTicketService
{
    private const string Purpose = "BoardGameTracker.ProfileImages.v2";
    private const char Separator = '|';

    private readonly IDataProtector _protector;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ProfileImageTicketService(IDataProtectionProvider provider, IOptions<JwtOptions> options, IDateTimeProvider dateTimeProvider)
    {
        _protector = provider.CreateProtector(Purpose);
        _dateTimeProvider = dateTimeProvider;
        Lifetime = TimeSpan.FromDays(options.Value.RefreshTokenExpiryDays);
    }

    public TimeSpan Lifetime { get; }

    public string Issue(string userId)
    {
        var expires = _dateTimeProvider.UtcNow.Add(Lifetime);
        return _protector.Protect($"{expires.Ticks.ToString(CultureInfo.InvariantCulture)}{Separator}{userId}");
    }

    public string? ReadUserId(string? ticket)
    {
        if (string.IsNullOrEmpty(ticket))
        {
            return null;
        }

        string payload;
        try
        {
            payload = _protector.Unprotect(ticket);
        }
        catch (CryptographicException)
        {
            return null;
        }

        var parts = payload.Split(Separator, 2);
        if (parts.Length != 2
            || string.IsNullOrEmpty(parts[1])
            || !long.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var ticks)
            || new DateTime(ticks, DateTimeKind.Utc) <= _dateTimeProvider.UtcNow)
        {
            return null;
        }

        return parts[1];
    }
}
