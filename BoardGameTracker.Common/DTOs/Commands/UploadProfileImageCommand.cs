using Microsoft.AspNetCore.Http;

namespace BoardGameTracker.Common.DTOs.Commands;

public class UploadProfileImageCommand
{
    public IFormFile File { get; set; } = null!;
}
