using BoardGameTracker.Core.Rag;
using FluentAssertions;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;
using Xunit;

namespace BoardGameTracker.Tests.Rag;

public class PdfTextExtractorTests
{
    private readonly PdfTextExtractor _extractor = new();

    private static MemoryStream CreatePdf(params (string Text, double Y)[][] pages)
    {
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        foreach (var lines in pages)
        {
            var page = builder.AddPage(PageSize.A4);
            foreach (var (text, y) in lines)
            {
                page.AddText(text, 12, new PdfPoint(50, y), font);
            }
        }

        return new MemoryStream(builder.Build());
    }

    [Fact]
    public void Extract_ShouldReturnEveryPageWithItsNumber()
    {
        using var pdf = CreatePdf([("Setup the board", 700)], [("Score the game", 700)]);

        var result = _extractor.Extract(pdf);

        result.Should().Equal(new PdfPageText(1, "Setup the board"), new PdfPageText(2, "Score the game"));
    }

    [Fact]
    public void Extract_ShouldReturnAnEmptyText_ForAPageWithoutWords()
    {
        using var pdf = CreatePdf([], [("Score the game", 700)]);

        var result = _extractor.Extract(pdf);

        result.Should().Equal(new PdfPageText(1, string.Empty), new PdfPageText(2, "Score the game"));
    }

    [Fact]
    public void Extract_ShouldSeparateBlocksByABlankLine()
    {
        using var pdf = CreatePdf([("Early line one", 700), ("Early line two", 686), ("Later line one", 200), ("Later line two", 186)]);

        var result = _extractor.Extract(pdf);

        var newLine = Environment.NewLine;
        var expected = $"Early line one\nEarly line two{newLine}{newLine}Later line one\nLater line two";
        result.Should().Equal(new PdfPageText(1, expected));
    }

    [Fact]
    public void Extract_ShouldThrow_WhenTheStreamIsNotAPdf()
    {
        using var stream = new MemoryStream("not a pdf"u8.ToArray());

        var act = () => _extractor.Extract(stream);

        act.Should().Throw<Exception>();
    }
}
