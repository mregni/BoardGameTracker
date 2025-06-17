using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Core.Badges.Interfaces;

public interface IBadgeEvaluator
{
    BadgeType BadgeType { get; }
    Task<bool> CanAwardBadge(int playerId, Badge badge, Session session);
}