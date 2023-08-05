using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Plays.Interfaces;

public interface IPlayRepository
{
    Task CreatePlay(Play play);
}