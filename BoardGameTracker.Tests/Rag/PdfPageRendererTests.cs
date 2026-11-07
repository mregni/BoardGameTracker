using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BoardGameTracker.Common.Helpers;
using BoardGameTracker.Core.Rag;
using BoardGameTracker.Tests.Support;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;
using Xunit;

namespace BoardGameTracker.Tests.Rag;

[Collection("EnvironmentVariables")]
public class PdfPageRendererTests : IDisposable
{
    private const string NotInstalled = "pdftoppm is not installed";
    private const string Unavailable = "pdftoppm is unavailable; rulebook page images are disabled";

    private readonly int _manualId = Random.Shared.Next(1_000_000, int.MaxValue);
    private readonly string _pdfPath = Path.GetTempFileName();
    private readonly string? _originalPath = Environment.GetEnvironmentVariable("PATH");
    private readonly Mock<ILogger<PdfPageRenderer>> _loggerMock = new();
    private readonly PdfPageRenderer _renderer;

    public PdfPageRendererTests()
    {
        _renderer = new PdfPageRenderer(_loggerMock.Object);
    }

    public static bool PdfToPpmIsInstalled { get; } = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
        .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
        .Any(directory => File.Exists(Path.Combine(directory, "pdftoppm")) || File.Exists(Path.Combine(directory, "pdftoppm.exe")));

