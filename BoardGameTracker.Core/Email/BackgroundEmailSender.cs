using BoardGameTracker.Core.Email.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Core.Email;

public class BackgroundEmailSender : IBackgroundEmailSender
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BackgroundEmailSender> _logger;

    public BackgroundEmailSender(IServiceScopeFactory scopeFactory, ILogger<BackgroundEmailSender> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public void Queue(string to, string subject, string htmlBody)
    {
        _ = Task.Run(() => SendAsync(to, subject, htmlBody));
    }

    private async Task SendAsync(string to, string subject, string htmlBody)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            await emailService.SendAsync(to, subject, htmlBody);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send a queued email with subject {Subject}", subject);
        }
    }
}
