using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Configuration.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BoardGameTracker.Core.Datastore;

public class MainContext : DbContext
{
    public DbSet<Game> Games { get; set; }
    public DbSet<GameAccessory> GameAccessories { get; set; }
    public DbSet<GameCategory> GameCategories { get; set; }
    public DbSet<GameMechanic> GameMechanics { get; set; }
    public DbSet<Person> People { get; set; }
    public MainContext(DbContextOptions<MainContext> options): base(options)
    {
        
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.EnableSensitiveDataLogging();
        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        BuildGame(builder);
        BuildGameAccessories(builder);
        BuildGameCategories(builder);
        BuildGameMechanics(builder);
        BuildGamePeople(builder);
    }

    private static void BuildGameAccessories(ModelBuilder builder)
    {
        builder.Entity<GameAccessory>().HasKey(x => x.Id);
    }
    
    private static void BuildGameCategories(ModelBuilder builder)
    {
        builder.Entity<GameCategory>().HasKey(x => x.Id);
    }
    
    private static void BuildGameMechanics(ModelBuilder builder)
    {
        builder.Entity<GameMechanic>().HasKey(x => x.Id);
    }
    
    private static void BuildGamePeople(ModelBuilder builder)
    {
        builder.Entity<Person>().HasKey(x => x.Id);
    }

    private static void BuildGame(ModelBuilder builder)
    {
        builder.Entity<Game>().HasKey(x => x.Id);

        builder.Entity<Game>()
            .HasMany(x => x.Expansions)
            .WithOne(x => x.BaseGame)
            .HasForeignKey(x => x.BaseGameId)
            .IsRequired(false)
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
}