    private string FiguresDirectory => Path.Combine(PathHelper.FullManualFiguresPath, _manualId.ToString(CultureInfo.InvariantCulture));

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("PATH", _originalPath);
        File.Delete(_pdfPath);
        _renderer.DeleteFigures(_manualId);
        GC.SuppressFinalize(this);
    }

    private void VerifyNoOtherCalls()
    {
        _loggerMock.VerifyNoOtherCalls();
    }

    private static void HidePdfToPpm()
    {
        Environment.SetEnvironmentVariable("PATH", Path.GetTempPath());
    }

    private async Task WritePdfAsync()
    {
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        builder.AddPage(PageSize.A6).AddText("Setup", 12, new PdfPoint(20, 200), font);
        await File.WriteAllBytesAsync(_pdfPath, builder.Build(), TestContext.Current.CancellationToken);
    }

    private void VerifyPdfToPpmFailed(int page)
    {
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.StartsWith($"pdftoppm failed for {_pdfPath} page {page}: ", StringComparison.Ordinal)),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task RenderPageAsync_ShouldReturnNull_WhenThePageIsBelowOne(int page)
    {
        var result = await _renderer.RenderPageAsync(_pdfPath, _manualId, page);

        result.Should().BeNull();
        Directory.Exists(FiguresDirectory).Should().BeFalse();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RenderPageAsync_ShouldReturnNull_WhenThePdfIsMissing()
    {
        var result = await _renderer.RenderPageAsync(_pdfPath + ".missing", _manualId, 1);

        result.Should().BeNull();
        Directory.Exists(FiguresDirectory).Should().BeFalse();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RenderPageAsync_ShouldLeaveNoFileBehind_WhenPdfToPpmIsUnavailable()
    {
        await File.WriteAllTextAsync(_pdfPath, "not a pdf", TestContext.Current.CancellationToken);
        HidePdfToPpm();

        var result = await _renderer.RenderPageAsync(_pdfPath, _manualId, 1);

        result.Should().BeNull();
        Directory.GetFiles(FiguresDirectory).Should().BeEmpty();
        _loggerMock.VerifyLog(LogLevel.Warning, Unavailable);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RenderPageAsync_ShouldRethrow_WhenTheCallerCancels()
    {
        await File.WriteAllTextAsync(_pdfPath, "not a pdf", TestContext.Current.CancellationToken);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var act = () => _renderer.RenderPageAsync(_pdfPath, _manualId, 1, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        Directory.GetFiles(FiguresDirectory).Should().BeEmpty();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RenderPageAsync_ShouldServeTheCachedPage_WhenItExists()
    {
        await File.WriteAllTextAsync(_pdfPath, "not a pdf", TestContext.Current.CancellationToken);
        Directory.CreateDirectory(FiguresDirectory);
        await File.WriteAllTextAsync(Path.Combine(FiguresDirectory, "page-2.png"), "cached", TestContext.Current.CancellationToken);
        HidePdfToPpm();

        await using var result = await _renderer.RenderPageAsync(_pdfPath, _manualId, 2);

        using var reader = new StreamReader(result!);
        (await reader.ReadToEndAsync(TestContext.Current.CancellationToken)).Should().Be("cached");
        VerifyNoOtherCalls();
    }

    [Fact(Skip = NotInstalled, SkipUnless = nameof(PdfToPpmIsInstalled))]
    public async Task RenderPageAsync_ShouldStoreAPng_AndServeItFromTheCacheAfterwards()
    {
        await WritePdfAsync();
        byte[] rendered;

        await using (var first = await _renderer.RenderPageAsync(_pdfPath, _manualId, 1))
        {
            using var png = new MemoryStream();
            await first!.CopyToAsync(png, TestContext.Current.CancellationToken);
            rendered = png.ToArray();
        }

        rendered[..8].Should().Equal(0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A);
        Directory.GetFiles(FiguresDirectory).Select(Path.GetFileName).Should().Equal("page-1.png");

        HidePdfToPpm();
        await using var second = await _renderer.RenderPageAsync(_pdfPath, _manualId, 1);
        using var cached = new MemoryStream();
        await second!.CopyToAsync(cached, TestContext.Current.CancellationToken);
        cached.ToArray().Should().Equal(rendered);
        VerifyNoOtherCalls();
    }

    [Fact(Skip = NotInstalled, SkipUnless = nameof(PdfToPpmIsInstalled))]
    public async Task RenderPageAsync_ShouldReturnNull_AndLog_WhenThePageDoesNotExist()
    {
        await WritePdfAsync();

        var result = await _renderer.RenderPageAsync(_pdfPath, _manualId, 7);

        result.Should().BeNull();
        Directory.GetFiles(FiguresDirectory).Should().BeEmpty();
        VerifyPdfToPpmFailed(7);
        VerifyNoOtherCalls();
    }

    [Fact(Skip = NotInstalled, SkipUnless = nameof(PdfToPpmIsInstalled))]
    public async Task RenderPageAsync_ShouldReturnNull_AndLog_WhenTheFileIsNotAPdf()
    {
        await File.WriteAllTextAsync(_pdfPath, "not a pdf", TestContext.Current.CancellationToken);

        var result = await _renderer.RenderPageAsync(_pdfPath, _manualId, 1);

        result.Should().BeNull();
        Directory.GetFiles(FiguresDirectory).Should().BeEmpty();
        VerifyPdfToPpmFailed(1);
        VerifyNoOtherCalls();
    }

    [Fact]
    public void DeleteFigures_ShouldRemoveTheFolderOfThatManualOnly()
    {
        var other = Path.Combine(PathHelper.FullManualFiguresPath, (_manualId - 1).ToString(CultureInfo.InvariantCulture));
        Directory.CreateDirectory(FiguresDirectory);
        Directory.CreateDirectory(other);
        File.WriteAllText(Path.Combine(FiguresDirectory, "page-1.png"), "figure");

        _renderer.DeleteFigures(_manualId);

        Directory.Exists(FiguresDirectory).Should().BeFalse();
        Directory.Exists(other).Should().BeTrue();
        Directory.Delete(other);
        VerifyNoOtherCalls();
    }

    [Fact]
    public void DeleteFigures_ShouldDoNothing_WhenTheManualHasNoFigures()
    {
        var act = () => _renderer.DeleteFigures(_manualId);

        act.Should().NotThrow();
        VerifyNoOtherCalls();
    }

    [Fact]
    public void ClearAllFigures_ShouldRemoveTheFiguresFolder_AndToleratePassingTwice()
    {
        Directory.CreateDirectory(FiguresDirectory);

        _renderer.ClearAllFigures();
        var again = () => _renderer.ClearAllFigures();

        Directory.Exists(PathHelper.FullManualFiguresPath).Should().BeFalse();
        again.Should().NotThrow();
        VerifyNoOtherCalls();
    }
}
