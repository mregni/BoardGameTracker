using AutoMapper;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.ViewModels;
using BoardGameTracker.Common.ViewModels.Language;
using BoardGameTracker.Common.ViewModels.Results;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Core.Languages.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Sentry;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/settings")]
public class SettingsController
{
    private readonly IConfigFileProvider _configFileProvider;
    private readonly IEnvironmentProvider _environmentProvider;
    private readonly ILanguageService _languageService;
    private readonly IMapper _mapper;
    public SettingsController(IConfigFileProvider configFileProvider, IEnvironmentProvider environmentProvider, ILanguageService languageService, IMapper mapper)
    {
        _configFileProvider = configFileProvider;
        _environmentProvider = environmentProvider;
        _languageService = languageService;
        _mapper = mapper;
    }

    [HttpGet]
    public IActionResult Get()
    {
        var uiResources = new UIResourceViewModel
        {
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
        _configFileProvider.UILanguage = model.UILanguage;
        
        return new OkObjectResult(model);
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
    
    [HttpGet]
    [Route("languages")]
    public async Task<IActionResult> GetLanguages()
    {
        var languages = await _languageService.GetAllAsync();

        var mappedLanguages = _mapper.Map<IList<LanguageViewModel>>(languages);
        return new OkObjectResult(mappedLanguages);
    }
}