using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BoardGameTracker.Api.Controllers.Admin;
using BoardGameTracker.Api.Infrastructure;
using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs.Auth;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Auth.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Auth;

public class UsersControllerTests
{
    private readonly Mock<IUserAdminService> _serviceMock = new();
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _controller = new UsersController(_serviceMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "admin-1")], "test")),
                },
            },
        };
    }

    private void VerifyNoOtherCalls()
    {
        _serviceMock.VerifyNoOtherCalls();
    }

    private static UserDto User(bool disabled = false) =>
        new("user-2", "jane", null, null, ["User"], DateTime.UtcNow, null, null, disabled, true, 1);

    [Fact]
    public async Task DisableUser_ShouldDisableAsTheCurrentAdmin()
    {
        var user = User(disabled: true);
        _serviceMock.Setup(x => x.DisableAsync("user-2", "admin-1")).ReturnsAsync(user);

        var result = await _controller.DisableUser("user-2");

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(user);
        _serviceMock.Verify(x => x.DisableAsync("user-2", "admin-1"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task EnableUser_ShouldEnableAsTheCurrentAdmin()
    {
        var user = User();
        _serviceMock.Setup(x => x.EnableAsync("user-2", "admin-1")).ReturnsAsync(user);

        var result = await _controller.EnableUser("user-2");

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(user);
        _serviceMock.Verify(x => x.EnableAsync("user-2", "admin-1"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ManageRoleByIdentityProvider_ShouldHandTheRoleBack()
    {
        var user = User();
        _serviceMock.Setup(x => x.ManageRoleByIdentityProviderAsync("user-2", "admin-1")).ReturnsAsync(user);

        var result = await _controller.ManageRoleByIdentityProvider("user-2");

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(user);
        _serviceMock.Verify(x => x.ManageRoleByIdentityProviderAsync("user-2", "admin-1"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetExternalLogins_ShouldListTheUsersIdentities()
    {
        var logins = new List<ExternalLoginDto> { new(3, "kc", "sub-1", "Jane", DateTime.UtcNow, null) };
        _serviceMock.Setup(x => x.GetExternalLoginsAsync("user-2")).ReturnsAsync(logins);

        var result = await _controller.GetExternalLogins("user-2");

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(logins);
        _serviceMock.Verify(x => x.GetExternalLoginsAsync("user-2"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UnlinkExternalLogin_ShouldReturnNoContent()
    {
        var result = await _controller.UnlinkExternalLogin("user-2", 3);

        result.Should().BeOfType<NoContentResult>();
        _serviceMock.Verify(x => x.UnlinkExternalLoginAsync("user-2", 3, "admin-1"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetUsers_ShouldReturnEveryUser()
    {
        var users = new List<UserDto> { User() };
        _serviceMock.Setup(x => x.GetAllAsync()).ReturnsAsync(users);

        var result = await _controller.GetUsers();

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(users);
        _serviceMock.Verify(x => x.GetAllAsync(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetUser_ShouldReturnThatUser()
    {
        var user = User();
        _serviceMock.Setup(x => x.GetByIdAsync("user-2")).ReturnsAsync(user);

        var result = await _controller.GetUser("user-2");

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(user);
        _serviceMock.Verify(x => x.GetByIdAsync("user-2"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateUserRole_ShouldUpdateAsTheCurrentAdmin()
    {
        var user = User();
        _serviceMock.Setup(x => x.UpdateRoleAsync("user-2", Constants.AuthRoles.Reader, "admin-1")).ReturnsAsync(user);

        var result = await _controller.UpdateUserRole("user-2", new UpdateUserRoleRequest(Constants.AuthRoles.Reader));

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(user);
        _serviceMock.Verify(x => x.UpdateRoleAsync("user-2", Constants.AuthRoles.Reader, "admin-1"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateUser_ShouldPassEveryFieldOn_AsTheCurrentAdmin()
    {
        var user = User();
        var request = new AdminUpdateUserRequest("jane", "jane@test.com", Constants.AuthRoles.User, 4);
        _serviceMock.Setup(x => x.UpdateUserAsync("user-2", "jane", "jane@test.com", Constants.AuthRoles.User, 4, "admin-1")).ReturnsAsync(user);

        var result = await _controller.UpdateUser("user-2", request);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(user);
        _serviceMock.Verify(x => x.UpdateUserAsync("user-2", "jane", "jane@test.com", Constants.AuthRoles.User, 4, "admin-1"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteUser_ShouldDeleteAsTheCurrentAdmin()
    {
        var result = await _controller.DeleteUser("user-2");

        result.Should().BeOfType<NoContentResult>();
        _serviceMock.Verify(x => x.DeleteAsync("user-2", "admin-1"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteUser_ShouldRefuse_WhenTheCallerHasNoUserId()
    {
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        var act = () => _controller.DeleteUser("user-2");

        await act.Should().ThrowAsync<AuthenticationFailedException>().WithMessage(Constants.Errors.NotAuthenticated);
        VerifyNoOtherCalls();
    }

    [Fact]
    public void Controller_ShouldOnlyBeOpenToAdmins_AndClosedWhileAuthIsDisabled()
    {
        var authorize = typeof(UsersController).GetCustomAttributes(typeof(AuthorizeAttribute), false).Cast<AuthorizeAttribute>().Single();
        var filter = typeof(UsersController).GetCustomAttributes(typeof(ServiceFilterAttribute), false).Cast<ServiceFilterAttribute>().Single();

        authorize.Roles.Should().Be(Constants.AuthRoles.Admin);
        filter.ServiceType.Should().Be<AuthDisabledFilter>();
        typeof(UsersController).GetMethods().Should()
            .NotContain(m => m.IsDefined(typeof(AllowAnonymousAttribute), false));
        VerifyNoOtherCalls();
    }
}
