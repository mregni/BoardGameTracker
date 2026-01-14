using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Core.Badges.Policies;

public interface IBadgeLevelProgressionPolicy
{
    bool CanProgressTo(BadgeLevel current, BadgeLevel next);
    BadgeLevel? GetNextLevel(BadgeLevel current);
    BadgeLevel? GetPreviousLevel(BadgeLevel current);
    bool IsMaxLevel(BadgeLevel level);
    bool IsStartingLevel(BadgeLevel level);
    int GetLevelOrder(BadgeLevel level);
    int CompareLevels(BadgeLevel level1, BadgeLevel level2);
}
