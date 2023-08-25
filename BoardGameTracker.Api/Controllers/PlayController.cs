using AutoMapper;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.ViewModels;
using BoardGameTracker.Core.Games.Interfaces;
using BoardGameTracker.Core.Plays.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/play")]
public class PlayController
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
            var failedViewModel = new CreationResultViewModel<PlayViewModel>(CreationResultType.Failed, null, "No data provided");
            return new OkObjectResult(failedViewModel);
        }

        var play = _mapper.Map<Play>(viewModel);
        try
        {
            await _playService.Create(play);
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            var failedViewModel = new CreationResultViewModel<PlayerViewModel>(CreationResultType.Failed, null, "Creation failed because of backend error, check logs for details");
            return new OkObjectResult(failedViewModel);
        }
        
        var resultViewModel = new CreationResultViewModel<PlayViewModel>(CreationResultType.Success, null);
        return new OkObjectResult(resultViewModel);
    }
    
    [HttpPut]
    public async Task<IActionResult> UpdatePlay([FromBody] PlayViewModel? viewModel)
    {
        if (viewModel is not {id: { }})
        {
            var failedViewModel = new CreationResultViewModel<PlayViewModel>(CreationResultType.Failed, null, "No data provided");
            return new OkObjectResult(failedViewModel);
        }

        var play = _mapper.Map<Play>(viewModel);
        try
        {
            await _playService.Update(play);
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            var failedViewModel = new CreationResultViewModel<PlayerViewModel>(CreationResultType.Failed, null, "Update failed because of backend error, check logs for details");
            return new OkObjectResult(failedViewModel);
        }
        
        var resultViewModel = new CreationResultViewModel<PlayViewModel>(CreationResultType.Success, null);
        return new OkObjectResult(resultViewModel);
    }
    
    [HttpDelete]
    [Route("{id:int}")]
    public async Task<IActionResult> DeletePlay(int id)
    {
        await _playService.Delete(id);
        return new OkObjectResult(new CreationResultViewModel<string>(CreationResultType.Success, null));
    }
}