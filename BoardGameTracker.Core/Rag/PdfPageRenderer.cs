using System.Diagnostics;
using BoardGameTracker.Common.Helpers;
using BoardGameTracker.Core.Rag.Interfaces;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Core.Rag;

public class PdfPageRenderer : IPdfPageRenderer
{
    private const int RenderDpi = 150;

    private readonly ILogger<PdfPageRenderer> _logger;

    public PdfPageRenderer(ILogger<PdfPageRenderer> logger)
    {
        _logger = logger;
    }

    public async Task<Stream?> RenderPageAsync(string pdfPath, int manualId, int page,
        CancellationToken cancellationToken = default)
    {
        if (page < 1 || !File.Exists(pdfPath))
        {
            return null;
        }

        var directory = GetFiguresDirectory(manualId);
        var target = Path.Combine(directory, $"page-{page}.png");
        if (File.Exists(target))
        {
            return File.OpenRead(target);
        }

        Directory.CreateDirectory(directory);
        var rendered = await RunPdfToPpmAsync(pdfPath, target, page, cancellationToken);
        if (!rendered || !File.Exists(target))
        {
            return null;
        }

        return File.OpenRead(target);
    }

    public void DeleteFigures(int manualId)
    {
        var directory = GetFiguresDirectory(manualId);
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, true);
        }
    }

    public void ClearAllFigures()
    {
        if (Directory.Exists(PathHelper.FullManualFiguresPath))
        {
            Directory.Delete(PathHelper.FullManualFiguresPath, true);
        }
    }

    private static string GetFiguresDirectory(int manualId) =>
        Path.Combine(PathHelper.FullManualFiguresPath, manualId.ToString());

    private async Task<bool> RunPdfToPpmAsync(string pdfPath, string targetPng, int page,
        CancellationToken cancellationToken)
    {
        var prefix = targetPng.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? targetPng[..^4] : targetPng;

        var startInfo = new ProcessStartInfo
        {
            FileName = "pdftoppm",
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("-png");
        startInfo.ArgumentList.Add("-f");
        startInfo.ArgumentList.Add(page.ToString());
        startInfo.ArgumentList.Add("-l");
        startInfo.ArgumentList.Add(page.ToString());
        startInfo.ArgumentList.Add("-r");
        startInfo.ArgumentList.Add(RenderDpi.ToString());
        startInfo.ArgumentList.Add("-singlefile");
        startInfo.ArgumentList.Add(pdfPath);
        startInfo.ArgumentList.Add(prefix);

        try
        {
            using var process = Process.Start(startInfo);
            if (process == null)
            {
                return false;
            }

            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            var error = await errorTask;

            if (process.ExitCode != 0)
            {
                _logger.LogWarning("pdftoppm failed for {Pdf} page {Page}: {Error}", pdfPath, page, error);
                return false;
            }

            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "pdftoppm is unavailable; rulebook page images are disabled");
            return false;
        }
    }
}
