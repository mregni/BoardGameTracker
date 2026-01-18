using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BoardGameTracker.Common;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Models.Bgg;
using BoardGameTracker.Core.Bgg;
using BoardGameTracker.Core.Images.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Bgg;

public class BggGameTranslatorTests
{
    private readonly Mock<IImageService> _imageServiceMock;
    private readonly BggGameTranslator _translator;

    public BggGameTranslatorTests()
    {
        _imageServiceMock = new Mock<IImageService>();
        _translator = new BggGameTranslator(_imageServiceMock.Object);
    }

    #region TranslateRawGame Tests

    [Fact]
    public void TranslateRawGame_ShouldTranslateBasicProperties()
    {
        // Arrange
        var rawGame = CreateBasicRawGame();

        // Act
        var result = _translator.TranslateRawGame(rawGame);

        // Assert
        result.BggId.Should().Be(12345);
        result.YearPublished.Should().Be(2020);
        result.MinPlayers.Should().Be(2);
        result.MaxPlayers.Should().Be(4);
        result.MinPlayTime.Should().Be(30);
        result.MaxPlayTime.Should().Be(60);
        result.MinAge.Should().Be(10);
        result.Thumbnail.Should().Be("https://example.com/thumb.jpg");
        result.Image.Should().Be("https://example.com/image.jpg");
        result.Description.Should().Be("A great board game");
    }

    [Fact]
    public void TranslateRawGame_ShouldTranslateNames()
    {
        // Arrange
        var rawGame = CreateBasicRawGame();
        rawGame.Names = new List<Name>
        {
            new() { Type = "primary", Value = "Primary Name", Sortindex = 1 },
            new() { Type = "alternate", Value = "Alternate Name", Sortindex = 2 }
        };

        // Act
        var result = _translator.TranslateRawGame(rawGame);

        // Assert
        result.Names.Should().HaveCount(2);
        result.Names.Should().Contain("Primary Name");
        result.Names.Should().Contain("Alternate Name");
    }

    [Fact]
    public void TranslateRawGame_ShouldTranslateRatings()
    {
        // Arrange
        var rawGame = CreateBasicRawGame();
        rawGame.Statistics.Ratings.Average.Value = 8.5;
        rawGame.Statistics.Ratings.AverageWeight.Value = 3.2;

        // Act
        var result = _translator.TranslateRawGame(rawGame);

        // Assert
        result.Rating.Should().Be(8.5);
        result.Weight.Should().Be(3.2);
    }

    [Fact]
    public void TranslateRawGame_ShouldTranslateDesigners()
    {
        // Arrange
        var rawGame = CreateBasicRawGame();
        rawGame.Links = new List<BggRawLink>
        {
            new() { Type = Constants.Bgg.Designer, Id = 1, Value = "Designer One" },
            new() { Type = Constants.Bgg.Designer, Id = 2, Value = "Designer Two" }
        };

        // Act
        var result = _translator.TranslateRawGame(rawGame);

        // Assert
        result.People.Should().HaveCount(2);
        result.People.Should().AllSatisfy(p => p.Type.Should().Be(PersonType.Designer));
        result.People.Should().Contain(p => p.Value == "Designer One" && p.Id == 1);
        result.People.Should().Contain(p => p.Value == "Designer Two" && p.Id == 2);
    }

    [Fact]
    public void TranslateRawGame_ShouldTranslateArtists()
    {
        // Arrange
        var rawGame = CreateBasicRawGame();
        rawGame.Links = new List<BggRawLink>
        {
            new() { Type = Constants.Bgg.Artist, Id = 1, Value = "Artist One" }
        };

        // Act
        var result = _translator.TranslateRawGame(rawGame);

        // Assert
        result.People.Should().HaveCount(1);
        result.People[0].Type.Should().Be(PersonType.Artist);
        result.People[0].Value.Should().Be("Artist One");
    }

