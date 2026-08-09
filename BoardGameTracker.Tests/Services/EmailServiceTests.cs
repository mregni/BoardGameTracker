using System;
using System.Threading;
using System.Threading.Tasks;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Core.Email;
using BoardGameTracker.Core.Email.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using MimeKit;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Services;

public class EmailServiceTests
{
    private readonly Mock<IEnvironmentProvider> _environmentProviderMock;
    private readonly Mock<ISmtpSender> _smtpSenderMock;
    private readonly Mock<ILogger<EmailService>> _loggerMock;
    private readonly EmailService _emailService;

    public EmailServiceTests()
    {
        _environmentProviderMock = new Mock<IEnvironmentProvider>();
        _smtpSenderMock = new Mock<ISmtpSender>();
        _loggerMock = new Mock<ILogger<EmailService>>();
        _emailService = new EmailService(_environmentProviderMock.Object, _smtpSenderMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void IsConfigured_ShouldReflectEnvironment()
    {
        _environmentProviderMock.SetupGet(x => x.EmailEnabled).Returns(true);
        _emailService.IsConfigured.Should().BeTrue();

        _environmentProviderMock.SetupGet(x => x.EmailEnabled).Returns(false);
        _emailService.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public async Task SendAsync_ShouldThrow_WhenNotConfigured()
    {
        _environmentProviderMock.SetupGet(x => x.EmailEnabled).Returns(false);

        var act = () => _emailService.SendAsync("to@test.com", "Subject", "<p>Body</p>");

        await act.Should().ThrowAsync<DomainException>();
        _smtpSenderMock.Verify(
            x => x.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SendAsync_ShouldBuildMessageAndDelegateToSender()
    {
        _environmentProviderMock.SetupGet(x => x.EmailEnabled).Returns(true);
        _environmentProviderMock.SetupGet(x => x.SmtpFromAddress).Returns("from@test.com");
        _environmentProviderMock.SetupGet(x => x.SmtpFromName).Returns("BGT");
        _environmentProviderMock.SetupGet(x => x.SmtpHost).Returns("smtp.test.com");
        _environmentProviderMock.SetupGet(x => x.SmtpPort).Returns(587);
        _environmentProviderMock.SetupGet(x => x.SmtpUseSsl).Returns(true);
        _environmentProviderMock.SetupGet(x => x.SmtpUsername).Returns("user");
        _environmentProviderMock.SetupGet(x => x.SmtpPassword).Returns("pass");

        MimeMessage? captured = null;
        _smtpSenderMock
            .Setup(x => x.SendAsync(It.IsAny<MimeMessage>(), "smtp.test.com", 587, true, "user", "pass", It.IsAny<CancellationToken>()))
            .Callback<MimeMessage, string, int, bool, string?, string?, CancellationToken>((m, _, _, _, _, _, _) => captured = m)
            .Returns(Task.CompletedTask);

        await _emailService.SendAsync("to@test.com", "Hello", "<p>Body</p>");

        captured.Should().NotBeNull();
        captured!.Subject.Should().Be("Hello");
        captured.To.ToString().Should().Contain("to@test.com");
        captured.From.ToString().Should().Contain("from@test.com");

        _smtpSenderMock.Verify(
            x => x.SendAsync(It.IsAny<MimeMessage>(), "smtp.test.com", 587, true, "user", "pass", It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
