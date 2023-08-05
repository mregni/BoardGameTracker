using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Plays.Interfaces;

namespace BoardGameTracker.Core.Plays;

public class PlayService : IPlayService
{
    private readonly IPlayRepository _playRepository;

    public PlayService(IPlayRepository playRepository)
    {
        _playRepository = playRepository;
    }

    public Task CreatePlay(Play play)
    {
        return _playRepository.CreatePlay(play);
    }
}