using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Entities.Auth;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.Maintenance.Interfaces;
using BoardGameTracker.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pgvector;
using Xunit;

namespace BoardGameTracker.IntegrationTests.Datastore;

[Collection(IntegrationCollection.Name)]
public class MaintenanceRepositoryTests : IAsyncLifetime
{
    private readonly IntegrationFixture _fixture;

    public MaintenanceRepositoryTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync() => await _fixture.ResetDataAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task GetChangeDetectionWatchIdsAsync_ShouldReturnOnlyTheWatchedGames()
    {
        var watchId = Guid.NewGuid().ToString();
        await using (var scope = _fixture.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            var watched = new Game("Watched", false, GameState.Wanted);
            watched.UpdateChangeDetectionWatchId(watchId);
            db.Games.AddRange(watched, new Game("Unwatched", false, GameState.Owned));
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using (var scope = _fixture.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IMaintenanceRepository>();

            var ids = await repository.GetChangeDetectionWatchIdsAsync(TestContext.Current.CancellationToken);

            ids.Should().Equal(watchId);
        }
    }

    [Fact]
    public async Task ClearUserDataAsync_ShouldEmptyEveryUserDataTable_AndOnlyResetTheProfileImagesOfUsers()
    {
        var token = TestContext.Current.CancellationToken;
        int userCount;
        await using (var scope = _fixture.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            var user = await db.Users.SingleAsync(u => u.UserName == "user", token);
            user.UseIdentityProviderPicture("profile.png", "https://idp.example.com/picture");
            var game = new Game("Full game", true, GameState.Owned);
            game.AddPerson(new Person("Designer", PersonType.Designer));
            game.AddCategory(new GameCategory("Clear category"));
            game.AddMechanic(new GameMechanic("Clear mechanic"));
            var host = new Player("Host");
            var guest = new Player("Guest");
            var location = new Location("Attic");
            db.AddRange(game, host, guest, location);
            await db.SaveChangesAsync(token);
            game.AddExpansion(new Expansion("Clear expansion", 4343, game.Id));
            var session = new Session(game.Id, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(-1).AddHours(1), string.Empty);
            session.AddPlayerSession(host.Id, 10, true, true);
            var gameNight = GameNight.Create("Clear night", string.Empty, DateTime.UtcNow.AddDays(2), host.Id, location.Id);
            gameNight.AddInvitedPlayers([guest.Id]);
            var manual = new Manual("Clear rules", "clear.pdf", "application/pdf", 10, game.Id, DateTime.UtcNow);
            db.AddRange(
                session,
                gameNight,
                manual,
                new GameAccessory("Sleeves", "Card sleeves", game.Id),
                new Loan(game.Id, guest.Id, DateTime.UtcNow.AddDays(-2)));
            await db.SaveChangesAsync(token);
            db.ManualChunks.Add(new ManualChunk(manual.Id, game.Id, 0, "chunk", 1, new Vector(new float[1024])));
            await db.SaveChangesAsync(token);
            userCount = await db.Users.CountAsync(token);
        }

        await using (var scope = _fixture.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IMaintenanceRepository>();

            await repository.ClearUserDataAsync(token);
        }

        await using (var scope = _fixture.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            (await db.Set<GameNightRsvp>().CountAsync(token)).Should().Be(0);
            (await db.PlayerSessions.CountAsync(token)).Should().Be(0);
            (await db.Loans.CountAsync(token)).Should().Be(0);
            (await db.GameNights.CountAsync(token)).Should().Be(0);
            (await db.Sessions.CountAsync(token)).Should().Be(0);
            (await db.Expansions.CountAsync(token)).Should().Be(0);
            (await db.GameAccessories.CountAsync(token)).Should().Be(0);
            (await db.Manuals.CountAsync(token)).Should().Be(0);
            (await db.ManualChunks.CountAsync(token)).Should().Be(0);
            (await db.Games.CountAsync(token)).Should().Be(0);
            (await db.People.CountAsync(token)).Should().Be(0);
            (await db.GameCategories.CountAsync(token)).Should().Be(0);
            (await db.GameMechanics.CountAsync(token)).Should().Be(0);
            (await db.Players.CountAsync(token)).Should().Be(0);
            (await db.Locations.CountAsync(token)).Should().Be(0);
            (await db.Users.CountAsync(token)).Should().Be(userCount);
            var user = await db.Users.AsNoTracking().SingleAsync(u => u.UserName == "user", token);
            user.ProfileImage.Should().BeNull();
            user.ProfileImageSource.Should().Be(ProfileImageSource.None);
            user.ProfileImageSourceUrl.Should().BeNull();
        }
    }

    [Fact]
    public async Task ClearSettingsAndAuthAsync_ShouldRemoveProvidersConfigUsersAndRoles()
    {
        var token = TestContext.Current.CancellationToken;
        await using var scope = _fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        var repository = scope.ServiceProvider.GetRequiredService<IMaintenanceRepository>();
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        db.OidcProviders.Add(new OidcProvider("Clear IdP", "https://idp.example.com", "bgt-client"));
        await db.SaveChangesAsync(token);
        (await db.Config.CountAsync(token)).Should().BePositive();
        (await db.Users.CountAsync(token)).Should().BePositive();
        (await db.Roles.CountAsync(token)).Should().BePositive();

        await repository.ClearSettingsAndAuthAsync(token);

        (await db.OidcProviders.CountAsync(token)).Should().Be(0);
        (await db.Config.CountAsync(token)).Should().Be(0);
        (await db.Users.CountAsync(token)).Should().Be(0);
        (await db.Roles.CountAsync(token)).Should().Be(0);
        await transaction.RollbackAsync(token);
    }
}
