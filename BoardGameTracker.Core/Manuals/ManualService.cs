using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Common.Helpers;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.Disk.Interfaces;
using BoardGameTracker.Core.GameNights.Specifications;
using BoardGameTracker.Core.Manuals.Interfaces;
using BoardGameTracker.Core.Manuals.Specifications;
using BoardGameTracker.Core.Rag.Interfaces;
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
    private readonly IReadRepository<GameNight> _gameNightRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IManualIndexingQueue _indexingQueue;
    private readonly IPdfPageRenderer _pageRenderer;
    private readonly IEnvironmentProvider _environmentProvider;
    private readonly ILogger<ManualService> _logger;

    public ManualService(
        IRepository<Manual> manualRepository,
        IDiskProvider diskProvider,
        IReadRepository<GameNight> gameNightRepository,
        IUnitOfWork unitOfWork,
        IManualIndexingQueue indexingQueue,
        IPdfPageRenderer pageRenderer,
        IEnvironmentProvider environmentProvider,
        ILogger<ManualService> logger)
    {
        _manualRepository = manualRepository;
        _diskProvider = diskProvider;
        _gameNightRepository = gameNightRepository;
        _unitOfWork = unitOfWork;
        _indexingQueue = indexingQueue;
        _pageRenderer = pageRenderer;
        _environmentProvider = environmentProvider;
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

        if (_environmentProvider.RagEnabled)
        {
            foreach (var manual in manuals)
            {
                _indexingQueue.Enqueue(manual.Id);
            }
        }

        _logger.LogInformation("Uploaded {Count} manual(s) for game {GameId}", manuals.Count, gameId);
        return manuals;
    }

    public async Task RequeueManualForIndexing(int id)
    {
        _logger.LogDebug("Requeueing manual {ManualId} for indexing", id);
        var manual = await _manualRepository.GetByIdAsync(id);
        if (manual == null)
        {
            throw new EntityNotFoundException(nameof(Manual), id);
        }

        manual.ResetIndexState();
        await _unitOfWork.SaveChangesAsync();

        if (_environmentProvider.RagEnabled)
        {
            _indexingQueue.Enqueue(id);
        }
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
        _pageRenderer.DeleteFigures(id);
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

    public async Task<ManualDownload?> GetManualPageImage(int id, int page, CancellationToken cancellationToken = default)
    {
        var manual = await _manualRepository.GetByIdAsync(id);
        if (manual == null)
        {
            throw new EntityNotFoundException(nameof(Manual), id);
        }

        var pdfPath = GetPhysicalPath(manual.StoredFileName);
        if (!_diskProvider.FileExists(pdfPath))
        {
            throw new EntityNotFoundException(nameof(Manual), id);
        }

        var stream = await _pageRenderer.RenderPageAsync(pdfPath, id, page, cancellationToken);
        if (stream == null)
        {
            return null;
        }

        return new ManualDownload
        {
            Stream = stream,
            ContentType = "image/png",
            FileName = $"page-{page}.png"
        };
    }

    public async Task<ManualDownload> GetManualForGameNightDownload(Guid linkId, int manualId)
    {
        var gameNight = await _gameNightRepository.SingleOrDefaultAsync(new GameNightByLinkIdSpec(linkId));
        var manual = await _manualRepository.GetByIdAsync(manualId);
        if (gameNight == null || manual == null || gameNight.SuggestedGames.All(g => g.Id != manual.GameId))
        {
            throw new EntityNotFoundException(nameof(Manual), manualId);
        }

        return OpenDownload(manual);
    }

    public async Task<List<GameNightManualsDto>> GetManualsForGameNight(Guid linkId)
    {
        var gameNight = await _gameNightRepository.SingleOrDefaultAsync(new GameNightByLinkIdSpec(linkId));
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
            _pageRenderer.DeleteFigures(manual.Id);
        }
    }

    public void ClearAllManuals()
    {
        _diskProvider.ClearFolder(PathHelper.FullManualsPath);
        _pageRenderer.ClearAllFigures();
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
