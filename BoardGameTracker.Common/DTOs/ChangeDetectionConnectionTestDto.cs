using BoardGameTracker.Common.Models.ChangeDetection;

namespace BoardGameTracker.Common.DTOs;

public class ChangeDetectionConnectionTestDto
{
    public bool Ok { get; set; }
    public ChangeDetectionStatus Status { get; set; }
    public string? Version { get; set; }
}
