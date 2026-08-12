using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Badges.Interfaces;

namespace BoardGameTracker.Core.Badges;

public class BadgeLevelProgressionPolicy : IBadgeLevelProgressionPolicy
{
    private static readonly Dictionary<BadgeLevel, int> LevelHierarchy = new()
    {
        { BadgeLevel.Green, 1 },
        { BadgeLevel.Blue, 2 },
        { BadgeLevel.Red, 3 },
        { BadgeLevel.Gold, 4 }
    };

    public bool CanProgressTo(BadgeLevel current, BadgeLevel next)
    {
        var currentOrder = LevelHierarchy.GetValueOrDefault(current, 0);
        var nextOrder = LevelHierarchy.GetValueOrDefault(next, 0);

        return nextOrder == currentOrder + 1;
    }

    public BadgeLevel? GetNextLevel(BadgeLevel current)
    {
        var currentOrder = LevelHierarchy.GetValueOrDefault(current, 0);
        return FindLevelByOrder(currentOrder + 1);
    }

    public BadgeLevel? GetPreviousLevel(BadgeLevel current)
    {
        var currentOrder = LevelHierarchy.GetValueOrDefault(current, 0);
        var previousOrder = currentOrder - 1;

        if (previousOrder < 1)
            return null;

        return FindLevelByOrder(previousOrder);
    }

    private static BadgeLevel? FindLevelByOrder(int order)
    {
        foreach (var (level, levelOrder) in LevelHierarchy)
        {
            if (levelOrder == order)
            {
                return level;
            }
        }

        return null;
    }

    public bool IsMaxLevel(BadgeLevel level)
    {
        var maxOrder = LevelHierarchy.Values.Max();
        var currentOrder = LevelHierarchy.GetValueOrDefault(level, 0);
        return currentOrder == maxOrder;
    }

    public bool IsStartingLevel(BadgeLevel level)
    {
        var minOrder = LevelHierarchy.Values.Min();
        var currentOrder = LevelHierarchy.GetValueOrDefault(level, 0);
        return currentOrder == minOrder;
    }

    public int GetLevelOrder(BadgeLevel level)
    {
        return LevelHierarchy.GetValueOrDefault(level, 0);
    }

    public int CompareLevels(BadgeLevel level1, BadgeLevel level2)
    {
        var order1 = LevelHierarchy.GetValueOrDefault(level1, 0);
        var order2 = LevelHierarchy.GetValueOrDefault(level2, 0);
        return order1.CompareTo(order2);
    }
}
