using BoardGameTracker.Common.DTOs;

namespace BoardGameTracker.Core.Rag.Interfaces;

public interface IRagService
{
    Task<RagAnswerDto> AskAsync(int gameId, string question, int? manualId = null, CancellationToken cancellationToken = default);
}
