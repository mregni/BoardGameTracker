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
    private readonly IGameService _gameService;
    private readonly IMapper _mapper;
    private readonly ILogger<SessionController> _logger;

    public SessionController(ISessionService sessionService, IMapper mapper, ILogger<SessionController> logger, IGameService gameService)
    {
        _sessionService = sessionService;
        _mapper = mapper;
        _logger = logger;
        _gameService = gameService;
    }
    
    [HttpGet]
    [Route("{id:int}")]
    public async Task<IActionResult> GetSession(int id)
    {
        var session = await _sessionService.Get(id);
        if (session == null)
        {
            return new NotFoundResult();
        }
        
        var mapped = _mapper.Map<SessionViewModel>(session);
        return new OkObjectResult(mapped);
    }

    [HttpPost]
    public async Task<IActionResult> CreateSession([FromBody] CreateSessionViewModel? viewModel)
    {
        if (viewModel == null)
        {
            return new BadRequestResult();
        }

        try
        {
            var play = _mapper.Map<Session>(viewModel);
            play.Expansions = await _gameService.GetGameExpansions(viewModel.ExpansionIds);
            play = await _sessionService.Create(play);

            var result = _mapper.Map<SessionViewModel>(play);
            return new OkObjectResult(result);
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            return StatusCode(500);
        }
    }
    
    [HttpPut]
    public async Task<IActionResult> UpdateSession([FromBody] CreateSessionViewModel? updateViewModel)
    {
        if (updateViewModel is not {Id: not null})
        {
            return new BadRequestResult();
        }

        var play = _mapper.Map<Session>(updateViewModel);
        try
        {
            play.Expansions = await _gameService.GetGameExpansions(updateViewModel.ExpansionIds);
            var result = await _sessionService.Update(play);
            var viewModel = _mapper.Map<SessionViewModel>(result);
            return new OkObjectResult(viewModel);
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            return StatusCode(500);
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