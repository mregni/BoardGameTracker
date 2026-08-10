using System.Text;
using BoardGameTracker.Core.Rag.Interfaces;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;
using UglyToad.PdfPig.DocumentLayoutAnalysis.ReadingOrderDetector;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;

namespace BoardGameTracker.Core.Rag;

public class PdfTextExtractor : IPdfTextExtractor
{
    public IReadOnlyList<PdfPageText> Extract(Stream pdfStream)
    {
        var pages = new List<PdfPageText>();

        using var document = PdfDocument.Open(pdfStream);
        foreach (var page in document.GetPages())
        {
            pages.Add(new PdfPageText(page.Number, ExtractPageText(page)));
        }

        return pages;
    }

    private static string ExtractPageText(Page page)
    {
        var words = page.GetWords(NearestNeighbourWordExtractor.Instance);
        var blocks = DocstrumBoundingBoxes.Instance.GetBlocks(words);
        if (blocks.Count == 0)
        {
            return page.Text ?? string.Empty;
        }

        var orderedBlocks = UnsupervisedReadingOrderDetector.Instance
            .Get(blocks)
            .OrderBy(block => block.ReadingOrder);

        var builder = new StringBuilder();
        foreach (var block in orderedBlocks)
        {
            builder.AppendLine(block.Text);
            builder.AppendLine();
        }

        return builder.ToString().Trim();
    }
}
