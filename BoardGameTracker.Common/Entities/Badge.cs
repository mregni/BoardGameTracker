using BoardGameTracker.Common.Entities.Helpers;
using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Common.Entities;

public class Badge : HasId
{
    public string DescriptionKey { get; set; }
    public string TitleKey { get; set; }
    public BadgeType Type { get; set; }
    public BadgeLevel? Level { get; set; }
    public string Image { get; set; }

    public ICollection<Player> Players { get; set; }
}