using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Core.Bgg;
using BoardGameTracker.Core.Games.Factories;
using BoardGameTracker.Core.Games.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Factories;

public class GameFactoryTests
{
    private readonly Mock<IGameRepository> _gameRepositoryMock;
    private readonly GameFactory _factory;

    public GameFactoryTests()
    {
        _gameRepositoryMock = new Mock<IGameRepository>();
        _factory = new GameFactory(_gameRepositoryMock.Object);

        // Default setup - repository accepts all data
        _gameRepositoryMock
            .Setup(x => x.AddGameCategoriesIfNotExists(It.IsAny<IEnumerable<GameCategory>>()))
            .Returns(Task.CompletedTask);
        _gameRepositoryMock
            .Setup(x => x.AddGameMechanicsIfNotExists(It.IsAny<IEnumerable<GameMechanic>>()))
            .Returns(Task.CompletedTask);
        _gameRepositoryMock
            .Setup(x => x.AddPeopleIfNotExists(It.IsAny<IEnumerable<Person>>()))
            .Returns(Task.CompletedTask);
    }

    #region CreateFromImportDataAsync Tests

    [Fact]
    public async Task CreateFromImportDataAsync_ShouldCreateGameWithBasicProperties()
    {
        // Arrange
        var data = CreateBasicImportData();

        // Act
        var result = await _factory.CreateFromImportDataAsync(data, true, GameState.Owned, null, null);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Test Game");
        result.HasScoring.Should().BeTrue();
        result.State.Should().Be(GameState.Owned);
    }

    [Fact]
    public async Task CreateFromImportDataAsync_ShouldSetImage()
    {
        // Arrange
        var data = CreateBasicImportData();
        data.ImageUrl = "game-image.jpg";

        // Act
        var result = await _factory.CreateFromImportDataAsync(data, false, GameState.Owned, null, null);

        // Assert
        result.Image.Should().Be("game-image.jpg");
    }

    [Fact]
    public async Task CreateFromImportDataAsync_ShouldSetDescription()
    {
        // Arrange
        var data = CreateBasicImportData();
        data.Description = "A wonderful strategy game";

        // Act
        var result = await _factory.CreateFromImportDataAsync(data, false, GameState.Owned, null, null);

        // Assert
        result.Description.Should().Be("A wonderful strategy game");
    }

    [Fact]
    public async Task CreateFromImportDataAsync_ShouldSetYearPublished()
    {
        // Arrange
        var data = CreateBasicImportData();
        data.YearPublished = 2023;

        // Act
        var result = await _factory.CreateFromImportDataAsync(data, false, GameState.Owned, null, null);

        // Assert
        result.YearPublished.Should().Be(2023);
    }

    [Fact]
    public async Task CreateFromImportDataAsync_ShouldSetPlayerCount()
    {
        // Arrange
        var data = CreateBasicImportData();
        data.MinPlayers = 2;
        data.MaxPlayers = 6;

        // Act
        var result = await _factory.CreateFromImportDataAsync(data, false, GameState.Owned, null, null);

        // Assert
        result.PlayerCount.Should().NotBeNull();
        result.PlayerCount!.Min.Should().Be(2);
        result.PlayerCount!.Max.Should().Be(6);
    }

    [Fact]
    public async Task CreateFromImportDataAsync_ShouldSetPlayTime()
    {
        // Arrange
        var data = CreateBasicImportData();
        data.MinPlayTime = 45;
        data.MaxPlayTime = 90;

        // Act
        var result = await _factory.CreateFromImportDataAsync(data, false, GameState.Owned, null, null);

        // Assert
        result.PlayTime.Should().NotBeNull();
        result.PlayTime!.MinMinutes.Should().Be(45);
        result.PlayTime!.MaxMinutes.Should().Be(90);
    }

    [Fact]
    public async Task CreateFromImportDataAsync_ShouldSetMinAge()
    {
        // Arrange
        var data = CreateBasicImportData();
        data.MinAge = 14;

        // Act
        var result = await _factory.CreateFromImportDataAsync(data, false, GameState.Owned, null, null);

        // Assert
        result.MinAge.Should().Be(14);
    }

    [Fact]
    public async Task CreateFromImportDataAsync_ShouldSetRating()
    {
        // Arrange
        var data = CreateBasicImportData();
        data.Rating = 8.7;

        // Act
        var result = await _factory.CreateFromImportDataAsync(data, false, GameState.Owned, null, null);

        // Assert
        result.Rating.Should().NotBeNull();
        result.Rating!.Value.Should().Be(8.7);
    }

    [Fact]
    public async Task CreateFromImportDataAsync_ShouldSetWeight()
    {
        // Arrange
        var data = CreateBasicImportData();
        data.Weight = 3.5;

        // Act
        var result = await _factory.CreateFromImportDataAsync(data, false, GameState.Owned, null, null);

        // Assert
        result.Weight.Should().NotBeNull();
        result.Weight!.Value.Should().Be(3.5);
    }

