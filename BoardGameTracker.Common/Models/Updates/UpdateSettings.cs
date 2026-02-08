using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Common.Models.Updates;

public class UpdateSettings
{
    public bool Enabled { get; set; }
    public VersionTrack VersionTrack { get; set; }
}
