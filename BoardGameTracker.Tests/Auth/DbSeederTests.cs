using System.Linq;
using System.Threading.Tasks;
using BoardGameTracker.Common;
using BoardGameTracker.Common.Entities.Auth;
using BoardGameTracker.Core.Auth;
using BoardGameTracker.Tests.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Auth;

public class DbSeederTests : IDisposable
{
    private const string RolesCreated = "Created role: ";
    private const string DefaultAdminWarning = "Created default admin user 'admin' with the default password. Change it after the first login or set ADMIN_PASSWORD";

    private readonly IdentityTestScope _identity = new();
    private readonly Mock<ILogger<DbSeederTests>> _loggerMock = new();

    public void Dispose()
    {
        _identity.Dispose();
        GC.SuppressFinalize(this);
    }

    private void VerifyNoOtherCalls()
    {
        _loggerMock.VerifyNoOtherCalls();
    }

    private void VerifyRolesCreated()
    {
        _loggerMock.VerifyLog(LogLevel.Information, RolesCreated + Constants.AuthRoles.Admin);
        _loggerMock.VerifyLog(LogLevel.Information, RolesCreated + Constants.AuthRoles.User);
        _loggerMock.VerifyLog(LogLevel.Information, RolesCreated + Constants.AuthRoles.Reader);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task SeedAuthData_ShouldCreateTheRolesAndAnAdminWithTheDefaultPassword_WhenAdminPasswordIsNotSet(string? adminPassword)
    {
        await DbSeeder.SeedAuthData(_identity.RoleManager, _identity.UserManager, _loggerMock.Object, adminPassword);

        var admin = await _identity.UserManager.FindByNameAsync("admin");
        admin.Should().NotBeNull();
        admin!.Email.Should().BeNull();
        admin.DisplayName.Should().Be("Administrator");
        (await _identity.UserManager.CheckPasswordAsync(admin, "admin")).Should().BeTrue();
        (await _identity.UserManager.GetRolesAsync(admin)).Should().Equal(Constants.AuthRoles.Admin);
        _identity.RoleManager.Roles.Select(x => x.Name).Should()
            .BeEquivalentTo(Constants.AuthRoles.Admin, Constants.AuthRoles.User, Constants.AuthRoles.Reader);
        VerifyRolesCreated();
        _loggerMock.VerifyLog(LogLevel.Warning, DefaultAdminWarning);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SeedAuthData_ShouldCreateTheAdminWithAdminPassword_WhenItMeetsTheRules()
    {
        await DbSeeder.SeedAuthData(_identity.RoleManager, _identity.UserManager, _loggerMock.Object, "a-long-enough-password");

        var admin = await _identity.UserManager.FindByNameAsync("admin");
        (await _identity.UserManager.CheckPasswordAsync(admin!, "a-long-enough-password")).Should().BeTrue();
        (await _identity.UserManager.CheckPasswordAsync(admin!, "admin")).Should().BeFalse();
        (await _identity.UserManager.GetRolesAsync(admin!)).Should().Equal(Constants.AuthRoles.Admin);
        VerifyRolesCreated();
        _loggerMock.VerifyLog(LogLevel.Information, "Created default admin user 'admin' using ADMIN_PASSWORD");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SeedAuthData_ShouldRefuseToStart_WhenAdminPasswordBreaksTheRules()
    {
        var act = () => DbSeeder.SeedAuthData(_identity.RoleManager, _identity.UserManager, _loggerMock.Object, "short");

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage(
            "ADMIN_PASSWORD does not meet the password rules: Passwords must be at least 8 characters. Choose a longer password or unset ADMIN_PASSWORD to start with admin/admin.");
        (await _identity.UserManager.FindByNameAsync("admin")).Should().BeNull();
        VerifyRolesCreated();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SeedAuthData_ShouldNotCreateAnAdmin_WhenAnyUserAlreadyExists()
    {
        await _identity.UserManager.CreateAsync(new ApplicationUser("someone", null), "a-long-enough-password");

        await DbSeeder.SeedAuthData(_identity.RoleManager, _identity.UserManager, _loggerMock.Object);

        (await _identity.UserManager.FindByNameAsync("admin")).Should().BeNull();
        VerifyRolesCreated();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SeedAuthData_ShouldNotJudgeAdminPassword_WhenAnyUserAlreadyExists()
    {
        await _identity.UserManager.CreateAsync(new ApplicationUser("someone", null), "a-long-enough-password");

        await DbSeeder.SeedAuthData(_identity.RoleManager, _identity.UserManager, _loggerMock.Object, "short");

        (await _identity.UserManager.FindByNameAsync("admin")).Should().BeNull();
        VerifyRolesCreated();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SeedAuthData_ShouldOnlyCreateTheMissingRoles()
    {
        await _identity.RoleManager.CreateAsync(new IdentityRole(Constants.AuthRoles.Admin));
        await _identity.RoleManager.CreateAsync(new IdentityRole(Constants.AuthRoles.User));

        await DbSeeder.SeedAuthData(_identity.RoleManager, _identity.UserManager, _loggerMock.Object);

        _identity.RoleManager.Roles.Select(x => x.Name).Should()
            .BeEquivalentTo(Constants.AuthRoles.Admin, Constants.AuthRoles.User, Constants.AuthRoles.Reader);
        _loggerMock.VerifyLog(LogLevel.Information, RolesCreated + Constants.AuthRoles.Reader);
        _loggerMock.VerifyLog(LogLevel.Warning, DefaultAdminWarning);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SeedAuthData_ShouldLogTheErrorsAndAssignNoRole_WhenTheAdminCannotBeCreated()
    {
        var validatorMock = new Mock<IUserValidator<ApplicationUser>>();
        var manager = _identity.UserManager;
        validatorMock
            .Setup(x => x.ValidateAsync(manager, It.Is<ApplicationUser>(u => u.UserName == "admin")))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "first" }, new IdentityError { Description = "second" }));
        manager.UserValidators.Add(validatorMock.Object);

        await DbSeeder.SeedAuthData(_identity.RoleManager, manager, _loggerMock.Object);

        (await manager.FindByNameAsync("admin")).Should().BeNull();
        VerifyRolesCreated();
        _loggerMock.VerifyLog(LogLevel.Error, "Failed to create default admin user: first, second");
        validatorMock.Verify(x => x.ValidateAsync(manager, It.Is<ApplicationUser>(u => u.UserName == "admin")), Times.Once);
        validatorMock.VerifyNoOtherCalls();
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    [InlineData("a-long-enough-password")]
    public async Task GetAdminPasswordErrorsAsync_ShouldReportNothing_ForAnEmptyOrValidPassword(string? password)
    {
        var errors = await DbSeeder.GetAdminPasswordErrorsAsync(_identity.UserManager, password);

        errors.Should().BeEmpty();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetAdminPasswordErrorsAsync_ShouldReportTheBrokenRule_ForATooShortPassword()
    {
        var errors = await DbSeeder.GetAdminPasswordErrorsAsync(_identity.UserManager, "short");

        errors.Should().Equal("Passwords must be at least 8 characters.");
        VerifyNoOtherCalls();
    }
}
