using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Common.Helpers;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.Disk.Interfaces;
using BoardGameTracker.Core.GameNights.Interfaces;
using BoardGameTracker.Core.Manuals.Interfaces;
using BoardGameTracker.Core.Manuals.Specifications;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Core.Manuals;

public class ManualService : IManualService
{
    private const long MaxManualBytes = 200L * 1024 * 1024;
    private const string PdfContentType = "application/pdf";
    private const string PdfExtension = ".pdf";

    private readonly IRepository<Manual> _manualRepository;
    private readonly IDiskProvider _diskProvider;
    private readonly IGameNightRepository _gameNightRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ManualService> _logger;

    public ManualService(
        IRepository<Manual> manualRepository,
        IDiskProvider diskProvider,
        IGameNightRepository gameNightRepository,
        IUnitOfWork unitOfWork,
        ILogger<ManualService> logger)
    {
        _manualRepository = manualRepository;
        _diskProvider = diskProvider;
        _gameNightRepository = gameNightRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public Task<List<Manual>> GetManualsForGame(int gameId)
    {
        _logger.LogDebug("Fetching manuals for game {GameId}", gameId);
        return _manualRepository.ListAsync(new ManualsByGameIdSpec(gameId));
    }

    public async Task<List<Manual>> UploadManuals(int gameId, IReadOnlyList<IFormFile> files)
    {
        _logger.LogDebug("Uploading {Count} manual(s) for game {GameId}", files.Count, gameId);

        if (files.Count == 0)
        {
            throw new ValidationException("Files", "No files were provided.");
        }

        foreach (var file in files)
        {
            ValidateFile(file);
        }

        var writtenFiles = new List<string>();
        var manuals = new List<Manual>();
        try
        {
            foreach (var file in files)
            {
                await using var stream = file.OpenReadStream();
                var storedFileName = await _diskProvider.WriteFile(stream, file.FileName, PathHelper.FullManualsPath);
                writtenFiles.Add(Path.Combine(PathHelper.FullManualsPath, storedFileName));

                manuals.Add(new Manual(file.FileName, storedFileName, PdfContentType, file.Length, gameId, DateTime.UtcNow));
            }

            await _manualRepository.CreateRangeAsync(manuals);
            await _unitOfWork.SaveChangesAsync();
        }
        catch
        {
            foreach (var path in writtenFiles)
            {
                _diskProvider.DeleteFile(path);
            }

            throw;
        }

        _logger.LogInformation("Uploaded {Count} manual(s) for game {GameId}", manuals.Count, gameId);
        return manuals;
    }

    public async Task DeleteManual(int id)
    {
        _logger.LogDebug("Deleting manual {ManualId}", id);
        var manual = await _manualRepository.GetByIdAsync(id);
        if (manual == null)
        {
            throw new EntityNotFoundException(nameof(Manual), id);
        }

        _diskProvider.DeleteFile(GetPhysicalPath(manual.StoredFileName));
        await _manualRepository.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Manual {ManualId} deleted", id);
    }

    public async Task<ManualDownload> GetManualForDownload(int id)
    {
        var manual = await _manualRepository.GetByIdAsync(id);
        if (manual == null)
        {
            throw new EntityNotFoundException(nameof(Manual), id);
        }

        return OpenDownload(manual);
    }

    public async Task<ManualDownload> GetManualForGameNightDownload(Guid linkId, int manualId)
    {
        var gameNight = await _gameNightRepository.GetGameNightByLinkId(linkId);
        var manual = await _manualRepository.GetByIdAsync(manualId);
        if (gameNight == null || manual == null || gameNight.SuggestedGames.All(g => g.Id != manual.GameId))
        {
            throw new EntityNotFoundException(nameof(Manual), manualId);
        }

        return OpenDownload(manual);
    }

    public async Task<List<GameNightManualsDto>> GetManualsForGameNight(Guid linkId)
    {
        var gameNight = await _gameNightRepository.GetGameNightByLinkId(linkId);
        if (gameNight == null || gameNight.SuggestedGames.Count == 0)
        {
            return [];
        }

        var games = gameNight.SuggestedGames.ToList();
        var gameIds = games.Select(g => g.Id).ToList();
        var manuals = await _manualRepository.ListAsync(new ManualsByGameIdsSpec(gameIds));

        return games
            .Select(game => new GameNightManualsDto
            {
                GameId = game.Id,
                GameTitle = game.Title,
                Manuals = manuals.Where(m => m.GameId == game.Id).ToListDto()
            })
            .Where(g => g.Manuals.Count > 0)
            .ToList();
    }

    public async Task DeleteManualFilesForGame(int gameId)
    {
        var manuals = await _manualRepository.ListAsync(new ManualsByGameIdSpec(gameId));
        foreach (var manual in manuals)
        {
            _diskProvider.DeleteFile(GetPhysicalPath(manual.StoredFileName));
        }
    }

    public void ClearAllManuals()
    {
        _diskProvider.ClearFolder(PathHelper.FullManualsPath);
    }

    private static void ValidateFile(IFormFile file)
    {
        if (file.Length == 0)
        {
            throw new ValidationException("Files", $"'{file.FileName}' is empty.");
        }

        var isPdf = string.Equals(file.ContentType, PdfContentType, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(Path.GetExtension(file.FileName), PdfExtension, StringComparison.OrdinalIgnoreCase);
        if (!isPdf)
        {
            throw new ValidationException("Files", $"'{file.FileName}' is not a PDF file.");
        }

        if (file.Length > MaxManualBytes)
        {
            throw new ValidationException("Files", $"'{file.FileName}' exceeds the maximum size of {MaxManualBytes / (1024 * 1024)} MB.");
        }
    }

    private ManualDownload OpenDownload(Manual manual)
    {
        var path = GetPhysicalPath(manual.StoredFileName);
        if (!_diskProvider.FileExists(path))
        {
            throw new EntityNotFoundException(nameof(Manual), manual.Id);
        }

        return new ManualDownload
        {
            Stream = _diskProvider.OpenRead(path),
            ContentType = manual.ContentType,
            FileName = manual.Title
        };
    }

    private static string GetPhysicalPath(string storedFileName)
    {
        var root = Path.GetFullPath(PathHelper.FullManualsPath);
        var fullPath = Path.GetFullPath(Path.Combine(root, storedFileName));
        if (!fullPath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new EntityNotFoundException(nameof(Manual), storedFileName);
        }

        return fullPath;
    }
}
