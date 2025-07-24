using System.Reflection;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Entities.Helpers;
using BoardGameTracker.Common.Enums;
using Microsoft.EntityFrameworkCore;

namespace BoardGameTracker.Core.Datastore;

// dotnet ef migrations add <NAME> --startup-project ../BoardGameTracker.Host --output-dir DataStore/Migrations/Postgres
public class MainDbContext : DbContext
{
    public DbSet<Game> Games { get; set; }
    public DbSet<Expansion> Expansions { get; set; }
    public DbSet<GameAccessory> GameAccessories { get; set; }
    public DbSet<GameCategory> GameCategories { get; set; }
    public DbSet<GameMechanic> GameMechanics { get; set; }
    public DbSet<Person> People { get; set; }
    public DbSet<Player> Players { get; set; }
    public DbSet<Session> Sessions { get; set; }
    public DbSet<Location> Locations { get; set; }
    public DbSet<Config> Config { get; set; }
    public DbSet<Language> Languages { get; set; }
    public DbSet<PlayerSession> PlayerSessions { get; set; }
    public DbSet<Badge> Badges { get; set; }

    public MainDbContext(DbContextOptions<MainDbContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.EnableSensitiveDataLogging();
        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        BuildIds(builder);
        BuildGame(builder);
        BuildGameSessions(builder);
        BuildPlayer(builder);
        BuildBadges(builder);

        SeedDatabase(builder);
    }

    private void BuildIds(ModelBuilder builder)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var @namespace = typeof(Game).Namespace;
        var types = assembly.GetTypes()
            .Where(t => t.Namespace == @namespace)
            .ToArray();

