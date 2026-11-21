using System;
using System.Linq;
using BoardGameTracker.Common;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Games.Import;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Games.Import;

public class CsvCollectionParserTests
{
    private static readonly DateTime Today = new(2026, 11, 21, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Parse_ShouldReadABggCollectionExport()
    {
        const string csv = """
            objectname,objectid,own,fortrade,want,wanttobuy,wanttoplay,prevowned,preordered,wishlist,itemtype,pricepaid,acquisitiondate
            Catan,13,1,0,0,0,0,0,0,0,standalone,45.50,2020-03-14
            "Catan: Seafarers",325,1,0,0,0,0,0,0,0,expansion,,
            Gloomhaven,174430,0,0,0,0,0,0,0,1,standalone,,
            Carcassonne,822,0,0,0,0,0,1,0,0,standalone,0,2019-01-01
            Azul,230802,1,1,0,0,0,0,0,0,standalone,,
            """;

        var result = CsvCollectionParser.Parse(csv, Today);

        result.Games.Select(x => (x.BggId, x.Title, x.State, x.Price, x.LastModified)).Should().Equal(
            (13, "Catan", GameState.Owned, 45.50m, new DateTime(2020, 3, 14)),
            (325, "Catan: Seafarers", GameState.Owned, null, Today),
            (174430, "Gloomhaven", GameState.Wanted, null, Today),
            (822, "Carcassonne", GameState.PreviouslyOwned, null, new DateTime(2019, 1, 1)),
            (230802, "Azul", GameState.ForTrade, null, Today));
        result.ExpansionIds.Should().BeEquivalentTo([325]);
        result.Lines[325].Should().Be(3);
        result.Rejected.Should().BeEmpty();
    }

    [Fact]
    public void Parse_ShouldTreatEveryRowAsOwned_WhenThereAreNoStateColumns()
    {
        var result = CsvCollectionParser.Parse("objectid\n13\n", Today);

        result.Games.Should().ContainSingle().Which.State.Should().Be(GameState.Owned);
    }

    [Fact]
    public void Parse_ShouldLinkAnExpansionToTheGivenBaseGame_AndTakeItsTitleFromTheFile()
    {
        var result = CsvCollectionParser.Parse("OBJECTID;ObjectName;basegame\n13;Catan;\n325;Seafarers;13\n", Today);

        var expansion = result.Games.Single(x => x.BggId == 325);
        expansion.BaseGameBggId.Should().Be(13);
        expansion.BaseGameTitle.Should().Be("Catan");
        result.ExpansionIds.Should().BeEquivalentTo([325]);
    }

    [Theory]
    [InlineData("12,50", 12.50)]
    [InlineData("1,234.50", 1234.50)]
    [InlineData("12.5", 12.5)]
    public void Parse_ShouldAcceptCommaAndDotPrices(string price, double expected)
    {
        var result = CsvCollectionParser.Parse($"objectid;pricepaid\n13;{price}\n", Today);

        result.Games.Single().Price.Should().Be((decimal)expected);
    }

    [Fact]
    public void Parse_ShouldRejectInvalidRowsWithAReason()
    {
        const string csv = """
            objectid,objectname,own,wanttoplay,pricepaid,acquisitiondate,basegame
            abc,Broken id,1,0,,,
            13,Catan,1,0,,,
            13,Catan again,1,0,,,
            14,Played only,0,1,,,
            15,Bad price,1,0,cheap,,
            16,Bad date,1,0,,14/03/2020,
            17,Own base,1,0,,,17
            """;

        var result = CsvCollectionParser.Parse(csv, Today);

        result.Games.Select(x => x.BggId).Should().Equal(13);
        result.Rejected.Select(x => (x.Line, x.Title, x.BggId, x.Reason)).Should().Equal(
            (2, "Broken id", null, CsvRejectReasons.InvalidId),
            (4, "Catan again", 13, CsvRejectReasons.Duplicate),
            (5, "Played only", 14, CsvRejectReasons.NoState),
            (6, "Bad price", 15, CsvRejectReasons.InvalidPrice),
            (7, "Bad date", 16, CsvRejectReasons.InvalidDate),
            (8, "Own base", 17, CsvRejectReasons.InvalidBaseGame));
    }

    [Fact]
    public void Parse_ShouldSkipEmptyLines()
    {
        var result = CsvCollectionParser.Parse("objectid\n13\n,\n\n822\n", Today);

        result.Games.Select(x => x.BggId).Should().Equal(13, 822);
    }

    [Fact]
    public void Parse_ShouldThrow_WhenTheObjectIdColumnIsMissing()
    {
        var act = () => CsvCollectionParser.Parse("id,name\n13,Catan\n", Today);

        act.Should().Throw<DomainException>().WithMessage(Constants.Errors.CsvImportMissingObjectId);
    }

    [Fact]
    public void Parse_ShouldThrow_WhenTheFileIsEmpty()
    {
        var act = () => CsvCollectionParser.Parse(string.Empty, Today);

        act.Should().Throw<DomainException>().WithMessage(Constants.Errors.CsvImportEmpty);
    }

    [Fact]
    public void Parse_ShouldThrow_WhenTheFileHasTooManyRows()
    {
        var csv = "objectid\n" + string.Join("\n", Enumerable.Range(1, CsvCollectionParser.MaxRows + 1));

        var act = () => CsvCollectionParser.Parse(csv, Today);

        act.Should().Throw<DomainException>().WithMessage(Constants.Errors.CsvImportTooManyRows);
    }
}
