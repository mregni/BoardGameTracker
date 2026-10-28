using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Common;
using BoardGameTracker.Core.Integrations.Interfaces;
using BoardGameTracker.Core.Languages.Interfaces;
using BoardGameTracker.Core.Settings.Interfaces;
using BoardGameTracker.Core.Updates.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/settings")]
[Authorize]
public class SettingsController : ControllerBase
{
    private readonly ISettingsService _settingsService;
    private readonly ILanguageService _languageService;
    private readonly IUpdateService _updateService;
    private readonly IIntegrationStatusService _integrationStatusService;

    public SettingsController(
        ISettingsService settingsService,
        ILanguageService languageService,
        IUpdateService updateService,
        IIntegrationStatusService integrationStatusService)
    {
        _settingsService = settingsService;
        _languageService = languageService;
        _updateService = updateService;
        _integrationStatusService = integrationStatusService;
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<UIResourceDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get()
    {
        var settings = await _settingsService.GetSettingsAsync();
        if (User?.IsInRole(Constants.AuthRoles.Admin) != true)
        {
            settings.ChangeDetectionBaseUrl = string.Empty;
        }

        return Ok(settings);
    }

    [HttpGet("version-info")]
    [ProducesResponseType<UpdateStatusDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVersionInfo()
    {
        var status = await _updateService.GetVersionInfoAsync();
        return Ok(status.ToDto());
    }

    [HttpPut]
    [Authorize(Roles = Constants.AuthRoles.Admin)]
    [ProducesResponseType<UIResourceDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Update([FromBody] UIResourceDto model)
    {
        var updated = await _settingsService.UpdateSettingsAsync(model);
        _integrationStatusService.Invalidate();
        return Ok(updated);
    }

    [HttpGet("languages")]
    [AllowAnonymous]
    [ProducesResponseType<List<Language>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLanguages()
    {
        var languages = await _languageService.GetAllAsync();
        return Ok(languages);
    }
}
