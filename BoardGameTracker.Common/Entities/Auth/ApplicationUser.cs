using BoardGameTracker.Common.Enums;
using Microsoft.AspNetCore.Identity;

namespace BoardGameTracker.Common.Entities.Auth;

public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public int? PlayerId { get; private set; }
    public Player? Player { get; private set; }
    public bool RoleManagedByIdentityProvider { get; private set; }
    public string? ProfileImage { get; private set; }
    public ProfileImageSource ProfileImageSource { get; private set; }
    public string? ProfileImageSourceUrl { get; private set; }
    public bool PlayerLinkPending { get; private set; }

    private ApplicationUser() { }

    public ApplicationUser(string userName, string? email, string? displayName = null)
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

    public void UpdateEmail(string? email, bool confirmed)
    {
        Email = email;
        EmailConfirmed = confirmed;
    }

    public void ConfirmEmail()
    {
        EmailConfirmed = true;
    }

    public void UpdateLastLogin()
    {
        LastLoginAt = DateTime.UtcNow;
    }

    public void ManageRoleByIdentityProvider()
    {
        RoleManagedByIdentityProvider = true;
    }

    public void ManageRoleLocally()
    {
        RoleManagedByIdentityProvider = false;
    }

    public bool IsDisabled => LockoutEnd.HasValue && LockoutEnd.Value > DateTimeOffset.UtcNow;

    public bool NeedsPlayerLink => PlayerLinkPending && PlayerId == null;

    public void LinkPlayer(int playerId)
    {
        PlayerId = playerId;
        PlayerLinkPending = false;
    }

    public void AwaitPlayerLink()
    {
        PlayerLinkPending = true;
    }

    public void SkipPlayerLink()
    {
        PlayerLinkPending = false;
    }

    public void UnlinkPlayer()
    {
        PlayerId = null;
    }

    public bool AcceptsIdentityProviderPicture(string pictureUrl)
    {
        return ProfileImageSource != ProfileImageSource.Upload
               && !string.Equals(ProfileImageSourceUrl, pictureUrl, StringComparison.Ordinal);
    }

    public void UseIdentityProviderPicture(string image, string pictureUrl)
    {
        ProfileImage = image;
        ProfileImageSource = ProfileImageSource.IdentityProvider;
        ProfileImageSourceUrl = pictureUrl;
    }

    public void UseUploadedProfileImage(string image)
    {
        ProfileImage = image;
        ProfileImageSource = ProfileImageSource.Upload;
    }

    public void RemoveProfileImage()
    {
        ProfileImage = null;
        ProfileImageSource = ProfileImageSource.None;
    }
}
