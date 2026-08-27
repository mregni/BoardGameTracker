using System.Collections.Generic;
using System.Linq;
using BoardGameTracker.Core.Rag;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Rag;

public class RulebookChunkerTests
{
    private readonly RulebookChunker _chunker = new();

    public static TheoryData<List<PdfPageText>> NoContentPages => new()
    {
        new List<PdfPageText>(),
        new List<PdfPageText> { new(1, "") },
        new List<PdfPageText> { new(1, "   \n  \t ") },
        new List<PdfPageText> { new(1, "\r\r") }
    };

    [Theory]
    [MemberData(nameof(NoContentPages))]
    public void Chunk_ShouldReturnNoChunks_WhenPagesHaveNoContent(List<PdfPageText> pages)
    {
        var result = _chunker.Chunk(pages);

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
        result.Should().OnlyContain(c => c.Content.Length <= 1000);
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
        result.Select(c => c.PageNumber).Should().BeInAscendingOrder();
        result.Should().Contain(c => c.PageNumber == 1);
        result.Should().Contain(c => c.PageNumber == 2);
    }

    [Fact]
    public void Chunk_ShouldReturnSingleFullChunk_WhenTextIsExactlyMaxChunkLength()
    {
        var text = new string('a', 1000);

        var result = _chunker.Chunk(new List<PdfPageText> { new(1, text) });

        result.Should().ContainSingle().Which.Content.Should().Be(text);
    }

    public static TheoryData<string, string, string> BoundaryBreakCases => new()
    {
        {
            new string('a', 900) + "\n" + new string('b', 400),
            new string('a', 900),
            new string('a', 199) + "\n" + new string('b', 400)
        },
        {
            new string('a', 900) + ". " + new string('b', 300),
            new string('a', 900) + ".",
            new string('a', 199) + ". " + new string('b', 300)
        },
        {
            new string('a', 900) + "! " + new string('b', 300),
            new string('a', 900) + "!",
            new string('a', 199) + "! " + new string('b', 300)
        },
        {
            new string('a', 900) + "? " + new string('b', 300),
            new string('a', 900) + "?",
            new string('a', 199) + "? " + new string('b', 300)
        }
    };

    [Theory]
    [MemberData(nameof(BoundaryBreakCases))]
    public void Chunk_ShouldBreakAtBoundary_WhenNewlineOrSentenceEndFallsWithinOverlapWindow(
        string text, string expectedFirst, string expectedSecond)
    {
        var result = _chunker.Chunk(new List<PdfPageText> { new(1, text) });

        result.Should().HaveCount(2);
        result[0].Content.Should().Be(expectedFirst);
        result[1].Content.Should().Be(expectedSecond);
    }

    [Fact]
    public void Chunk_ShouldNotBreakAtPunctuation_WhenPunctuationIsNotFollowedByWhitespace()
    {
        var text = new string('a', 900) + "3.5" + new string('b', 300);

        var result = _chunker.Chunk(new List<PdfPageText> { new(1, text) });

        result.Should().HaveCount(2);
        result[0].Content.Should().Be(new string('a', 900) + "3.5" + new string('b', 97));
        result[1].Content.Should().Be(new string('a', 100) + "3.5" + new string('b', 300));
    }

    [Fact]
    public void Chunk_ShouldIgnoreNewline_WhenNewlineFallsBeforeOverlapWindow()
    {
        var text = new string('a', 500) + "\n" + new string('a', 999);

        var result = _chunker.Chunk(new List<PdfPageText> { new(1, text) });

        result.Should().HaveCount(2);
        result[0].Content.Should().Be(new string('a', 500) + "\n" + new string('a', 499));
        result[1].Content.Should().Be(new string('a', 700));
    }

    [Fact]
    public void Chunk_ShouldHardSplitAtMaxLengthWithOverlap_WhenNoBoundaryExists()
    {
        var text = new string('a', 1500);

        var result = _chunker.Chunk(new List<PdfPageText> { new(1, text) });

        result.Should().HaveCount(2);
        result[0].Content.Should().Be(new string('a', 1000));
        result[1].Content.Should().Be(new string('a', 700));
    }

    [Fact]
    public void Chunk_ShouldEmitShortSecondChunk_WhenTextIsJustOverMaxChunkLength()
    {
        var text = new string('a', 1001);

        var result = _chunker.Chunk(new List<PdfPageText> { new(1, text) });

        result.Should().HaveCount(2);
        result[0].Content.Should().Be(new string('a', 1000));
        result[1].Content.Should().Be(new string('a', 201));
    }

    [Fact]
    public void Chunk_ShouldOverlapConsecutiveChunksByExactlyOverlapSize_WhenLongTextHasNoBoundaries()
    {
        var text = string.Concat(Enumerable.Range(0, 5000).Select(i => (char)('a' + i % 26)));

        var result = _chunker.Chunk(new List<PdfPageText> { new(1, text) });

        result.Should().HaveCount(6);
        for (var i = 0; i < result.Count; i++)
        {
            result[i].Content.Should().Be(text.Substring(i * 800, 1000));
        }
    }

    [Fact]
    public void Chunk_CurrentlySplitsSurrogatePairs_WhenHardSplitFallsMidCharacter()
    {
        var text = new string('a', 999) + "\U0001D11E" + new string('b', 300);

        var result = _chunker.Chunk(new List<PdfPageText> { new(1, text) });

        result.Should().HaveCount(2);
        result[0].Content.Should().HaveLength(1000);
        char.IsHighSurrogate(result[0].Content[^1]).Should().BeTrue();
        result[1].Content.Should().Be(new string('a', 199) + "\U0001D11E" + new string('b', 300));
    }

    [Theory]
    [InlineData("First line.\r\nSecond line.", "First line.\nSecond line.")]
    [InlineData("Draw  two \t cards.", "Draw two cards.")]
    [InlineData("Line one\n   indented", "Line one\nindented")]
    [InlineData("  padded text  ", "padded text")]
    public void Chunk_ShouldNormalizeWhitespace_WhenTextContainsCarriageReturnsTabsOrRepeatedSpaces(string input, string expected)
    {
        var result = _chunker.Chunk(new List<PdfPageText> { new(1, input) });

        result.Should().ContainSingle().Which.Content.Should().Be(expected);
    }
}