    [Fact]
    public void TranslateRawGame_ShouldTranslatePublishers()
    {
        // Arrange
        var rawGame = CreateBasicRawGame();
        rawGame.Links = new List<BggRawLink>
        {
            new() { Type = Constants.Bgg.Publisher, Id = 1, Value = "Publisher One" }
        };

        // Act
        var result = _translator.TranslateRawGame(rawGame);

        // Assert
        result.People.Should().HaveCount(1);
        result.People[0].Type.Should().Be(PersonType.Publisher);
        result.People[0].Value.Should().Be("Publisher One");
    }

    [Fact]
    public void TranslateRawGame_ShouldTranslateCategories()
    {
        // Arrange
        var rawGame = CreateBasicRawGame();
        rawGame.Links = new List<BggRawLink>
        {
            new() { Type = Constants.Bgg.Category, Id = 1, Value = "Strategy" },
            new() { Type = Constants.Bgg.Category, Id = 2, Value = "Economic" }
        };

        // Act
        var result = _translator.TranslateRawGame(rawGame);

        // Assert
        result.Categories.Should().HaveCount(2);
        result.Categories.Should().Contain(c => c.Value == "Strategy" && c.Id == 1);
        result.Categories.Should().Contain(c => c.Value == "Economic" && c.Id == 2);
    }

    [Fact]
    public void TranslateRawGame_ShouldTranslateMechanics()
    {
        // Arrange
        var rawGame = CreateBasicRawGame();
        rawGame.Links = new List<BggRawLink>
        {
            new() { Type = Constants.Bgg.Mechanic, Id = 1, Value = "Worker Placement" },
            new() { Type = Constants.Bgg.Mechanic, Id = 2, Value = "Deck Building" }
        };

        // Act
        var result = _translator.TranslateRawGame(rawGame);

        // Assert
        result.Mechanics.Should().HaveCount(2);
        result.Mechanics.Should().Contain(m => m.Value == "Worker Placement" && m.Id == 1);
        result.Mechanics.Should().Contain(m => m.Value == "Deck Building" && m.Id == 2);
    }

    [Fact]
    public void TranslateRawGame_ShouldTranslateExpansions()
    {
        // Arrange
        var rawGame = CreateBasicRawGame();
        rawGame.Links = new List<BggRawLink>
        {
            new() { Type = Constants.Bgg.Expansion, Id = 1, Value = "Expansion One" },
            new() { Type = Constants.Bgg.Expansion, Id = 2, Value = "Expansion Two" }
        };

        // Act
        var result = _translator.TranslateRawGame(rawGame);

        // Assert
        result.Expansions.Should().HaveCount(2);
        result.Expansions.Should().Contain(e => e.Value == "Expansion One" && e.Id == 1);
        result.Expansions.Should().Contain(e => e.Value == "Expansion Two" && e.Id == 2);
    }

    [Fact]
    public void TranslateRawGame_ShouldFilterOutNonPersonLinks()
    {
        // Arrange
        var rawGame = CreateBasicRawGame();
        rawGame.Links = new List<BggRawLink>
        {
            new() { Type = Constants.Bgg.Designer, Id = 1, Value = "Designer" },
            new() { Type = Constants.Bgg.Category, Id = 2, Value = "Category" },
            new() { Type = Constants.Bgg.Mechanic, Id = 3, Value = "Mechanic" },
            new() { Type = "unknowntype", Id = 4, Value = "Unknown" }
        };

        // Act
        var result = _translator.TranslateRawGame(rawGame);

        // Assert
        result.People.Should().HaveCount(1);
        result.People[0].Value.Should().Be("Designer");
    }

    [Fact]
    public void TranslateRawGame_ShouldHandleMixedLinkTypes()
    {
        // Arrange
        var rawGame = CreateBasicRawGame();
        rawGame.Links = new List<BggRawLink>
        {
            new() { Type = Constants.Bgg.Designer, Id = 1, Value = "Designer One" },
            new() { Type = Constants.Bgg.Artist, Id = 2, Value = "Artist One" },
            new() { Type = Constants.Bgg.Publisher, Id = 3, Value = "Publisher One" },
            new() { Type = Constants.Bgg.Category, Id = 4, Value = "Category One" },
            new() { Type = Constants.Bgg.Mechanic, Id = 5, Value = "Mechanic One" },
            new() { Type = Constants.Bgg.Expansion, Id = 6, Value = "Expansion One" }
        };

        // Act
        var result = _translator.TranslateRawGame(rawGame);

        // Assert
        result.People.Should().HaveCount(3);
        result.Categories.Should().HaveCount(1);
        result.Mechanics.Should().HaveCount(1);
        result.Expansions.Should().HaveCount(1);
    }

