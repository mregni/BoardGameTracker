using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Common.DTOs;

public class IntegrationStatusDto
{
    public IntegrationKind Integration { get; set; }
    public IntegrationConnectionStatus Status { get; set; }
    public DateTime CheckedAt { get; set; }
    public string MessageKey { get; set; } = string.Empty;
}
