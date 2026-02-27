using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BoardGameTracker.Common;
using BoardGameTracker.Common.Entities.Auth;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Auth;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Auth;

public class UserAdminServiceTests
{
    private readonly Mock<IUserStore<ApplicationUser>> _userStoreMock;
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<ILogger<UserAdminService>> _loggerMock;
    private readonly UserAdminService _service;

    public UserAdminServiceTests()
    {
        _userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            _userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        _loggerMock = new Mock<ILogger<UserAdminService>>();

        _service = new UserAdminService(
            _userManagerMock.Object,
            null!,
            _loggerMock.Object);

        _userManagerMock.Invocations.Clear();
    }

    private void VerifyNoOtherCalls()
    {
        _userManagerMock.VerifyNoOtherCalls();
        _loggerMock.VerifyNoOtherCalls();
    }

    #region GetByIdAsync

    [Fact]
    public async Task GetByIdAsync_ShouldReturnUserDto_WhenUserExists()
    {
        // Arrange
        var user = new ApplicationUser("testuser", "test@test.com", "Test User");
        var roles = new List<string> { Constants.AuthRoles.Admin };

        _userManagerMock
            .Setup(x => x.FindByIdAsync("user-1"))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(roles);

        // Act
        var result = await _service.GetByIdAsync("user-1");

        // Assert
        result.Should().NotBeNull();
        result.Username.Should().Be("testuser");
        result.Email.Should().Be("test@test.com");
        result.DisplayName.Should().Be("Test User");
        result.Roles.Should().ContainSingle().Which.Should().Be(Constants.AuthRoles.Admin);

        _userManagerMock.Verify(x => x.FindByIdAsync("user-1"), Times.Once);
        _userManagerMock.Verify(x => x.GetRolesAsync(user), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldThrowEntityNotFoundException_WhenUserDoesNotExist()
    {
        // Arrange
        _userManagerMock
            .Setup(x => x.FindByIdAsync("missing-user"))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var act = () => _service.GetByIdAsync("missing-user");

        // Assert
        await act.Should().ThrowAsync<EntityNotFoundException>()
            .Where(e => e.EntityType == nameof(ApplicationUser) && e.EntityId.Equals("missing-user"));

        _userManagerMock.Verify(x => x.FindByIdAsync("missing-user"), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region DeleteAsync

    [Fact]
    public async Task DeleteAsync_ShouldDeleteUser_WhenUserExistsAndIsNotLastAdmin()
    {
        // Arrange
        var currentUserId = "admin-1";
        var targetUserId = "user-2";
        var targetUser = new ApplicationUser("targetuser", "target@test.com");
        var otherAdmin = new ApplicationUser("anotheradmin", "other@test.com");

        _userManagerMock
            .Setup(x => x.FindByIdAsync(targetUserId))
            .ReturnsAsync(targetUser);

        _userManagerMock
            .Setup(x => x.GetUsersInRoleAsync(Constants.AuthRoles.Admin))
            .ReturnsAsync(new List<ApplicationUser> { otherAdmin, targetUser });

        _userManagerMock
            .Setup(x => x.DeleteAsync(targetUser))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        await _service.DeleteAsync(targetUserId, currentUserId);

        // Assert
        _userManagerMock.Verify(x => x.FindByIdAsync(targetUserId), Times.Once);
        _userManagerMock.Verify(x => x.GetUsersInRoleAsync(Constants.AuthRoles.Admin), Times.Once);
        _userManagerMock.Verify(x => x.DeleteAsync(targetUser), Times.Once);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains(currentUserId) && v.ToString()!.Contains(targetUserId)),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrowDomainException_WhenDeletingOwnAccount()
    {
        // Arrange
        var userId = "self-user-id";

        // Act
        var act = () => _service.DeleteAsync(userId, userId);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage(Constants.Errors.CannotDeleteSelf);

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrowEntityNotFoundException_WhenUserDoesNotExist()
    {
        // Arrange
        var currentUserId = "admin-1";
        var targetUserId = "missing-user";

        _userManagerMock
            .Setup(x => x.FindByIdAsync(targetUserId))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var act = () => _service.DeleteAsync(targetUserId, currentUserId);

        // Assert
        await act.Should().ThrowAsync<EntityNotFoundException>()
            .Where(e => e.EntityType == nameof(ApplicationUser) && e.EntityId.Equals(targetUserId));

        _userManagerMock.Verify(x => x.FindByIdAsync(targetUserId), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrowDomainException_WhenDeletingLastAdmin()
    {
        // Arrange
        var currentUserId = "admin-1";
        var targetUserId = "last-admin-id";
        var lastAdmin = new ApplicationUser("lastadmin", "lastadmin@test.com") { Id = targetUserId };

        _userManagerMock
            .Setup(x => x.FindByIdAsync(targetUserId))
            .ReturnsAsync(lastAdmin);

        _userManagerMock
            .Setup(x => x.GetUsersInRoleAsync(Constants.AuthRoles.Admin))
            .ReturnsAsync(new List<ApplicationUser> { lastAdmin });

        // Act
        var act = () => _service.DeleteAsync(targetUserId, currentUserId);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage(Constants.Errors.CannotDeleteLastAdmin);

        _userManagerMock.Verify(x => x.FindByIdAsync(targetUserId), Times.Once);
        _userManagerMock.Verify(x => x.GetUsersInRoleAsync(Constants.AuthRoles.Admin), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion
}
