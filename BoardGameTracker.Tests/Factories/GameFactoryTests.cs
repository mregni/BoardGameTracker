using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BoardGamer.BoardGameGeek.BoardGameGeekXmlApi2;
using BoardGameTracker.Common;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Games.Factories;
using BoardGameTracker.Core.Games.Interfaces;
using BoardGameTracker.Core.Images.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Factories;

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
            .Setup(x => x.AddGameCategoriesIfNotExists(It.IsAny<IEnumerable<GameCategory>>()))
            .Returns(Task.CompletedTask);
        _gameRepositoryMock
            .Setup(x => x.AddGameMechanicsIfNotExists(It.IsAny<IEnumerable<GameMechanic>>()))
            .Returns(Task.CompletedTask);
        _gameRepositoryMock
            .Setup(x => x.AddPeopleIfNotExists(It.IsAny<IEnumerable<Person>>()))
            .Returns(Task.CompletedTask);
        _imageServiceMock
            .Setup(x => x.DownloadImage(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("downloaded-image.jpg");
    }

    #region CreateFromBggAsync Tests

    [Fact]
    public async Task CreateFromBggAsync_ShouldCreateGameWithBasicProperties()
    {
        var item = CreateBasicItem();

        var result = await _factory.CreateFromBggAsync(item, true, GameState.Owned, null, null);

        result.Should().NotBeNull();
        result.Title.Should().Be("Test Game");
        result.HasScoring.Should().BeTrue();
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
    public async Task CreateFromBggAsync_ShouldAddCategoriesFromLinks()
    {
        var item = CreateBasicItem();
        item.Links =
        [
            new ThingResponse.Link { Type = Constants.Bgg.Category, Id = 1, Value = "Strategy" },
            new ThingResponse.Link { Type = Constants.Bgg.Category, Id = 2, Value = "Economic" }
        ];

        List<GameCategory>? captured = null;
        _gameRepositoryMock
            .Setup(x => x.AddGameCategoriesIfNotExists(It.IsAny<IEnumerable<GameCategory>>()))
            .Callback<IEnumerable<GameCategory>>(c => captured = c.ToList())
            .Returns(Task.CompletedTask);

        await _factory.CreateFromBggAsync(item, false, GameState.Owned, null, null);

        captured.Should().HaveCount(2);
        captured.Should().Contain(c => c.Name == "Strategy");
        captured.Should().Contain(c => c.Name == "Economic");
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

        List<GameMechanic>? captured = null;
        _gameRepositoryMock
            .Setup(x => x.AddGameMechanicsIfNotExists(It.IsAny<IEnumerable<GameMechanic>>()))
            .Callback<IEnumerable<GameMechanic>>(m => captured = m.ToList())
            .Returns(Task.CompletedTask);

        await _factory.CreateFromBggAsync(item, false, GameState.Owned, null, null);

        captured.Should().HaveCount(2);
        captured.Should().Contain(m => m.Name == "Worker Placement");
        captured.Should().Contain(m => m.Name == "Deck Building");
    }

    [Fact]
    public async Task CreateFromBggAsync_ShouldAddPeopleFromLinks()
    {
        var item = CreateBasicItem();
        item.Links =
        [
            new ThingResponse.Link { Type = Constants.Bgg.Designer, Id = 1, Value = "Designer One" },
            new ThingResponse.Link { Type = Constants.Bgg.Artist, Id = 2, Value = "Artist One" }
        ];

        List<Person>? captured = null;
        _gameRepositoryMock
            .Setup(x => x.AddPeopleIfNotExists(It.IsAny<IEnumerable<Person>>()))
            .Callback<IEnumerable<Person>>(p => captured = p.ToList())
            .Returns(Task.CompletedTask);

        await _factory.CreateFromBggAsync(item, false, GameState.Owned, null, null);

        captured.Should().HaveCount(2);
        captured.Should().Contain(p => p.Name == "Designer One" && p.Type == PersonType.Designer);
        captured.Should().Contain(p => p.Name == "Artist One" && p.Type == PersonType.Artist);
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

        List<GameCategory>? captured = null;
        _gameRepositoryMock
            .Setup(x => x.AddGameCategoriesIfNotExists(It.IsAny<IEnumerable<GameCategory>>()))
            .Callback<IEnumerable<GameCategory>>(c => captured = c.ToList())
            .Returns(Task.CompletedTask);

        await _factory.CreateFromBggAsync(item, false, GameState.Owned, null, null);

        captured.Should().HaveCount(1);
        captured.Should().Contain(c => c.Name == "Strategy");
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
