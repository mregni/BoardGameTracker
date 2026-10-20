namespace BoardGameTracker.Core.Auth.Interfaces;

public interface IUserAccessCheck
{
    Task<bool> CanSignInAsync(string userId, CancellationToken cancellationToken = default);
    void Forget(string userId);
}
