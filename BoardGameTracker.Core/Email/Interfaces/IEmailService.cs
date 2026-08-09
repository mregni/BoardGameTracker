namespace BoardGameTracker.Core.Email.Interfaces;

public interface IEmailService
{
    bool IsConfigured { get; }
    Task SendAsync(string to, string subject, string htmlBody, string? textBody = null, CancellationToken cancellationToken = default);
}
