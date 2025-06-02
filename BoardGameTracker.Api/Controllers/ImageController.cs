using BoardGameTracker.Common.ViewModels;
using BoardGameTracker.Core.Images.Interfaces;
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
            var name = await _imageService.SaveImage(upload.File, upload.Type);
            return new OkObjectResult(name);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while uploading image");
            return StatusCode(500);
        }
    }
}