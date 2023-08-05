using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Plays.Interfaces;

public interface IPlayService
{
    Task CreatePlay(Play play);
}