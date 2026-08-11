using System.Collections.Generic;
using System.Linq;
using BoardGameTracker.Core.Rag;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Rag;

public class RulebookChunkerTests
{
    private readonly RulebookChunker _chunker = new();

    [Fact]
    public void Chunk_EmptyPages_ReturnsNoChunks()
    {
        var result = _chunker.Chunk(new List<PdfPageText>());

        result.Should().BeEmpty();
    }

    [Fact]
    public void Chunk_WhitespacePage_ReturnsNoChunks()
    {
        var result = _chunker.Chunk(new List<PdfPageText> { new(1, "   \n  \t ") });

        result.Should().BeEmpty();
    }

    [Fact]
    public void Chunk_ShortPage_ReturnsSingleChunkCarryingPageNumber()
    {
        var result = _chunker.Chunk(new List<PdfPageText> { new(3, "Setup: each player takes 7 cards.") });

        result.Should().HaveCount(1);
        result[0].PageNumber.Should().Be(3);
        result[0].Index.Should().Be(0);
        result[0].Content.Should().Contain("7 cards");
    }

    [Fact]
    public void Chunk_LongPage_SplitsIntoMultipleChunksWithSequentialIndices()
    {
        var text = string.Join(" ", Enumerable.Repeat("word", 900));
        var result = _chunker.Chunk(new List<PdfPageText> { new(1, text) });

        result.Should().HaveCountGreaterThan(1);
        result.Should().OnlyContain(c => c.PageNumber == 1);
        result.Select(c => c.Index).Should().Equal(Enumerable.Range(0, result.Count));
    }

    [Fact]
    public void Chunk_MultiplePages_AssignsGlobalIndicesAndCarriesEachPage()
    {
        var pages = new List<PdfPageText>
        {
            new(1, string.Join(" ", Enumerable.Repeat("alpha", 400))),
            new(2, string.Join(" ", Enumerable.Repeat("beta", 400)))
        };

        var result = _chunker.Chunk(pages);

        result.Select(c => c.Index).Should().Equal(Enumerable.Range(0, result.Count));
        result.Should().Contain(c => c.PageNumber == 1);
        result.Should().Contain(c => c.PageNumber == 2);
    }
}