    #endregion

    #region TranslateFromBggAsync Tests

    [Fact]
    public async Task TranslateFromBggAsync_ShouldThrow_WhenGameHasNoName()
    {
        // Arrange
        var bggGame = CreateBasicBggGame();
        bggGame.Names = Array.Empty<string>();

        // Act
        var action = async () => await _translator.TranslateFromBggAsync(bggGame);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Game must have a valid name from BGG");
    }

    [Fact]
    public async Task TranslateFromBggAsync_ShouldThrow_WhenGameHasOnlyWhitespaceName()
    {
        // Arrange
        var bggGame = CreateBasicBggGame();
        bggGame.Names = new[] { "   ", "" };

        // Act
        var action = async () => await _translator.TranslateFromBggAsync(bggGame);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Game must have a valid name from BGG");
    }

    [Fact]
    public async Task TranslateFromBggAsync_ShouldThrow_WhenMinPlayersGreaterThanMaxPlayers()
    {
        // Arrange
        var bggGame = CreateBasicBggGame();
        bggGame.MinPlayers = 5;
        bggGame.MaxPlayers = 2;

        // Act
        var action = async () => await _translator.TranslateFromBggAsync(bggGame);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Invalid player count range from BGG: 5-2");
    }

    [Fact]
    public async Task TranslateFromBggAsync_ShouldThrow_WhenMinPlayTimeGreaterThanMaxPlayTime()
    {
        // Arrange
        var bggGame = CreateBasicBggGame();
        bggGame.MinPlayTime = 120;
        bggGame.MaxPlayTime = 30;

        // Act
        var action = async () => await _translator.TranslateFromBggAsync(bggGame);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Invalid play time range from BGG: 120-30");
    }

    [Fact]
    public async Task TranslateFromBggAsync_ShouldTranslateBasicProperties()
    {
        // Arrange
        var bggGame = CreateBasicBggGame();
        _imageServiceMock
            .Setup(x => x.DownloadImage(bggGame.Image, bggGame.BggId.ToString()))
            .ReturnsAsync("downloaded-image.jpg");

        // Act
        var result = await _translator.TranslateFromBggAsync(bggGame);

        // Assert
        result.Title.Should().Be("Test Game");
        result.BggId.Should().Be(12345);
        result.Description.Should().Be("A great board game");
        result.YearPublished.Should().Be(2020);
        result.MinPlayers.Should().Be(2);
        result.MaxPlayers.Should().Be(4);
        result.MinPlayTime.Should().Be(30);
        result.MaxPlayTime.Should().Be(60);
        result.MinAge.Should().Be(10);
        result.Rating.Should().Be(8.5);
        result.Weight.Should().Be(3.2);
        result.ImageUrl.Should().Be("downloaded-image.jpg");
    }

