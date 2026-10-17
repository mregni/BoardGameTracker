using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using BoardGameTracker.Api.Controllers.Admin;
using BoardGameTracker.Common.DTOs.Auth;
using BoardGameTracker.Core.Auth.Interfaces;
using FluentAssertions;
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
        _serviceMock.Setup(x => x.DisableAsync("user-2", "admin-1")).ReturnsAsync(User(disabled: true));

        var result = await _controller.DisableUser("user-2");

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeOfType<UserDto>().Which.IsDisabled.Should().BeTrue();
        _serviceMock.Verify(x => x.DisableAsync("user-2", "admin-1"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task EnableUser_ShouldEnableAsTheCurrentAdmin()
    {
        _serviceMock.Setup(x => x.EnableAsync("user-2", "admin-1")).ReturnsAsync(User());

        var result = await _controller.EnableUser("user-2");

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeOfType<UserDto>().Which.IsDisabled.Should().BeFalse();
        _serviceMock.Verify(x => x.EnableAsync("user-2", "admin-1"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ManageRoleByIdentityProvider_ShouldHandTheRoleBack()
    {
        _serviceMock.Setup(x => x.ManageRoleByIdentityProviderAsync("user-2", "admin-1")).ReturnsAsync(User());

        var result = await _controller.ManageRoleByIdentityProvider("user-2");

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeOfType<UserDto>().Which.RoleManagedByIdentityProvider.Should().BeTrue();
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
}
