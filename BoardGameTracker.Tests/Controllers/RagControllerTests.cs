using System.Threading;
using System.Threading.Tasks;
using BoardGameTracker.Api.Controllers;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Core.Rag.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Controllers;

public class RagControllerTests
{
    private readonly Mock<IRagService> _ragServiceMock;
    private readonly Mock<IEnvironmentProvider> _environmentProviderMock;
    private readonly RagController _controller;

    public RagControllerTests()
    {
        _ragServiceMock = new Mock<IRagService>();
        _environmentProviderMock = new Mock<IEnvironmentProvider>();
        _controller = new RagController(_ragServiceMock.Object, _environmentProviderMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _ragServiceMock.VerifyNoOtherCalls();
        _environmentProviderMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Ask_ShouldReturnNotFound_WhenRagDisabled()
    {
        _environmentProviderMock.Setup(x => x.RagEnabled).Returns(false);
        var command = new AskRagCommand { Question = "How does scoring work?" };

        var result = await _controller.Ask(5, command, CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();

        _environmentProviderMock.Verify(x => x.RagEnabled, Times.Once);
        _ragServiceMock.Verify(x => x.AskAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Never);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Ask_ShouldReturnOkWithAnswer_WhenRagEnabled()
    {
        _environmentProviderMock.Setup(x => x.RagEnabled).Returns(true);
        var command = new AskRagCommand { Question = "How does scoring work?", ManualId = 3 };
        var answer = new RagAnswerDto { Answer = "Count the points.", HasContext = true };
        _ragServiceMock
            .Setup(x => x.AskAsync(5, "How does scoring work?", 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(answer);

        var result = await _controller.Ask(5, command, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(answer);

        _environmentProviderMock.Verify(x => x.RagEnabled, Times.Once);
        _ragServiceMock.Verify(x => x.AskAsync(5, "How does scoring work?", 3, It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Ask_ShouldPassNullManualId_WhenCommandHasNoManualId()
    {
        _environmentProviderMock.Setup(x => x.RagEnabled).Returns(true);
        var command = new AskRagCommand { Question = "What is the setup?" };
        _ragServiceMock
            .Setup(x => x.AskAsync(8, "What is the setup?", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RagAnswerDto());

        var result = await _controller.Ask(8, command, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();

        _environmentProviderMock.Verify(x => x.RagEnabled, Times.Once);
        _ragServiceMock.Verify(x => x.AskAsync(8, "What is the setup?", null, It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }
}