    [Fact]
    public async Task TranslateFromBggAsync_ShouldUseFirstNameAsTitle()
    {
        // Arrange
        var bggGame = CreateBasicBggGame();
        bggGame.Names = new[] { "Primary Name", "Alternate Name" };
        _imageServiceMock
            .Setup(x => x.DownloadImage(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("image.jpg");

        // Act
        var result = await _translator.TranslateFromBggAsync(bggGame);

        // Assert
        result.Title.Should().Be("Primary Name");
    }

    [Fact]
    public async Task TranslateFromBggAsync_ShouldHandleNullDescription()
    {
        // Arrange
        var bggGame = CreateBasicBggGame();
        bggGame.Description = null!;
        _imageServiceMock
            .Setup(x => x.DownloadImage(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("image.jpg");

        // Act
        var result = await _translator.TranslateFromBggAsync(bggGame);

        // Assert
        result.Description.Should().BeEmpty();
    }

    [Fact]
    public async Task TranslateFromBggAsync_ShouldTranslateCategories()
    {
        // Arrange
        var bggGame = CreateBasicBggGame();
        bggGame.Categories = new[]
        {
            new BggLink { Value = "Strategy", Id = 1 },
            new BggLink { Value = "Economic", Id = 2 }
        };
        _imageServiceMock
            .Setup(x => x.DownloadImage(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("image.jpg");

        // Act
        var result = await _translator.TranslateFromBggAsync(bggGame);

        // Assert
        result.Categories.Should().HaveCount(2);
        result.Categories.Should().Contain(c => c.Name == "Strategy" && c.BggId == 1);
        result.Categories.Should().Contain(c => c.Name == "Economic" && c.BggId == 2);
    }

    [Fact]
    public async Task TranslateFromBggAsync_ShouldFilterOutEmptyCategories()
    {
        // Arrange
        var bggGame = CreateBasicBggGame();
        bggGame.Categories = new[]
        {
            new BggLink { Value = "Strategy", Id = 1 },
            new BggLink { Value = "", Id = 2 },
            new BggLink { Value = "   ", Id = 3 }
        };
        _imageServiceMock
            .Setup(x => x.DownloadImage(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("image.jpg");

        // Act
        var result = await _translator.TranslateFromBggAsync(bggGame);

        // Assert
        result.Categories.Should().HaveCount(1);
        result.Categories.Should().Contain(c => c.Name == "Strategy");
    }

    [Fact]
    public async Task TranslateFromBggAsync_ShouldTranslateMechanics()
    {
        // Arrange
        var bggGame = CreateBasicBggGame();
        bggGame.Mechanics = new[]
        {
            new BggLink { Value = "Worker Placement", Id = 1 },
            new BggLink { Value = "Deck Building", Id = 2 }
        };
        _imageServiceMock
            .Setup(x => x.DownloadImage(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("image.jpg");

        // Act
        var result = await _translator.TranslateFromBggAsync(bggGame);

        // Assert
        result.Mechanics.Should().HaveCount(2);
        result.Mechanics.Should().Contain(m => m.Name == "Worker Placement" && m.BggId == 1);
        result.Mechanics.Should().Contain(m => m.Name == "Deck Building" && m.BggId == 2);
    }

    [Fact]
    public async Task TranslateFromBggAsync_ShouldFilterOutEmptyMechanics()
    {
        // Arrange
        var bggGame = CreateBasicBggGame();
        bggGame.Mechanics = new[]
        {
            new BggLink { Value = "Worker Placement", Id = 1 },
            new BggLink { Value = "", Id = 2 }
        };
        _imageServiceMock
            .Setup(x => x.DownloadImage(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("image.jpg");

        // Act
        var result = await _translator.TranslateFromBggAsync(bggGame);

        // Assert
        result.Mechanics.Should().HaveCount(1);
    }

    [Fact]
    public async Task TranslateFromBggAsync_ShouldTranslatePeople()
    {
        // Arrange
        var bggGame = CreateBasicBggGame();
        bggGame.People = new[]
        {
            new BggPerson { Value = "Designer One", Id = 1, Type = PersonType.Designer },
            new BggPerson { Value = "Artist One", Id = 2, Type = PersonType.Artist }
        };
        _imageServiceMock
            .Setup(x => x.DownloadImage(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("image.jpg");

        // Act
        var result = await _translator.TranslateFromBggAsync(bggGame);

        // Assert
        result.People.Should().HaveCount(2);
        result.People.Should().Contain(p => p.Name == "Designer One" && p.Type == "Designer");
        result.People.Should().Contain(p => p.Name == "Artist One" && p.Type == "Artist");
    }

    [Fact]
    public async Task TranslateFromBggAsync_ShouldFilterOutEmptyPeople()
    {
        // Arrange
        var bggGame = CreateBasicBggGame();
        bggGame.People = new[]
        {
            new BggPerson { Value = "Designer One", Id = 1, Type = PersonType.Designer },
            new BggPerson { Value = "", Id = 2, Type = PersonType.Artist }
        };
        _imageServiceMock
            .Setup(x => x.DownloadImage(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("image.jpg");

        // Act
        var result = await _translator.TranslateFromBggAsync(bggGame);

        // Assert
        result.People.Should().HaveCount(1);
    }

    [Fact]
    public async Task TranslateFromBggAsync_ShouldTranslateExpansions()
    {
        // Arrange
        var bggGame = CreateBasicBggGame();
        bggGame.Expansions = new[]
        {
            new BggLink { Value = "Expansion One", Id = 1 },
            new BggLink { Value = "Expansion Two", Id = 2 }
        };
        _imageServiceMock
            .Setup(x => x.DownloadImage(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("image.jpg");

        // Act
        var result = await _translator.TranslateFromBggAsync(bggGame);

        // Assert
        result.Expansions.Should().HaveCount(2);
        result.Expansions.Should().Contain(e => e.Title == "Expansion One" && e.BggId == 1);
        result.Expansions.Should().Contain(e => e.Title == "Expansion Two" && e.BggId == 2);
    }

    [Fact]
    public async Task TranslateFromBggAsync_ShouldFilterOutEmptyExpansions()
    {
        // Arrange
        var bggGame = CreateBasicBggGame();
        bggGame.Expansions = new[]
        {
            new BggLink { Value = "Expansion One", Id = 1 },
            new BggLink { Value = "", Id = 2 }
        };
        _imageServiceMock
            .Setup(x => x.DownloadImage(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("image.jpg");

        // Act
        var result = await _translator.TranslateFromBggAsync(bggGame);

        // Assert
        result.Expansions.Should().HaveCount(1);
    }

    [Fact]
    public async Task TranslateFromBggAsync_ShouldCallImageServiceWithCorrectParameters()
    {
        // Arrange
        var bggGame = CreateBasicBggGame();
        bggGame.Image = "https://example.com/game-image.jpg";
        bggGame.BggId = 99999;
        _imageServiceMock
            .Setup(x => x.DownloadImage("https://example.com/game-image.jpg", "99999"))
            .ReturnsAsync("downloaded.jpg");

        // Act
        await _translator.TranslateFromBggAsync(bggGame);

        // Assert
        _imageServiceMock.Verify(x => x.DownloadImage("https://example.com/game-image.jpg", "99999"), Times.Once);
    }

    #endregion

    #region Helper Methods

    private static BggRawGame CreateBasicRawGame()
    {
        return new BggRawGame
        {
            Id = 12345,
            Type = "boardgame",
            Thumbnail = "https://example.com/thumb.jpg",
            Image = "https://example.com/image.jpg",
            Description = "A great board game",
            Names = new List<Name>
            {
                new() { Type = "primary", Value = "Test Game", Sortindex = 1 }
            },
            YearPublished = new YearPublished { Value = 2020 },
            MinPlayers = new MinPlayers { Value = 2 },
            MaxPlayers = new MaxPlayers { Value = 4 },
            MinPlayTime = new MinPlayTime { Value = 30 },
            MaxPlayTime = new MaxPlayTime { Value = 60 },
            MinAge = new MinAge { Value = 10 },
            Statistics = new Statistics
            {
                Ratings = new Ratings
                {
                    Average = new Average { Value = 8.5 },
                    AverageWeight = new AverageWeight { Value = 3.2 }
                }
            },
            Links = new List<BggRawLink>()
        };
    }

    private static BggGame CreateBasicBggGame()
    {
        return new BggGame
        {
            BggId = 12345,
            Names = new[] { "Test Game" },
            Thumbnail = "https://example.com/thumb.jpg",
            Image = "https://example.com/image.jpg",
            Description = "A great board game",
            YearPublished = 2020,
            MinPlayers = 2,
            MaxPlayers = 4,
            MinPlayTime = 30,
            MaxPlayTime = 60,
            MinAge = 10,
            Rating = 8.5,
            Weight = 3.2,
            Categories = Array.Empty<BggLink>(),
            Mechanics = Array.Empty<BggLink>(),
            Expansions = Array.Empty<BggLink>(),
            People = Array.Empty<BggPerson>()
        };
    }

    #endregion
}
