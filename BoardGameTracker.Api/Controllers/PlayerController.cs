﻿using AutoMapper;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.ViewModels;
using BoardGameTracker.Core.Players.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/player")]
public class PlayerController
{
    private readonly IPlayerService _playerService;
    private readonly IMapper _mapper;
    private readonly ILogger<PlayerController> _logger;

    public PlayerController(IPlayerService playerService, IMapper mapper, ILogger<PlayerController> logger)
    {
        _playerService = playerService;
        _mapper = mapper;
        _logger = logger;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetPlayers()
    {
        var games = await _playerService.GetList();
        var mappedGames = _mapper.Map<IList<PlayerViewModel>>(games);

        var resultViewModel = new ListResultViewModel<PlayerViewModel>(mappedGames);
        return new OkObjectResult(resultViewModel);
    }

    [HttpPost]
    public async Task<IActionResult> CreatePlayer([FromBody] PlayerCreationViewModel? playerViewModel)
    {
        if (playerViewModel == null)
        {
            var failedViewModel = new CreationResultViewModel<PlayerCreationViewModel>(CreationResultType.Failed, null, "No data provided");
            return new OkObjectResult(failedViewModel);
        }

        var player = _mapper.Map<Player>(playerViewModel);

        try
        {
            await _playerService.Create(player);
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            var failedViewModel = new CreationResultViewModel<PlayerViewModel>(CreationResultType.Failed, null, "Creation failed because of backend error, check logs for details");
            return new OkObjectResult(failedViewModel);
        }    
        
        var resultViewModel = new CreationResultViewModel<PlayerViewModel>(CreationResultType.Success, null);
        return new OkObjectResult(resultViewModel);
    }
    
    [HttpPut]
    public async Task<IActionResult> UpdatePlayer([FromBody] PlayerViewModel? viewModel)
    {
        if (viewModel is not {Id: { }})
        {
            var failedViewModel = new CreationResultViewModel<PlayerViewModel>(CreationResultType.Failed, null, "No data provided");
            return new OkObjectResult(failedViewModel);
        }

        var player = _mapper.Map<Player>(viewModel);
        try
        {
            await _playerService.Update(player);
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            var failedViewModel = new CreationResultViewModel<PlayerViewModel>(CreationResultType.Failed, null, "Update failed because of backend error, check logs for details");
            return new OkObjectResult(failedViewModel);
        }
        
        var resultViewModel = new CreationResultViewModel<PlayerViewModel>(CreationResultType.Success, viewModel);
        return new OkObjectResult(resultViewModel);
    }

    
    [HttpGet]
    [Route("{id:int}")]
    public async Task<IActionResult> GetPlayerById(int id)
    {
        var player = await _playerService.Get(id);
        if (player == null)
        {
            return new OkObjectResult(SearchResultViewModel<PlayerViewModel>.CreateSearchResult(null));
        }

        var viewModel = _mapper.Map<PlayerViewModel>(player);
        return new OkObjectResult(SearchResultViewModel<PlayerViewModel>.CreateSearchResult(viewModel));
    }
     
    [HttpDelete]
    [Route("{id:int}")]
    public async Task<IActionResult> DeleteGameById(int id)
    {
        await _playerService.Delete(id);
        return new OkObjectResult(new CreationResultViewModel<string>(CreationResultType.Success, null));
    }
    
    [HttpGet]
    [Route("{id:int}/plays")]
    public async Task<IActionResult> GetGamePlays(int id, [FromQuery] int skip, [FromQuery] int take)
    {
        var plays = await _playerService.GetPlays(id);

        var playViewModel = _mapper.Map<IList<PlayViewModel>>(plays);
        return new OkObjectResult(SearchResultViewModel<IList<PlayViewModel>>.CreateSearchResult(playViewModel)); 
    }
    
    [HttpGet]
    [Route("{id:int}/stats")]
    public async Task<IActionResult> GetGameStats(int id)
    {
        var stats = await _playerService.GetStats(id);

        var statsViewModel = _mapper.Map<PlayerStatisticsViewModel>(stats);
        return new OkObjectResult(SearchResultViewModel<PlayerStatisticsViewModel>.CreateSearchResult(statsViewModel)); 
    }
}