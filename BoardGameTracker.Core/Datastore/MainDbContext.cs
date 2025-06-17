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

        optionsBuilder.UseSeeding((context, _) =>
        {
            CheckLanguages(context);
        });
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        BuildIds(builder);
        BuildGame(builder);
        BuildGameSessions(builder);
        BuildPlayer(builder);
        BuildBadges(builder);
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
            .WithOne(x => x.BaseGame)
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
            .HasConversion<BadgeLevel>();
        
        builder.Entity<Badge>()
            .Property(x => x.Type)
            .HasConversion<BadgeType>();
    }

    private static void CheckLanguages(DbContext context)
    {
        var languages = new []
        {
            new Language { Key = "en-us", TranslationKey = "english" },
            new Language { Key = "nl-be", TranslationKey = "dutch" } 
        };

        foreach (var language in languages)
        {
            var dblang = context.Set<Language>().FirstOrDefault(b => b.Key == language.Key);
            if (dblang == null)
            {
                context.Set<Language>().Add(language);
            }
        }
        context.SaveChanges();
    }
}