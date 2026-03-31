using BoardGameTracker.Common.Enums;
using Microsoft.AspNetCore.Http;

namespace BoardGameTracker.Common.DTOs.Commands;

public class UploadImageCommand
{
    public IFormFile File { get; set; } = null!;
    public UploadFileType Type { get; set; }
}
