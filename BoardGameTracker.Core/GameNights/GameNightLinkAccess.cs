using BoardGameTracker.Common;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Configuration.Interfaces;

namespace BoardGameTracker.Core.GameNights;

public static class GameNightLinkAccess
{
    public static async Task EnsureAllowedAsync(IConfigRepository configRepository, bool isAuthenticated)
    {
        if (isAuthenticated)
        {
            return;
        }

        var authenticationRequired = await configRepository.GetConfigValueOrDefaultAsync(Constants.AppConfig.RsvpAuthenticationEnabled, false);
        if (authenticationRequired)
        {
            throw new AuthenticationFailedException(Constants.Errors.NotAuthenticated);
        }
    }
}
