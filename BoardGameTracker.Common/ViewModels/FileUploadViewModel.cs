using BoardGameTracker.Common.Enums;
using Microsoft.AspNetCore.Http;

namespace BoardGameTracker.Common.ViewModels;

public class FileUploadViewModel
{
    public UploadFileType Type { get; set; }
    public IFormFile? File { get; set; }
}