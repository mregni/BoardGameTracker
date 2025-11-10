using BoardGameTracker.Common.Entities.Helpers;
using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Common.Entities;

public class Badge : HasId
{
    public string DescriptionKey { get; set; } = string.Empty;
    public string TitleKey { get; set; } = string.Empty;
    public BadgeType Type { get; set; }
    public BadgeLevel? Level { get; set; }
    public string Image { get; set; } = string.Empty;

    public ICollection<Player> Players { get; set; } = [];
}