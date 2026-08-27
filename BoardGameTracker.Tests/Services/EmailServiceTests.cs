using System;
using System.Linq;
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

    private void VerifyNoOtherCalls()
    {
        _smtpSenderMock.VerifyNoOtherCalls();
    }

    private void SetupConfiguredSmtp(string? fromName = "BGT")
    {
        _environmentProviderMock.SetupGet(x => x.EmailEnabled).Returns(true);
        _environmentProviderMock.SetupGet(x => x.SmtpFromAddress).Returns("from@test.com");
        _environmentProviderMock.SetupGet(x => x.SmtpFromName).Returns(fromName);
        _environmentProviderMock.SetupGet(x => x.SmtpHost).Returns("smtp.test.com");
        _environmentProviderMock.SetupGet(x => x.SmtpPort).Returns(587);
        _environmentProviderMock.SetupGet(x => x.SmtpUseSsl).Returns(true);
        _environmentProviderMock.SetupGet(x => x.SmtpUsername).Returns("user");
        _environmentProviderMock.SetupGet(x => x.SmtpPassword).Returns("pass");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IsConfigured_ShouldReflectEnvironment(bool emailEnabled)
    {
        _environmentProviderMock.SetupGet(x => x.EmailEnabled).Returns(emailEnabled);

        _emailService.IsConfigured.Should().Be(emailEnabled);

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SendAsync_ShouldThrow_WhenNotConfigured()
    {
        _environmentProviderMock.SetupGet(x => x.EmailEnabled).Returns(false);

        var act = () => _emailService.SendAsync("to@test.com", "Subject", "<p>Body</p>");

        await act.Should().ThrowAsync<DomainException>();

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SendAsync_ShouldBuildMessageAndDelegateToSender()
    {
        SetupConfiguredSmtp();

        MimeMessage? captured = null;
        _smtpSenderMock
            .Setup(x => x.SendAsync(It.IsAny<MimeMessage>(), "smtp.test.com", 587, true, "user", "pass", It.IsAny<CancellationToken>()))
            .Callback<MimeMessage, string, int, bool, string?, string?, CancellationToken>((m, _, _, _, _, _, _) => captured = m)
            .Returns(Task.CompletedTask);

        await _emailService.SendAsync("to@test.com", "Hello", "<p>Body</p>", "Plain body");

        captured.Should().NotBeNull();
        captured!.Subject.Should().Be("Hello");
        captured.HtmlBody.Should().Be("<p>Body</p>");
        captured.TextBody.Should().Be("Plain body");
        captured.To.Mailboxes.Single().Address.Should().Be("to@test.com");
        var from = captured.From.Mailboxes.Single();
        from.Address.Should().Be("from@test.com");
        from.Name.Should().Be("BGT");

        _smtpSenderMock.Verify(
            x => x.SendAsync(It.IsAny<MimeMessage>(), "smtp.test.com", 587, true, "user", "pass", It.IsAny<CancellationToken>()),
            Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SendAsync_ShouldUseFromAddressAsDisplayName_WhenFromNameNotConfigured()
    {
        SetupConfiguredSmtp(fromName: null);

        MimeMessage? captured = null;
        _smtpSenderMock
            .Setup(x => x.SendAsync(It.IsAny<MimeMessage>(), "smtp.test.com", 587, true, "user", "pass", It.IsAny<CancellationToken>()))
            .Callback<MimeMessage, string, int, bool, string?, string?, CancellationToken>((m, _, _, _, _, _, _) => captured = m)
            .Returns(Task.CompletedTask);

        await _emailService.SendAsync("to@test.com", "Hello", "<p>Body</p>");

        captured.Should().NotBeNull();
        var from = captured!.From.Mailboxes.Single();
        from.Address.Should().Be("from@test.com");
        from.Name.Should().Be("from@test.com");

        _smtpSenderMock.Verify(
            x => x.SendAsync(It.IsAny<MimeMessage>(), "smtp.test.com", 587, true, "user", "pass", It.IsAny<CancellationToken>()),
            Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SendAsync_ShouldThrowAndNotSend_WhenRecipientAddressIsInvalid()
    {
        SetupConfiguredSmtp();

        var act = () => _emailService.SendAsync(string.Empty, "Hello", "<p>Body</p>");

        await act.Should().ThrowAsync<ParseException>();

        VerifyNoOtherCalls();
    }
}
