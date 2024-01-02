using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.ViewModels;
using BoardGameTracker.Core.Images.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/image")]
public class ImageController
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
                var result = new CreationResultViewModel<string>(CreationResultType.Success, name);
                return new OkObjectResult(result);
            }
            
            var failedResult = new CreationResultViewModel<string>(CreationResultType.Failed, string.Empty, "Type not supported");
            return new OkObjectResult(failedResult);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while uploading image");
            var result = new CreationResultViewModel<string>(CreationResultType.Failed, string.Empty, "Error while uploading image");
            return new OkObjectResult(result);
        }
    }
}