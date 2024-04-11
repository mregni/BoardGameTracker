using System.Net;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.ViewModels;
using BoardGameTracker.Common.ViewModels.Results;
using BoardGameTracker.Core.Images.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/image")]
public class ImageController : ControllerBase
{
    private readonly IImageService _imageService;
    private readonly ILogger<ImageController> _logger;

    public ImageController(IImageService imageService, ILogger<ImageController> logger)
    {
        _imageService = imageService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> UploadImage([FromForm] FileUploadViewModel upload)
    {
        try
        {
            if (upload.Type == UploadFileType.Profile)
            {
                var name = await _imageService.SaveProfileImage(upload.File);
                return ResultViewModel<string>.CreateCreatedResult($"/{name}");
            }
            
            var failedResult = new FailResultViewModel("error.image.type-not-supported");
            return new BadRequestObjectResult(failedResult);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while uploading image");
            var result = new FailResultViewModel("error.image.error");
            return StatusCode(500, result);
        }
    }
}