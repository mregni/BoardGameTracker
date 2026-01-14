using System.Reflection;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Entities.Helpers;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.ValueObjects;
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
    public DbSet<Loan> Loans { get; set; }

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
        ConfigureValueObjects(builder);
        BuildGame(builder);
        BuildGameSessions(builder);
        BuildPlayer(builder);
        BuildBadges(builder);
        BuildLoans(builder);

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

    private static void ConfigureValueObjects(ModelBuilder builder)
    {
        builder.Entity<Game>()
            .OwnsOne(g => g.BuyingPrice, cost =>
            {
                cost.Property(c => c.Amount)
                    .HasColumnName(nameof(Game.BuyingPrice))
                    .HasPrecision(18, 2)
                    .IsRequired();
            });
        
        builder.Entity<Game>()
            .OwnsOne(g => g.SoldPrice, cost =>
            {
                cost.Property(c => c.Amount)
                    .HasColumnName(nameof(Game.SoldPrice))
                    .HasPrecision(18, 2)
                    .IsRequired();
            });
        
        builder.Entity<Game>()
            .OwnsOne(g => g.Rating, cost =>
            {
                cost.Property(c => c.Value)
                    .HasColumnName(nameof(Game.Rating))
                    .HasPrecision(18, 2)
                    .IsRequired();
            });

        builder.Entity<Game>()
            .OwnsOne(g => g.Weight, cost =>
            {
                cost.Property(c => c.Value)
                    .HasColumnName(nameof(Game.Weight))
                    .HasPrecision(18, 2)
                    .IsRequired();
            });
        
        builder.Entity<Game>()
            .OwnsOne(x => x.PlayerCount, pcr =>
            {
                pcr.Property(p => p.Min).HasColumnName("MinPlayers");
                pcr.Property(p => p.Max).HasColumnName("MaxPlayers");
            });

        builder.Entity<Game>()
            .OwnsOne(x => x.PlayTime, ptr =>
            {
                ptr.Property(p => p.MinMinutes).HasColumnName("MinPlayTime");
                ptr.Property(p => p.MaxMinutes).HasColumnName("MaxPlayTime");
            });
    }

    private static void BuildLoans(ModelBuilder builder)
    {
        builder.Entity<Loan>()
            .HasOne(x => x.Game)
            .WithMany(x => x.Loans)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.Entity<Loan>()
            .HasOne(x => x.Player)
            .WithMany(x => x.Loans)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void BuildGame(ModelBuilder builder)
    {
        builder.Entity<Game>()
            .HasIndex(x => x.BggId)
            .IsUnique();

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
                Badge.CreateWithId(1, "different-games.green.title", "different-games.green.description", BadgeType.DifferentGames, "different-games-green.png", BadgeLevel.Green),
                Badge.CreateWithId(2, "different-games.blue.title", "different-games.blue.description", BadgeType.DifferentGames, "different-games-blue.png", BadgeLevel.Blue),
                Badge.CreateWithId(3, "different-games.red.title", "different-games.red.description", BadgeType.DifferentGames, "different-games-red.png", BadgeLevel.Red),
                Badge.CreateWithId(4, "different-games.gold.title", "different-games.gold.description", BadgeType.DifferentGames, "different-games-gold.png", BadgeLevel.Gold),
                Badge.CreateWithId(5, "sessions.green.title", "sessions.green.description", BadgeType.Sessions, "sessions-green.png", BadgeLevel.Green),
                Badge.CreateWithId(6, "sessions.blue.title", "sessions.blue.description", BadgeType.Sessions, "sessions-blue.png", BadgeLevel.Blue),
                Badge.CreateWithId(7, "sessions.red.title", "sessions.red.description", BadgeType.Sessions, "sessions-red.png", BadgeLevel.Red),
                Badge.CreateWithId(8, "sessions.gold.title", "sessions.gold.description", BadgeType.Sessions, "sessions-gold.png", BadgeLevel.Gold),
                Badge.CreateWithId(9, "wins.green.title", "wins.green.description", BadgeType.Wins, "wins-green.png", BadgeLevel.Green),
                Badge.CreateWithId(10, "wins.blue.title", "wins.blue.description", BadgeType.Wins, "wins-blue.png", BadgeLevel.Blue),
                Badge.CreateWithId(11, "wins.red.title", "wins.red.description", BadgeType.Wins, "wins-red.png", BadgeLevel.Red),
                Badge.CreateWithId(12, "wins.gold.title", "wins.gold.description", BadgeType.Wins, "wins-gold.png", BadgeLevel.Gold),
                Badge.CreateWithId(13, "duration.green.title", "duration.green.description", BadgeType.Duration, "duration-green.png", BadgeLevel.Green),
                Badge.CreateWithId(14, "duration.blue.title", "duration.blue.description", BadgeType.Duration, "duration-blue.png", BadgeLevel.Blue),
                Badge.CreateWithId(15, "duration.red.title", "duration.red.description", BadgeType.Duration, "duration-red.png", BadgeLevel.Red),
                Badge.CreateWithId(16, "duration.gold.title", "duration.gold.description", BadgeType.Duration, "duration-gold.png", BadgeLevel.Gold),
                Badge.CreateWithId(17, "win-percentage.green.title", "win-percentage.green.description", BadgeType.WinPercentage, "win-percentage-green.png", BadgeLevel.Green),
                Badge.CreateWithId(18, "win-percentage.blue.title", "win-percentage.blue.description", BadgeType.WinPercentage, "win-percentage-blue.png", BadgeLevel.Blue),
                Badge.CreateWithId(19, "win-percentage.red.title", "win-percentage.red.description", BadgeType.WinPercentage, "win-percentage-red.png", BadgeLevel.Red),
                Badge.CreateWithId(20, "win-percentage.gold.title", "win-percentage.gold.description", BadgeType.WinPercentage, "win-percentage-gold.png", BadgeLevel.Gold),
                Badge.CreateWithId(21, "solo-specialist.green.title", "solo-specialist.green.description", BadgeType.SoloSpecialist, "solo-specialist-green.png", BadgeLevel.Green),
                Badge.CreateWithId(22, "solo-specialist.blue.title", "solo-specialist.blue.description", BadgeType.SoloSpecialist, "solo-specialist-blue.png", BadgeLevel.Blue),
                Badge.CreateWithId(23, "solo-specialist.red.title", "solo-specialist.red.description", BadgeType.SoloSpecialist, "solo-specialist-red.png", BadgeLevel.Red),
                Badge.CreateWithId(24, "solo-specialist.gold.title", "solo-specialist.gold.description", BadgeType.SoloSpecialist, "solo-specialist-gold.png", BadgeLevel.Gold),
                Badge.CreateWithId(25, "winning-streak.green.title", "winning-streak.green.description", BadgeType.WinningStreak, "winning-streak-green.png", BadgeLevel.Green),
                Badge.CreateWithId(26, "winning-streak.blue.title", "winning-streak.blue.description", BadgeType.WinningStreak, "winning-streak-blue.png", BadgeLevel.Blue),
                Badge.CreateWithId(27, "winning-streak.red.title", "winning-streak.red.description", BadgeType.WinningStreak, "winning-streak-red.png", BadgeLevel.Red),
                Badge.CreateWithId(28, "winning-streak.gold.title", "winning-streak.gold.description", BadgeType.WinningStreak, "winning-streak-gold.png", BadgeLevel.Gold),
                Badge.CreateWithId(29, "social-player.green.title", "social-player.green.description", BadgeType.SocialPlayer, "social-player-green.png", BadgeLevel.Green),
                Badge.CreateWithId(30, "social-player.blue.title", "social-player.blue.description", BadgeType.SocialPlayer, "social-player-blue.png", BadgeLevel.Blue),
                Badge.CreateWithId(31, "social-player.red.title", "social-player.red.description", BadgeType.SocialPlayer, "social-player-red.png", BadgeLevel.Red),
                Badge.CreateWithId(32, "social-player.gold.title", "social-player.gold.description", BadgeType.SocialPlayer, "social-player-gold.png", BadgeLevel.Gold),
                Badge.CreateWithId(33, "close-win.title", "close-win.description", BadgeType.CloseWin, "close-win.png"),
                Badge.CreateWithId(34, "close-loss.title", "close-loss.description", BadgeType.CLoseLoss, "close-loss.png"),
                Badge.CreateWithId(35, "marathon-runner.title", "marathon-runner.description", BadgeType.MarathonRunner, "marathon.png"),
                Badge.CreateWithId(36, "first-try.title", "first-try.description", BadgeType.FirstTry, "first-try.png"),
                Badge.CreateWithId(37, "learning-curve.title", "learning-curve.description", BadgeType.LearningCurve, "learning-curve.png"),
                Badge.CreateWithId(38, "monthly-goal.title", "monthly-goal.description", BadgeType.MonthlyGoal, "monthly-goal.png"),
                Badge.CreateWithId(39, "consistent-schedule.title", "consistent-schedule.description", BadgeType.ConsistentSchedule, "consistent-schedule.png")
            );
    }
}