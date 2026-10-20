namespace BoardGameTracker.Core.Auth.Interfaces;

public interface IProfileImageTicketService
{
    TimeSpan Lifetime { get; }
    string Issue(string userId);
    string? ReadUserId(string? ticket);
}
