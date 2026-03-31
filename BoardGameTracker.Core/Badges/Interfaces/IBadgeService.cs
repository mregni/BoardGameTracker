using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Badges.Interfaces;

public interface IBadgeService
{
    Task AwardBadgesAsync(Session session);
    Task<List<Badge>> GetAllBadgesAsync();
}