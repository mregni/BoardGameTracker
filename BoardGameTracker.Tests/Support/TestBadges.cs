using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Tests.Support;

public static class TestBadges
{
    public static Badge Create(BadgeType type, BadgeLevel? level = null, int id = 1)
    {
        return Badge.CreateWithId(id, $"{type}.title", $"{type}.description", type, $"{type}.png", level);
    }
}
