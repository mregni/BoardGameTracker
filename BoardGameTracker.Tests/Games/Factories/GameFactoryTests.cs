using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BoardGamer.BoardGameGeek.BoardGameGeekXmlApi2;
using BoardGameTracker.Common;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Core.Games.Factories;
using BoardGameTracker.Core.Games.Interfaces;
using BoardGameTracker.Core.Images.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Games.Factories;

public class GameFactoryTests
{
    private const string BggId = "12345";
    private const string StoredImage = "downloaded-image.jpg";

    private string[] _categories = [];
    private string[] _mechanics = [];
    private PersonKey[] _people = [];
    private readonly Mock<IGameRepository> _gameRepositoryMock;
    private readonly Mock<IImageService> _imageServiceMock;
    private readonly GameFactory _factory;

    public GameFactoryTests()
    {
        _gameRepositoryMock = new Mock<IGameRepository>();
        _imageServiceMock = new Mock<IImageService>();
        _factory = new GameFactory(_gameRepositoryMock.Object, _imageServiceMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _gameRepositoryMock.VerifyNoOtherCalls();
        _imageServiceMock.VerifyNoOtherCalls();
    }

    private void SetupCalls(ThingResponse.Item item, string storedImage = StoredImage)
    {
        _imageServiceMock
            .Setup(x => x.DownloadImage(item.Image ?? string.Empty, BggId))
            .ReturnsAsync(storedImage);
        _gameRepositoryMock
            .Setup(x => x.GetOrCreateCategoriesAsync(It.Is<IEnumerable<string>>(n => n.SequenceEqual(_categories))))
            .ReturnsAsync(() => _categories.Select(n => new GameCategory(n)).ToList());
        _gameRepositoryMock
            .Setup(x => x.GetOrCreateMechanicsAsync(It.Is<IEnumerable<string>>(n => n.SequenceEqual(_mechanics))))
            .ReturnsAsync(() => _mechanics.Select(n => new GameMechanic(n)).ToList());
        _gameRepositoryMock
            .Setup(x => x.GetOrCreatePeopleAsync(It.Is<IEnumerable<PersonKey>>(k => k.SequenceEqual(_people))))
            .ReturnsAsync(() => _people.Select(k => new Person(k.Name, k.Type)).ToList());
    }

    private void VerifyCalls(ThingResponse.Item item)
    {
        _imageServiceMock.Verify(x => x.DownloadImage(item.Image ?? string.Empty, BggId), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetOrCreateCategoriesAsync(It.Is<IEnumerable<string>>(n => n.SequenceEqual(_categories))), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetOrCreateMechanicsAsync(It.Is<IEnumerable<string>>(n => n.SequenceEqual(_mechanics))), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetOrCreatePeopleAsync(It.Is<IEnumerable<PersonKey>>(k => k.SequenceEqual(_people))), Times.Once);
    }

    #region CreateFromBggAsync Tests

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CreateFromBggAsync_ShouldCreateGameWithBasicProperties(bool hasScoring)
    {
        var item = CreateBasicItem();
        SetupCalls(item);

        var result = await _factory.CreateFromBggAsync(item, hasScoring, GameState.Owned, null, null);

        result.Should().NotBeNull();
        result.Title.Should().Be("Test Game");
        result.HasScoring.Should().Be(hasScoring);
        result.State.Should().Be(GameState.Owned);
        VerifyCalls(item);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateFromBggAsync_ShouldDownloadImage()
    {
        var item = CreateBasicItem();
        item.Image = "https://example.com/game.jpg";
        SetupCalls(item);

        var result = await _factory.CreateFromBggAsync(item, false, GameState.Owned, null, null);

        result.Image.Should().Be(StoredImage);
        VerifyCalls(item);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateFromBggAsync_ShouldSetDescription()
    {
        var item = CreateBasicItem();
        item.Description = "A wonderful strategy game";
        SetupCalls(item);

        var result = await _factory.CreateFromBggAsync(item, false, GameState.Owned, null, null);

        result.Description.Should().Be("A wonderful strategy game");
        VerifyCalls(item);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateFromBggAsync_ShouldSetYearPublished()
    {
        var item = CreateBasicItem();
        item.YearPublished = 2023;
        SetupCalls(item);

        var result = await _factory.CreateFromBggAsync(item, false, GameState.Owned, null, null);

        result.YearPublished.Should().Be(2023);
        VerifyCalls(item);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateFromBggAsync_ShouldNotSetYearPublished_WhenNull()
    {
        var item = CreateBasicItem();
        item.YearPublished = null;
        SetupCalls(item);

        var result = await _factory.CreateFromBggAsync(item, false, GameState.Owned, null, null);

        result.YearPublished.Should().BeNull();
        VerifyCalls(item);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(12, 12)]
    [InlineData(0, null)]
    [InlineData(null, null)]
    public async Task CreateFromBggAsync_ShouldSetMinAge(int? minAge, int? expected)
    {
        var item = CreateBasicItem();
        item.MinAge = minAge;
        SetupCalls(item);

        var result = await _factory.CreateFromBggAsync(item, false, GameState.Owned, null, null);

        result.MinAge.Should().Be(expected);
        VerifyCalls(item);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateFromBggAsync_ShouldSetPlayerCount()
    {
        var item = CreateBasicItem();
        item.MinPlayers = 2;
        item.MaxPlayers = 6;
        SetupCalls(item);

        var result = await _factory.CreateFromBggAsync(item, false, GameState.Owned, null, null);

        result.PlayerCount.Should().NotBeNull();
        result.PlayerCount!.Min.Should().Be(2);
        result.PlayerCount!.Max.Should().Be(6);
        VerifyCalls(item);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(0, 6)]
    [InlineData(2, 0)]
    [InlineData(0, 0)]
    public async Task CreateFromBggAsync_ShouldNotSetPlayerCount_WhenAnyValueIsZero(int minPlayers, int maxPlayers)
    {
        var item = CreateBasicItem();
        item.MinPlayers = minPlayers;
        item.MaxPlayers = maxPlayers;
        SetupCalls(item);

        var result = await _factory.CreateFromBggAsync(item, false, GameState.Owned, null, null);

        result.PlayerCount.Should().BeNull();
        VerifyCalls(item);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateFromBggAsync_ShouldSetPlayTime()
    {
        var item = CreateBasicItem();
        item.MinPlayingTime = 45;
        item.MaxPlayingTime = 90;
        SetupCalls(item);

        var result = await _factory.CreateFromBggAsync(item, false, GameState.Owned, null, null);

        result.PlayTime.Should().NotBeNull();
        result.PlayTime!.MinMinutes.Should().Be(45);
        result.PlayTime!.MaxMinutes.Should().Be(90);
        VerifyCalls(item);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateFromBggAsync_ShouldLeavePlayTimeEmpty_WhenTimesAreNull()
    {
        var item = CreateBasicItem();
        item.MinPlayingTime = null;
        item.MaxPlayingTime = null;
        SetupCalls(item);

        var result = await _factory.CreateFromBggAsync(item, false, GameState.Owned, null, null);

        result.PlayTime.Should().BeNull();
        VerifyCalls(item);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null, 60, 60, 60)]
    [InlineData(45, null, 45, 45)]
    [InlineData(0, 90, 90, 90)]
    public async Task CreateFromBggAsync_ShouldUseTheKnownBound_WhenOnlyOnePlayTimeIsProvided(
        int? min, int? max, int expectedMin, int expectedMax)
    {
        var item = CreateBasicItem();
        item.MinPlayingTime = min;
        item.MaxPlayingTime = max;
        SetupCalls(item);

        var result = await _factory.CreateFromBggAsync(item, false, GameState.Owned, null, null);

        result.PlayTime.Should().NotBeNull();
        result.PlayTime!.MinMinutes.Should().Be(expectedMin);
        result.PlayTime!.MaxMinutes.Should().Be(expectedMax);
        VerifyCalls(item);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateFromBggAsync_ShouldSetRatingAndWeight()
    {
        var item = CreateBasicItem();
        item.Statistics = new ThingResponse.Statistics
        {
            Ratings = new ThingResponse.Ratings
            {
                Average = 8.7,
                AverageWeight = 3.5
            }
        };
        SetupCalls(item);

        var result = await _factory.CreateFromBggAsync(item, false, GameState.Owned, null, null);

        result.Rating.Should().NotBeNull();
        result.Rating!.Value.Should().Be(8.7);
        result.Weight.Should().NotBeNull();
        result.Weight!.Value.Should().Be(3.5);
        VerifyCalls(item);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CreateFromBggAsync_ShouldNotSetWeight_WhenBggHasNoWeightVotes(double averageWeight)
    {
        var item = CreateBasicItem();
        item.Statistics = new ThingResponse.Statistics
        {
            Ratings = new ThingResponse.Ratings
            {
                Average = 7.1,
                AverageWeight = averageWeight
            }
        };
        SetupCalls(item);

        var result = await _factory.CreateFromBggAsync(item, false, GameState.Owned, null, null);

        result.Weight.Should().BeNull();
        result.Rating!.Value.Should().Be(7.1);
        VerifyCalls(item);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateFromBggAsync_ShouldNotSetRatingAndWeight_WhenStatisticsAreNull()
    {
        var item = CreateBasicItem();
        item.Statistics = null;
        SetupCalls(item);

        var result = await _factory.CreateFromBggAsync(item, false, GameState.Owned, null, null);

        result.Rating.Should().BeNull();
        result.Weight.Should().BeNull();
        VerifyCalls(item);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateFromBggAsync_ShouldDownloadImageWithEmptyUrl_WhenImageIsNull()
    {
        var item = CreateBasicItem();
        item.Image = null;
        SetupCalls(item);

        var result = await _factory.CreateFromBggAsync(item, false, GameState.Owned, null, null);

        result.Image.Should().Be(StoredImage);
        VerifyCalls(item);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateFromBggAsync_ShouldLeaveImageEmpty_WhenNoImageCouldBeStored()
    {
        var item = CreateBasicItem();
        item.Image = "https://example.com/broken.jpg";
        SetupCalls(item, storedImage: string.Empty);

        var result = await _factory.CreateFromBggAsync(item, false, GameState.Owned, null, null);

        result.Image.Should().BeNull();
        VerifyCalls(item);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateFromBggAsync_ShouldSetBggId()
    {
        var item = CreateBasicItem();
        item.Id = 12345;
        SetupCalls(item);

        var result = await _factory.CreateFromBggAsync(item, false, GameState.Owned, null, null);

        result.BggId.Should().Be(12345);
        VerifyCalls(item);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateFromBggAsync_ShouldSetBuyingPrice()
    {
        var item = CreateBasicItem();
        SetupCalls(item);

        var result = await _factory.CreateFromBggAsync(item, false, GameState.Owned, 49.99m, null);

        result.BuyingPrice.Should().NotBeNull();
        result.BuyingPrice!.Amount.Should().Be(49.99m);
        VerifyCalls(item);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateFromBggAsync_ShouldNotSetBuyingPrice_WhenNull()
    {
        var item = CreateBasicItem();
        SetupCalls(item);

        var result = await _factory.CreateFromBggAsync(item, false, GameState.Owned, null, null);

        result.BuyingPrice.Should().BeNull();
        VerifyCalls(item);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("https://shop.example.com/game", "https://shop.example.com/game")]
    [InlineData(null, null)]
    public async Task CreateFromBggAsync_ShouldSetShopUrl_WhenProvided(string? shopUrl, string? expected)
    {
        var item = CreateBasicItem();
        SetupCalls(item);

        var result = await _factory.CreateFromBggAsync(item, false, GameState.Owned, null, null, shopUrl);

        result.ShopUrl.Should().Be(expected);
        VerifyCalls(item);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateFromBggAsync_ShouldSetAdditionDate_WhenProvided()
    {
        var item = CreateBasicItem();
        var additionDate = new DateTime(2023, 6, 15);
        SetupCalls(item);

        var result = await _factory.CreateFromBggAsync(item, false, GameState.Owned, null, additionDate);

        result.AdditionDate.Should().Be(additionDate);
        VerifyCalls(item);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(GameState.Owned)]
    [InlineData(GameState.Wanted)]
    [InlineData(GameState.ForTrade)]
    [InlineData(GameState.PreviouslyOwned)]
    [InlineData(GameState.Sold)]
    public async Task CreateFromBggAsync_ShouldSupportAllGameStates(GameState state)
    {
        var item = CreateBasicItem();
        SetupCalls(item);

        var result = await _factory.CreateFromBggAsync(item, false, state, null, null);

        result.State.Should().Be(state);
        VerifyCalls(item);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateFromBggAsync_ShouldThrow_WhenNameIsEmpty()
    {
        var item = CreateBasicItem();
        item.Name = "";

        var act = () => _factory.CreateFromBggAsync(item, false, GameState.Owned, null, null);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Game must have a valid name from BGG");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateFromBggAsync_ShouldThrow_WhenMinPlayersExceedsMaxPlayers()
    {
        var item = CreateBasicItem();
        item.MinPlayers = 5;
        item.MaxPlayers = 2;

        var act = () => _factory.CreateFromBggAsync(item, false, GameState.Owned, null, null);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Invalid player count range from BGG: 5-2");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateFromBggAsync_ShouldThrow_WhenMinPlayTimeExceedsMaxPlayTime()
    {
        var item = CreateBasicItem();
        item.MinPlayingTime = 90;
        item.MaxPlayingTime = 30;

        var act = () => _factory.CreateFromBggAsync(item, false, GameState.Owned, null, null);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Invalid play time range from BGG: 90-30");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateFromBggAsync_ShouldDeleteTheDownloadedImage_WhenTheGameCannotBeBuilt()
    {
        var item = CreateBasicItem();
        item.YearPublished = DateTime.UtcNow.Year + 50;
        SetupCalls(item);

        var act = () => _factory.CreateFromBggAsync(item, false, GameState.Owned, null, null);

        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("year");
        _imageServiceMock.Verify(x => x.DeleteImage(StoredImage), Times.Once);
        VerifyCalls(item);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateFromBggAsync_ShouldDeleteTheDownloadedImage_WhenTaxonomyLookupFails()
    {
        var item = CreateBasicItem();
        item.Links = [new ThingResponse.Link { Type = Constants.Bgg.Category, Id = 1, Value = "Strategy" }];
        _imageServiceMock.Setup(x => x.DownloadImage(item.Image!, BggId)).ReturnsAsync(StoredImage);
        _gameRepositoryMock
            .Setup(x => x.GetOrCreateCategoriesAsync(It.Is<IEnumerable<string>>(n => n.SequenceEqual(new[] { "Strategy" }))))
            .ThrowsAsync(new InvalidOperationException("connection lost"));

        var act = () => _factory.CreateFromBggAsync(item, false, GameState.Owned, null, null);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("connection lost");
        _imageServiceMock.Verify(x => x.DownloadImage(item.Image!, BggId), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetOrCreateCategoriesAsync(It.Is<IEnumerable<string>>(n => n.SequenceEqual(new[] { "Strategy" }))), Times.Once);
        _imageServiceMock.Verify(x => x.DeleteImage(StoredImage), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateFromBggAsync_ShouldKeepTheImage_WhenTheGameIsBuilt()
    {
        var item = CreateBasicItem();
        SetupCalls(item);

        var result = await _factory.CreateFromBggAsync(item, false, GameState.Owned, null, null);

        result.Image.Should().Be(StoredImage);
        VerifyCalls(item);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateFromBggAsync_ShouldAddCategoriesFromLinks()
    {
        var item = CreateBasicItem();
        item.Links =
        [
            new ThingResponse.Link { Type = Constants.Bgg.Category, Id = 1, Value = "Strategy" },
            new ThingResponse.Link { Type = Constants.Bgg.Category, Id = 2, Value = "Economic" }
        ];

        _categories = ["Strategy", "Economic"];
        SetupCalls(item);

        var result = await _factory.CreateFromBggAsync(item, false, GameState.Owned, null, null);

        result.Categories.Select(c => c.Name).Should().Equal("Strategy", "Economic");
        VerifyCalls(item);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateFromBggAsync_ShouldAddMechanicsFromLinks()
    {
        var item = CreateBasicItem();
        item.Links =
        [
            new ThingResponse.Link { Type = Constants.Bgg.Mechanic, Id = 1, Value = "Worker Placement" },
            new ThingResponse.Link { Type = Constants.Bgg.Mechanic, Id = 2, Value = "Deck Building" }
        ];

        _mechanics = ["Worker Placement", "Deck Building"];
        SetupCalls(item);

        var result = await _factory.CreateFromBggAsync(item, false, GameState.Owned, null, null);

        result.Mechanics.Select(m => m.Name).Should().Equal("Worker Placement", "Deck Building");
        VerifyCalls(item);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateFromBggAsync_ShouldAddPeopleFromLinks()
    {
        var item = CreateBasicItem();
        item.Links =
        [
            new ThingResponse.Link { Type = Constants.Bgg.Designer, Id = 1, Value = "Designer One" },
            new ThingResponse.Link { Type = Constants.Bgg.Artist, Id = 2, Value = "Artist One" },
            new ThingResponse.Link { Type = Constants.Bgg.Publisher, Id = 3, Value = "Publisher One" }
        ];
        _people =
        [
            new PersonKey("Designer One", PersonType.Designer),
            new PersonKey("Artist One", PersonType.Artist),
            new PersonKey("Publisher One", PersonType.Publisher)
        ];
        SetupCalls(item);

        var result = await _factory.CreateFromBggAsync(item, false, GameState.Owned, null, null);

        result.People.Select(p => (p.Name, p.Type)).Should().Equal(
            ("Designer One", PersonType.Designer),
            ("Artist One", PersonType.Artist),
            ("Publisher One", PersonType.Publisher));
        VerifyCalls(item);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateFromBggAsync_ShouldFilterOutEmptyLinkValues()
    {
        var item = CreateBasicItem();
        item.Links =
        [
            new ThingResponse.Link { Type = Constants.Bgg.Category, Id = 1, Value = "Strategy" },
            new ThingResponse.Link { Type = Constants.Bgg.Category, Id = 2, Value = "" },
            new ThingResponse.Link { Type = Constants.Bgg.Category, Id = 3, Value = " " }
        ];
        _categories = ["Strategy"];
        SetupCalls(item);

        var result = await _factory.CreateFromBggAsync(item, false, GameState.Owned, null, null);

        result.Categories.Should().ContainSingle().Which.Name.Should().Be("Strategy");
        VerifyCalls(item);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(3, 3)]
    [InlineData(1, 2)]
    public async Task CreateFromBggAsync_ShouldAcceptAPlayerRange_WhenMinIsNotAboveMax(int min, int max)
    {
        var item = CreateBasicItem();
        item.MinPlayers = min;
        item.MaxPlayers = max;
        SetupCalls(item);

        var result = await _factory.CreateFromBggAsync(item, false, GameState.Owned, null, null);

        result.PlayerCount!.Min.Should().Be(min);
        result.PlayerCount!.Max.Should().Be(max);
        VerifyCalls(item);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateFromBggAsync_ShouldAcceptAPlayTime_WhenMinEqualsMax()
    {
        var item = CreateBasicItem();
        item.MinPlayingTime = 60;
        item.MaxPlayingTime = 60;
        SetupCalls(item);

        var result = await _factory.CreateFromBggAsync(item, false, GameState.Owned, null, null);

        result.PlayTime!.MinMinutes.Should().Be(60);
        result.PlayTime!.MaxMinutes.Should().Be(60);
        VerifyCalls(item);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateFromBggAsync_ShouldNotSetRatingAndWeight_WhenRatingsAreNull()
    {
        var item = CreateBasicItem();
        item.Statistics = new ThingResponse.Statistics { Ratings = null };
        SetupCalls(item);

        var result = await _factory.CreateFromBggAsync(item, false, GameState.Owned, null, null);

        result.Rating.Should().BeNull();
        result.Weight.Should().BeNull();
        VerifyCalls(item);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateFromBggAsync_ShouldLookUpNothing_WhenBggSendsNoLinks()
    {
        var item = CreateBasicItem();
        item.Links = null;
        SetupCalls(item);

        var result = await _factory.CreateFromBggAsync(item, false, GameState.Owned, null, null);

        result.Categories.Should().BeEmpty();
        result.Mechanics.Should().BeEmpty();
        result.People.Should().BeEmpty();
        VerifyCalls(item);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateFromBggAsync_ShouldIgnoreLinksOfAnotherType()
    {
        var item = CreateBasicItem();
        item.Links = [new ThingResponse.Link { Type = "boardgameexpansion", Id = 1, Value = "Some Expansion" }];
        SetupCalls(item);

        var result = await _factory.CreateFromBggAsync(item, false, GameState.Owned, null, null);

        result.Categories.Should().BeEmpty();
        result.Mechanics.Should().BeEmpty();
        result.People.Should().BeEmpty();
        VerifyCalls(item);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateFromBggAsync_ShouldLeaveTheDescriptionEmpty_WhenBggSendsNone()
    {
        var item = CreateBasicItem();
        item.Description = null;
        SetupCalls(item);

        var result = await _factory.CreateFromBggAsync(item, false, GameState.Owned, null, null);

        result.Description.Should().BeEmpty();
        VerifyCalls(item);
        VerifyNoOtherCalls();
    }

    #endregion

    #region Helper Methods

    private static ThingResponse.Item CreateBasicItem()
    {
        return new ThingResponse.Item
        {
            Id = 12345,
            Name = "Test Game",
            Type = "boardgame",
            Description = "A test game description",
            Thumbnail = "thumb.jpg",
            Image = "image.jpg",
            YearPublished = 2020,
            MinPlayers = 2,
            MaxPlayers = 4,
            MinPlayingTime = 30,
            MaxPlayingTime = 60,
            MinAge = 10,
            Links = []
        };
    }

    #endregion
}
