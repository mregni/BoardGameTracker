using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BoardGameTracker.Common;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Core.Email;
using BoardGameTracker.Core.Email.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MimeKit;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Email;

public class EmailServiceTests
{
    private readonly Mock<IEnvironmentProvider> _environmentProviderMock;
    private readonly Mock<ISmtpSender> _smtpSenderMock;
    private readonly EmailService _emailService;

    public EmailServiceTests()
    {
        _environmentProviderMock = new Mock<IEnvironmentProvider>();
        _smtpSenderMock = new Mock<ISmtpSender>();
        _emailService = new EmailService(_environmentProviderMock.Object, _smtpSenderMock.Object, NullLogger<EmailService>.Instance);
    }

    private void VerifyNoOtherCalls()
    {
        _smtpSenderMock.VerifyNoOtherCalls();
        _environmentProviderMock.VerifyNoOtherCalls();
    }

    private void VerifySmtpSettingsRead()
    {
        _environmentProviderMock.VerifyGet(x => x.EmailEnabled, Times.Once);
        _environmentProviderMock.VerifyGet(x => x.SmtpFromAddress, Times.Once);
        _environmentProviderMock.VerifyGet(x => x.SmtpFromName, Times.Once);
        _environmentProviderMock.VerifyGet(x => x.SmtpHost, Times.Once);
        _environmentProviderMock.VerifyGet(x => x.SmtpPort, Times.Once);
        _environmentProviderMock.VerifyGet(x => x.SmtpUseSsl, Times.Once);
        _environmentProviderMock.VerifyGet(x => x.SmtpUsername, Times.Once);
        _environmentProviderMock.VerifyGet(x => x.SmtpPassword, Times.Once);
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

        _environmentProviderMock.VerifyGet(x => x.EmailEnabled, Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SendAsync_ShouldThrow_WhenNotConfigured()
    {
        _environmentProviderMock.SetupGet(x => x.EmailEnabled).Returns(false);

        var act = () => _emailService.SendAsync("to@test.com", "Subject", "<p>Body</p>");

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.EmailNotConfigured);

        _environmentProviderMock.VerifyGet(x => x.EmailEnabled, Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SendAsync_ShouldBuildMessageAndDelegateToSender()
    {
        SetupConfiguredSmtp();
        var token = new CancellationTokenSource().Token;

        MimeMessage? captured = null;
        _smtpSenderMock
            .Setup(x => x.SendAsync(It.Is<MimeMessage>(m => m.Subject == "Hello"), "smtp.test.com", 587, true, "user", "pass", token))
            .Callback<MimeMessage, string, int, bool, string?, string?, CancellationToken>((m, _, _, _, _, _, _) => captured = m)
            .Returns(Task.CompletedTask);

        await _emailService.SendAsync("to@test.com", "Hello", "<p>Body</p>", "Plain body", token);

        captured.Should().NotBeNull();
        captured!.Subject.Should().Be("Hello");
        captured.HtmlBody.Should().Be("<p>Body</p>");
        captured.TextBody.Should().Be("Plain body");
        captured.To.Mailboxes.Single().Address.Should().Be("to@test.com");
        var from = captured.From.Mailboxes.Single();
        from.Address.Should().Be("from@test.com");
        from.Name.Should().Be("BGT");

        _smtpSenderMock.Verify(
            x => x.SendAsync(It.Is<MimeMessage>(m => m.Subject == "Hello"), "smtp.test.com", 587, true, "user", "pass", token),
            Times.Once);
        VerifySmtpSettingsRead();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SendAsync_ShouldUseFromAddressAsDisplayName_WhenFromNameNotConfigured()
    {
        SetupConfiguredSmtp(fromName: null);

        MimeMessage? captured = null;
        _smtpSenderMock
            .Setup(x => x.SendAsync(It.Is<MimeMessage>(m => m.Subject == "Hello"), "smtp.test.com", 587, true, "user", "pass", CancellationToken.None))
            .Callback<MimeMessage, string, int, bool, string?, string?, CancellationToken>((m, _, _, _, _, _, _) => captured = m)
            .Returns(Task.CompletedTask);

        await _emailService.SendAsync("to@test.com", "Hello", "<p>Body</p>");

        captured.Should().NotBeNull();
        var from = captured!.From.Mailboxes.Single();
        from.Address.Should().Be("from@test.com");
        from.Name.Should().Be("from@test.com");

        _smtpSenderMock.Verify(
            x => x.SendAsync(It.Is<MimeMessage>(m => m.Subject == "Hello"), "smtp.test.com", 587, true, "user", "pass", CancellationToken.None),
            Times.Once);
        VerifySmtpSettingsRead();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SendAsync_ShouldThrowAndNotSend_WhenRecipientAddressIsInvalid()
    {
        SetupConfiguredSmtp();

        var act = () => _emailService.SendAsync(string.Empty, "Hello", "<p>Body</p>");

        await act.Should().ThrowAsync<ParseException>();

        _environmentProviderMock.VerifyGet(x => x.EmailEnabled, Times.Once);
        _environmentProviderMock.VerifyGet(x => x.SmtpFromAddress, Times.Once);
        _environmentProviderMock.VerifyGet(x => x.SmtpFromName, Times.Once);
        VerifyNoOtherCalls();
    }
}
