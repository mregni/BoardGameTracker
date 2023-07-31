using System.Reflection;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Entities.Helpers;
using Microsoft.EntityFrameworkCore;

namespace BoardGameTracker.Core.Datastore;

public class MainDbContext : DbContext
{
    public DbSet<Game> Games { get; set; }
    public DbSet<GameAccessory> GameAccessories { get; set; }
    public DbSet<GameCategory> GameCategories { get; set; }
    public DbSet<GameMechanic> GameMechanics { get; set; }
    public DbSet<Person> People { get; set; }
    public DbSet<Player> Players { get; set; }
    public MainDbContext(DbContextOptions<MainDbContext> options): base(options)
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
        BuildGamePlays(builder);
        BuildPlayer(builder);
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
            .HasMany(x => x.Plays)
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

    private static void BuildGamePlays(ModelBuilder builder)
    {
        builder.Entity<Play>()
            .HasMany(x => x.Sessions)
            .WithOne(x => x.Play)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.Entity<Play>()
            .HasOne(x => x.Location)
            .WithMany(x => x.Plays)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Play>()
            .HasMany(x => x.ExtraImages)
            .WithOne(x => x.Play)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.Entity<Play>()
            .HasMany(x => x.Expansions)
            .WithMany(x => x.Plays);

        builder.Entity<Play>()
            .HasMany(x => x.Players)
            .WithOne(x => x.Play)
            .HasForeignKey(x => x.PlayId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void BuildPlayer(ModelBuilder builder)
    {
        builder.Entity<Player>()
            .HasMany(x => x.Plays)
            .WithOne(x => x.Player)
            .HasForeignKey(x => x.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}