    [Fact]
    public async Task CreateFromImportDataAsync_ShouldSetBggId()
    {
        // Arrange
        var data = CreateBasicImportData();
        data.BggId = 12345;

        // Act
        var result = await _factory.CreateFromImportDataAsync(data, false, GameState.Owned, null, null);

        // Assert
        result.BggId.Should().Be(12345);
    }

    [Fact]
    public async Task CreateFromImportDataAsync_ShouldSetBuyingPrice()
    {
        // Arrange
        var data = CreateBasicImportData();

        // Act
        var result = await _factory.CreateFromImportDataAsync(data, false, GameState.Owned, 49.99m, null);

        // Assert
        result.BuyingPrice.Should().NotBeNull();
        result.BuyingPrice!.Amount.Should().Be(49.99m);
    }

    [Fact]
    public async Task CreateFromImportDataAsync_ShouldNotSetBuyingPrice_WhenNull()
    {
        // Arrange
        var data = CreateBasicImportData();

        // Act
        var result = await _factory.CreateFromImportDataAsync(data, false, GameState.Owned, null, null);

        // Assert
        result.BuyingPrice.Should().BeNull();
    }

    [Fact]
    public async Task CreateFromImportDataAsync_ShouldSetAdditionDate_WhenProvided()
    {
        // Arrange
        var data = CreateBasicImportData();
        var additionDate = new DateTime(2023, 6, 15);

        // Act
        var result = await _factory.CreateFromImportDataAsync(data, false, GameState.Owned, null, additionDate);

        // Assert
        result.AdditionDate.Should().Be(additionDate);
    }

