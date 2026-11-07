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

    [Fact]
    public void Chunk_ShouldDropControlCharacters_ThatPostgresCannotStore()
    {
        var result = _chunker.Chunk([new PdfPageText(1, "Setup\0 the board.\u0001\u0007 Each player\tdraws\nfive cards.")]);

        result.Should().ContainSingle().Which.Content.Should().Be("Setup the board. Each player draws\nfive cards.");
    }

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

        result.Should().Equal(new TextChunk(0, "Setup: each player takes 7 cards.", 3));
    }

    [Fact]
    public void Chunk_LongPage_SplitsIntoMultipleChunksWithSequentialIndices()
    {
        var text = string.Join(" ", Enumerable.Repeat("word", 900));
        var result = _chunker.Chunk(new List<PdfPageText> { new(1, text) });

        result.Should().HaveCount(6);
        result.Should().OnlyContain(c => c.PageNumber == 1);
        result.Should().OnlyContain(c => c.Content.Length <= 1000);
        result.Select(c => c.Index).Should().Equal(0, 1, 2, 3, 4, 5);
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

        result.Select(c => c.Index).Should().Equal(0, 1, 2, 3, 4, 5);
        result.Select(c => c.PageNumber).Should().Equal(1, 1, 1, 2, 2, 2);
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
    public void Chunk_ShouldNotSplitSurrogatePair_WhenHardSplitFallsMidCharacter()
    {
        var text = new string('a', 999) + "\U0001D11E" + new string('b', 300);

        var result = _chunker.Chunk(new List<PdfPageText> { new(1, text) });

        result.Should().HaveCount(2);
        result.Should().OnlyContain(c => !HasLoneSurrogate(c.Content));
        result[0].Content.Should().Be(new string('a', 999));
        result[1].Content.Should().Be(new string('a', 200) + "\U0001D11E" + new string('b', 300));
    }

    private static bool HasLoneSurrogate(string value)
    {
        for (var i = 0; i < value.Length; i++)
        {
            if (char.IsHighSurrogate(value[i]))
            {
                if (i + 1 >= value.Length || !char.IsLowSurrogate(value[i + 1]))
                {
                    return true;
                }

                i++;
            }
            else if (char.IsLowSurrogate(value[i]))
            {
                return true;
            }
        }

        return false;
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

    [Fact]
    public void Chunk_ShouldBreakAtANewline_OnTheFirstIndexOfTheOverlapWindow()
    {
        var text = new string('a', 800) + "\n" + new string('b', 600);

        var result = _chunker.Chunk(new List<PdfPageText> { new(1, text) });

        result.Select(c => c.Content).Should().Equal(
            new string('a', 800),
            new string('a', 199) + "\n" + new string('b', 600));
    }

    [Fact]
    public void Chunk_ShouldIgnoreANewline_JustBeforeTheOverlapWindow()
    {
        var text = new string('a', 799) + "\n" + new string('b', 600);

        var result = _chunker.Chunk(new List<PdfPageText> { new(1, text) });

        result.Select(c => c.Content).Should().Equal(
            new string('a', 799) + "\n" + new string('b', 200),
            new string('b', 600));
    }

    [Fact]
    public void Chunk_ShouldBreakAtANewline_OnTheLastIndexOfTheChunk()
    {
        var text = new string('a', 999) + "\n" + new string('b', 100);

        var result = _chunker.Chunk(new List<PdfPageText> { new(1, text) });

        result.Select(c => c.Content).Should().Equal(
            new string('a', 999),
            new string('a', 199) + "\n" + new string('b', 100));
    }

    [Fact]
    public void Chunk_ShouldSkipAWindowOfOnlyNewlines_AndKeepTheText()
    {
        var text = "a" + new string('\n', 3000) + "b";

        var result = _chunker.Chunk(new List<PdfPageText> { new(1, text) });

        result.Select(c => c.Content).Should().Equal("a", "b");
        result.Select(c => c.Index).Should().Equal(0, 1);
    }

    [Fact]
    public void Chunk_ShouldNotSplitSurrogatePair_WhenTheOverlapStartFallsMidCharacter()
    {
        var text = new string('a', 799) + "\U0001D11E" + new string('b', 500);

        var result = _chunker.Chunk(new List<PdfPageText> { new(1, text) });

        result.Should().OnlyContain(c => !HasLoneSurrogate(c.Content));
    }
}
