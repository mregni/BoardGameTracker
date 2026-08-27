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
        var currentOrder = GetOrder(current, nameof(current));
        var nextOrder = GetOrder(next, nameof(next));

        return nextOrder == currentOrder + 1;
    }

    public BadgeLevel? GetNextLevel(BadgeLevel current)
    {
        var currentOrder = GetOrder(current, nameof(current));
        return FindLevelByOrder(currentOrder + 1);
    }

    public BadgeLevel? GetPreviousLevel(BadgeLevel current)
    {
        var currentOrder = GetOrder(current, nameof(current));
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
        return GetOrder(level, nameof(level)) == maxOrder;
    }

    public bool IsStartingLevel(BadgeLevel level)
    {
        var minOrder = LevelHierarchy.Values.Min();
        return GetOrder(level, nameof(level)) == minOrder;
    }

    public int GetLevelOrder(BadgeLevel level)
    {
        return GetOrder(level, nameof(level));
    }

    public int CompareLevels(BadgeLevel level1, BadgeLevel level2)
    {
        var order1 = GetOrder(level1, nameof(level1));
        var order2 = GetOrder(level2, nameof(level2));
        return order1.CompareTo(order2);
    }

    private static int GetOrder(BadgeLevel level, string paramName)
    {
        if (!LevelHierarchy.TryGetValue(level, out var order))
        {
            throw new ArgumentOutOfRangeException(paramName, level, $"Undefined badge level: {level}.");
        }

        return order;
    }
}
