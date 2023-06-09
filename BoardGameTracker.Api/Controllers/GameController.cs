﻿using AutoMapper;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Helpers;
using BoardGameTracker.Common.ViewModels;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Core.Disk.Interfaces;
using BoardGameTracker.Core.Games.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/game")]
public class GameController
{
    private readonly IDiskProvider _diskProvider;
    private readonly IGameService _gameService;
    private readonly IMapper _mapper;

    public GameController(IDiskProvider diskProvider, IGameService gameService, IMapper mapper)
    {
        _diskProvider = diskProvider;
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
         var game = await _gameService.GetGame(id);
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
}