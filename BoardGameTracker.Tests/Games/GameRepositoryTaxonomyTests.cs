using System;
using System.Linq;
using System.Threading.Tasks;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.Games;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BoardGameTracker.Tests.Games;

public class GameRepositoryTaxonomyTests
{
    private static MainDbContext CreateContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<MainDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
        return new MainDbContext(options);
    }

    [Fact]
    public async Task GetOrCreateCategoriesAsync_ShouldReuseExistingRowsAndCreateMissingOnes()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using (var seed = CreateContext(databaseName))
        {
            seed.GameCategories.Add(new GameCategory("Strategy"));
            await seed.SaveChangesAsync();
        }

        await using var context = CreateContext(databaseName);
        var repository = new GameRepository(context);

        var result = await repository.GetOrCreateCategoriesAsync(["Strategy", "Economic", " Economic ", ""]);
        await context.SaveChangesAsync();

        result.Select(x => x.Name).Should().Equal("Strategy", "Economic");
        (await context.GameCategories.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task GetOrCreateCategoriesAsync_ShouldReusePendingRowsAcrossCallsBeforeSave()
    {
        await using var context = CreateContext(Guid.NewGuid().ToString());
        var repository = new GameRepository(context);

        var first = await repository.GetOrCreateCategoriesAsync(["Strategy"]);
        var second = await repository.GetOrCreateCategoriesAsync(["Strategy", "Party"]);
        await context.SaveChangesAsync();

        second[0].Should().BeSameAs(first[0]);
        (await context.GameCategories.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task GetOrCreatePeopleAsync_ShouldDistinguishSameNameByType()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using (var seed = CreateContext(databaseName))
        {
            seed.People.Add(new Person("Uwe Rosenberg", PersonType.Designer));
            await seed.SaveChangesAsync();
        }

        await using var context = CreateContext(databaseName);
        var repository = new GameRepository(context);

        var result = await repository.GetOrCreatePeopleAsync(
        [
            new PersonKey("Uwe Rosenberg", PersonType.Designer),
            new PersonKey("Uwe Rosenberg", PersonType.Artist)
        ]);
        await context.SaveChangesAsync();

        result.Should().HaveCount(2);
        result[0].Id.Should().BePositive();
        (await context.People.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task ImportedGame_ShouldBeLinkedToItsTaxonomy()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using (var context = CreateContext(databaseName))
        {
            var repository = new GameRepository(context);
            var game = new Game("Agricola");
            foreach (var category in await repository.GetOrCreateCategoriesAsync(["Farming", "Economic"]))
            {
                game.AddCategory(category);
            }

            foreach (var mechanic in await repository.GetOrCreateMechanicsAsync(["Worker Placement"]))
            {
                game.AddMechanic(mechanic);
            }

            await repository.CreateAsync(game);
            await context.SaveChangesAsync();
        }

        await using var verify = CreateContext(databaseName);
        var persisted = await verify.Games
            .Include(x => x.Categories)
            .Include(x => x.Mechanics)
            .SingleAsync();
        persisted.Categories.Select(x => x.Name).Should().BeEquivalentTo("Farming", "Economic");
        persisted.Mechanics.Select(x => x.Name).Should().Equal("Worker Placement");
    }
}
