using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Plays.Interfaces;

public interface IPlayService
{
    Task Create(Play play);
    Task Delete(int id);
    Task Update(Play play);
}