namespace BoardGameTracker.Core.Email.Interfaces;

public interface IBackgroundEmailSender
{
    void Queue(string to, string subject, string htmlBody);
}
