using System.Linq;
using System.Threading.Tasks;
using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Entities.Auth;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Auth;
using BoardGameTracker.Core.Auth.Interfaces;
using BoardGameTracker.Core.Images.Interfaces;
using BoardGameTracker.Core.Players.Interfaces;
using BoardGameTracker.Tests.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Auth;

public sealed class PlayerLinkPromptServiceTests : IDisposable
{
    private readonly IdentityTestScope _scope = new();
    private readonly Mock<IPlayerService> _playerServiceMock = new();
    private readonly Mock<IPlayerAccountImageService> _accountImagesMock = new();
    private readonly PlayerLinkPromptService _service;

    public PlayerLinkPromptServiceTests()
    {
        var userAdminService = new UserAdminService(
            _scope.UserManager,
            Mock.Of<ITokenService>(),
            Mock.Of<IUserAccessCheck>(),
            _scope.Context,
            Mock.Of<IImageService>(),
            _accountImagesMock.Object,
            NullLogger<UserAdminService>.Instance);

        _service = new PlayerLinkPromptService(
            _scope.UserManager,
            userAdminService,
            _playerServiceMock.Object,
            _scope.Context,
            NullLogger<PlayerLinkPromptService>.Instance);

        _playerServiceMock
            .Setup(x => x.Create(It.IsAny<CreatePlayerCommand>()))
            .Returns(async (CreatePlayerCommand command) =>
            {
                var player = new Player(command.Name, command.Image, command.Email);
                _scope.Context.Players.Add(player);
                await _scope.Context.SaveChangesAsync();
                return player;
            });
        _accountImagesMock.Setup(x => x.ApplyAccountImageAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(true);
    }

    public void Dispose() => _scope.Dispose();

    private void VerifyNoOtherCalls()
    {
        _playerServiceMock.VerifyNoOtherCalls();
        _accountImagesMock.VerifyNoOtherCalls();
    }

    private async Task<ApplicationUser> CreateUserAsync(string role, string userName = "mregni", string? email = "mikhael@example.com", string? displayName = "Mikhaël Regni", bool pending = true, bool emailConfirmed = true)
    {
        if (!await _scope.RoleManager.RoleExistsAsync(role))
        {
            await _scope.RoleManager.CreateAsync(new IdentityRole(role));
        }

        var user = new ApplicationUser(userName, email, displayName);
        if (emailConfirmed)
        {
            user.ConfirmEmail();
        }

        if (pending)
        {
            user.AwaitPlayerLink();
        }

        (await _scope.UserManager.CreateAsync(user)).Succeeded.Should().BeTrue();
        await _scope.UserManager.AddToRoleAsync(user, role);
        return user;
    }

    private async Task<Player> AddPlayerAsync(string name, string? email = null)
    {
        var player = new Player(name, null, email);
        _scope.Context.Players.Add(player);
        await _scope.Context.SaveChangesAsync();
        return player;
    }

    private async Task<ApplicationUser> ReloadAsync(string userId)
    {
        var user = await _scope.Context.Users.AsNoTracking().SingleAsync(u => u.Id == userId);
        return user;
    }

    [Fact]
    public async Task GetSuggestionsAsync_ShouldRankUnlinkedPlayers_AndLeaveOutPlayersLinkedToAnotherAccount()
    {
        var user = await CreateUserAsync(Constants.AuthRoles.User);
        var byEmail = await AddPlayerAsync("Zed", "MIKHAEL@example.com");
        var byName = await AddPlayerAsync("mikhael regni");
        var partly = await AddPlayerAsync("Mikhaël");
        var unrelated = await AddPlayerAsync("Anna");
        var taken = await AddPlayerAsync("Mikhael R");
        var other = await CreateUserAsync(Constants.AuthRoles.User, "other", "other@example.com", "Other", pending: false);
        other.LinkPlayer(taken.Id);
        await _scope.UserManager.UpdateAsync(other);

        var result = await _service.GetSuggestionsAsync(user.Id);

        result.Suggestions.Select(s => s.Id).Should().Equal(byEmail.Id, byName.Id, partly.Id);
        result.Suggestions.Select(s => s.Reason).Should().Equal(PlayerLinkMatchReason.Email, PlayerLinkMatchReason.Name, PlayerLinkMatchReason.Partial);
        result.Players.Select(p => p.Id).Should().BeEquivalentTo([byEmail.Id, byName.Id, partly.Id, unrelated.Id]);
        result.Players.Select(p => p.Name).Should().BeInAscendingOrder(StringComparer.InvariantCultureIgnoreCase);
        result.CanCreatePlayer.Should().BeTrue();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetSuggestionsAsync_ShouldLeaveOutPlayersWithADifferentEmail()
    {
        var user = await CreateUserAsync(Constants.AuthRoles.User);
        var same = await AddPlayerAsync("Mikhaël", " Mikhael@Example.com ");
        var noEmail = await AddPlayerAsync("Anna");
        await AddPlayerAsync("Mikhael R", "someone@example.com");

        var result = await _service.GetSuggestionsAsync(user.Id);

        result.Players.Select(p => p.Id).Should().BeEquivalentTo([same.Id, noEmail.Id]);
        result.Suggestions.Select(s => s.Id).Should().Equal(same.Id);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetSuggestionsAsync_ShouldOnlyOfferPlayersWithoutAnEmail_WhenTheAccountHasNone()
    {
        var user = await CreateUserAsync(Constants.AuthRoles.User, "sso_123", "123" + OidcService.PlaceholderEmailDomain, null);
        var noEmail = await AddPlayerAsync("Anna");
        await AddPlayerAsync("Bert", "bert@example.com");

        var result = await _service.GetSuggestionsAsync(user.Id);

        result.Players.Select(p => p.Id).Should().Equal(noEmail.Id);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetSuggestionsAsync_ShouldTellAReaderThatTheyCannotCreateAPlayer()
    {
        var user = await CreateUserAsync(Constants.AuthRoles.Reader);

        var result = await _service.GetSuggestionsAsync(user.Id);

        result.Suggestions.Should().BeEmpty();
        result.Players.Should().BeEmpty();
        result.CanCreatePlayer.Should().BeFalse();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetSuggestionsAsync_ShouldThrow_WhenTheUserDoesNotExist()
    {
        var act = () => _service.GetSuggestionsAsync("missing");

        await act.Should().ThrowAsync<EntityNotFoundException>();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task LinkAsync_ShouldLinkThePlayer_ClearTheFlag_AndSyncTheAvatar()
    {
        var user = await CreateUserAsync(Constants.AuthRoles.Reader);
        var player = await AddPlayerAsync("Mikhaël");

        var profile = await _service.LinkAsync(user.Id, player.Id);

        profile.PlayerId.Should().Be(player.Id);
        profile.NeedsPlayerLink.Should().BeFalse();
        var stored = await ReloadAsync(user.Id);
        stored.PlayerId.Should().Be(player.Id);
        stored.PlayerLinkPending.Should().BeFalse();
        _accountImagesMock.Verify(x => x.ReleaseAsync(null), Times.Once);
        _accountImagesMock.Verify(x => x.ApplyAccountImageAsync(It.Is<ApplicationUser>(u => u.Id == user.Id && u.PlayerId == player.Id)), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task LinkAsync_ShouldRefuseAPlayerLinkedToAnotherAccount_WithAConflict()
    {
        var user = await CreateUserAsync(Constants.AuthRoles.User);
        var player = await AddPlayerAsync("Mikhaël");
        var other = await CreateUserAsync(Constants.AuthRoles.User, "other", "other@example.com", "Other", pending: false);
        other.LinkPlayer(player.Id);
        await _scope.UserManager.UpdateAsync(other);

        var act = () => _service.LinkAsync(user.Id, player.Id);

        (await act.Should().ThrowAsync<ConflictException>()).WithMessage(Constants.Errors.PlayerAlreadyLinked);
        (await ReloadAsync(user.Id)).PlayerLinkPending.Should().BeTrue();
        (await ReloadAsync(other.Id)).PlayerId.Should().Be(player.Id);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task LinkAsync_ShouldRefuseAPlayerWithADifferentEmail()
    {
        var user = await CreateUserAsync(Constants.AuthRoles.User);
        var player = await AddPlayerAsync("Mikhaël", "someone@example.com");

        var act = () => _service.LinkAsync(user.Id, player.Id);

        (await act.Should().ThrowAsync<ForbiddenException>()).WithMessage(Constants.Errors.PlayerEmailMismatch);
        var stored = await ReloadAsync(user.Id);
        stored.PlayerLinkPending.Should().BeTrue();
        stored.PlayerId.Should().BeNull();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task LinkAsync_ShouldRefuseAPlayerWithTheSameEmail_WhenTheAccountEmailIsNotConfirmed()
    {
        var user = await CreateUserAsync(Constants.AuthRoles.User, email: "alice@example.com", emailConfirmed: false);
        var player = await AddPlayerAsync("Alice", "alice@example.com");

        var act = () => _service.LinkAsync(user.Id, player.Id);

        (await act.Should().ThrowAsync<ForbiddenException>()).WithMessage(Constants.Errors.PlayerEmailMismatch);
        (await ReloadAsync(user.Id)).PlayerId.Should().BeNull();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetSuggestionsAsync_ShouldLeaveOutPlayersWithTheSameEmail_WhenTheAccountEmailIsNotConfirmed()
    {
        var user = await CreateUserAsync(Constants.AuthRoles.User, email: "alice@example.com", emailConfirmed: false);
        await AddPlayerAsync("Alice", "alice@example.com");
        var noEmail = await AddPlayerAsync("Anna");

        var result = await _service.GetSuggestionsAsync(user.Id);

        result.Players.Select(p => p.Id).Should().Equal(noEmail.Id);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task LinkAsync_ShouldThrow_WhenThePlayerDoesNotExist()
    {
        var user = await CreateUserAsync(Constants.AuthRoles.User);

        var act = () => _service.LinkAsync(user.Id, 999);

        await act.Should().ThrowAsync<EntityNotFoundException>();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreatePlayerAsync_ShouldCreateAPlayerNamedAfterTheDisplayName_WithTheEmail_AndLinkIt()
    {
        var user = await CreateUserAsync(Constants.AuthRoles.User);

        var profile = await _service.CreatePlayerAsync(user.Id);

        var player = await _scope.Context.Players.AsNoTracking().SingleAsync();
        player.Name.Should().Be("Mikhaël Regni");
        player.Email.Should().Be("mikhael@example.com");
        profile.PlayerId.Should().Be(player.Id);
        profile.NeedsPlayerLink.Should().BeFalse();
        (await ReloadAsync(user.Id)).PlayerLinkPending.Should().BeFalse();
        _playerServiceMock.Verify(x => x.Create(It.Is<CreatePlayerCommand>(c => c.Name == "Mikhaël Regni" && c.Email == "mikhael@example.com")), Times.Once);
        _accountImagesMock.Verify(x => x.ReleaseAsync(null), Times.Once);
        _accountImagesMock.Verify(x => x.ApplyAccountImageAsync(It.Is<ApplicationUser>(u => u.Id == user.Id)), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreatePlayerAsync_ShouldFallBackToTheUsername_AndLeaveOutAnEmailAnotherPlayerHas()
    {
        var user = await CreateUserAsync(Constants.AuthRoles.Admin);
        await AddPlayerAsync(" mikhaël regni ", " Mikhael@Example.com ");

        await _service.CreatePlayerAsync(user.Id);

        _playerServiceMock.Verify(x => x.Create(It.Is<CreatePlayerCommand>(c => c.Name == "mregni" && c.Email == null)), Times.Once);
        _accountImagesMock.Verify(x => x.ReleaseAsync(null), Times.Once);
        _accountImagesMock.Verify(x => x.ApplyAccountImageAsync(It.IsAny<ApplicationUser>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreatePlayerAsync_ShouldUseTheUsername_AndSkipThePlaceholderEmail_WhenTheAccountHasNoRealOnes()
    {
        var user = await CreateUserAsync(Constants.AuthRoles.User, "sso_123", "123" + OidcService.PlaceholderEmailDomain, null);

        await _service.CreatePlayerAsync(user.Id);

        _playerServiceMock.Verify(x => x.Create(It.Is<CreatePlayerCommand>(c => c.Name == "sso_123" && c.Email == null)), Times.Once);
        _accountImagesMock.Verify(x => x.ReleaseAsync(null), Times.Once);
        _accountImagesMock.Verify(x => x.ApplyAccountImageAsync(It.IsAny<ApplicationUser>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreatePlayerAsync_ShouldKeepTheDisplayName_WhenEveryNameIsTaken()
    {
        var user = await CreateUserAsync(Constants.AuthRoles.User, displayName: "Mikhaël Regni");
        await AddPlayerAsync("Mikhaël Regni");
        await AddPlayerAsync("mregni");

        var act = () => _service.CreatePlayerAsync(user.Id);
        _playerServiceMock
            .Setup(x => x.Create(It.IsAny<CreatePlayerCommand>()))
            .ThrowsAsync(new ValidationException("Name", Constants.Errors.PlayerNameAlreadyExists));

        await act.Should().ThrowAsync<ValidationException>();
        _playerServiceMock.Verify(x => x.Create(It.Is<CreatePlayerCommand>(c => c.Name == "Mikhaël Regni")), Times.Once);
        (await ReloadAsync(user.Id)).PlayerLinkPending.Should().BeTrue();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreatePlayerAsync_ShouldRefuseAReader()
    {
        var user = await CreateUserAsync(Constants.AuthRoles.Reader);

        var act = () => _service.CreatePlayerAsync(user.Id);

        (await act.Should().ThrowAsync<ForbiddenException>()).WithMessage(Constants.Errors.PlayerCreateNotAllowed);
        (await ReloadAsync(user.Id)).PlayerLinkPending.Should().BeTrue();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SkipAsync_ShouldClearTheFlag_WithoutLinkingAPlayer()
    {
        var user = await CreateUserAsync(Constants.AuthRoles.Reader);

        var profile = await _service.SkipAsync(user.Id);

        profile.NeedsPlayerLink.Should().BeFalse();
        profile.PlayerId.Should().BeNull();
        var stored = await ReloadAsync(user.Id);
        stored.PlayerLinkPending.Should().BeFalse();
        stored.PlayerId.Should().BeNull();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SkipAsync_ShouldThrow_WhenTheUserDoesNotExist()
    {
        var act = () => _service.SkipAsync("missing");

        await act.Should().ThrowAsync<EntityNotFoundException>();
        VerifyNoOtherCalls();
    }
}
