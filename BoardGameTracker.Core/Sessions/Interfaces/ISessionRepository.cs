using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Sessions.Interfaces;

public interface ISessionRepository
{
    Task<Session> Create(Session session);
    Task Delete(int id);
    Task<Session> Update(Session session);
}