namespace BoardGameTracker.Core.Rag.Interfaces;

public interface IRulebookChunker
{
    IReadOnlyList<TextChunk> Chunk(IReadOnlyList<PdfPageText> pages);
}
