using AutoMapper;
using BoardGameTracker.Common.Models.Bgg;
using BoardGameTracker.Core.Bgg.Interfaces;

namespace BoardGameTracker.Core.Bgg;

public class BggService : IBggService
{
    private readonly IBggApi _bggApi;
    private readonly IMapper _mapper;
    public BggService(IBggApi bggApi, IMapper mapper)
    {
        _bggApi = bggApi;
        _mapper = mapper;
    }

    public async Task<BggGame?> SearchGame(int id)
    {
        var response = await _bggApi.SearchGame("boardgame", 1, id);
        var firstResult = response.Content?.Games?.FirstOrDefault();
        if (!response.IsSuccessStatusCode || firstResult == null)
        {
            return null;
        }

        return _mapper.Map<BggGame>(firstResult);
    }
}