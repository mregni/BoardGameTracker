using BoardGameTracker.Common.ViewModels;
using BoardGameTracker.Core.Configuration.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/settings")]
public class SettingsController
{
    private readonly IConfigFileProvider _configFileProvider;

    public SettingsController(IConfigFileProvider configFileProvider)
    {
        _configFileProvider = configFileProvider;
    }

    [HttpGet]
    public IActionResult Get()
    {
        var uiResources = new UIResourceViewModel
        {
            TimeZone = _configFileProvider.TimeZone,
            DateTimeFormat = _configFileProvider.DateTimeFormat,
            DateFormat = _configFileProvider.DateFormat,
            UILanguage = _configFileProvider.UILanguage
        };

        return new OkObjectResult(uiResources);
    }
}