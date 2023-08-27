using AutoMapper;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Common.Models.Bgg;
using BoardGameTracker.Common.ViewModels;
using BoardGameTracker.Core.Bgg.Interfaces;
using BoardGameTracker.Core.Games.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/bgg")]
public class BggController
{
    private readonly IBggService _bggService;
    private readonly IGameService _gameService;
    private readonly IMapper _mapper;

    public BggController(IBggService bggService, IMapper mapper, IGameService gameService)
    {
        _bggService = bggService;
        _mapper = mapper;
        _gameService = gameService;
    }

    [HttpPost("search")]
    public async Task<IActionResult> SearchOnBgg([FromBody] BggSearch search)
    {
        var existingGame = await _gameService.GetGameByBggId(search.BggId);
        if (existingGame != null)
        {
            var existingGameViewModel = _mapper.Map<GameViewModel>(existingGame);
            return new OkObjectResult(SearchResultViewModel<GameViewModel>.CreateDuplicateResult(existingGameViewModel));
        }

        var game = await _bggService.SearchGame(search.BggId);
        if (game == null)
        {
            return new OkObjectResult(SearchResultViewModel<GameViewModel>.CreateSearchResult(null));
        }

        var dbGame = await _gameService.ProcessBggGameData(game, search);
        var result = _mapper.Map<GameViewModel>(dbGame);
        return new OkObjectResult(SearchResultViewModel<GameViewModel>.CreateSearchResult(result));
    }
}