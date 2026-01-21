using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Core.Languages.Interfaces;
using BoardGameTracker.Core.Updates.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/settings")]
public class SettingsController : ControllerBase
{
    private readonly IConfigFileProvider _configFileProvider;
    private readonly IEnvironmentProvider _environmentProvider;
    private readonly ILanguageService _languageService;
    private readonly IUpdateService _updateService;

    public SettingsController(
        IConfigFileProvider configFileProvider,
        IEnvironmentProvider environmentProvider,
        ILanguageService languageService,
        IUpdateService updateService)
    {
        _configFileProvider = configFileProvider;
        _environmentProvider = environmentProvider;
        _languageService = languageService;
        _updateService = updateService;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var updateSettings = await _updateService.GetUpdateSettingsAsync();

        var uiResources = new UIResourceDto
        {
            TimeFormat = _configFileProvider.TimeFormat,
            DateFormat = _configFileProvider.DateFormat,
            UILanguage = _configFileProvider.UILanguage,
            Currency = _configFileProvider.Currency,
            Statistics = _environmentProvider.EnableStatistics,
            UpdateCheckEnabled = updateSettings.Enabled,
            UpdateCheckIntervalHours = updateSettings.IntervalHours
        };

        return Ok(uiResources);
    }

    [HttpGet("version-info")]
    public async Task<IActionResult> GetVersionInfo()
    {
        var status = await _updateService.GetVersionInfoAsync();
        return Ok(status.ToDto());
    }
    
    [HttpPut]
    [Route("")]
    public async Task<IActionResult> Update([FromBody] UIResourceDto model)
    {
        _configFileProvider.Currency = model.Currency;
        _configFileProvider.TimeFormat = model.TimeFormat;
        _configFileProvider.DateFormat = model.DateFormat;
        _configFileProvider.UILanguage = model.UILanguage;

        await _updateService.UpdateSettingsAsync(model.UpdateCheckEnabled, model.UpdateCheckIntervalHours);

        return Ok(model);
    }

    [HttpGet]
    [Route("environment")]
    public IActionResult GetEnvironment()
    {
        var resources = new UIEnvironmentDto
        {
            EnableStatistics = _environmentProvider.EnableStatistics,
            LogLevel = _environmentProvider.LogLevel,
            EnvironmentName = _environmentProvider.EnvironmentName,
            Port = _environmentProvider.Port,
            Version = _updateService.GetCurrentVersion()
        };

        return Ok(resources);
    }

    [HttpGet]
    [Route("languages")]
    public async Task<IActionResult> GetLanguages()
    {
        var languages = await _languageService.GetAllAsync();
        return Ok(languages);
    }
}
