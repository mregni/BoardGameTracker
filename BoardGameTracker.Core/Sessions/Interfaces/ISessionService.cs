using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Sessions.Interfaces;

public interface ISessionService
{
    Task<Session> Create(Session session);
    Task Delete(int id);
    Task<Session> Update(Session session);
    Task<Session?> Get(int id);
}