using Microsoft.AspNetCore.Identity;

namespace BoardGameTracker.Common.Entities.Auth;

public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public int? PlayerId { get; private set; }
    public Player? Player { get; private set; }

    private ApplicationUser() { }

    public ApplicationUser(string userName, string email, string? displayName = null)
    {
        UserName = userName;
        Email = email;
        DisplayName = displayName;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateDisplayName(string? displayName)
    {
        DisplayName = displayName;
    }

    public void UpdateEmail(string? email)
    {
        Email = email;
    }

    public void UpdateLastLogin()
    {
        LastLoginAt = DateTime.UtcNow;
    }

    public void LinkPlayer(int playerId)
    {
        PlayerId = playerId;
    }

    public void UnlinkPlayer()
    {
        PlayerId = null;
    }
}
