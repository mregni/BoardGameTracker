using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Models;
using Microsoft.AspNetCore.Http;

namespace BoardGameTracker.Core.Manuals.Interfaces;

public interface IManualService
{
    Task<List<Manual>> GetManualsForGame(int gameId);
    Task<List<Manual>> UploadManuals(int gameId, IReadOnlyList<IFormFile> files);
    Task RequeueManualForIndexing(int id);
    Task DeleteManual(int id);
    Task<ManualDownload> GetManualForDownload(int id);
    Task<ManualDownload?> GetManualPageImage(int id, int page, CancellationToken cancellationToken = default);
    Task<ManualDownload> GetManualForGameNightDownload(Guid linkId, int manualId);
    Task<List<GameNightManualsDto>> GetManualsForGameNight(Guid linkId);
    Task DeleteManualFilesForGame(int gameId);
    void ClearAllManuals();
}
