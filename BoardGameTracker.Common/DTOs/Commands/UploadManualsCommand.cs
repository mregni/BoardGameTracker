using Microsoft.AspNetCore.Http;

namespace BoardGameTracker.Common.DTOs.Commands;

public class UploadManualsCommand
{
    public List<IFormFile> Files { get; set; } = [];
}
