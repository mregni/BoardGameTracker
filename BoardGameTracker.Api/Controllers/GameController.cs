using BoardGameTracker.Common.Helpers;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Core.Disk.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GameController
{
    private readonly IConfigFileProvider _configFileProvider;
    private readonly IDiskProvider _diskProvider;

    public GameController(IConfigFileProvider configFileProvider, IDiskProvider diskProvider)
    {
        _configFileProvider = configFileProvider;
        _diskProvider = diskProvider;
    }

    [HttpPost]
    [Route("image")]
    public async Task<IActionResult> UploadImage(IFormFile? file)
    {
        if (file is not {Length: > 0})
        {
            return new OkObjectResult("No file to upload");
        }

        var name = await _diskProvider.WriteFile(file, PathHelper.TempFilePath);
        return new OkObjectResult(new { fileName = name });
    }
}