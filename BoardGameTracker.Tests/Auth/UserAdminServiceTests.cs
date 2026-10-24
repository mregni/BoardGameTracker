using System.Collections.Generic;
using System.Threading.Tasks;
using BoardGameTracker.Common;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Entities.Auth;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Auth;
using BoardGameTracker.Core.Auth.Interfaces;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.Players.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Auth;

public class UserAdminServiceTests : IDisposable
{
    private readonly Mock<IUserStore<ApplicationUser>> _userStoreMock;
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<ITokenService> _tokenServiceMock = new();
    private readonly Mock<IUserAccessCheck> _accessCheckMock = new();
    private readonly MainDbContext _context;
    private readonly Mock<ILogger<UserAdminService>> _loggerMock;
    private readonly Mock<BoardGameTracker.Core.Images.Interfaces.IImageService> _imageServiceMock = new();
    private readonly Mock<IPlayerAccountImageService> _playerAccountImageServiceMock = new();
    private readonly UserAdminService _service;

    public UserAdminServiceTests()
    {
        _userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            _userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        _loggerMock = new Mock<ILogger<UserAdminService>>();

        var options = new DbContextOptionsBuilder<MainDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new MainDbContext(options);

        _service = new UserAdminService(
            _userManagerMock.Object,
            _tokenServiceMock.Object,
            _accessCheckMock.Object,
            _context,
            _imageServiceMock.Object,
            _playerAccountImageServiceMock.Object,
            _loggerMock.Object);

        _userManagerMock.Invocations.Clear();
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    private void VerifyNoOtherCalls()
    {
        _userManagerMock.VerifyNoOtherCalls();
        _tokenServiceMock.VerifyNoOtherCalls();
        _accessCheckMock.VerifyNoOtherCalls();
        _playerAccountImageServiceMock.VerifyNoOtherCalls();
        _loggerMock.VerifyNoOtherCalls();
    }

    private void VerifyInformationLogged()
    {
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private void VerifyInformationLogged(string currentUserId, string targetUserId)
    {
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains(currentUserId) && v.ToString()!.Contains(targetUserId)),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #region GetAllAsync

    [Fact]
    public async Task GetAllAsync_ShouldReturnUsersWithTheirRoles()
    {
        var user = new ApplicationUser("alice", "alice@test.com", "Alice");
        var role = new IdentityRole(Constants.AuthRoles.Admin);
        _context.Users.Add(user);
        _context.Roles.Add(role);
        _context.UserRoles.Add(new IdentityUserRole<string> { UserId = user.Id, RoleId = role.Id });
        await _context.SaveChangesAsync();

        var result = await _service.GetAllAsync();

        result.Should().ContainSingle();
        result[0].Username.Should().Be("alice");
        result[0].Email.Should().Be("alice@test.com");
        result[0].Roles.Should().ContainSingle().Which.Should().Be(Constants.AuthRoles.Admin);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetAllAsync_ShouldExcludeUsersWithoutRoles()
    {
        var withRole = new ApplicationUser("alice", "alice@test.com");
        var withoutRole = new ApplicationUser("bob", "bob@test.com");
        var role = new IdentityRole(Constants.AuthRoles.User);
        _context.Users.AddRange(withRole, withoutRole);
        _context.Roles.Add(role);
        _context.UserRoles.Add(new IdentityUserRole<string> { UserId = withRole.Id, RoleId = role.Id });
        await _context.SaveChangesAsync();

        var result = await _service.GetAllAsync();

        result.Should().ContainSingle().Which.Username.Should().Be("alice");
        VerifyNoOtherCalls();
    }

    #endregion

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
        _accessCheckMock.Verify(x => x.Forget(targetUserId), Times.Once);
        _playerAccountImageServiceMock.Verify(x => x.ReleaseAsync(null), Times.Once);
        VerifyInformationLogged(currentUserId, targetUserId);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeleteUser_WhenSingleAdminExistsButTargetIsNotAdmin()
    {
        var currentUserId = "admin-1";
        var targetUserId = "user-2";
        var targetUser = new ApplicationUser("regularuser", "regular@test.com") { Id = targetUserId };
        var onlyAdmin = new ApplicationUser("theadmin", "admin@test.com") { Id = currentUserId };

        _userManagerMock
            .Setup(x => x.FindByIdAsync(targetUserId))
            .ReturnsAsync(targetUser);

        _userManagerMock
            .Setup(x => x.GetUsersInRoleAsync(Constants.AuthRoles.Admin))
            .ReturnsAsync(new List<ApplicationUser> { onlyAdmin });

        _userManagerMock
            .Setup(x => x.DeleteAsync(targetUser))
            .ReturnsAsync(IdentityResult.Success);

        await _service.DeleteAsync(targetUserId, currentUserId);

        _userManagerMock.Verify(x => x.FindByIdAsync(targetUserId), Times.Once);
        _userManagerMock.Verify(x => x.GetUsersInRoleAsync(Constants.AuthRoles.Admin), Times.Once);
        _userManagerMock.Verify(x => x.DeleteAsync(targetUser), Times.Once);
        _accessCheckMock.Verify(x => x.Forget(targetUserId), Times.Once);
        _playerAccountImageServiceMock.Verify(x => x.ReleaseAsync(null), Times.Once);
        VerifyInformationLogged(currentUserId, targetUserId);
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
        var currentUserId = "admin-1";
        var targetUserId = "user-2";
        var targetUser = new ApplicationUser("targetuser", "target@test.com");
        var currentRoles = new List<string> { Constants.AuthRoles.User };
        var updatedRoles = new List<string> { Constants.AuthRoles.Reader };

        _userManagerMock.Setup(x => x.FindByIdAsync(targetUserId)).ReturnsAsync(targetUser);
        _userManagerMock.Setup(x => x.RemoveFromRolesAsync(targetUser, currentRoles)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.AddToRoleAsync(targetUser, Constants.AuthRoles.Reader)).ReturnsAsync(IdentityResult.Success);

        _userManagerMock.SetupSequence(x => x.GetRolesAsync(targetUser))
            .ReturnsAsync(currentRoles)
            .ReturnsAsync(updatedRoles);

        var result = await _service.UpdateRoleAsync(targetUserId, Constants.AuthRoles.Reader, currentUserId);

        result.Should().NotBeNull();
        result.Roles.Should().Contain(Constants.AuthRoles.Reader);

        _userManagerMock.Verify(x => x.FindByIdAsync(targetUserId), Times.Once);
        _userManagerMock.Verify(x => x.GetRolesAsync(targetUser), Times.Exactly(2));
        _userManagerMock.Verify(x => x.RemoveFromRolesAsync(targetUser, currentRoles), Times.Once);
        _userManagerMock.Verify(x => x.AddToRoleAsync(targetUser, Constants.AuthRoles.Reader), Times.Once);
        VerifyInformationLogged(currentUserId, targetUserId);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateRoleAsync_ShouldThrowValidationException_WhenRoleIsInvalid()
    {
        var act = () => _service.UpdateRoleAsync("user-1", "InvalidRole", "admin-1");

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage(Constants.Errors.InvalidRole);

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateRoleAsync_ShouldThrowEntityNotFoundException_WhenUserDoesNotExist()
    {
        _userManagerMock
            .Setup(x => x.FindByIdAsync("missing-user"))
            .ReturnsAsync((ApplicationUser?)null);

        var act = () => _service.UpdateRoleAsync("missing-user", Constants.AuthRoles.User, "admin-1");

        await act.Should().ThrowAsync<EntityNotFoundException>()
            .Where(e => e.EntityType == nameof(ApplicationUser) && e.EntityId.Equals("missing-user"));

        _userManagerMock.Verify(x => x.FindByIdAsync("missing-user"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateRoleAsync_ShouldThrowDomainException_WhenLastAdminDemotesSelf()
    {
        var adminUserId = "admin-1";
        var adminUser = new ApplicationUser("admin", "admin@test.com") { Id = adminUserId };

        _userManagerMock.Setup(x => x.FindByIdAsync(adminUserId)).ReturnsAsync(adminUser);
        _userManagerMock.Setup(x => x.GetUsersInRoleAsync(Constants.AuthRoles.Admin))
            .ReturnsAsync(new List<ApplicationUser> { adminUser });

        var act = () => _service.UpdateRoleAsync(adminUserId, Constants.AuthRoles.User, adminUserId);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage(Constants.Errors.CannotRemoveLastAdmin);

        _userManagerMock.Verify(x => x.FindByIdAsync(adminUserId), Times.Once);
        _userManagerMock.Verify(x => x.GetUsersInRoleAsync(Constants.AuthRoles.Admin), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateRoleAsync_ShouldAllowSelfDemotion_WhenAnotherAdminExists()
    {
        var adminUserId = "admin-1";
        var adminUser = new ApplicationUser("admin", "admin@test.com") { Id = adminUserId };
        var otherAdmin = new ApplicationUser("otheradmin", "other@test.com");
        var currentRoles = new List<string> { Constants.AuthRoles.Admin };
        var updatedRoles = new List<string> { Constants.AuthRoles.User };

        _userManagerMock.Setup(x => x.FindByIdAsync(adminUserId)).ReturnsAsync(adminUser);
        _userManagerMock.Setup(x => x.GetUsersInRoleAsync(Constants.AuthRoles.Admin))
            .ReturnsAsync(new List<ApplicationUser> { adminUser, otherAdmin });
        _userManagerMock.Setup(x => x.RemoveFromRolesAsync(adminUser, currentRoles)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.AddToRoleAsync(adminUser, Constants.AuthRoles.User)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.SetupSequence(x => x.GetRolesAsync(adminUser))
            .ReturnsAsync(currentRoles)
            .ReturnsAsync(updatedRoles);

        var result = await _service.UpdateRoleAsync(adminUserId, Constants.AuthRoles.User, adminUserId);

        result.Roles.Should().Contain(Constants.AuthRoles.User);

        _userManagerMock.Verify(x => x.FindByIdAsync(adminUserId), Times.Once);
        _userManagerMock.Verify(x => x.GetUsersInRoleAsync(Constants.AuthRoles.Admin), Times.Once);
        _userManagerMock.Verify(x => x.GetRolesAsync(adminUser), Times.Exactly(2));
        _userManagerMock.Verify(x => x.RemoveFromRolesAsync(adminUser, currentRoles), Times.Once);
        _userManagerMock.Verify(x => x.AddToRoleAsync(adminUser, Constants.AuthRoles.User), Times.Once);
        VerifyInformationLogged(adminUserId, adminUserId);
        VerifyNoOtherCalls();
    }

    #endregion

    #region UpdateUserAsync

    [Fact]
    public async Task UpdateUserAsync_ShouldUpdateAllFields_WhenValidDataProvided()
    {
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

        var result = await _service.UpdateUserAsync(targetUserId, "newuser", "new@test.com", Constants.AuthRoles.Reader, null, currentUserId);

        result.Should().NotBeNull();
        result.Roles.Should().Contain(Constants.AuthRoles.Reader);

        _userManagerMock.Verify(x => x.FindByIdAsync(targetUserId), Times.Once);
        _userManagerMock.Verify(x => x.FindByNameAsync("newuser"), Times.Once);
        _userManagerMock.Verify(x => x.SetUserNameAsync(targetUser, "newuser"), Times.Once);
        _userManagerMock.Verify(x => x.UpdateAsync(targetUser), Times.Once);
        _playerAccountImageServiceMock.Verify(x => x.RelinkAsync(It.IsAny<int?>(), targetUser), Times.Once);
        _userManagerMock.Verify(x => x.GetRolesAsync(targetUser), Times.Exactly(2));
        _userManagerMock.Verify(x => x.RemoveFromRolesAsync(targetUser, currentRoles), Times.Once);
        _userManagerMock.Verify(x => x.AddToRoleAsync(targetUser, Constants.AuthRoles.Reader), Times.Once);
        VerifyInformationLogged(currentUserId, targetUserId);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateUserAsync_ShouldSkipUsernameChange_WhenUsernameIsSame()
    {
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

        var result = await _service.UpdateUserAsync(targetUserId, "sameuser", "new@test.com", Constants.AuthRoles.Admin, null, currentUserId);

        result.Should().NotBeNull();
        result.Roles.Should().Contain(Constants.AuthRoles.Admin);

        _userManagerMock.Verify(x => x.FindByIdAsync(targetUserId), Times.Once);
        _userManagerMock.Verify(x => x.UpdateAsync(targetUser), Times.Once);
        _playerAccountImageServiceMock.Verify(x => x.RelinkAsync(It.IsAny<int?>(), targetUser), Times.Once);
        _userManagerMock.Verify(x => x.GetRolesAsync(targetUser), Times.Exactly(2));
        _userManagerMock.Verify(x => x.RemoveFromRolesAsync(targetUser, currentRoles), Times.Once);
        _userManagerMock.Verify(x => x.AddToRoleAsync(targetUser, Constants.AuthRoles.Admin), Times.Once);
        VerifyInformationLogged(currentUserId, targetUserId);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateUserAsync_ShouldSkipUsernameChange_WhenUsernameDiffersOnlyByCase()
    {
        var currentUserId = "admin-1";
        var targetUserId = "user-2";
        var targetUser = new ApplicationUser("sameuser", "old@test.com");
        var currentRoles = new List<string> { Constants.AuthRoles.User };

        _userManagerMock.Setup(x => x.FindByIdAsync(targetUserId)).ReturnsAsync(targetUser);
        _userManagerMock.Setup(x => x.UpdateAsync(targetUser)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.SetupSequence(x => x.GetRolesAsync(targetUser))
            .ReturnsAsync(currentRoles)
            .ReturnsAsync(currentRoles);

        await _service.UpdateUserAsync(targetUserId, "SAMEUSER", "old@test.com", Constants.AuthRoles.User, null, currentUserId);

        _userManagerMock.Verify(x => x.FindByNameAsync(It.IsAny<string>()), Times.Never);
        _userManagerMock.Verify(x => x.SetUserNameAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
        _userManagerMock.Verify(x => x.FindByIdAsync(targetUserId), Times.Once);
        _userManagerMock.Verify(x => x.UpdateAsync(targetUser), Times.Once);
        _playerAccountImageServiceMock.Verify(x => x.RelinkAsync(It.IsAny<int?>(), targetUser), Times.Once);
        _userManagerMock.Verify(x => x.GetRolesAsync(targetUser), Times.Exactly(2));
        _userManagerMock.Verify(x => x.RemoveFromRolesAsync(targetUser, It.IsAny<IEnumerable<string>>()), Times.Never);
        _userManagerMock.Verify(x => x.AddToRoleAsync(targetUser, It.IsAny<string>()), Times.Never);
        VerifyInformationLogged(currentUserId, targetUserId);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateUserAsync_ShouldThrowEntityNotFoundException_WhenUserDoesNotExist()
    {
        _userManagerMock
            .Setup(x => x.FindByIdAsync("missing-user"))
            .ReturnsAsync((ApplicationUser?)null);

        var act = () => _service.UpdateUserAsync("missing-user", "validuser", null, Constants.AuthRoles.User, null, "admin-1");

        await act.Should().ThrowAsync<EntityNotFoundException>()
            .Where(e => e.EntityType == nameof(ApplicationUser) && e.EntityId.Equals("missing-user"));

        _userManagerMock.Verify(x => x.FindByIdAsync("missing-user"), Times.Once);
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
        var act = () => _service.UpdateUserAsync(targetUserId, "takenuser", null, Constants.AuthRoles.User, null, currentUserId);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage(Constants.Errors.UsernameAlreadyExists);

        _userManagerMock.Verify(x => x.FindByIdAsync(targetUserId), Times.Once);
        _userManagerMock.Verify(x => x.FindByNameAsync("takenuser"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("", Constants.AuthRoles.User, Constants.Errors.UsernameRequired)]
    [InlineData("validuser", "InvalidRole", Constants.Errors.InvalidRole)]
    public async Task UpdateUserAsync_ShouldThrowValidationException_WhenInputIsInvalid(string username, string role, string expectedMessage)
    {
        var act = () => _service.UpdateUserAsync("user-1", username, null, role, null, "admin-1");

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage(expectedMessage);

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
        var act = () => _service.UpdateUserAsync(adminUserId, "admin", null, Constants.AuthRoles.User, null, adminUserId);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage(Constants.Errors.CannotRemoveLastAdmin);

        _userManagerMock.Verify(x => x.FindByIdAsync(adminUserId), Times.Once);
        _userManagerMock.Verify(x => x.GetUsersInRoleAsync(Constants.AuthRoles.Admin), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateUserAsync_ShouldLinkPlayer_WhenPlayerIdProvided()
    {
        _context.Players.Add(new Player("Target") { Id = 5 });
        await _context.SaveChangesAsync();

        var currentUserId = "admin-1";
        var targetUserId = "user-2";
        var targetUser = new ApplicationUser("bob", "b@test.com");
        targetUser.AwaitPlayerLink();
        var roles = new List<string> { Constants.AuthRoles.User };

        _userManagerMock.Setup(x => x.FindByIdAsync(targetUserId)).ReturnsAsync(targetUser);
        _userManagerMock.Setup(x => x.UpdateAsync(targetUser)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.SetupSequence(x => x.GetRolesAsync(targetUser)).ReturnsAsync(roles).ReturnsAsync(roles);

        var result = await _service.UpdateUserAsync(targetUserId, "bob", "b@test.com", Constants.AuthRoles.User, 5, currentUserId);

        result.PlayerId.Should().Be(5);
        targetUser.PlayerId.Should().Be(5);
        targetUser.PlayerLinkPending.Should().BeFalse();
        _playerAccountImageServiceMock.Verify(x => x.RelinkAsync(null, targetUser), Times.Once);

        _userManagerMock.Verify(x => x.FindByIdAsync(targetUserId), Times.Once);
        _userManagerMock.Verify(x => x.UpdateAsync(targetUser), Times.Once);
        _playerAccountImageServiceMock.Verify(x => x.RelinkAsync(It.IsAny<int?>(), targetUser), Times.Once);
        _userManagerMock.Verify(x => x.GetRolesAsync(targetUser), Times.Exactly(2));
        _userManagerMock.Verify(x => x.RemoveFromRolesAsync(targetUser, It.IsAny<IEnumerable<string>>()), Times.Never);
        _userManagerMock.Verify(x => x.AddToRoleAsync(targetUser, It.IsAny<string>()), Times.Never);
        VerifyInformationLogged();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateUserAsync_ShouldUnlinkPlayer_WhenPlayerIdIsNull()
    {
        var currentUserId = "admin-1";
        var targetUserId = "user-2";
        var targetUser = new ApplicationUser("bob", "b@test.com");
        targetUser.LinkPlayer(5);
        var roles = new List<string> { Constants.AuthRoles.User };

        _userManagerMock.Setup(x => x.FindByIdAsync(targetUserId)).ReturnsAsync(targetUser);
        _userManagerMock.Setup(x => x.UpdateAsync(targetUser)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.SetupSequence(x => x.GetRolesAsync(targetUser)).ReturnsAsync(roles).ReturnsAsync(roles);

        var result = await _service.UpdateUserAsync(targetUserId, "bob", "b@test.com", Constants.AuthRoles.User, null, currentUserId);

        result.PlayerId.Should().BeNull();
        targetUser.PlayerId.Should().BeNull();
        _playerAccountImageServiceMock.Verify(x => x.RelinkAsync(5, targetUser), Times.Once);

        _userManagerMock.Verify(x => x.FindByIdAsync(targetUserId), Times.Once);
        _userManagerMock.Verify(x => x.UpdateAsync(targetUser), Times.Once);
        _playerAccountImageServiceMock.Verify(x => x.RelinkAsync(It.IsAny<int?>(), targetUser), Times.Once);
        _userManagerMock.Verify(x => x.GetRolesAsync(targetUser), Times.Exactly(2));
        _userManagerMock.Verify(x => x.RemoveFromRolesAsync(targetUser, It.IsAny<IEnumerable<string>>()), Times.Never);
        _userManagerMock.Verify(x => x.AddToRoleAsync(targetUser, It.IsAny<string>()), Times.Never);
        VerifyInformationLogged();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateUserAsync_ShouldMoveThePlayer_WhenItIsLinkedToAnotherUser()
    {
        _context.Players.Add(new Player("Target") { Id = 5 });
        var otherUser = new ApplicationUser("other", "o@test.com");
        otherUser.LinkPlayer(5);
        _context.Users.Add(otherUser);
        await _context.SaveChangesAsync();

        var currentUserId = "admin-1";
        var targetUserId = "user-2";
        var targetUser = new ApplicationUser("bob", "b@test.com");
        var roles = new List<string> { Constants.AuthRoles.User };
        _userManagerMock.Setup(x => x.FindByIdAsync(targetUserId)).ReturnsAsync(targetUser);
        _userManagerMock.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.SetupSequence(x => x.GetRolesAsync(targetUser)).ReturnsAsync(roles).ReturnsAsync(roles);

        var result = await _service.UpdateUserAsync(targetUserId, "bob", "b@test.com", Constants.AuthRoles.User, 5, currentUserId);

        result.PlayerId.Should().Be(5);
        otherUser.PlayerId.Should().BeNull();
        _userManagerMock.Verify(x => x.FindByIdAsync(targetUserId), Times.Once);
        _userManagerMock.Verify(x => x.UpdateAsync(otherUser), Times.Once);
        _userManagerMock.Verify(x => x.UpdateAsync(targetUser), Times.Once);
        _playerAccountImageServiceMock.Verify(x => x.RelinkAsync(null, targetUser), Times.Once);
        _userManagerMock.Verify(x => x.GetRolesAsync(targetUser), Times.Exactly(2));
        VerifyInformationLogged();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateUserAsync_ShouldThrowDomainException_WhenTheMovedLinkCannotBeSaved()
    {
        _context.Players.Add(new Player("Target") { Id = 5 });
        var otherUser = new ApplicationUser("other", "o@test.com");
        otherUser.LinkPlayer(5);
        _context.Users.Add(otherUser);
        await _context.SaveChangesAsync();

        var targetUserId = "user-2";
        var targetUser = new ApplicationUser("bob", "b@test.com");
        _userManagerMock.Setup(x => x.FindByIdAsync(targetUserId)).ReturnsAsync(targetUser);
        _userManagerMock.Setup(x => x.UpdateAsync(otherUser)).ReturnsAsync(IdentityResult.Failed());

        var act = () => _service.UpdateUserAsync(targetUserId, "bob", "b@test.com", Constants.AuthRoles.User, 5, "admin-1");

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.PlayerLinkUpdateFailed);
        _userManagerMock.Verify(x => x.FindByIdAsync(targetUserId), Times.Once);
        _userManagerMock.Verify(x => x.UpdateAsync(otherUser), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateUserAsync_ShouldThrow_WhenThePlayerDoesNotExist()
    {
        var targetUserId = "user-2";
        var targetUser = new ApplicationUser("bob", "b@test.com");
        _userManagerMock.Setup(x => x.FindByIdAsync(targetUserId)).ReturnsAsync(targetUser);

        var act = () => _service.UpdateUserAsync(targetUserId, "bob", "b@test.com", Constants.AuthRoles.User, 99, "admin-1");

        await act.Should().ThrowAsync<EntityNotFoundException>();
        _userManagerMock.Verify(x => x.FindByIdAsync(targetUserId), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region Deprovisioning

    private ApplicationUser AddUser(string id, string? passwordHash = null)
    {
        var user = new ApplicationUser(id, $"{id}@test.com") { Id = id, PasswordHash = passwordHash };
        _context.Users.Add(user);
        _context.SaveChanges();
        return user;
    }

    [Fact]
    public async Task GetAllAsync_ShouldCountTheLinkedIdentitiesAndReportDisabledUsers()
    {
        var user = new ApplicationUser("alice", "alice@test.com") { LockoutEnd = DateTimeOffset.MaxValue };
        user.ManageRoleByIdentityProvider();
        var role = new IdentityRole(Constants.AuthRoles.User);
        _context.Users.Add(user);
        _context.Roles.Add(role);
        _context.UserRoles.Add(new IdentityUserRole<string> { UserId = user.Id, RoleId = role.Id });
        _context.ExternalLogins.Add(new ExternalLogin(user.Id, "kc", "https://sso.example.com", "sub-1"));
        _context.ExternalLogins.Add(new ExternalLogin(user.Id, "kc", "https://other.example.com", "sub-1"));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _service.GetAllAsync();

        result.Should().ContainSingle();
        result[0].ExternalLoginCount.Should().Be(2);
        result[0].IsDisabled.Should().BeTrue();
        result[0].RoleManagedByIdentityProvider.Should().BeTrue();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DisableAsync_ShouldLockTheUserOut_RevokeTheirTokens_AndDropTheirOidcSessions()
    {
        var user = AddUser("user-2");
        _context.OidcSessions.Add(new OidcSession("hash", "user-2", 1, "token", DateTime.UtcNow.AddDays(1)));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        _userManagerMock.Setup(x => x.FindByIdAsync("user-2")).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.IsInRoleAsync(user, Constants.AuthRoles.Admin)).ReturnsAsync(false);
        _userManagerMock.Setup(x => x.SetLockoutEnabledAsync(user, true)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue))
            .Callback(() => user.LockoutEnd = DateTimeOffset.MaxValue)
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync([Constants.AuthRoles.User]);

        var result = await _service.DisableAsync("user-2", "admin-1");

        result.IsDisabled.Should().BeTrue();
        (await _context.OidcSessions.CountAsync(TestContext.Current.CancellationToken)).Should().Be(0);
        _userManagerMock.Verify(x => x.FindByIdAsync("user-2"), Times.Once);
        _userManagerMock.Verify(x => x.IsInRoleAsync(user, Constants.AuthRoles.Admin), Times.Once);
        _userManagerMock.Verify(x => x.SetLockoutEnabledAsync(user, true), Times.Once);
        _userManagerMock.Verify(x => x.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue), Times.Once);
        _userManagerMock.Verify(x => x.GetRolesAsync(user), Times.Once);
        _tokenServiceMock.Verify(x => x.RevokeAllUserTokensAsync("user-2", "Account disabled"), Times.Once);
        _accessCheckMock.Verify(x => x.Forget("user-2"), Times.Once);
        VerifyInformationLogged("admin-1", "user-2");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DisableAsync_ShouldRefuseToDisableTheCurrentUser()
    {
        var act = () => _service.DisableAsync("admin-1", "admin-1");

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.CannotDisableSelf);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DisableAsync_ShouldThrow_WhenTheUserDoesNotExist()
    {
        var act = () => _service.DisableAsync("missing", "admin-1");

        await act.Should().ThrowAsync<EntityNotFoundException>();
        _userManagerMock.Verify(x => x.FindByIdAsync("missing"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DisableAsync_ShouldRefuseToDisableTheLastActiveAdmin()
    {
        var admin = new ApplicationUser("admin-2", null) { Id = "admin-2" };
        var disabledAdmin = new ApplicationUser("admin-3", null) { Id = "admin-3", LockoutEnd = DateTimeOffset.MaxValue };
        _userManagerMock.Setup(x => x.FindByIdAsync("admin-2")).ReturnsAsync(admin);
        _userManagerMock.Setup(x => x.IsInRoleAsync(admin, Constants.AuthRoles.Admin)).ReturnsAsync(true);
        _userManagerMock.Setup(x => x.GetUsersInRoleAsync(Constants.AuthRoles.Admin)).ReturnsAsync([admin, disabledAdmin]);

        var act = () => _service.DisableAsync("admin-2", "admin-1");

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.CannotDisableLastAdmin);
        _userManagerMock.Verify(x => x.FindByIdAsync("admin-2"), Times.Once);
        _userManagerMock.Verify(x => x.IsInRoleAsync(admin, Constants.AuthRoles.Admin), Times.Once);
        _userManagerMock.Verify(x => x.GetUsersInRoleAsync(Constants.AuthRoles.Admin), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task EnableAsync_ShouldClearTheLockout()
    {
        var user = AddUser("user-2");
        user.LockoutEnd = DateTimeOffset.MaxValue;
        _userManagerMock.Setup(x => x.FindByIdAsync("user-2")).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.SetLockoutEndDateAsync(user, null))
            .Callback(() => user.LockoutEnd = null)
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync([Constants.AuthRoles.User]);

        var result = await _service.EnableAsync("user-2", "admin-1");

        result.IsDisabled.Should().BeFalse();
        _userManagerMock.Verify(x => x.FindByIdAsync("user-2"), Times.Once);
        _userManagerMock.Verify(x => x.SetLockoutEndDateAsync(user, null), Times.Once);
        _userManagerMock.Verify(x => x.GetRolesAsync(user), Times.Once);
        _accessCheckMock.Verify(x => x.Forget("user-2"), Times.Once);
        VerifyInformationLogged("admin-1", "user-2");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task EnableAsync_ShouldThrow_WhenTheUserDoesNotExist()
    {
        var act = () => _service.EnableAsync("missing", "admin-1");

        await act.Should().ThrowAsync<EntityNotFoundException>();
        _userManagerMock.Verify(x => x.FindByIdAsync("missing"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetExternalLoginsAsync_ShouldListTheLinkedIdentitiesOfThatUser()
    {
        AddUser("user-2");
        _context.ExternalLogins.Add(new ExternalLogin("user-2", "kc", "https://sso.example.com", "sub-1", "Jane"));
        _context.ExternalLogins.Add(new ExternalLogin("user-3", "kc", "https://sso.example.com", "sub-2"));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _service.GetExternalLoginsAsync("user-2");

        result.Should().ContainSingle(x => x.ProviderKey == "sub-1" && x.ProviderDisplayName == "Jane");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetExternalLoginsAsync_ShouldThrow_WhenTheUserDoesNotExist()
    {
        var act = () => _service.GetExternalLoginsAsync("missing");

        await act.Should().ThrowAsync<EntityNotFoundException>();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UnlinkExternalLoginAsync_ShouldRemoveTheLink_EvenIfItWasTheOnlySignInMethod()
    {
        AddUser("user-2");
        var login = new ExternalLogin("user-2", "kc", "https://sso.example.com", "sub-1");
        _context.ExternalLogins.Add(login);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await _service.UnlinkExternalLoginAsync("user-2", login.Id, "admin-1");

        (await _context.ExternalLogins.CountAsync(TestContext.Current.CancellationToken)).Should().Be(0);
        VerifyInformationLogged("admin-1", "user-2");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UnlinkExternalLoginAsync_ShouldThrow_WhenTheLinkBelongsToAnotherUser()
    {
        var login = new ExternalLogin("user-3", "kc", "https://sso.example.com", "sub-1");
        _context.ExternalLogins.Add(login);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var act = () => _service.UnlinkExternalLoginAsync("user-2", login.Id, "admin-1");

        await act.Should().ThrowAsync<EntityNotFoundException>();
        (await _context.ExternalLogins.CountAsync(TestContext.Current.CancellationToken)).Should().Be(1);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ManageRoleByIdentityProviderAsync_ShouldHandTheRoleBackToTheIdentityProvider()
    {
        var user = AddUser("user-2");
        _context.ExternalLogins.Add(new ExternalLogin("user-2", "kc", "https://sso.example.com", "sub-1"));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        _userManagerMock.Setup(x => x.FindByIdAsync("user-2")).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync([Constants.AuthRoles.User]);

        var result = await _service.ManageRoleByIdentityProviderAsync("user-2", "admin-1");

        result.RoleManagedByIdentityProvider.Should().BeTrue();
        result.ExternalLoginCount.Should().Be(1);
        _userManagerMock.Verify(x => x.FindByIdAsync("user-2"), Times.Once);
        _userManagerMock.Verify(x => x.UpdateAsync(It.Is<ApplicationUser>(u => u.RoleManagedByIdentityProvider)), Times.Once);
        _userManagerMock.Verify(x => x.GetRolesAsync(user), Times.Once);
        VerifyInformationLogged("admin-1", "user-2");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ManageRoleByIdentityProviderAsync_ShouldRefuse_ForAUserWithoutLinkedIdentities()
    {
        var user = AddUser("user-2");
        _userManagerMock.Setup(x => x.FindByIdAsync("user-2")).ReturnsAsync(user);

        var act = () => _service.ManageRoleByIdentityProviderAsync("user-2", "admin-1");

        await act.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.OidcRoleNotManaged);
        _userManagerMock.Verify(x => x.FindByIdAsync("user-2"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateRoleAsync_ShouldMakeTheRoleALocalOverride_WhenItChanges()
    {
        var user = new ApplicationUser("jane", "jane@test.com") { Id = "user-2" };
        user.ManageRoleByIdentityProvider();
        _userManagerMock.Setup(x => x.FindByIdAsync("user-2")).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync([Constants.AuthRoles.User]);
        _userManagerMock.Setup(x => x.AddToRoleAsync(user, Constants.AuthRoles.Reader)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.RemoveFromRolesAsync(user, It.IsAny<IEnumerable<string>>())).ReturnsAsync(IdentityResult.Success);

        await _service.UpdateRoleAsync("user-2", Constants.AuthRoles.Reader, "admin-1");

        user.RoleManagedByIdentityProvider.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateRoleAsync_ShouldKeepTheIdentityProviderInCharge_WhenTheRoleIsUnchanged()
    {
        var user = new ApplicationUser("jane", "jane@test.com") { Id = "user-2" };
        user.ManageRoleByIdentityProvider();
        _userManagerMock.Setup(x => x.FindByIdAsync("user-2")).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync([Constants.AuthRoles.User]);

        await _service.UpdateRoleAsync("user-2", Constants.AuthRoles.User, "admin-1");

        user.RoleManagedByIdentityProvider.Should().BeTrue();
    }

    #endregion

    #region LinkPlayerAccountAsync

    private async Task AddPlayerAsync(int id)
    {
        _context.Players.Add(new Player($"Player {id}") { Id = id });
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task DeleteAsync_ShouldReleaseTheLinkedPlayersAccountPicture()
    {
        var target = new ApplicationUser("linked", "linked@test.com") { Id = "user-2" };
        target.LinkPlayer(5);
        _userManagerMock.Setup(x => x.FindByIdAsync("user-2")).ReturnsAsync(target);
        _userManagerMock.Setup(x => x.GetUsersInRoleAsync(Constants.AuthRoles.Admin)).ReturnsAsync(new List<ApplicationUser>());
        _userManagerMock.Setup(x => x.DeleteAsync(target)).ReturnsAsync(IdentityResult.Success);

        await _service.DeleteAsync("user-2", "admin-1");

        _userManagerMock.Verify(x => x.FindByIdAsync("user-2"), Times.Once);
        _userManagerMock.Verify(x => x.GetUsersInRoleAsync(Constants.AuthRoles.Admin), Times.Once);
        _userManagerMock.Verify(x => x.DeleteAsync(target), Times.Once);
        _accessCheckMock.Verify(x => x.Forget("user-2"), Times.Once);
        _playerAccountImageServiceMock.Verify(x => x.ReleaseAsync(5), Times.Once);
        VerifyInformationLogged("admin-1", "user-2");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task LinkPlayerAccountAsync_ShouldLinkTheAccount_AndApplyItsPicture()
    {
        await AddPlayerAsync(5);
        var target = new ApplicationUser("bob", "b@test.com") { Id = "user-2" };
        target.AwaitPlayerLink();
        _userManagerMock.Setup(x => x.FindByIdAsync("user-2")).ReturnsAsync(target);
        _userManagerMock.Setup(x => x.UpdateAsync(target)).ReturnsAsync(IdentityResult.Success);
        _playerAccountImageServiceMock.Setup(x => x.ApplyAccountImageAsync(target)).ReturnsAsync(true);

        await _service.LinkPlayerAccountAsync(5, "user-2");

        target.PlayerId.Should().Be(5);
        target.PlayerLinkPending.Should().BeFalse();
        target.NeedsPlayerLink.Should().BeFalse();
        _userManagerMock.Verify(x => x.FindByIdAsync("user-2"), Times.Once);
        _userManagerMock.Verify(x => x.UpdateAsync(target), Times.Once);
        _playerAccountImageServiceMock.Verify(x => x.ReleaseAsync(null), Times.Once);
        _playerAccountImageServiceMock.Verify(x => x.ApplyAccountImageAsync(target), Times.Once);
        VerifyInformationLogged();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task LinkPlayerAccountAsync_ShouldMoveTheLink_FromTheOtherPlayerAndAccount()
    {
        await AddPlayerAsync(5);
        await AddPlayerAsync(7);
        var current = new ApplicationUser("old", "old@test.com") { Id = "user-1" };
        current.LinkPlayer(5);
        _context.Users.Add(current);
        await _context.SaveChangesAsync();
        var target = new ApplicationUser("bob", "b@test.com") { Id = "user-2" };
        target.LinkPlayer(7);
        _userManagerMock.Setup(x => x.FindByIdAsync("user-2")).ReturnsAsync(target);
        _userManagerMock.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);
        _playerAccountImageServiceMock.Setup(x => x.ApplyAccountImageAsync(target)).ReturnsAsync(true);

        await _service.LinkPlayerAccountAsync(5, "user-2");

        current.PlayerId.Should().BeNull();
        target.PlayerId.Should().Be(5);
        _userManagerMock.Verify(x => x.FindByIdAsync("user-2"), Times.Once);
        _userManagerMock.Verify(x => x.UpdateAsync(current), Times.Once);
        _userManagerMock.Verify(x => x.UpdateAsync(target), Times.Once);
        _playerAccountImageServiceMock.Verify(x => x.ReleaseAsync(7), Times.Once);
        _playerAccountImageServiceMock.Verify(x => x.ApplyAccountImageAsync(target), Times.Once);
        VerifyInformationLogged();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task LinkPlayerAccountAsync_ShouldReleaseThePlayer_WhenTheNewAccountPictureCannotBeApplied()
    {
        await AddPlayerAsync(5);
        var target = new ApplicationUser("bob", "b@test.com") { Id = "user-2" };
        _userManagerMock.Setup(x => x.FindByIdAsync("user-2")).ReturnsAsync(target);
        _userManagerMock.Setup(x => x.UpdateAsync(target)).ReturnsAsync(IdentityResult.Success);
        _playerAccountImageServiceMock.Setup(x => x.ApplyAccountImageAsync(target)).ReturnsAsync(false);

        await _service.LinkPlayerAccountAsync(5, "user-2");

        _userManagerMock.Verify(x => x.FindByIdAsync("user-2"), Times.Once);
        _userManagerMock.Verify(x => x.UpdateAsync(target), Times.Once);
        _playerAccountImageServiceMock.Verify(x => x.ReleaseAsync(null), Times.Once);
        _playerAccountImageServiceMock.Verify(x => x.ApplyAccountImageAsync(target), Times.Once);
        _playerAccountImageServiceMock.Verify(x => x.ReleaseAsync(5), Times.Once);
        VerifyInformationLogged();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task LinkPlayerAccountAsync_ShouldUnlinkTheCurrentAccount_WhenNoAccountIsGiven()
    {
        await AddPlayerAsync(5);
        var current = new ApplicationUser("old", "old@test.com") { Id = "user-1" };
        current.LinkPlayer(5);
        _context.Users.Add(current);
        await _context.SaveChangesAsync();
        _userManagerMock.Setup(x => x.UpdateAsync(current)).ReturnsAsync(IdentityResult.Success);

        await _service.LinkPlayerAccountAsync(5, null);

        current.PlayerId.Should().BeNull();
        _userManagerMock.Verify(x => x.UpdateAsync(current), Times.Once);
        _playerAccountImageServiceMock.Verify(x => x.ReleaseAsync(5), Times.Once);
        VerifyInformationLogged();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task LinkPlayerAccountAsync_ShouldDoNothing_WhenTheAccountIsAlreadyLinked()
    {
        await AddPlayerAsync(5);
        var current = new ApplicationUser("old", "old@test.com") { Id = "user-1" };
        current.LinkPlayer(5);
        _context.Users.Add(current);
        await _context.SaveChangesAsync();
        _userManagerMock.Setup(x => x.FindByIdAsync("user-1")).ReturnsAsync(current);

        await _service.LinkPlayerAccountAsync(5, "user-1");

        current.PlayerId.Should().Be(5);
        _userManagerMock.Verify(x => x.FindByIdAsync("user-1"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task LinkPlayerAccountAsync_ShouldThrow_WhenThePlayerDoesNotExist()
    {
        var act = () => _service.LinkPlayerAccountAsync(42, "user-2");

        await act.Should().ThrowAsync<EntityNotFoundException>()
            .Where(e => e.EntityType == nameof(Player));
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task LinkPlayerAccountAsync_ShouldThrow_WhenTheAccountDoesNotExist()
    {
        await AddPlayerAsync(5);
        _userManagerMock.Setup(x => x.FindByIdAsync("missing")).ReturnsAsync((ApplicationUser?)null);

        var act = () => _service.LinkPlayerAccountAsync(5, "missing");

        await act.Should().ThrowAsync<EntityNotFoundException>()
            .Where(e => e.EntityType == nameof(ApplicationUser));
        _userManagerMock.Verify(x => x.FindByIdAsync("missing"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task LinkPlayerAccountAsync_ShouldThrowAClearError_WhenTheLinkCannotBeSaved()
    {
        await AddPlayerAsync(5);
        var target = new ApplicationUser("bob", "b@test.com") { Id = "user-2" };
        _userManagerMock.Setup(x => x.FindByIdAsync("user-2")).ReturnsAsync(target);
        _userManagerMock.Setup(x => x.UpdateAsync(target)).ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "ConcurrencyFailure" }));

        var act = () => _service.LinkPlayerAccountAsync(5, "user-2");

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.PlayerLinkUpdateFailed);
        _userManagerMock.Verify(x => x.FindByIdAsync("user-2"), Times.Once);
        _userManagerMock.Verify(x => x.UpdateAsync(target), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion
}
