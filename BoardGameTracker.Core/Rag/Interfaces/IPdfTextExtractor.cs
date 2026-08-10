namespace BoardGameTracker.Core.Rag.Interfaces;

public interface IPdfTextExtractor
{
    IReadOnlyList<PdfPageText> Extract(Stream pdfStream);
}
