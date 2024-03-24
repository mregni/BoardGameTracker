using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Plays.Interfaces;

public interface IPlayRepository
{
    Task<Play> Create(Play play);
    Task Delete(int id);
    Task<Play> Update(Play play);
}