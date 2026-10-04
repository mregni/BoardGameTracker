using System;
using System.Linq;
using System.Threading.Tasks;
using BoardGameTracker.Common;
using BoardGameTracker.Common.Entities.Auth;
using BoardGameTracker.Core.Auth;
using BoardGameTracker.Tests.Support;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Auth;

public class DbSeederTests : IDisposable
{
    private readonly IdentityTestScope _identity = new();
    private readonly Mock<ILogger> _loggerMock = new();

    public void Dispose()
    {
        _identity.Dispose();
        GC.SuppressFinalize(this);
    }

    private void VerifyLogged(LogLevel level, string fragment)
    {
        _loggerMock.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains(fragment)),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SeedAuthData_ShouldCreateTheRolesAndAnAdminWithTheDefaultPassword_WhenAdminPasswordIsNotSet()
    {
        await DbSeeder.SeedAuthData(_identity.RoleManager, _identity.UserManager, _loggerMock.Object);

        var admin = await _identity.UserManager.FindByNameAsync("admin");
        admin.Should().NotBeNull();
        (await _identity.UserManager.CheckPasswordAsync(admin!, "admin")).Should().BeTrue();
        (await _identity.UserManager.IsInRoleAsync(admin!, Constants.AuthRoles.Admin)).Should().BeTrue();
        _identity.RoleManager.Roles.Select(x => x.Name).Should()
            .BeEquivalentTo(Constants.AuthRoles.Admin, Constants.AuthRoles.User, Constants.AuthRoles.Reader);
        VerifyLogged(LogLevel.Warning, "default password");
    }

    [Fact]
    public async Task SeedAuthData_ShouldCreateTheAdminWithAdminPassword_WhenItMeetsTheRules()
    {
        await DbSeeder.SeedAuthData(_identity.RoleManager, _identity.UserManager, _loggerMock.Object, "a-long-enough-password");

        var admin = await _identity.UserManager.FindByNameAsync("admin");
        (await _identity.UserManager.CheckPasswordAsync(admin!, "a-long-enough-password")).Should().BeTrue();
        (await _identity.UserManager.CheckPasswordAsync(admin!, "admin")).Should().BeFalse();
        VerifyLogged(LogLevel.Information, "using ADMIN_PASSWORD");
    }

    [Fact]
    public async Task SeedAuthData_ShouldRefuseToStart_WhenAdminPasswordBreaksTheRules()
    {
        var act = () => DbSeeder.SeedAuthData(_identity.RoleManager, _identity.UserManager, _loggerMock.Object, "short");

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("ADMIN_PASSWORD does not meet the password rules*");
        (await _identity.UserManager.FindByNameAsync("admin")).Should().BeNull();
    }

    [Fact]
    public async Task SeedAuthData_ShouldNotCreateAnAdmin_WhenAnyUserAlreadyExists()
    {
        await _identity.UserManager.CreateAsync(new ApplicationUser("someone", null), "a-long-enough-password");

        await DbSeeder.SeedAuthData(_identity.RoleManager, _identity.UserManager, _loggerMock.Object);

        (await _identity.UserManager.FindByNameAsync("admin")).Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    [InlineData("a-long-enough-password")]
    public async Task GetAdminPasswordErrorsAsync_ShouldReportNothing_ForAnEmptyOrValidPassword(string? password)
    {
        var errors = await DbSeeder.GetAdminPasswordErrorsAsync(_identity.UserManager, password);

        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAdminPasswordErrorsAsync_ShouldReportTheBrokenRule_ForATooShortPassword()
    {
        var errors = await DbSeeder.GetAdminPasswordErrorsAsync(_identity.UserManager, "short");

        errors.Should().ContainSingle();
    }
}
