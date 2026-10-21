using System.Threading.Tasks;
using BoardGameTracker.Api.Controllers.Admin;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.DTOs.Auth;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Auth.Interfaces;
using BoardGameTracker.Core.Players.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Auth;

public class PlayerAccountControllerTests
{
    private readonly Mock<IUserAdminService> _userAdminServiceMock = new();
    private readonly Mock<IPlayerService> _playerServiceMock = new();
    private readonly PlayerAccountController _controller;

    public PlayerAccountControllerTests()
    {
        _controller = new PlayerAccountController(_userAdminServiceMock.Object, _playerServiceMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _userAdminServiceMock.VerifyNoOtherCalls();
        _playerServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task LinkAccount_ShouldLinkAndReturnThePlayerWithItsAccount()
    {
        _playerServiceMock.Setup(x => x.Get(4)).ReturnsAsync(new Player("Jane") { Id = 4 });
        _playerServiceMock.Setup(x => x.GetLinkedAccountAsync(4)).ReturnsAsync(new PlayerLinkedAccountDto { Id = "u1", Username = "jane" });

        var result = await _controller.LinkAccount(4, new LinkPlayerAccountRequest { UserId = "u1" });

        var dto = result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeOfType<PlayerDto>().Subject;
        dto.Id.Should().Be(4);
        dto.LinkedAccount!.Id.Should().Be("u1");
        _userAdminServiceMock.Verify(x => x.LinkPlayerAccountAsync(4, "u1"), Times.Once);
        _playerServiceMock.Verify(x => x.Get(4), Times.Once);
        _playerServiceMock.Verify(x => x.GetLinkedAccountAsync(4), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task LinkAccount_ShouldThrowNotFound_WhenThePlayerDisappeared()
    {
        _playerServiceMock.Setup(x => x.Get(4)).ReturnsAsync((Player?)null);

        var act = () => _controller.LinkAccount(4, new LinkPlayerAccountRequest());

        await act.Should().ThrowAsync<EntityNotFoundException>();
        _userAdminServiceMock.Verify(x => x.LinkPlayerAccountAsync(4, null), Times.Once);
        _playerServiceMock.Verify(x => x.Get(4), Times.Once);
        VerifyNoOtherCalls();
    }
}
