namespace BoardGameTracker.Core.Auth.Interfaces;

public interface ILoginAttemptTracker
{
    bool IsLockedOut(string username, string clientAddress);
    void RecordFailure(string username, string clientAddress);
    void Reset(string username, string clientAddress);
}
