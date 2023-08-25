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

    public Task Create(Play play)
    {
        return _playRepository.Create(play);
    }

    public Task Delete(int id)
    {
        return _playRepository.Delete(id);
    }

    public Task Update(Play play)
    {
        return _playRepository.Update(play);
    }
}