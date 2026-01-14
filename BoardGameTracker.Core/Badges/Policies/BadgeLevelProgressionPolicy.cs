using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Core.Badges.Policies;

/// <summary>
/// Encapsulates business rules for badge level progression
/// </summary>
public class BadgeLevelProgressionPolicy : IBadgeLevelProgressionPolicy
{
    private static readonly Dictionary<BadgeLevel, int> LevelHierarchy = new()
    {
        { BadgeLevel.Green, 1 },
        { BadgeLevel.Blue, 2 },
        { BadgeLevel.Red, 3 },
        { BadgeLevel.Gold, 4 }
    };

    /// <summary>
    /// Determines if progression from current level to next level is valid
    /// </summary>
    public bool CanProgressTo(BadgeLevel current, BadgeLevel next)
    {
        var currentOrder = LevelHierarchy.GetValueOrDefault(current, 0);
        var nextOrder = LevelHierarchy.GetValueOrDefault(next, 0);

        // Can only progress to the immediate next level
        return nextOrder == currentOrder + 1;
    }

    /// <summary>
    /// Gets the next level in the progression hierarchy
    /// </summary>
    public BadgeLevel? GetNextLevel(BadgeLevel current)
    {
        var currentOrder = LevelHierarchy.GetValueOrDefault(current, 0);
        var nextOrder = currentOrder + 1;

        var nextLevel = LevelHierarchy.FirstOrDefault(kvp => kvp.Value == nextOrder);
        return nextLevel.Key != default ? nextLevel.Key : null;
    }

    /// <summary>
    /// Gets the previous level in the progression hierarchy
    /// </summary>
    public BadgeLevel? GetPreviousLevel(BadgeLevel current)
    {
        var currentOrder = LevelHierarchy.GetValueOrDefault(current, 0);
        var previousOrder = currentOrder - 1;

        if (previousOrder < 1)
            return null;

        var previousLevel = LevelHierarchy.FirstOrDefault(kvp => kvp.Value == previousOrder);
        return previousLevel.Key != default ? previousLevel.Key : null;
    }

    /// <summary>
    /// Checks if a level is the highest achievable level
    /// </summary>
    public bool IsMaxLevel(BadgeLevel level)
    {
        var maxOrder = LevelHierarchy.Values.Max();
        var currentOrder = LevelHierarchy.GetValueOrDefault(level, 0);
        return currentOrder == maxOrder;
    }

    /// <summary>
    /// Checks if a level is the starting level
    /// </summary>
    public bool IsStartingLevel(BadgeLevel level)
    {
        var minOrder = LevelHierarchy.Values.Min();
        var currentOrder = LevelHierarchy.GetValueOrDefault(level, 0);
        return currentOrder == minOrder;
    }

    /// <summary>
    /// Gets the level order (1-4)
    /// </summary>
    public int GetLevelOrder(BadgeLevel level)
    {
        return LevelHierarchy.GetValueOrDefault(level, 0);
    }

    /// <summary>
    /// Compares two badge levels
    /// </summary>
    public int CompareLevels(BadgeLevel level1, BadgeLevel level2)
    {
        var order1 = LevelHierarchy.GetValueOrDefault(level1, 0);
        var order2 = LevelHierarchy.GetValueOrDefault(level2, 0);
        return order1.CompareTo(order2);
    }
}
