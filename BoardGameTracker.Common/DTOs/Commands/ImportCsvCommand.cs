using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace BoardGameTracker.Common.DTOs.Commands;

public class ImportCsvCommand
{
    [Required]
    public IFormFile File { get; set; } = null!;
}
