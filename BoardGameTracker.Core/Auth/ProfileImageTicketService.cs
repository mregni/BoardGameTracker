using System.Globalization;
using System.Security.Cryptography;
using BoardGameTracker.Core.Auth.Interfaces;
using BoardGameTracker.Core.Common;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace BoardGameTracker.Core.Auth;

public class ProfileImageTicketService : IProfileImageTicketService
{
    private const string Purpose = "BoardGameTracker.ProfileImages.v1";

    private readonly IDataProtector _protector;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ProfileImageTicketService(IDataProtectionProvider provider, IOptions<JwtOptions> options, IDateTimeProvider dateTimeProvider)
    {
        _protector = provider.CreateProtector(Purpose);
        _dateTimeProvider = dateTimeProvider;
        Lifetime = TimeSpan.FromDays(options.Value.RefreshTokenExpiryDays);
    }

    public TimeSpan Lifetime { get; }

    public string Issue()
    {
        var expires = _dateTimeProvider.UtcNow.Add(Lifetime);
        return _protector.Protect(expires.Ticks.ToString(CultureInfo.InvariantCulture));
    }

    public bool IsValid(string? ticket)
    {
        if (string.IsNullOrEmpty(ticket))
        {
            return false;
        }

        try
        {
            var payload = _protector.Unprotect(ticket);
            return long.TryParse(payload, NumberStyles.None, CultureInfo.InvariantCulture, out var ticks)
                   && new DateTime(ticks, DateTimeKind.Utc) > _dateTimeProvider.UtcNow;
        }
        catch (CryptographicException)
        {
            return false;
        }
    }
}
