namespace BoardGameTracker.Core.Auth.Interfaces;

public interface IProfileImageTicketService
{
    TimeSpan Lifetime { get; }
    string Issue();
    bool IsValid(string? ticket);
}
