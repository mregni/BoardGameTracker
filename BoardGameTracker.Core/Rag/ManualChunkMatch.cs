using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Rag;

public record ManualChunkMatch(ManualChunk Chunk, string ManualTitle, double Distance);
