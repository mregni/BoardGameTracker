using AutoMapper;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.ViewModels;
using BoardGameTracker.Core.Games.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/game")]
public class GameController
{
    private readonly IGameService _gameService;
    private readonly IMapper _mapper;

    public GameController(IGameService gameService, IMapper mapper)
    {
        _gameService = gameService;
        _mapper = mapper;
    }
    
     [HttpGet]
     public async Task<IActionResult> GetGames()
     {
         var games = await _gameService.GetGames();
         var mappedGames = _mapper.Map<IList<GameViewModel>>(games);

         var resultViewModel = new ListResultViewModel<GameViewModel>(mappedGames);
         return new OkObjectResult(resultViewModel);
     }

     [HttpGet]
     [Route("{id:int}")]
     public async Task<IActionResult> GetGameById(int id)
     {
         var game = await _gameService.GetGameById(id);
         if (game == null)
         {
             return new OkObjectResult(SearchResultViewModel<GameViewModel>.CreateSearchResult(null));
         }

         var viewModel = _mapper.Map<GameViewModel>(game);
         return new OkObjectResult(SearchResultViewModel<GameViewModel>.CreateSearchResult(viewModel));
     }
     
     [HttpDelete]
     [Route("{id:int}")]
     public async Task<IActionResult> DeleteGameById(int id)
     {
         await _gameService.Delete(id);
         return new OkObjectResult(new CreationResultViewModel<string>(CreationResultType.Success, null));
     }

     [HttpGet]
     [Route("{id:int}/plays")]
     public async Task<IActionResult> GetGamePlays(int id, [FromQuery] int skip, [FromQuery] int take)
     {
         var plays = await _gameService.GetPlays(id);

         var playViewModel = _mapper.Map<IList<PlayViewModel>>(plays);
         return new OkObjectResult(SearchResultViewModel<IList<PlayViewModel>>.CreateSearchResult(playViewModel)); 
     }
     
     
     [HttpGet]
     [Route("{id:int}/stats")]
     public async Task<IActionResult> GetGameStats(int id)
     {
         var stats = await _gameService.GetStats(id);

         var statsViewModel = _mapper.Map<GameStatisticsViewModel>(stats);
         return new OkObjectResult(SearchResultViewModel<GameStatisticsViewModel>.CreateSearchResult(statsViewModel)); 
     }
}