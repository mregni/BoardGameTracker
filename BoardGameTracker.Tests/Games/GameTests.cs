using System.Linq;
using BoardGameTracker.Common;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Exceptions;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Games;

public class GameTests
{
    #region UpdateShopUrl

    [Fact]
    public void UpdateShopUrl_ShouldStoreValue_WhenValidHttpsUrl()
    {
        var game = new Game("Test Game");

        game.UpdateShopUrl("https://shop.example.com/game");

        game.ShopUrl.Should().Be("https://shop.example.com/game");
    }

    [Fact]
    public void UpdateShopUrl_ShouldStoreValue_WhenValidHttpUrl()
    {
        var game = new Game("Test Game");

        game.UpdateShopUrl("http://shop.example.com/game");

        game.ShopUrl.Should().Be("http://shop.example.com/game");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateShopUrl_ShouldStoreNull_WhenNullOrWhitespace(string? value)
    {
        var game = new Game("Test Game");

        game.UpdateShopUrl(value);

        game.ShopUrl.Should().BeNull();
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("not a url")]
    [InlineData("ftp://shop.example.com/game")]
    [InlineData("/relative/path")]
    public void UpdateShopUrl_ShouldThrow_WhenNotAbsoluteHttpUrl(string value)
    {
        var game = new Game("Test Game");
        game.UpdateShopUrl("https://shop.example.com/original");

        var action = () => game.UpdateShopUrl(value);

        action.Should().Throw<ArgumentException>().WithParameterName("shopUrl");
        game.ShopUrl.Should().Be("https://shop.example.com/original");
    }

    #endregion

    #region UpdateChangeDetectionWatchId

    [Fact]
    public void UpdateChangeDetectionWatchId_ShouldStoreValue_WhenValidGuid()
    {
        var game = new Game("Test Game");
        var watchId = "e0808154-28da-4b85-9a71-24a409e694f1";

        game.UpdateChangeDetectionWatchId(watchId);

        game.ChangeDetectionWatchId.Should().Be(watchId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateChangeDetectionWatchId_ShouldStoreNull_WhenNullOrWhitespace(string? value)
    {
        var game = new Game("Test Game");

        game.UpdateChangeDetectionWatchId(value);

        game.ChangeDetectionWatchId.Should().BeNull();
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("12345")]
    [InlineData("e0808154-28da-4b85-9a71")]
    public void UpdateChangeDetectionWatchId_ShouldThrow_WhenNotAGuid(string value)
    {
        var game = new Game("Test Game");
        game.UpdateChangeDetectionWatchId("e0808154-28da-4b85-9a71-24a409e694f1");

        var action = () => game.UpdateChangeDetectionWatchId(value);

        action.Should().Throw<ArgumentException>().WithParameterName("watchId");
        game.ChangeDetectionWatchId.Should().Be("e0808154-28da-4b85-9a71-24a409e694f1");
    }

    #endregion

    #region UpdateLanguage

    [Fact]
    public void UpdateLanguage_ShouldStoreValue_WhenProvided()
    {
        var game = new Game("Test Game");

        game.UpdateLanguage("en");

        game.Language.Should().Be("en");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateLanguage_ShouldStoreNull_WhenNullOrWhitespace(string? value)
    {
        var game = new Game("Test Game");

        game.UpdateLanguage(value);

        game.Language.Should().BeNull();
    }

    #endregion

    #region UpdateYearPublished

    [Theory]
    [InlineData(1630)]
    [InlineData(1876)]
    [InlineData(1899)]
    [InlineData(1900)]
    [InlineData(2020)]
    public void UpdateYearPublished_ShouldStoreValue_WhenYearIsValid(int year)
    {
        var game = new Game("Test Game");

        game.UpdateYearPublished(year);

        game.YearPublished.Should().Be(year);
    }

    [Fact]
    public void UpdateYearPublished_ShouldStoreNull_WhenNull()
    {
        var game = new Game("Test Game");

        game.UpdateYearPublished(null);

        game.YearPublished.Should().BeNull();
    }

    [Fact]
    public void UpdateYearPublished_ShouldStoreValue_WhenYearIsAtUpperBound()
    {
        var game = new Game("Test Game");
        var upperBound = DateTime.UtcNow.Year + 10;

        game.UpdateYearPublished(upperBound);

        game.YearPublished.Should().Be(upperBound);
    }

    [Fact]
    public void UpdateYearPublished_ShouldThrow_WhenYearIsTooFarInTheFuture()
    {
        var game = new Game("Test Game");
        game.UpdateYearPublished(2020);

        var action = () => game.UpdateYearPublished(DateTime.UtcNow.Year + 11);

        action.Should().Throw<ArgumentOutOfRangeException>();
        game.YearPublished.Should().Be(2020);
    }

    #endregion

    #region UpdateTitle

    [Fact]
    public void UpdateTitle_ShouldStoreValue_WhenValid()
    {
        var game = new Game("Test Game");

        game.UpdateTitle("New Title");

        game.Title.Should().Be("New Title");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateTitle_ShouldThrow_WhenNullOrWhitespace(string value)
    {
        var game = new Game("Test Game");

        var action = () => game.UpdateTitle(value);

        action.Should().Throw<ArgumentException>();
        game.Title.Should().Be("Test Game");
    }

    #endregion

    #region UpdateMinAge

    [Fact]
    public void UpdateMinAge_ShouldStoreValue_WhenPositive()
    {
        var game = new Game("Test Game");

        game.UpdateMinAge(8);

        game.MinAge.Should().Be(8);
    }

    [Fact]
    public void UpdateMinAge_ShouldStoreNull_WhenNull()
    {
        var game = new Game("Test Game");
        game.UpdateMinAge(8);

        game.UpdateMinAge(null);

        game.MinAge.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void UpdateMinAge_ShouldThrow_WhenZeroOrNegative(int value)
    {
        var game = new Game("Test Game");
        game.UpdateMinAge(8);

        var action = () => game.UpdateMinAge(value);

        action.Should().Throw<ArgumentException>();
        game.MinAge.Should().Be(8);
    }

    #endregion

    #region UpdateBggId

    [Fact]
    public void UpdateBggId_ShouldStoreValue_WhenPositive()
    {
        var game = new Game("Test Game");

        game.UpdateBggId(12345);

        game.BggId.Should().Be(12345);
    }

    [Fact]
    public void UpdateBggId_ShouldStoreNull_WhenNull()
    {
        var game = new Game("Test Game");
        game.UpdateBggId(12345);

        game.UpdateBggId(null);

        game.BggId.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void UpdateBggId_ShouldThrow_WhenZeroOrNegative(int value)
    {
        var game = new Game("Test Game");
        game.UpdateBggId(12345);

        var action = () => game.UpdateBggId(value);

        action.Should().Throw<ArgumentException>();
        game.BggId.Should().Be(12345);
    }

    #endregion

    #region UpdatePlayerCount

    [Fact]
    public void UpdatePlayerCount_ShouldStoreRange_WhenBothValuesProvided()
    {
        var game = new Game("Test Game");

        game.UpdatePlayerCount(2, 6);

        game.PlayerCount.Should().NotBeNull();
        game.PlayerCount!.Min.Should().Be(2);
        game.PlayerCount.Max.Should().Be(6);
    }

    [Theory]
    [InlineData(null, 6)]
    [InlineData(2, null)]
    [InlineData(null, null)]
    public void UpdatePlayerCount_ShouldStoreNull_WhenAnyValueMissing(int? min, int? max)
    {
        var game = new Game("Test Game");
        game.UpdatePlayerCount(2, 6);

        game.UpdatePlayerCount(min, max);

        game.PlayerCount.Should().BeNull();
    }

    #endregion

    #region UpdatePlayTime

    [Fact]
    public void UpdatePlayTime_ShouldStoreRange_WhenBothValuesProvided()
    {
        var game = new Game("Test Game");

        game.UpdatePlayTime(30, 90);

        game.PlayTime.Should().NotBeNull();
        game.PlayTime!.MinMinutes.Should().Be(30);
        game.PlayTime.MaxMinutes.Should().Be(90);
    }

    [Theory]
    [InlineData(null, 90)]
    [InlineData(30, null)]
    [InlineData(null, null)]
    public void UpdatePlayTime_ShouldStoreNull_WhenAnyValueMissing(int? min, int? max)
    {
        var game = new Game("Test Game");
        game.UpdatePlayTime(30, 90);

        game.UpdatePlayTime(min, max);

        game.PlayTime.Should().BeNull();
    }

    #endregion

    #region UpdateCoopMode

    [Fact]
    public void Constructor_ShouldStartWithoutCoopMode()
    {
        var game = new Game("Pandemic");

        game.HasCoopMode.Should().BeFalse();
        game.CoopByDefault.Should().BeFalse();
    }

    [Theory]
    [InlineData(true, true, true, true)]
    [InlineData(true, false, true, false)]
    [InlineData(false, true, false, false)]
    [InlineData(false, false, false, false)]
    public void UpdateCoopMode_ShouldOnlyDefaultToCoop_WhenTheGameHasCoopMode(bool hasCoopMode, bool coopByDefault, bool expectedCoopMode, bool expectedDefault)
    {
        var game = new Game("Pandemic");

        game.UpdateCoopMode(hasCoopMode, coopByDefault);

        game.HasCoopMode.Should().Be(expectedCoopMode);
        game.CoopByDefault.Should().Be(expectedDefault);
    }

    #endregion

    #region Constructor

    [Fact]
    public void Constructor_ShouldStoreTheValuesAndStartEmpty()
    {
        var before = DateTime.UtcNow;

        var game = new Game("Brass", true, GameState.Wanted);

        game.Title.Should().Be("Brass");
        game.HasScoring.Should().BeTrue();
        game.State.Should().Be(GameState.Wanted);
        game.AdditionDate.Should().BeOnOrAfter(before).And.BeOnOrBefore(DateTime.UtcNow);
        game.Expansions.Should().BeEmpty();
        game.Accessories.Should().BeEmpty();
        game.Categories.Should().BeEmpty();
        game.Mechanics.Should().BeEmpty();
        game.People.Should().BeEmpty();
        game.Manuals.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_ShouldDefaultToAnOwnedGameWithoutScoring()
    {
        var game = new Game("Brass");

        game.State.Should().Be(GameState.Owned);
        game.HasScoring.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Constructor_ShouldThrow_WhenTheTitleIsBlank(string title)
    {
        var action = () => new Game(title);

        action.Should().Throw<ArgumentException>();
    }

    #endregion

    #region Simple updates

    [Fact]
    public void UpdateDescription_ShouldStoreValue()
    {
        var game = new Game("Test Game");

        game.UpdateDescription("Canal era");

        game.Description.Should().Be("Canal era");
    }

    [Fact]
    public void UpdateImage_ShouldStoreValue_AndAcceptNull()
    {
        var game = new Game("Test Game");

        game.UpdateImage("/images/cover/brass.webp");
        game.Image.Should().Be("/images/cover/brass.webp");

        game.UpdateImage(null);
        game.Image.Should().BeNull();
    }

    [Fact]
    public void UpdateHasScoring_ShouldStoreValue()
    {
        var game = new Game("Test Game", true);

        game.UpdateHasScoring(false);

        game.HasScoring.Should().BeFalse();
    }

    [Fact]
    public void UpdateState_ShouldStoreValue()
    {
        var game = new Game("Test Game");

        game.UpdateState(GameState.Sold);

        game.State.Should().Be(GameState.Sold);
    }

    [Fact]
    public void UpdateState_ShouldThrow_WhenTheStateDoesNotExist()
    {
        var game = new Game("Test Game");

        var action = () => game.UpdateState((GameState)99);

        action.Should().Throw<ArgumentException>();
        game.State.Should().Be(GameState.Owned);
    }

    [Fact]
    public void UpdateAdditionDate_ShouldStoreValue_AndAcceptNull()
    {
        var game = new Game("Test Game");
        var date = new DateTime(2020, 5, 1, 0, 0, 0, DateTimeKind.Utc);

        game.UpdateAdditionDate(date);
        game.AdditionDate.Should().Be(date);

        game.UpdateAdditionDate(null);
        game.AdditionDate.Should().BeNull();
    }

    #endregion

    #region Prices

    [Fact]
    public void UpdateBuyingPrice_ShouldStoreTheAmount_AndClearItOnNull()
    {
        var game = new Game("Test Game");

        game.UpdateBuyingPrice(49.99m);
        game.BuyingPrice!.Amount.Should().Be(49.99m);
        game.SoldPrice.Should().BeNull();

        game.UpdateBuyingPrice(null);
        game.BuyingPrice.Should().BeNull();
    }

    [Fact]
    public void UpdateSoldPrice_ShouldStoreTheAmount_AndClearItOnNull()
    {
        var game = new Game("Test Game");

        game.UpdateSoldPrice(25m);
        game.SoldPrice!.Amount.Should().Be(25m);
        game.BuyingPrice.Should().BeNull();

        game.UpdateSoldPrice(null);
        game.SoldPrice.Should().BeNull();
    }

    [Fact]
    public void UpdateBuyingPrice_ShouldAcceptZero_ForAFreeGame()
    {
        var game = new Game("Test Game");

        game.UpdateBuyingPrice(0m);

        game.BuyingPrice!.Amount.Should().Be(0m);
    }

    [Fact]
    public void UpdatePrices_ShouldThrow_WhenTheAmountIsNegative()
    {
        var game = new Game("Test Game");
        game.UpdateBuyingPrice(10m);
        game.UpdateSoldPrice(5m);

        var buying = () => game.UpdateBuyingPrice(-0.01m);
        var sold = () => game.UpdateSoldPrice(-0.01m);

        buying.Should().Throw<ArgumentException>();
        sold.Should().Throw<ArgumentException>();
        game.BuyingPrice!.Amount.Should().Be(10m);
        game.SoldPrice!.Amount.Should().Be(5m);
    }

    #endregion

    #region Rating and weight

    [Fact]
    public void UpdateRating_ShouldStoreValue_AndClearItOnNull()
    {
        var game = new Game("Test Game");

        game.UpdateRating(7.5);
        game.Rating!.Value.Should().Be(7.5);

        game.UpdateRating(null);
        game.Rating.Should().BeNull();
    }

    [Fact]
    public void UpdateWeight_ShouldStoreValue_AndClearItOnNull()
    {
        var game = new Game("Test Game");

        game.UpdateWeight(2.8);
        game.Weight!.Value.Should().Be(2.8);

        game.UpdateWeight(null);
        game.Weight.Should().BeNull();
    }

    #endregion

    #region Links

    [Fact]
    public void AddCategory_ShouldSkipACategoryWithTheSameName()
    {
        var game = new Game("Test Game");

        game.AddCategory(new GameCategory("Economic"));
        game.AddCategory(new GameCategory("Economic"));
        game.AddCategory(new GameCategory("Strategy"));

        game.Categories.Select(c => c.Name).Should().Equal("Economic", "Strategy");
    }

    [Fact]
    public void AddMechanic_ShouldSkipAMechanicWithTheSameName()
    {
        var game = new Game("Test Game");

        game.AddMechanic(new GameMechanic("Hand Management"));
        game.AddMechanic(new GameMechanic("Hand Management"));
        game.AddMechanic(new GameMechanic("Network Building"));

        game.Mechanics.Select(m => m.Name).Should().Equal("Hand Management", "Network Building");
    }

    [Fact]
    public void AddPerson_ShouldOnlySkipTheSameNameWithTheSameType()
    {
        var game = new Game("Test Game");

        game.AddPerson(new Person("Martin Wallace", PersonType.Designer));
        game.AddPerson(new Person("Martin Wallace", PersonType.Designer));
        game.AddPerson(new Person("Martin Wallace", PersonType.Artist));
        game.AddPerson(new Person("Gavan Brown", PersonType.Designer));

        game.People.Select(p => (p.Name, p.Type)).Should().Equal(
            ("Martin Wallace", PersonType.Designer),
            ("Martin Wallace", PersonType.Artist),
            ("Gavan Brown", PersonType.Designer));
    }

    [Fact]
    public void AddLinks_ShouldThrow_WhenTheValueIsNull()
    {
        var game = new Game("Test Game");

        new Action(() => game.AddCategory(null!)).Should().Throw<ArgumentNullException>();
        new Action(() => game.AddMechanic(null!)).Should().Throw<ArgumentNullException>();
        new Action(() => game.AddPerson(null!)).Should().Throw<ArgumentNullException>();
        new Action(() => game.AddExpansion(null!)).Should().Throw<ArgumentNullException>();
        new Action(() => game.RemoveExpansion(null!)).Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddExpansion_ShouldSkipTheSameBggId_ButKeepOtherExpansions()
    {
        var game = new Game("Test Game");

        game.AddExpansion(new Expansion("First", 10, 1));
        game.AddExpansion(new Expansion("Same id", 10, 1));
        game.AddExpansion(new Expansion("Second", 11, 1));

        game.Expansions.Select(e => e.Title).Should().Equal("First", "Second");
    }

    [Fact]
    public void AddExpansion_ShouldSkipAManualExpansionWithTheSameTitle_IgnoringCase()
    {
        var game = new Game("Test Game");

        game.AddExpansion(new Expansion("Promo", null, 1));
        game.AddExpansion(new Expansion("PROMO", null, 1));
        game.AddExpansion(new Expansion("Other", null, 1));

        game.Expansions.Select(e => e.Title).Should().Equal("Promo", "Other");
    }

    [Fact]
    public void AddExpansion_ShouldKeepAManualAndABggExpansionWithTheSameTitle()
    {
        var game = new Game("Test Game");

        game.AddExpansion(new Expansion("Promo", null, 1));
        game.AddExpansion(new Expansion("Promo", 10, 1));
        game.AddExpansion(new Expansion("Promo", null, 1));

        game.Expansions.Select(e => e.BggId).Should().Equal(null, 10);
    }

    [Fact]
    public void RemoveExpansion_ShouldRemoveOnlyThatExpansion()
    {
        var game = new Game("Test Game");
        var kept = new Expansion("Kept", 10, 1);
        var removed = new Expansion("Removed", 11, 1);
        game.AddExpansion(kept);
        game.AddExpansion(removed);

        game.RemoveExpansion(removed);

        game.Expansions.Should().ContainSingle().Which.Should().BeSameAs(kept);
    }

    #endregion

    #region Loans

    private static readonly DateTime LoanStart = new(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void LoanToPlayer_ShouldAddALoanOfThisGame()
    {
        var game = new Game("Test Game") { Id = 4 };

        var loan = game.LoanToPlayer(9, LoanStart, LoanStart.AddDays(7));

        game.Loans.Should().ContainSingle().Which.Should().BeSameAs(loan);
        loan.GameId.Should().Be(4);
        loan.PlayerId.Should().Be(9);
        loan.LoanDate.Should().Be(LoanStart);
        loan.DueDate.Should().Be(LoanStart.AddDays(7));
    }

    [Fact]
    public void LoanToPlayer_ShouldThrow_WhenTheGameIsAlreadyOnLoan()
    {
        var game = new Game("Test Game") { Id = 4 };
        game.LoanToPlayer(9, LoanStart);

        var action = () => game.LoanToPlayer(10, LoanStart.AddDays(1));

        action.Should().Throw<DomainException>().WithMessage(Constants.Errors.GameAlreadyOnLoan);
        game.Loans.Should().ContainSingle();
    }

    [Fact]
    public void IsLoanedAt_ShouldFollowTheLoan()
    {
        var game = new Game("Test Game") { Id = 4 };
        game.IsLoanedAt(LoanStart).Should().BeFalse();

        var loan = game.LoanToPlayer(9, LoanStart);
        game.IsLoanedAt(LoanStart.AddDays(1)).Should().BeTrue();

        loan.MarkAsReturned(LoanStart.AddDays(2));
        game.IsLoanedAt(LoanStart.AddDays(3)).Should().BeFalse();
    }

    [Fact]
    public void EnsureLoanDatesAreFree_ShouldIgnoreTheLoanItself()
    {
        var game = new Game("Test Game") { Id = 4 };
        var loan = game.LoanToPlayer(9, LoanStart);

        var action = () => game.EnsureLoanDatesAreFree(loan, LoanStart.AddDays(1), null);

        action.Should().NotThrow();
    }

    [Fact]
    public void EnsureLoanDatesAreFree_ShouldThrow_WhenAnotherLoanOverlaps()
    {
        var game = new Game("Test Game") { Id = 4 };
        var other = game.LoanToPlayer(9, LoanStart);
        other.Id = 1;
        var moved = new Loan(4, 10, LoanStart.AddYears(-1)) { Id = 2 };

        var action = () => game.EnsureLoanDatesAreFree(moved, LoanStart.AddDays(1), LoanStart.AddDays(2));

        action.Should().Throw<DomainException>().WithMessage(Constants.Errors.GameAlreadyOnLoan);
    }

    [Fact]
    public void EnsureLoanDatesAreFree_ShouldIgnoreTheSameLoanLoadedTwice()
    {
        var game = new Game("Test Game") { Id = 4 };
        var stored = game.LoanToPlayer(9, LoanStart);
        stored.Id = 1;
        var sameLoan = new Loan(4, 9, LoanStart) { Id = 1 };

        var action = () => game.EnsureLoanDatesAreFree(sameLoan, LoanStart.AddDays(1), null);

        action.Should().NotThrow();
    }

    #endregion
}
