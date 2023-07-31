using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.ViewModels;
using BoardGameTracker.Core.Images.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/image")]
public class ImageController
{
    private readonly IImageService _imageService;

    public ImageController(IImageService imageService)
    {
        _imageService = imageService;
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
            var result = new CreationResultViewModel<string>(CreationResultType.Failed, string.Empty);
            return new OkObjectResult(result);
        }
    }
}