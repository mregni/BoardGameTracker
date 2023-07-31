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
            TimeFormat = _configFileProvider.TimeFormat,
            TimeZone = _configFileProvider.TimeZone,
            LongDateFormat = _configFileProvider.LongDateFormat,
            ShortDateFormat = _configFileProvider.ShortDateFormat,
            UILanguage = _configFileProvider.UILanguage
        };

        return new OkObjectResult(uiResources);
    }
}