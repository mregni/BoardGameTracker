using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using BoardGameTracker.Common.Helpers;
using BoardGameTracker.Core.Rag;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Rag;

public class PdfPageRendererTests : IDisposable
{
    private readonly int _manualId = Random.Shared.Next(1_000_000, int.MaxValue);
    private readonly string _pdfPath = Path.GetTempFileName();
    private readonly PdfPageRenderer _renderer = new(new Mock<ILogger<PdfPageRenderer>>().Object);

    private string FiguresDirectory => Path.Combine(PathHelper.FullManualFiguresPath, _manualId.ToString(CultureInfo.InvariantCulture));

    public void Dispose()
    {
        File.Delete(_pdfPath);
        _renderer.DeleteFigures(_manualId);
    }

    [Fact]
    public async Task RenderPageAsync_ShouldLeaveNoFileBehind_WhenRenderingFails()
    {
        await File.WriteAllTextAsync(_pdfPath, "not a pdf");

        var result = await _renderer.RenderPageAsync(_pdfPath, _manualId, 1);

        result.Should().BeNull();
        Directory.GetFiles(FiguresDirectory).Should().BeEmpty();
    }

    [Fact]
    public async Task RenderPageAsync_ShouldServeTheCachedPage_WhenItExists()
    {
        await File.WriteAllTextAsync(_pdfPath, "not a pdf");
        Directory.CreateDirectory(FiguresDirectory);
        await File.WriteAllTextAsync(Path.Combine(FiguresDirectory, "page-2.png"), "cached");

        await using var result = await _renderer.RenderPageAsync(_pdfPath, _manualId, 2);

        result.Should().NotBeNull();
        using var reader = new StreamReader(result!);
        (await reader.ReadToEndAsync()).Should().Be("cached");
    }
}
