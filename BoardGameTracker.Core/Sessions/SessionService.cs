using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Badges.Interfaces;
using BoardGameTracker.Core.Sessions.Interfaces;

namespace BoardGameTracker.Core.Sessions;

public class SessionService : ISessionService
{
    private readonly ISessionRepository _sessionRepository;
    private readonly IBadgeService _badgeService;

    public SessionService(ISessionRepository sessionRepository, IBadgeService badgeService)
    {
        _sessionRepository = sessionRepository;
        _badgeService = badgeService;
    }

    public async Task<Session> Create(Session session)
    {
        session = await _sessionRepository.CreateAsync(session);
        await _badgeService.AwardBadgesAsync(session);
        
        return session;
    }

    public Task Delete(int id)
    {
        return _sessionRepository.DeleteAsync(id);
    }

    public async Task<Session> Update(Session session)
    {
        session = await _sessionRepository.UpdateAsync(session);
        await _badgeService.AwardBadgesAsync(session);
        
        return session;
    }

    public Task<Session?> Get(int id)
    {
        return _sessionRepository.GetByIdAsync(id);
    }
}