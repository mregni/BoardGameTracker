namespace BoardGameTracker.Core.Badges.BadgeEvaluators;

/// <summary>
/// Constants used by badge evaluators for business logic
/// </summary>
public static class BadgeEvaluatorConstants
{
    /// <summary>
    /// Number of days in a week for consistent schedule calculations
    /// </summary>
    public const int DaysInWeek = 7;

    /// <summary>
    /// Number of months to look back for monthly goal calculations
    /// </summary>
    public const int MonthlyGoalLookbackMonths = 1;

    /// <summary>
    /// Number of consecutive weeks required for consistency badges
    /// </summary>
    public const int ConsistentWeeksRequired = 4;
}
