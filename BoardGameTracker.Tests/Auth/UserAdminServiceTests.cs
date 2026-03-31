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

    #region UpdateRoleAsync

    [Fact]
    public async Task UpdateRoleAsync_ShouldUpdateRole_WhenReaderRoleIsProvided()
    {
        // Arrange
        var currentUserId = "admin-1";
        var targetUserId = "user-2";
        var targetUser = new ApplicationUser("targetuser", "target@test.com");
        var currentRoles = new List<string> { Constants.AuthRoles.User };
        var updatedRoles = new List<string> { Constants.AuthRoles.Reader };

        _userManagerMock.Setup(x => x.FindByIdAsync(targetUserId)).ReturnsAsync(targetUser);
        _userManagerMock.Setup(x => x.GetRolesAsync(targetUser)).ReturnsAsync(currentRoles);
        _userManagerMock.Setup(x => x.RemoveFromRolesAsync(targetUser, currentRoles)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.AddToRoleAsync(targetUser, Constants.AuthRoles.Reader)).ReturnsAsync(IdentityResult.Success);

        // Second call to GetRolesAsync returns updated roles
        _userManagerMock.SetupSequence(x => x.GetRolesAsync(targetUser))
            .ReturnsAsync(currentRoles)
            .ReturnsAsync(updatedRoles);

        // Act
        var result = await _service.UpdateRoleAsync(targetUserId, Constants.AuthRoles.Reader, currentUserId);

        // Assert
        result.Should().NotBeNull();
        result.Roles.Should().Contain(Constants.AuthRoles.Reader);

        _userManagerMock.Verify(x => x.FindByIdAsync(targetUserId), Times.Once);
        _userManagerMock.Verify(x => x.GetRolesAsync(targetUser), Times.Exactly(2));
        _userManagerMock.Verify(x => x.RemoveFromRolesAsync(targetUser, currentRoles), Times.Once);
        _userManagerMock.Verify(x => x.AddToRoleAsync(targetUser, Constants.AuthRoles.Reader), Times.Once);
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
    public async Task UpdateRoleAsync_ShouldThrowValidationException_WhenRoleIsInvalid()
    {
        // Act
        var act = () => _service.UpdateRoleAsync("user-1", "InvalidRole", "admin-1");

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage(Constants.Errors.InvalidRole);

        VerifyNoOtherCalls();
    }

    #endregion

    #region UpdateUserAsync

    [Fact]
    public async Task UpdateUserAsync_ShouldUpdateAllFields_WhenValidDataProvided()
    {
        // Arrange
        var currentUserId = "admin-1";
        var targetUserId = "user-2";
        var targetUser = new ApplicationUser("olduser", "old@test.com");
        var currentRoles = new List<string> { Constants.AuthRoles.User };
        var updatedRoles = new List<string> { Constants.AuthRoles.Reader };

        _userManagerMock.Setup(x => x.FindByIdAsync(targetUserId)).ReturnsAsync(targetUser);
        _userManagerMock.Setup(x => x.FindByNameAsync("newuser")).ReturnsAsync((ApplicationUser?)null);
        _userManagerMock.Setup(x => x.SetUserNameAsync(targetUser, "newuser")).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.UpdateAsync(targetUser)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.RemoveFromRolesAsync(targetUser, currentRoles)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.AddToRoleAsync(targetUser, Constants.AuthRoles.Reader)).ReturnsAsync(IdentityResult.Success);

        _userManagerMock.SetupSequence(x => x.GetRolesAsync(targetUser))
            .ReturnsAsync(currentRoles)
            .ReturnsAsync(updatedRoles);

        // Act
        var result = await _service.UpdateUserAsync(targetUserId, "newuser", "new@test.com", Constants.AuthRoles.Reader, currentUserId);

        // Assert
        result.Should().NotBeNull();
        result.Roles.Should().Contain(Constants.AuthRoles.Reader);

        _userManagerMock.Verify(x => x.FindByIdAsync(targetUserId), Times.Once);
        _userManagerMock.Verify(x => x.FindByNameAsync("newuser"), Times.Once);
        _userManagerMock.Verify(x => x.SetUserNameAsync(targetUser, "newuser"), Times.Once);
        _userManagerMock.Verify(x => x.UpdateAsync(targetUser), Times.Once);
        _userManagerMock.Verify(x => x.GetRolesAsync(targetUser), Times.Exactly(2));
        _userManagerMock.Verify(x => x.RemoveFromRolesAsync(targetUser, currentRoles), Times.Once);
        _userManagerMock.Verify(x => x.AddToRoleAsync(targetUser, Constants.AuthRoles.Reader), Times.Once);
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
    public async Task UpdateUserAsync_ShouldSkipUsernameChange_WhenUsernameIsSame()
    {
        // Arrange
        var currentUserId = "admin-1";
        var targetUserId = "user-2";
        var targetUser = new ApplicationUser("sameuser", "old@test.com");
        var currentRoles = new List<string> { Constants.AuthRoles.User };
        var updatedRoles = new List<string> { Constants.AuthRoles.Admin };

        _userManagerMock.Setup(x => x.FindByIdAsync(targetUserId)).ReturnsAsync(targetUser);
        _userManagerMock.Setup(x => x.UpdateAsync(targetUser)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.RemoveFromRolesAsync(targetUser, currentRoles)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.AddToRoleAsync(targetUser, Constants.AuthRoles.Admin)).ReturnsAsync(IdentityResult.Success);

        _userManagerMock.SetupSequence(x => x.GetRolesAsync(targetUser))
            .ReturnsAsync(currentRoles)
            .ReturnsAsync(updatedRoles);

        // Act
        var result = await _service.UpdateUserAsync(targetUserId, "sameuser", "new@test.com", Constants.AuthRoles.Admin, currentUserId);

        // Assert
        result.Should().NotBeNull();
        result.Roles.Should().Contain(Constants.AuthRoles.Admin);

        _userManagerMock.Verify(x => x.FindByIdAsync(targetUserId), Times.Once);
        _userManagerMock.Verify(x => x.UpdateAsync(targetUser), Times.Once);
        _userManagerMock.Verify(x => x.GetRolesAsync(targetUser), Times.Exactly(2));
        _userManagerMock.Verify(x => x.RemoveFromRolesAsync(targetUser, currentRoles), Times.Once);
        _userManagerMock.Verify(x => x.AddToRoleAsync(targetUser, Constants.AuthRoles.Admin), Times.Once);
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
    public async Task UpdateUserAsync_ShouldThrowValidationException_WhenUsernameAlreadyExists()
    {
        // Arrange
        var currentUserId = "admin-1";
        var targetUserId = "user-2";
        var targetUser = new ApplicationUser("olduser", "old@test.com");
        var existingUser = new ApplicationUser("takenuser", "taken@test.com");

        _userManagerMock.Setup(x => x.FindByIdAsync(targetUserId)).ReturnsAsync(targetUser);
        _userManagerMock.Setup(x => x.FindByNameAsync("takenuser")).ReturnsAsync(existingUser);

        // Act
        var act = () => _service.UpdateUserAsync(targetUserId, "takenuser", null, Constants.AuthRoles.User, currentUserId);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage(Constants.Errors.UsernameAlreadyExists);

        _userManagerMock.Verify(x => x.FindByIdAsync(targetUserId), Times.Once);
        _userManagerMock.Verify(x => x.FindByNameAsync("takenuser"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateUserAsync_ShouldThrowValidationException_WhenUsernameIsEmpty()
    {
        // Act
        var act = () => _service.UpdateUserAsync("user-1", "", null, Constants.AuthRoles.User, "admin-1");

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage(Constants.Errors.UsernameRequired);

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateUserAsync_ShouldThrowValidationException_WhenRoleIsInvalid()
    {
        // Act
        var act = () => _service.UpdateUserAsync("user-1", "validuser", null, "InvalidRole", "admin-1");

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage(Constants.Errors.InvalidRole);

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateUserAsync_ShouldThrowDomainException_WhenRemovingLastAdmin()
    {
        // Arrange
        var adminUserId = "admin-1";
        var adminUser = new ApplicationUser("admin", "admin@test.com") { Id = adminUserId };

        _userManagerMock.Setup(x => x.FindByIdAsync(adminUserId)).ReturnsAsync(adminUser);
        _userManagerMock.Setup(x => x.GetUsersInRoleAsync(Constants.AuthRoles.Admin))
            .ReturnsAsync(new List<ApplicationUser> { adminUser });

        // Act
        var act = () => _service.UpdateUserAsync(adminUserId, "admin", null, Constants.AuthRoles.User, adminUserId);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage(Constants.Errors.CannotRemoveLastAdmin);

        _userManagerMock.Verify(x => x.FindByIdAsync(adminUserId), Times.Once);
        _userManagerMock.Verify(x => x.GetUsersInRoleAsync(Constants.AuthRoles.Admin), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion
}
