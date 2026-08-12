namespace BoardGameTracker.Core.Rag.Interfaces;

public interface IPdfPageRenderer
{
    Task<Stream?> RenderPageAsync(string pdfPath, int manualId, int page, CancellationToken cancellationToken = default);
    void DeleteFigures(int manualId);
    void ClearAllFigures();
}
