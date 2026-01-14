using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Sessions.Interfaces;

public interface ISessionService
{
    Task<Session> Create(Session session);
    Task<Session> CreateFromDto(SessionDto dto);
    Task<Session> CreateFromCommand(CreateSessionCommand command);
    Task Delete(int id);
    Task<Session> Update(Session session);
    Task<Session> UpdateFromDto(SessionDto dto);
    Task<Session> UpdateFromCommand(UpdateSessionCommand command);
    Task<Session?> Get(int id);
}