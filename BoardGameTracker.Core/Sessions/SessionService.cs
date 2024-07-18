using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Sessions.Interfaces;

namespace BoardGameTracker.Core.Sessions;

public class SessionService : ISessionService
{
    private readonly ISessionRepository _sessionRepository;

    public SessionService(ISessionRepository sessionRepository)
    {
        _sessionRepository = sessionRepository;
    }

    public Task<Session> Create(Session session)
    {
        return _sessionRepository.Create(session);
    }

    public Task Delete(int id)
    {
        return _sessionRepository.Delete(id);
    }

    public Task<Session> Update(Session session)
    {
        return _sessionRepository.Update(session);
    }
}