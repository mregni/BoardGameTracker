using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.GameNights.Interfaces;

public interface IRunningGameService
{
    Task<RunningGame?> GetById(int id);
    Task<RunningGame> Start(int gameNightId, StartRunningGameCommand command);
    Task<Session> Finish(int id, FinishRunningGameCommand command);
    Task Delete(int id);
}
