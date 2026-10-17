namespace BoardGameTracker.Core.Auth;

public static class AdminRoleGate
{
    public static readonly SemaphoreSlim Lock = new(1, 1);
}
