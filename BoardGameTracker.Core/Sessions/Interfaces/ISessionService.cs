using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Sessions.Interfaces;

public interface ISessionService
{
    Task<Session> CreateFromCommand(CreateSessionCommand command);
    Task Delete(int id);
    Task<Session> UpdateFromCommand(UpdateSessionCommand command);
    Task<Session?> Get(int id);
}