        foreach (var type in types)
        {
            if (typeof(HasId).IsAssignableFrom(type) && !type.IsAbstract)
            {
                builder.Entity(type).HasKey("Id");
            }
        }
    }

    private static void BuildGame(ModelBuilder builder)
    {
        builder.Entity<Game>()
            .HasMany(x => x.Expansions)
            .WithOne(x => x.Game)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Game>()
            .HasMany(x => x.Sessions)
            .WithOne(x => x.Game)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Game>()
            .HasMany(x => x.Categories)
            .WithMany(x => x.Games);

        builder.Entity<Game>()
            .HasMany(x => x.Mechanics)
            .WithMany(x => x.Games);

        builder.Entity<Game>()
            .HasMany(x => x.People)
            .WithMany(x => x.Games);

        builder.Entity<Game>()
            .HasMany(x => x.Accessories)
            .WithOne(x => x.Game)
            .HasForeignKey(x => x.GameId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void BuildGameSessions(ModelBuilder builder)
    {
        builder.Entity<Session>()
            .HasOne(x => x.Location)
            .WithMany(x => x.Sessions)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Session>()
            .HasMany(x => x.ExtraImages)
            .WithOne(x => x.Play)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Session>()
            .HasMany(x => x.Expansions)
            .WithMany(x => x.Sessions);

        builder.Entity<Session>()
            .HasMany(x => x.PlayerSessions)
            .WithOne(x => x.Session)
            .HasForeignKey(x => x.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PlayerSession>()
            .HasKey(x => new {x.PlayerId, PlayId = x.SessionId});
    }

    private static void BuildPlayer(ModelBuilder builder)
    {
        builder.Entity<Player>()
            .HasMany(x => x.PlayerSessions)
            .WithOne(x => x.Player)
            .HasForeignKey(x => x.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Player>()
            .HasMany(x => x.Badges)
            .WithMany(x => x.Players);
    }

    private static void BuildBadges(ModelBuilder builder)
    {
        builder.Entity<Badge>()
            .Property(x => x.Level)
            .HasConversion<string>();

        builder.Entity<Badge>()
            .Property(x => x.Type)
            .HasConversion<string>();
    }

    private static void SeedDatabase(ModelBuilder builder)
    {
        SeedLanguages(builder);
        SeedBadges(builder);
    }

    private static void SeedLanguages(ModelBuilder builder)
    {
        builder
            .Entity<Language>()
            .HasData(
                new Language {Id = 1, Key = "en-us", TranslationKey = "english"},
                new Language {Id = 2, Key = "nl-be", TranslationKey = "dutch"}
            );
    }

    private static void SeedBadges(ModelBuilder builder)
    {
        builder
            .Entity<Badge>()
            .HasData(
                new Badge
                {
                    Id = 1, Type = BadgeType.DifferentGames, Level = BadgeLevel.Green,
                    DescriptionKey = "different-games.green.description",
                    TitleKey = "different-games.green.title", Image = "different-games-green.png"
                },
                new Badge
                {
                    Id = 2, Type = BadgeType.DifferentGames, Level = BadgeLevel.Blue,
                    DescriptionKey = "different-games.blue.description",
                    TitleKey = "different-games.blue.title", Image = "different-games-blue.png"
                },
                new Badge
                {
                    Id = 3, Type = BadgeType.DifferentGames, Level = BadgeLevel.Red,
                    DescriptionKey = "different-games.red.description",
                    TitleKey = "different-games.red.title", Image = "different-games-red.png"
                },
                new Badge
                {
                    Id = 4, Type = BadgeType.DifferentGames, Level = BadgeLevel.Gold,
                    DescriptionKey = "different-games.gold.description",
                    TitleKey = "different-games.gold.title", Image = "different-games-gold.png"
                },
                new Badge
                {
                    Id = 5, Type = BadgeType.Sessions, Level = BadgeLevel.Green,
                    DescriptionKey = "sessions.green.description",
                    TitleKey = "sessions.green.title", Image = "sessions-green.png"
                },
                new Badge
                {
                    Id = 6, Type = BadgeType.Sessions, Level = BadgeLevel.Blue,
                    DescriptionKey = "sessions.blue.description",
                    TitleKey = "sessions.blue.title", Image = "sessions-blue.png"
                },
                new Badge
                {
                    Id = 7, Type = BadgeType.Sessions, Level = BadgeLevel.Red,
                    DescriptionKey = "sessions.red.description",
                    TitleKey = "sessions.red.title", Image = "sessions-red.png"
                },
                new Badge
                {
                    Id = 8, Type = BadgeType.Sessions, Level = BadgeLevel.Gold,
                    DescriptionKey = "sessions.gold.description",
                    TitleKey = "sessions.gold.title", Image = "sessions-gold.png"
                },
                new Badge
                {
                    Id = 9, Type = BadgeType.Wins, Level = BadgeLevel.Green,
                    DescriptionKey = "wins.green.description",
                    TitleKey = "wins.green.title", Image = "wins-green.png"
                },
                new Badge
                {
                    Id = 10, Type = BadgeType.Wins, Level = BadgeLevel.Blue,
                    DescriptionKey = "wins.blue.description",
                    TitleKey = "wins.blue.title", Image = "wins-blue.png"
                },
                new Badge
                {
                    Id = 11, Type = BadgeType.Wins, Level = BadgeLevel.Red,
                    DescriptionKey = "wins.red.description",
                    TitleKey = "wins.red.title", Image = "wins-red.png"
                },
                new Badge
                {
                    Id = 12, Type = BadgeType.Wins, Level = BadgeLevel.Gold,
                    DescriptionKey = "wins.gold.description",
                    TitleKey = "wins.gold.title", Image = "wins-gold.png"
                },
                new Badge
                {
                    Id = 13, Type = BadgeType.Duration, Level = BadgeLevel.Green,
                    DescriptionKey = "duration.green.description",
                    TitleKey = "duration.green.title", Image = "duration-green.png"
                },
                new Badge
                {
                    Id = 14, Type = BadgeType.Duration, Level = BadgeLevel.Blue,
                    DescriptionKey = "duration.blue.description",
                    TitleKey = "duration.blue.title", Image = "duration-blue.png"
                },
                new Badge
                {
                    Id = 15, Type = BadgeType.Duration, Level = BadgeLevel.Red,
                    DescriptionKey = "duration.red.description",
                    TitleKey = "duration.red.title", Image = "duration-red.png"
                },
                new Badge
                {
                    Id = 16, Type = BadgeType.Duration, Level = BadgeLevel.Gold,
                    DescriptionKey = "duration.gold.description",
                    TitleKey = "duration.gold.title", Image = "duration-gold.png"
                },
                new Badge
                {
                    Id = 17, Type = BadgeType.WinPercentage, Level = BadgeLevel.Green,
                    DescriptionKey = "win-percentage.green.description",
                    TitleKey = "win-percentage.green.title", Image = "win-percentage-green.png"
                },
                new Badge
                {
                    Id = 18, Type = BadgeType.WinPercentage, Level = BadgeLevel.Blue,
                    DescriptionKey = "win-percentage.blue.description",
                    TitleKey = "win-percentage.blue.title", Image = "win-percentage-blue.png"
                },
                new Badge
                {
                    Id = 19, Type = BadgeType.WinPercentage, Level = BadgeLevel.Red,
                    DescriptionKey = "win-percentage.red.description",
                    TitleKey = "win-percentage.red.title", Image = "win-percentage-red.png"
                },
                new Badge
                {
                    Id = 20, Type = BadgeType.WinPercentage, Level = BadgeLevel.Gold,
                    DescriptionKey = "win-percentage.gold.description",
                    TitleKey = "win-percentage.gold.title", Image = "win-percentage-gold.png"
                },
                new Badge
                {
                    Id = 21, Type = BadgeType.SoloSpecialist, Level = BadgeLevel.Green,
                    DescriptionKey = "solo-specialist.green.description",
                    TitleKey = "solo-specialist.green.title", Image = "solo-specialist-green.png"
                },
                new Badge
                {
                    Id = 22, Type = BadgeType.SoloSpecialist, Level = BadgeLevel.Blue,
                    DescriptionKey = "solo-specialist.blue.description",
                    TitleKey = "solo-specialist.blue.title", Image = "solo-specialist-blue.png"
                },
                new Badge
                {
                    Id = 23, Type = BadgeType.SoloSpecialist, Level = BadgeLevel.Red,
                    DescriptionKey = "solo-specialist.red.description",
                    TitleKey = "solo-specialist.red.title", Image = "solo-specialist-red.png"
                },
                new Badge
                {
                    Id = 24, Type = BadgeType.SoloSpecialist, Level = BadgeLevel.Gold,
                    DescriptionKey = "solo-specialist.gold.description",
                    TitleKey = "solo-specialist.gold.title", Image = "solo-specialist-gold.png"
                },
                new Badge
                {
                    Id = 25, Type = BadgeType.WinningStreak, Level = BadgeLevel.Green,
                    DescriptionKey = "winning-streak.green.description",
                    TitleKey = "winning-streak.green.title", Image = "winning-streak-green.png"
                },
                new Badge
                {
                    Id = 26, Type = BadgeType.WinningStreak, Level = BadgeLevel.Blue,
                    DescriptionKey = "winning-streak.blue.description",
                    TitleKey = "winning-streak.blue.title", Image = "winning-streak-blue.png"
                },
                new Badge
                {
                    Id = 27, Type = BadgeType.WinningStreak, Level = BadgeLevel.Red,
                    DescriptionKey = "winning-streak.red.description",
                    TitleKey = "winning-streak.red.title", Image = "winning-streak-red.png"
                },
                new Badge
                {
                    Id = 28, Type = BadgeType.WinningStreak, Level = BadgeLevel.Gold,
                    DescriptionKey = "winning-streak.gold.description",
                    TitleKey = "winning-streak.gold.title", Image = "winning-streak-gold.png"
                },
                new Badge
                {
                    Id = 29, Type = BadgeType.SocialPlayer, Level = BadgeLevel.Green,
                    DescriptionKey = "social-player.green.description",
                    TitleKey = "social-player.green.title", Image = "social-player-green.png"
                },
                new Badge
                {
                    Id = 30, Type = BadgeType.SocialPlayer, Level = BadgeLevel.Blue,
                    DescriptionKey = "social-player.blue.description",
                    TitleKey = "social-player.blue.title", Image = "social-player-blue.png"
                },
                new Badge
                {
                    Id = 31, Type = BadgeType.SocialPlayer, Level = BadgeLevel.Red,
                    DescriptionKey = "social-player.red.description",
                    TitleKey = "social-player.red.title", Image = "social-player-red.png"
                },
                new Badge
                {
                    Id = 32, Type = BadgeType.SocialPlayer, Level = BadgeLevel.Gold,
                    DescriptionKey = "social-player.gold.description",
                    TitleKey = "social-player.gold.title", Image = "social-player-gold.png"
                },
                new Badge
                {
                    Id = 33, Type = BadgeType.CloseWin, Level = null,
                    DescriptionKey = "close-win.description",
                    TitleKey = "close-win.title", Image = "close-win.png"
                },
                new Badge
                {
                    Id = 34, Type = BadgeType.CLoseLoss, Level = null,
                    DescriptionKey = "close-loss.description",
                    TitleKey = "close-loss.title", Image = "close-loss.png"
                },
                new Badge()
                {
                    Id = 35, Type = BadgeType.MarathonRunner, Level = null,
                    DescriptionKey = "marathon-runner.description",
                    TitleKey = "marathon-runner.title", Image = "marathon.png"
                },
                new Badge
                {
                    Id = 36, Type = BadgeType.FirstTry, Level = null,
                    DescriptionKey = "first-try.description",
                    TitleKey = "first-try.title", Image = "first-try.png"
                },
                new Badge
                {
                    Id = 37, Type = BadgeType.LearningCurve, Level = null,
                    DescriptionKey = "learning-curve.description",
                    TitleKey = "learning-curve.title", Image = "learning-curve.png"
                },
                new Badge
                {
                    Id = 38, Type = BadgeType.MonthlyGoal, Level = null,
                    DescriptionKey = "monthly-goal.description",
                    TitleKey = "monthly-goal.title", Image = "monthly-goal.png"
                },
                new Badge
                {
                    Id = 39, Type = BadgeType.ConsistentSchedule, Level = null,
                    DescriptionKey = "consistent-schedule.description",
                    TitleKey = "consistent-schedule.title", Image = "consistent-schedule.png"
                }
            );
    }
}