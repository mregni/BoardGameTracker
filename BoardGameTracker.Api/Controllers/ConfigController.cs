using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.ViewModels;
using BoardGameTracker.Core.Configuration.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Sentry;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/settings")]
public class ConfigController
{
    private readonly IConfigFileProvider _configFileProvider;
    private readonly IEnvironmentProvider _environmentProvider;
    
    public ConfigController(IConfigFileProvider configFileProvider, IEnvironmentProvider environmentProvider)
    {
        _configFileProvider = configFileProvider;
        _environmentProvider = environmentProvider;
    }

    [HttpGet]
    public IActionResult Get()
    {
        var uiResources = new UIResourceViewModel
        {
            TimeZone = _configFileProvider.TimeZone,
            TimeFormat = _configFileProvider.TimeFormat,
            DateFormat = _configFileProvider.DateFormat,
            UILanguage = _configFileProvider.UILanguage,
            Currency = _configFileProvider.Currency,
            DecimalSeparator = _configFileProvider.DecimalSeparator,
            Statistics = _environmentProvider.EnableStatistics
        };

        return new OkObjectResult(uiResources);
    }

    [HttpPut]
    public IActionResult Update([FromBody] UIResourceViewModel model)
    {
        _configFileProvider.Currency = model.Currency;
        _configFileProvider.DecimalSeparator = model.DecimalSeparator;
        _configFileProvider.TimeFormat = model.TimeFormat;
        _configFileProvider.DateFormat = model.DateFormat;
        
        var resultViewModel = new CreationResultViewModel<UIResourceViewModel>(CreationResultType.Success, model);
        return new OkObjectResult(resultViewModel);
    }

    [HttpGet]
    [Route("environment")]
    public IActionResult GetEnvironment()
    {
        var resources = new UIEnvironmentViewModel
        {
            EnableStatistics = _environmentProvider.EnableStatistics,
            LogLevel = _environmentProvider.LogLevel,
            EnvironmentName = _environmentProvider.EnvironmentName,
            Port = _environmentProvider.Port
        };
        
        return new OkObjectResult(resources);
    }
}