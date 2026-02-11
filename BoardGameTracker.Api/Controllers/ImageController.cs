using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Core.Images.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/image")]
public class ImageController : ControllerBase
{
    private readonly IImageService _imageService;

    public ImageController(IImageService imageService)
    {
        _imageService = imageService;
    }

    [HttpPost]
    public async Task<IActionResult> UploadImage([FromForm] UploadImageCommand command)
    {
        var name = await _imageService.SaveImage(command.File, command.Type);
        return Ok(name);
    }
}
