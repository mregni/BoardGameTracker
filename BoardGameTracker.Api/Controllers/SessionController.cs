using AutoMapper;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.ViewModels;
using BoardGameTracker.Common.ViewModels.Results;
using BoardGameTracker.Core.Games.Interfaces;
using BoardGameTracker.Core.Sessions.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/session")]
public class SessionController : ControllerBase
{
    private readonly ISessionService _sessionService;
    private readonly IMapper _mapper;
    private readonly ILogger<SessionController> _logger;

    public SessionController(ISessionService sessionService, IMapper mapper, ILogger<SessionController> logger)
    {
        _sessionService = sessionService;
        _mapper = mapper;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> CreateSession([FromBody] SessionViewModel? viewModel)
    {
        if (viewModel == null)
        {
            //TODO: Add to translation file
            var failedViewModel = new FailResultViewModel("No data provided");
            return new OkObjectResult(failedViewModel);
        }

        try
        {
            var play = _mapper.Map<Session>(viewModel);
            play = await _sessionService.Create(play);

            var result = _mapper.Map<SessionViewModel>(play);
            return ResultViewModel<SessionViewModel>.CreateCreatedResult( result);
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
    public async Task<IActionResult> UpdateSession([FromBody] SessionViewModel? updateViewModel)
    {
        if (updateViewModel is not {Id: not null})
        {
            //TODO: Add to translation file
            var failedViewModel = new FailResultViewModel("No data provided");
            return new OkObjectResult(failedViewModel);
        }

        var play = _mapper.Map<Session>(updateViewModel);
        try
        {
            var result = await _sessionService.Update(play);
            var viewModel = _mapper.Map<SessionViewModel>(result);
            return ResultViewModel<SessionViewModel>.CreateUpdatedResult(viewModel);
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
    public async Task<IActionResult> DeleteSession(int id)
    {
        await _sessionService.Delete(id);
        return new OkObjectResult(new DeletionResultViewModel(ResultState.Success));
    }
}