using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Rag.Specifications;
using FluentAssertions;
using Pgvector;
using Xunit;

namespace BoardGameTracker.Tests.Specifications.Rag;

public class NearestManualChunksSpecTests
{
    private static ManualChunk CreateChunk(int manualId, int gameId) =>
        new(manualId, gameId, 0, "content", 1, new Vector(new float[1024]));

    private static Vector QueryVector() => new(new float[1024]);

    [Theory]
    [InlineData(null, 10, 1, true)]
    [InlineData(null, 11, 1, true)]
    [InlineData(null, 10, 2, false)]
    [InlineData(10, 10, 1, true)]
    [InlineData(10, 11, 1, false)]
    [InlineData(10, 10, 2, false)]
    public void IsSatisfiedBy_ShouldMatchOnlyRequestedGameAndOptionalManual(int? specManualId, int chunkManualId, int chunkGameId, bool expected)
    {
        var spec = new NearestManualChunksSpec(1, QueryVector(), 5, specManualId);

        spec.IsSatisfiedBy(CreateChunk(chunkManualId, chunkGameId)).Should().Be(expected);
    }

    [Fact]
    public void Spec_ShouldTakeRequestedK_ProjectMatches_AndNotTrack()
    {
        var spec = new NearestManualChunksSpec(1, QueryVector(), 7);

        spec.Take.Should().Be(7);
        spec.Selector.Should().NotBeNull();
        spec.AsNoTracking.Should().BeTrue();
    }
}
