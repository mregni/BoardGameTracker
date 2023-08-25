using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Plays.Interfaces;

public interface IPlayRepository
{
    Task Create(Play play);
    Task Delete(int id);
    Task Update(Play play);
}