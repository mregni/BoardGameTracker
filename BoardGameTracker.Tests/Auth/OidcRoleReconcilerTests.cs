using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using BoardGameTracker.Common;
using BoardGameTracker.Common.Entities.Auth;
using BoardGameTracker.Core.Auth;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Auth;

public class OidcRoleReconcilerTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly OidcRoleReconciler _reconciler;

    public OidcRoleReconcilerTests()
    {
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            Mock.Of<IUserStore<ApplicationUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);
        _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.RemoveFromRolesAsync(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<string>>())).ReturnsAsync(IdentityResult.Success);
        _reconciler = new OidcRoleReconciler(_userManagerMock.Object, Mock.Of<ILogger<OidcRoleReconciler>>());
        _userManagerMock.Invocations.Clear();
    }

    private void VerifyNoOtherCalls()
    {
        _userManagerMock.VerifyNoOtherCalls();
    }

    private static OidcProvider Provider(string? rolesClaim = "groups", string? adminGroup = "bgt-admins", string defaultRole = "User")
    {
        var provider = new OidcProvider("Keycloak", "https://idp.example.com", "bgt");
        provider.Update("Keycloak", "https://idp.example.com", "bgt", null, true, "openid", true,
            null, null, null, null, null, null, rolesClaim, adminGroup, null, null);
        provider.SetDefaultRole(defaultRole);
        return provider;
    }

    private static OidcClaims Claims(params string[] groups) =>
        OidcClaims.Parse(JsonSerializer.Serialize(new { sub = "s", groups }));

    private static ApplicationUser IdpUser(string id = "user-1")
    {
        var user = new ApplicationUser("jane", "jane@example.com") { Id = id };
        user.ManageRoleByIdentityProvider();
        return user;
    }

    [Theory]
    [InlineData(new[] { "bgt-admins" }, "User", "Admin")]
    [InlineData(new[] { "BGT-ADMINS", "staff" }, "User", "Admin")]
    [InlineData(new[] { "staff" }, "User", "User")]
    [InlineData(new[] { "staff" }, "Reader", "Reader")]
    [InlineData(new string[0], "Reader", "Reader")]
    public async Task AssignInitialRoleAsync_ShouldMapTheAdminGroupToAdmin_AndOtherwiseUseTheDefaultRole(string[] groups, string defaultRole, string expected)
    {
        var user = new ApplicationUser("jane", "jane@example.com") { Id = "user-1" };

        await _reconciler.AssignInitialRoleAsync(user, Provider(defaultRole: defaultRole), Claims(groups));

        _userManagerMock.Verify(x => x.AddToRoleAsync(user, expected), Times.Once);
        user.RoleManagedByIdentityProvider.Should().BeTrue();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AssignInitialRoleAsync_ShouldUseTheDefaultRole_WhenNoRoleMappingIsConfigured()
    {
        var user = new ApplicationUser("jane", "jane@example.com") { Id = "user-1" };

        await _reconciler.AssignInitialRoleAsync(user, Provider(rolesClaim: null, defaultRole: "Reader"), Claims("bgt-admins"));

        _userManagerMock.Verify(x => x.AddToRoleAsync(user, "Reader"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AssignInitialRoleAsync_ShouldNeverUseAnUnsupportedDefaultRole()
    {
        var user = new ApplicationUser("jane", "jane@example.com") { Id = "user-1" };

        await _reconciler.AssignInitialRoleAsync(user, Provider(defaultRole: "Admin"), Claims());

        _userManagerMock.Verify(x => x.AddToRoleAsync(user, "User"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ReconcileAsync_ShouldPromote_WhenTheUserJoinedTheAdminGroup()
    {
        var user = IdpUser();
        _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(["User"]);

        await _reconciler.ReconcileAsync(user, Provider(), Claims("bgt-admins"));

        _userManagerMock.Verify(x => x.GetRolesAsync(user), Times.Once);
        _userManagerMock.Verify(x => x.AddToRoleAsync(user, "Admin"), Times.Once);
        _userManagerMock.Verify(x => x.RemoveFromRolesAsync(user, It.Is<IEnumerable<string>>(r => r.SequenceEqual(new[] { "User" }))), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ReconcileAsync_ShouldDemoteToTheDefaultRole_WhenAnotherActiveAdminRemains()
    {
        var user = IdpUser();
        _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(["Admin"]);
        _userManagerMock.Setup(x => x.GetUsersInRoleAsync("Admin")).ReturnsAsync([user, new ApplicationUser("admin", null) { Id = "admin" }]);

        await _reconciler.ReconcileAsync(user, Provider(defaultRole: "Reader"), Claims("staff"));

        _userManagerMock.Verify(x => x.GetRolesAsync(user), Times.Once);
        _userManagerMock.Verify(x => x.GetUsersInRoleAsync("Admin"), Times.Once);
        _userManagerMock.Verify(x => x.AddToRoleAsync(user, "Reader"), Times.Once);
        _userManagerMock.Verify(x => x.RemoveFromRolesAsync(user, It.Is<IEnumerable<string>>(r => r.SequenceEqual(new[] { "Admin" }))), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ReconcileAsync_ShouldKeepTheLastAdmin()
    {
        var user = IdpUser();
        _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(["Admin"]);
        _userManagerMock.Setup(x => x.GetUsersInRoleAsync("Admin")).ReturnsAsync([user]);

        await _reconciler.ReconcileAsync(user, Provider(), Claims("staff"));

        _userManagerMock.Verify(x => x.GetRolesAsync(user), Times.Once);
        _userManagerMock.Verify(x => x.GetUsersInRoleAsync("Admin"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ReconcileAsync_ShouldKeepTheLastAdmin_WhenTheOnlyOtherAdminIsDisabled()
    {
        var user = IdpUser();
        var disabledAdmin = new ApplicationUser("old-admin", null) { Id = "old-admin", LockoutEnd = DateTimeOffset.MaxValue };
        _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(["Admin"]);
        _userManagerMock.Setup(x => x.GetUsersInRoleAsync("Admin")).ReturnsAsync([user, disabledAdmin]);

        await _reconciler.ReconcileAsync(user, Provider(), Claims());

        _userManagerMock.Verify(x => x.GetRolesAsync(user), Times.Once);
        _userManagerMock.Verify(x => x.GetUsersInRoleAsync("Admin"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ReconcileAsync_ShouldChangeNothing_WhenTheRoleAlreadyMatches()
    {
        var user = IdpUser();
        _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(["User"]);

        await _reconciler.ReconcileAsync(user, Provider(), Claims("staff"));

        _userManagerMock.Verify(x => x.GetRolesAsync(user), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ReconcileAsync_ShouldLeaveALocallyManagedRoleAlone()
    {
        var user = new ApplicationUser("jane", "jane@example.com") { Id = "user-1" };

        await _reconciler.ReconcileAsync(user, Provider(), Claims("bgt-admins"));

        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null, "bgt-admins")]
    [InlineData("groups", null)]
    [InlineData(" ", "bgt-admins")]
    public async Task ReconcileAsync_ShouldDoNothing_WhenTheRoleMappingIsNotConfigured(string? rolesClaim, string? adminGroup)
    {
        var user = IdpUser();

        await _reconciler.ReconcileAsync(user, Provider(rolesClaim, adminGroup), Claims());

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ReconcileAsync_ShouldTreatAMissingGroupsClaimAsNoAdminGroup()
    {
        var user = IdpUser();
        _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(["Reader"]);

        await _reconciler.ReconcileAsync(user, Provider(), OidcClaims.Parse("{\"sub\":\"s\"}"));

        _userManagerMock.Verify(x => x.GetRolesAsync(user), Times.Once);
        _userManagerMock.Verify(x => x.AddToRoleAsync(user, Constants.AuthRoles.User), Times.Once);
        _userManagerMock.Verify(x => x.RemoveFromRolesAsync(user, It.Is<IEnumerable<string>>(r => r.SequenceEqual(new[] { "Reader" }))), Times.Once);
        VerifyNoOtherCalls();
    }
}