    [Fact]
    public async Task CreateFromImportDataAsync_ShouldNotOverrideAdditionDate_WhenNull()
    {
        // Arrange
        var data = CreateBasicImportData();

        // Act
        var result = await _factory.CreateFromImportDataAsync(data, false, GameState.Owned, null, null);

        // Assert
        // When additionDate is null, the game should have its default AdditionDate (set in constructor)
        result.AdditionDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CreateFromImportDataAsync_ShouldSetGameState()
    {
        // Arrange
        var data = CreateBasicImportData();

        // Act
        var result = await _factory.CreateFromImportDataAsync(data, false, GameState.Wanted, null, null);

        // Assert
        result.State.Should().Be(GameState.Wanted);
    }

    [Theory]
    [InlineData(GameState.Owned)]
    [InlineData(GameState.Wanted)]
    [InlineData(GameState.ForTrade)]
    [InlineData(GameState.PreviouslyOwned)]
    public async Task CreateFromImportDataAsync_ShouldSupportAllGameStates(GameState state)
    {
        // Arrange
        var data = CreateBasicImportData();

        // Act
        var result = await _factory.CreateFromImportDataAsync(data, false, state, null, null);

        // Assert
        result.State.Should().Be(state);
    }

    [Fact]
    public async Task CreateFromImportDataAsync_ShouldAddCategoriesToRepository()
    {
        // Arrange
        var data = CreateBasicImportData();
        data.Categories = new List<CategoryData>
        {
            new() { Name = "Strategy", BggId = 1 },
            new() { Name = "Economic", BggId = 2 }
        };

        List<GameCategory>? capturedCategories = null;
        _gameRepositoryMock
            .Setup(x => x.AddGameCategoriesIfNotExists(It.IsAny<IEnumerable<GameCategory>>()))
            .Callback<IEnumerable<GameCategory>>(c => capturedCategories = c.ToList())
            .Returns(Task.CompletedTask);

        // Act
        await _factory.CreateFromImportDataAsync(data, false, GameState.Owned, null, null);

        // Assert
        capturedCategories.Should().NotBeNull();
        capturedCategories.Should().HaveCount(2);
        capturedCategories.Should().Contain(c => c.Name == "Strategy");
        capturedCategories.Should().Contain(c => c.Name == "Economic");
    }

    [Fact]
    public async Task CreateFromImportDataAsync_ShouldAddMechanicsToRepository()
    {
        // Arrange
        var data = CreateBasicImportData();
        data.Mechanics = new List<MechanicData>
        {
            new() { Name = "Worker Placement", BggId = 1 },
            new() { Name = "Deck Building", BggId = 2 }
        };

        List<GameMechanic>? capturedMechanics = null;
        _gameRepositoryMock
            .Setup(x => x.AddGameMechanicsIfNotExists(It.IsAny<IEnumerable<GameMechanic>>()))
            .Callback<IEnumerable<GameMechanic>>(m => capturedMechanics = m.ToList())
            .Returns(Task.CompletedTask);

        // Act
        await _factory.CreateFromImportDataAsync(data, false, GameState.Owned, null, null);

        // Assert
        capturedMechanics.Should().NotBeNull();
        capturedMechanics.Should().HaveCount(2);
        capturedMechanics.Should().Contain(m => m.Name == "Worker Placement");
        capturedMechanics.Should().Contain(m => m.Name == "Deck Building");
    }

    [Fact]
    public async Task CreateFromImportDataAsync_ShouldAddPeopleToRepository()
    {
        // Arrange
        var data = CreateBasicImportData();
        data.People = new List<PersonData>
        {
            new() { Name = "Designer One", Type = "Designer" },
            new() { Name = "Artist One", Type = "Artist" }
        };

        List<Person>? capturedPeople = null;
        _gameRepositoryMock
            .Setup(x => x.AddPeopleIfNotExists(It.IsAny<IEnumerable<Person>>()))
            .Callback<IEnumerable<Person>>(p => capturedPeople = p.ToList())
            .Returns(Task.CompletedTask);

        // Act
        await _factory.CreateFromImportDataAsync(data, false, GameState.Owned, null, null);

        // Assert
        capturedPeople.Should().NotBeNull();
        capturedPeople.Should().HaveCount(2);
        capturedPeople.Should().Contain(p => p.Name == "Designer One" && p.Type == PersonType.Designer);
        capturedPeople.Should().Contain(p => p.Name == "Artist One" && p.Type == PersonType.Artist);
    }

    [Fact]
    public async Task CreateFromImportDataAsync_ShouldHandleEmptyCategories()
    {
        // Arrange
        var data = CreateBasicImportData();
        data.Categories = Enumerable.Empty<CategoryData>();

        // Act
        var result = await _factory.CreateFromImportDataAsync(data, false, GameState.Owned, null, null);

        // Assert
        result.Should().NotBeNull();
        _gameRepositoryMock.Verify(x => x.AddGameCategoriesIfNotExists(It.Is<IEnumerable<GameCategory>>(c => !c.Any())), Times.Once);
    }

    [Fact]
    public async Task CreateFromImportDataAsync_ShouldHandleEmptyMechanics()
    {
        // Arrange
        var data = CreateBasicImportData();
        data.Mechanics = Enumerable.Empty<MechanicData>();

        // Act
        var result = await _factory.CreateFromImportDataAsync(data, false, GameState.Owned, null, null);

        // Assert
        result.Should().NotBeNull();
        _gameRepositoryMock.Verify(x => x.AddGameMechanicsIfNotExists(It.Is<IEnumerable<GameMechanic>>(m => !m.Any())), Times.Once);
    }

    [Fact]
    public async Task CreateFromImportDataAsync_ShouldHandleEmptyPeople()
    {
        // Arrange
        var data = CreateBasicImportData();
        data.People = Enumerable.Empty<PersonData>();

        // Act
        var result = await _factory.CreateFromImportDataAsync(data, false, GameState.Owned, null, null);

        // Assert
        result.Should().NotBeNull();
        _gameRepositoryMock.Verify(x => x.AddPeopleIfNotExists(It.Is<IEnumerable<Person>>(p => !p.Any())), Times.Once);
    }

    [Fact]
    public async Task CreateFromImportDataAsync_ShouldCallRepositoryMethodsInOrder()
    {
        // Arrange
        var data = CreateBasicImportData();
        data.Categories = new List<CategoryData> { new() { Name = "Cat", BggId = 1 } };
        data.Mechanics = new List<MechanicData> { new() { Name = "Mech", BggId = 1 } };
        data.People = new List<PersonData> { new() { Name = "Person", Type = "Designer" } };

        var callOrder = new List<string>();
        _gameRepositoryMock
            .Setup(x => x.AddGameCategoriesIfNotExists(It.IsAny<IEnumerable<GameCategory>>()))
            .Callback(() => callOrder.Add("Categories"))
            .Returns(Task.CompletedTask);
        _gameRepositoryMock
            .Setup(x => x.AddGameMechanicsIfNotExists(It.IsAny<IEnumerable<GameMechanic>>()))
            .Callback(() => callOrder.Add("Mechanics"))
            .Returns(Task.CompletedTask);
        _gameRepositoryMock
            .Setup(x => x.AddPeopleIfNotExists(It.IsAny<IEnumerable<Person>>()))
            .Callback(() => callOrder.Add("People"))
            .Returns(Task.CompletedTask);

        // Act
        await _factory.CreateFromImportDataAsync(data, false, GameState.Owned, null, null);

        // Assert
        callOrder.Should().ContainInOrder("Categories", "Mechanics", "People");
    }

    #endregion

    #region Helper Methods

    private static GameImportData CreateBasicImportData()
    {
        return new GameImportData
        {
            Title = "Test Game",
            BggId = 12345,
            Description = "A test game description",
            YearPublished = 2020,
            MinPlayers = 2,
            MaxPlayers = 4,
            MinPlayTime = 30,
            MaxPlayTime = 60,
            MinAge = 10,
            Rating = 7.5,
            Weight = 2.5,
            ImageUrl = "test-image.jpg",
            Categories = Enumerable.Empty<CategoryData>(),
            Mechanics = Enumerable.Empty<MechanicData>(),
            People = Enumerable.Empty<PersonData>(),
            Expansions = Enumerable.Empty<ExpansionData>()
        };
    }

    #endregion
}
