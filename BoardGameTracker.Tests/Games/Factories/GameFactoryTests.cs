using System;
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
    private readonly Mock<IGameRepository> _gameRepositoryMock;
    private readonly Mock<IImageService> _imageServiceMock;
    private readonly GameFactory _factory;

    public GameFactoryTests()
    {
        _gameRepositoryMock = new Mock<IGameRepository>();
        _imageServiceMock = new Mock<IImageService>();
        _factory = new GameFactory(_gameRepositoryMock.Object, _imageServiceMock.Object);

        _gameRepositoryMock
            .Setup(x => x.GetOrCreateCategoriesAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync((IEnumerable<string> names) => names.Select(n => new GameCategory(n)).ToList());
        _gameRepositoryMock
            .Setup(x => x.GetOrCreateMechanicsAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync((IEnumerable<string> names) => names.Select(n => new GameMechanic(n)).ToList());
        _gameRepositoryMock
            .Setup(x => x.GetOrCreatePeopleAsync(It.IsAny<IEnumerable<PersonKey>>()))
            .ReturnsAsync((IEnumerable<PersonKey> keys) => keys.Select(k => new Person(k.Name, k.Type)).ToList());
        _imageServiceMock
            .Setup(x => x.DownloadImage(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("downloaded-image.jpg");
    }

    #region CreateFromBggAsync Tests

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CreateFromBggAsync_ShouldCreateGameWithBasicProperties(bool hasScoring)
    {
        var item = CreateBasicItem();

        var result = await _factory.CreateFromBggAsync(item, hasScoring, GameState.Owned, null, null);

        result.Should().NotBeNull();
        result.Title.Should().Be("Test Game");
        result.HasScoring.Should().Be(hasScoring);
        result.State.Should().Be(GameState.Owned);
    }

    [Fact]
    public async Task CreateFromBggAsync_ShouldDownloadImage()
    {
        var item = CreateBasicItem();
        item.Image = "https://example.com/game.jpg";

        var result = await _factory.CreateFromBggAsync(item, false, GameState.Owned, null, null);

        result.Image.Should().Be("downloaded-image.jpg");
        _imageServiceMock.Verify(x => x.DownloadImage("https://example.com/game.jpg", item.Id.ToString()), Times.Once);
    }

    [Fact]
    public async Task CreateFromBggAsync_ShouldSetDescription()
    {
        var item = CreateBasicItem();
        item.Description = "A wonderful strategy game";

        var result = await _factory.CreateFromBggAsync(item, false, GameState.Owned, null, null);

        result.Description.Should().Be("A wonderful strategy game");
    }

    [Fact]
    public async Task CreateFromBggAsync_ShouldSetYearPublished()
    {
        var item = CreateBasicItem();
        item.YearPublished = 2023;

        var result = await _factory.CreateFromBggAsync(item, false, GameState.Owned, null, null);

        result.YearPublished.Should().Be(2023);
    }

    [Fact]
    public async Task CreateFromBggAsync_ShouldNotSetYearPublished_WhenNull()
    {
        var item = CreateBasicItem();
        item.YearPublished = null;

        var result = await _factory.CreateFromBggAsync(item, false, GameState.Owned, null, null);

        result.YearPublished.Should().BeNull();
    }

    [Theory]
    [InlineData(12, 12)]
    [InlineData(0, null)]
    [InlineData(null, null)]
    public async Task CreateFromBggAsync_ShouldSetMinAge(int? minAge, int? expected)
    {
        var item = CreateBasicItem();
        item.MinAge = minAge;

        var result = await _factory.CreateFromBggAsync(item, false, GameState.Owned, null, null);

        result.MinAge.Should().Be(expected);
    }

    [Fact]
    public async Task CreateFromBggAsync_ShouldSetPlayerCount()
    {
        var item = CreateBasicItem();
        item.MinPlayers = 2;
        item.MaxPlayers = 6;

        var result = await _factory.CreateFromBggAsync(item, false, GameState.Owned, null, null);

        result.PlayerCount.Should().NotBeNull();
        result.PlayerCount!.Min.Should().Be(2);
        result.PlayerCount!.Max.Should().Be(6);
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

        var result = await _factory.CreateFromBggAsync(item, false, GameState.Owned, null, null);

        result.PlayerCount.Should().BeNull();
    }

    [Fact]
    public async Task CreateFromBggAsync_ShouldSetPlayTime()
    {
        var item = CreateBasicItem();
        item.MinPlayingTime = 45;
        item.MaxPlayingTime = 90;

        var result = await _factory.CreateFromBggAsync(item, false, GameState.Owned, null, null);

        result.PlayTime.Should().NotBeNull();
        result.PlayTime!.MinMinutes.Should().Be(45);
        result.PlayTime!.MaxMinutes.Should().Be(90);
    }

    [Fact]
    public async Task CreateFromBggAsync_ShouldLeavePlayTimeEmpty_WhenTimesAreNull()
    {
        var item = CreateBasicItem();
        item.MinPlayingTime = null;
        item.MaxPlayingTime = null;

        var result = await _factory.CreateFromBggAsync(item, false, GameState.Owned, null, null);

        result.PlayTime.Should().BeNull();
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

        var result = await _factory.CreateFromBggAsync(item, false, GameState.Owned, null, null);

        result.PlayTime.Should().NotBeNull();
        result.PlayTime!.MinMinutes.Should().Be(expectedMin);
        result.PlayTime!.MaxMinutes.Should().Be(expectedMax);
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

        var result = await _factory.CreateFromBggAsync(item, false, GameState.Owned, null, null);

        result.Rating.Should().NotBeNull();
        result.Rating!.Value.Should().Be(8.7);
        result.Weight.Should().NotBeNull();
        result.Weight!.Value.Should().Be(3.5);
    }

    [Fact]
    public async Task CreateFromBggAsync_ShouldNotSetRatingAndWeight_WhenStatisticsAreNull()
    {
        var item = CreateBasicItem();
        item.Statistics = null;

        var result = await _factory.CreateFromBggAsync(item, false, GameState.Owned, null, null);

        result.Rating.Should().BeNull();
        result.Weight.Should().BeNull();
    }

    [Fact]
    public async Task CreateFromBggAsync_ShouldDownloadImageWithEmptyUrl_WhenImageIsNull()
    {
        var item = CreateBasicItem();
        item.Image = null;

        await _factory.CreateFromBggAsync(item, false, GameState.Owned, null, null);

        _imageServiceMock.Verify(x => x.DownloadImage(string.Empty, item.Id.ToString()), Times.Once);
    }

    [Fact]
    public async Task CreateFromBggAsync_ShouldSetBggId()
    {
        var item = CreateBasicItem();
        item.Id = 12345;

        var result = await _factory.CreateFromBggAsync(item, false, GameState.Owned, null, null);

        result.BggId.Should().Be(12345);
    }

    [Fact]
    public async Task CreateFromBggAsync_ShouldSetBuyingPrice()
    {
        var item = CreateBasicItem();

        var result = await _factory.CreateFromBggAsync(item, false, GameState.Owned, 49.99m, null);

        result.BuyingPrice.Should().NotBeNull();
        result.BuyingPrice!.Amount.Should().Be(49.99m);
    }

    [Fact]
    public async Task CreateFromBggAsync_ShouldNotSetBuyingPrice_WhenNull()
    {
        var item = CreateBasicItem();

        var result = await _factory.CreateFromBggAsync(item, false, GameState.Owned, null, null);

        result.BuyingPrice.Should().BeNull();
    }

    [Theory]
    [InlineData("https://shop.example.com/game", "https://shop.example.com/game")]
    [InlineData(null, null)]
    public async Task CreateFromBggAsync_ShouldSetShopUrl_WhenProvided(string? shopUrl, string? expected)
    {
        var item = CreateBasicItem();

        var result = await _factory.CreateFromBggAsync(item, false, GameState.Owned, null, null, shopUrl);

        result.ShopUrl.Should().Be(expected);
    }

    [Fact]
    public async Task CreateFromBggAsync_ShouldSetAdditionDate_WhenProvided()
    {
        var item = CreateBasicItem();
        var additionDate = new DateTime(2023, 6, 15);

        var result = await _factory.CreateFromBggAsync(item, false, GameState.Owned, null, additionDate);

        result.AdditionDate.Should().Be(additionDate);
    }

    [Theory]
    [InlineData(GameState.Owned)]
    [InlineData(GameState.Wanted)]
    [InlineData(GameState.ForTrade)]
    [InlineData(GameState.PreviouslyOwned)]
    public async Task CreateFromBggAsync_ShouldSupportAllGameStates(GameState state)
    {
        var item = CreateBasicItem();

        var result = await _factory.CreateFromBggAsync(item, false, state, null, null);

        result.State.Should().Be(state);
    }

    [Fact]
    public async Task CreateFromBggAsync_ShouldThrow_WhenNameIsEmpty()
    {
        var item = CreateBasicItem();
        item.Name = "";

        var act = () => _factory.CreateFromBggAsync(item, false, GameState.Owned, null, null);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*valid name*");
    }

    [Fact]
    public async Task CreateFromBggAsync_ShouldThrow_WhenMinPlayersExceedsMaxPlayers()
    {
        var item = CreateBasicItem();
        item.MinPlayers = 5;
        item.MaxPlayers = 2;

        var act = () => _factory.CreateFromBggAsync(item, false, GameState.Owned, null, null);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*player count*");
    }

    [Fact]
    public async Task CreateFromBggAsync_ShouldThrow_WhenMinPlayTimeExceedsMaxPlayTime()
    {
        var item = CreateBasicItem();
        item.MinPlayingTime = 90;
        item.MaxPlayingTime = 30;

        var act = () => _factory.CreateFromBggAsync(item, false, GameState.Owned, null, null);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*play time*");
    }

    [Fact]
    public async Task CreateFromBggAsync_ShouldDeleteTheDownloadedImage_WhenTheGameCannotBeBuilt()
    {
        var item = CreateBasicItem();
        item.YearPublished = DateTime.UtcNow.Year + 50;

        var act = () => _factory.CreateFromBggAsync(item, false, GameState.Owned, null, null);

        await act.Should().ThrowAsync<ArgumentException>();
        _imageServiceMock.Verify(x => x.DeleteImage("downloaded-image.jpg"), Times.Once);
    }

    [Fact]
    public async Task CreateFromBggAsync_ShouldDeleteTheDownloadedImage_WhenTaxonomyLookupFails()
    {
        var item = CreateBasicItem();
        item.Links = [new ThingResponse.Link { Type = Constants.Bgg.Category, Id = 1, Value = "Strategy" }];
        _gameRepositoryMock
            .Setup(x => x.GetOrCreateCategoriesAsync(It.IsAny<IEnumerable<string>>()))
            .ThrowsAsync(new InvalidOperationException("connection lost"));

        var act = () => _factory.CreateFromBggAsync(item, false, GameState.Owned, null, null);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _imageServiceMock.Verify(x => x.DeleteImage("downloaded-image.jpg"), Times.Once);
    }

    [Fact]
    public async Task CreateFromBggAsync_ShouldKeepTheImage_WhenTheGameIsBuilt()
    {
        var item = CreateBasicItem();

        await _factory.CreateFromBggAsync(item, false, GameState.Owned, null, null);

        _imageServiceMock.Verify(x => x.DeleteImage(It.IsAny<string?>()), Times.Never);
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

        var result = await _factory.CreateFromBggAsync(item, false, GameState.Owned, null, null);

        _gameRepositoryMock.Verify(x => x.GetOrCreateCategoriesAsync(
            It.Is<IEnumerable<string>>(n => n.SequenceEqual(new[] { "Strategy", "Economic" }))), Times.Once);
        result.Categories.Should().HaveCount(2);
        result.Categories.Should().Contain(c => c.Name == "Strategy");
        result.Categories.Should().Contain(c => c.Name == "Economic");
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

        var result = await _factory.CreateFromBggAsync(item, false, GameState.Owned, null, null);

        _gameRepositoryMock.Verify(x => x.GetOrCreateMechanicsAsync(
            It.Is<IEnumerable<string>>(n => n.SequenceEqual(new[] { "Worker Placement", "Deck Building" }))), Times.Once);
        result.Mechanics.Should().HaveCount(2);
        result.Mechanics.Should().Contain(m => m.Name == "Worker Placement");
        result.Mechanics.Should().Contain(m => m.Name == "Deck Building");
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

        var result = await _factory.CreateFromBggAsync(item, false, GameState.Owned, null, null);

        _gameRepositoryMock.Verify(x => x.GetOrCreatePeopleAsync(
            It.Is<IEnumerable<PersonKey>>(k => k.SequenceEqual(new[]
            {
                new PersonKey("Designer One", PersonType.Designer),
                new PersonKey("Artist One", PersonType.Artist),
                new PersonKey("Publisher One", PersonType.Publisher)
            }))), Times.Once);
        result.People.Should().HaveCount(3);
        result.People.Should().Contain(p => p.Name == "Designer One" && p.Type == PersonType.Designer);
        result.People.Should().Contain(p => p.Name == "Artist One" && p.Type == PersonType.Artist);
        result.People.Should().Contain(p => p.Name == "Publisher One" && p.Type == PersonType.Publisher);
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

        var result = await _factory.CreateFromBggAsync(item, false, GameState.Owned, null, null);

        _gameRepositoryMock.Verify(x => x.GetOrCreateCategoriesAsync(
            It.Is<IEnumerable<string>>(n => n.SequenceEqual(new[] { "Strategy" }))), Times.Once);
        result.Categories.Should().ContainSingle(c => c.Name == "Strategy");
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
