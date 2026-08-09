using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Common.Extensions;

public static class ManualDtoExtensions
{
    public static ManualDto ToDto(this Manual manual)
    {
        return new ManualDto
        {
            Id = manual.Id,
            GameId = manual.GameId,
            Title = manual.Title,
            FileSizeBytes = manual.FileSizeBytes,
            UploadDate = manual.UploadDate,
            ContentType = manual.ContentType
        };
    }

    public static List<ManualDto> ToListDto(this IEnumerable<Manual> manuals)
    {
        return manuals.Select(m => m.ToDto()).ToList();
    }
}
