using AutoMapper;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.ViewModels;
using BoardGameTracker.Common.ViewModels.Results;
using BoardGameTracker.Core.Games.Interfaces;
using BoardGameTracker.Core.Plays.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/play")]
public class PlayController : ControllerBase
{
    private readonly IPlayService _playService;
    private readonly IMapper _mapper;
    private readonly ILogger<PlayController> _logger;

    public PlayController(IPlayService playService, IMapper mapper, ILogger<PlayController> logger)
    {
        _playService = playService;
        _mapper = mapper;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> CreatePlay([FromBody] PlayViewModel? viewModel)
    {
        if (viewModel == null)
        {
            //TODO: Add to translation file
            var failedViewModel = new FailResultViewModel("No data provided");
            return new OkObjectResult(failedViewModel);
        }

        try
        {
            var play = _mapper.Map<Play>(viewModel);
            play = await _playService.Create(play);

            var result = _mapper.Map<PlayViewModel>(play);
            return ResultViewModel<PlayViewModel>.CreateCreatedResult( result);
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            //TODO: Add to translation file
            var failedViewModel = new FailResultViewModel("Creation failed because of backend error, check logs for details");
            return StatusCode(500, failedViewModel);
        }
    }
    
    [HttpPut]
    public async Task<IActionResult> UpdatePlay([FromBody] PlayViewModel? updateViewModel)
    {
        if (updateViewModel is not {Id: not null})
        {
            //TODO: Add to translation file
            var failedViewModel = new FailResultViewModel("No data provided");
            return new OkObjectResult(failedViewModel);
        }

        var play = _mapper.Map<Play>(updateViewModel);
        try
        {
            var result = await _playService.Update(play);
            var viewModel = _mapper.Map<PlayViewModel>(result);
            return ResultViewModel<PlayViewModel>.CreateUpdatedResult(viewModel);
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            //TODO: Add to translation file
            var failedViewModel = new FailResultViewModel("Update failed because of backend error, check logs for details");
            return StatusCode(500, failedViewModel);
        }
    }
    
    [HttpDelete]
    [Route("{id:int}")]
    public async Task<IActionResult> DeletePlay(int id)
    {
        await _playService.Delete(id);
        return new OkObjectResult(new DeletionResultViewModel(ResultState.Success));
    }
}