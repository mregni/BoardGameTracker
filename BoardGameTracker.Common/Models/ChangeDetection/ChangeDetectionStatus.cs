namespace BoardGameTracker.Common.Models.ChangeDetection;

public enum ChangeDetectionStatus
{
    Ok,
    NotConfigured,
    Misconfigured,
    Unauthorized,
    WatchNotFound,
    Unreachable,
    ParseError
}
