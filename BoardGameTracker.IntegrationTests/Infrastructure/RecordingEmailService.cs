using BoardGameTracker.Core.Email.Interfaces;

namespace BoardGameTracker.IntegrationTests.Infrastructure;

public sealed class RecordingEmailService : IEmailService
{
    private readonly List<string> _recipients = [];

    public bool IsConfigured => true;

    public IReadOnlyList<string> Recipients
    {
        get
        {
            lock (_recipients)
            {
                return _recipients.ToList();
            }
        }
    }

    public Task SendAsync(string to, string subject, string htmlBody, string? textBody = null, CancellationToken cancellationToken = default)
    {
        lock (_recipients)
        {
            _recipients.Add(to);
        }

        return Task.CompletedTask;
    }
}
