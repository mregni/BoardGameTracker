using BoardGameTracker.Core.Games.Import;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Games.Import;

public class CsvReaderTests
{
    [Fact]
    public void Read_ShouldSplitCommaSeparatedRows()
    {
        var rows = CsvReader.Read("objectid,objectname\r\n13,Catan\n822,Carcassonne");

        rows.Should().BeEquivalentTo(new[]
        {
            new[] { "objectid", "objectname" },
            new[] { "13", "Catan" },
            new[] { "822", "Carcassonne" }
        }, o => o.WithStrictOrdering());
    }

    [Fact]
    public void Read_ShouldUseSemicolons_WhenTheHeaderHasMoreSemicolonsThanCommas()
    {
        var rows = CsvReader.Read("objectid;pricepaid\n13;12,50\n");

        rows.Should().BeEquivalentTo(new[]
        {
            new[] { "objectid", "pricepaid" },
            new[] { "13", "12,50" }
        }, o => o.WithStrictOrdering());
    }

    [Fact]
    public void Read_ShouldKeepSeparatorsQuotesAndNewlinesInsideQuotedFields()
    {
        var rows = CsvReader.Read("objectname,objectid\n\"Catan: Cities, Knights\",926\n\"Say \"\"hi\"\"\nthere\",1");

        rows.Should().BeEquivalentTo(new[]
        {
            new[] { "objectname", "objectid" },
            new[] { "Catan: Cities, Knights", "926" },
            new[] { "Say \"hi\"\nthere", "1" }
        }, o => o.WithStrictOrdering());
    }

    [Fact]
    public void Read_ShouldIgnoreSeparatorsInsideQuotedHeaderFields_WhenDetectingTheSeparator()
    {
        var rows = CsvReader.Read("\"a;b;c\",objectid,x\n1,2,3");

        rows[1].Should().Equal("1", "2", "3");
    }

    [Fact]
    public void Read_ShouldKeepEmptyTrailingFields()
    {
        var rows = CsvReader.Read("a,b,c\n1,,\n");

        rows[1].Should().Equal("1", "", "");
    }

    [Fact]
    public void Read_ShouldReturnNoRows_WhenTheTextIsEmpty()
    {
        CsvReader.Read(string.Empty).Should().BeEmpty();
    }
}
