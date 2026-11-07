using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs.Auth;
using BoardGameTracker.Common.Entities.Auth;
using BoardGameTracker.Common.Extensions;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Extensions;

public class AuthDtoExtensionsTests
{
    private static readonly string[] Roles = [Constants.AuthRoles.Admin];

    private static ApplicationUser User()
    {
        var user = new ApplicationUser("bob", "bob@test.com", "Bob") { LockoutEnd = DateTimeOffset.UtcNow.AddDays(1) };
        user.UpdateLastLogin();
        user.ManageRoleByIdentityProvider();
        user.UseUploadedProfileImage("bob.png");
        user.LinkPlayer(7);
        return user;
    }

    private static OidcProvider Provider(string? clientSecret)
    {
        var provider = new OidcProvider("Display", "https://authority.test", "client");
        provider.Update("Display", "https://authority.test", "client", clientSecret, false, "openid", false,
            "https://authority.test/authorize", "https://authority.test/token", "https://authority.test/userinfo",
            "preferred_username", "mail", "name", "groups", "admins", "https://authority.test/icon.png", "#112233");
        provider.SetDefaultRole(Constants.AuthRoles.Admin);
        provider.SetPictureClaimType("picture");
        return provider;
    }

    [Fact]
    public void ToDto_ShouldMapEveryFieldOfTheUser()
    {
        var user = User();

        var dto = user.ToDto(Roles, 3);

        dto.Should().BeEquivalentTo(new UserDto(user.Id, "bob", "bob@test.com", "Bob", Roles, user.CreatedAt, user.LastLoginAt,
            7, true, true, 3, "bob.png"));
        dto.LastLoginAt.Should().NotBeNull();
    }

    [Fact]
    public void ToDto_ShouldDefaultToNoExternalLogins()
    {
        User().ToDto(Roles).ExternalLoginCount.Should().Be(0);
    }

    [Fact]
    public void Mapping_ShouldUseAnEmptyName_WhenTheUserHasNone()
    {
        var user = User();
        user.UserName = null;

        user.ToDto(Roles).Username.Should().BeEmpty();
        user.ToProfileDto(Roles).Username.Should().BeEmpty();
        user.ToUserInfo(Roles).Username.Should().BeEmpty();
    }

    [Fact]
    public void ToProfileDto_ShouldMapEveryFieldOfTheUser()
    {
        var user = User();

        user.ToProfileDto(Roles).Should().BeEquivalentTo(new ProfileResponse(user.Id, "bob", "bob@test.com", "Bob", Roles,
            user.CreatedAt, user.LastLoginAt, 7, "bob.png", false));
    }

    [Fact]
    public void ToUserInfo_ShouldMapEveryFieldOfTheUser()
    {
        var user = new ApplicationUser("bob", null, "Bob");
        user.UseUploadedProfileImage("bob.png");
        user.AwaitPlayerLink();

        user.ToUserInfo(Roles).Should().BeEquivalentTo(new UserInfo(user.Id, "bob", "Bob", Roles, "bob.png", true));
        user.ToProfileDto(Roles).NeedsPlayerLink.Should().BeTrue();
    }

    [Fact]
    public void ToDto_ShouldMapEveryFieldOfTheProvider_WithoutTheSecret()
    {
        var dto = Provider("secret").ToDto();

        dto.Should().BeEquivalentTo(new OidcProviderDto(0, "Display", false, "https://authority.test", "client", true, "openid", false,
            "https://authority.test/authorize", "https://authority.test/token", "https://authority.test/userinfo",
            "preferred_username", "mail", "name", "groups", "admins", "https://authority.test/icon.png", "#112233",
            Constants.AuthRoles.Admin, "picture"));
    }

    [Fact]
    public void ToDto_ShouldSayThereIsNoSecret_WhenTheProviderHasNone()
    {
        Provider(null).ToDto().HasClientSecret.Should().BeFalse();
    }

    [Fact]
    public void ToListDto_ShouldMapTheProviderSummary()
    {
        Provider(null).ToListDto().Should().Be(new OidcProviderListDto(0, "Display", false));
    }

    [Fact]
    public void ToDto_ShouldMapEveryFieldOfTheExternalLogin()
    {
        var login = new ExternalLogin("user-1", "authentik", "https://authority.test", "subject-1", "Bob");
        login.UpdateLastUsed();

        login.ToDto().Should().Be(new ExternalLoginDto(0, "authentik", "subject-1", "Bob", login.LinkedAt, login.LastUsedAt));
        login.LastUsedAt.Should().NotBeNull();
    }
}
