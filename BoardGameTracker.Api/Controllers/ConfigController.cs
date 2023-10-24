using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.ViewModels;
using BoardGameTracker.Core.Configuration.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/settings")]
public class ConfigController
{
    private readonly IConfigFileProvider _configFileProvider;

    public ConfigController(IConfigFileProvider configFileProvider)
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
            UILanguage = _configFileProvider.UILanguage,
            Currency = _configFileProvider.Currency,
            DecimalSeparator = _configFileProvider.DecimalSeparator
        };

        return new OkObjectResult(uiResources);
    }

    [HttpPut]
    public IActionResult Update([FromBody] UIResourceViewModel model)
    {
        _configFileProvider.Currency = model.Currency;
        _configFileProvider.DecimalSeparator = model.DecimalSeparator;
        
        var resultViewModel = new CreationResultViewModel<UIResourceViewModel>(CreationResultType.Success, model);
        return new OkObjectResult(resultViewModel);
    }
}