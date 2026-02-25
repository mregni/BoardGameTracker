using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Core.Languages.Interfaces;
using BoardGameTracker.Core.Settings.Interfaces;
using BoardGameTracker.Core.Updates.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/settings")]
[Authorize]
public class SettingsController : ControllerBase
{
    private readonly ISettingsService _settingsService;
    private readonly IEnvironmentProvider _environmentProvider;
    private readonly ILanguageService _languageService;
    private readonly IUpdateService _updateService;

    public SettingsController(
        ISettingsService settingsService,
        IEnvironmentProvider environmentProvider,
        ILanguageService languageService,
        IUpdateService updateService)
    {
        _settingsService = settingsService;
        _environmentProvider = environmentProvider;
        _languageService = languageService;
        _updateService = updateService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Get()
    {
        var settings = await _settingsService.GetSettingsAsync();
        return Ok(settings);
    }

    [HttpGet("version-info")]
    [AllowAnonymous]
    public async Task<IActionResult> GetVersionInfo()
    {
        var status = await _updateService.GetVersionInfoAsync();
        return Ok(status.ToDto());
    }

    [HttpPut]
    [Authorize(Roles = Constants.AuthRoles.Admin)]
    public async Task<IActionResult> Update([FromBody] UIResourceDto model)
    {
        var updated = await _settingsService.UpdateSettingsAsync(model);
        return Ok(updated);
    }

    [HttpGet("environment")]
    [Authorize(Roles = Constants.AuthRoles.Admin)]
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

    [HttpGet("languages")]
    [AllowAnonymous]
    public async Task<IActionResult> GetLanguages()
    {
        var languages = await _languageService.GetAllAsync();
        return Ok(languages);
    }
}
