﻿// <auto-generated />
using System;
using BoardGameTracker.Core.Datastore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BoardGameTracker.Core.DataStore.Migrations.Postgres
{
    [DbContext(typeof(MainDbContext))]
    [Migration("20240606053952_AddedBackgroundImage")]
    partial class AddedBackgroundImage
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.3")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("BoardGameTracker.Common.Entities.Config", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Key")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Value")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("Config");
                });

            modelBuilder.Entity("BoardGameTracker.Common.Entities.Expansion", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<DateTime?>("AdditionDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("BackgroundImage")
                        .HasColumnType("text");

                    b.Property<int?>("BaseGameId")
                        .HasColumnType("integer");

                    b.Property<int?>("BggId")
                        .HasColumnType("integer");

                    b.Property<double?>("BuyingPrice")
                        .HasColumnType("double precision");

                    b.Property<string>("Image")
                        .HasColumnType("text");

                    b.Property<int?>("MaxPlayTime")
                        .HasColumnType("integer");

                    b.Property<int?>("MaxPlayers")
                        .HasColumnType("integer");

                    b.Property<int?>("MinAge")
                        .HasColumnType("integer");

                    b.Property<int?>("MinPlayTime")
                        .HasColumnType("integer");

                    b.Property<int?>("MinPlayers")
                        .HasColumnType("integer");

                    b.Property<double?>("Rating")
                        .HasColumnType("double precision");

                    b.Property<double?>("SoldPrice")
                        .HasColumnType("double precision");

                    b.Property<int>("State")
                        .HasColumnType("integer");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<double?>("Weight")
                        .HasColumnType("double precision");

                    b.Property<int?>("YearPublished")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("BaseGameId");

                    b.ToTable("Expansion");
                });

            modelBuilder.Entity("BoardGameTracker.Common.Entities.Game", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<DateTime?>("AdditionDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("BackgroundImage")
                        .HasColumnType("text");

                    b.Property<int?>("BggId")
                        .HasColumnType("integer");

                    b.Property<double?>("BuyingPrice")
                        .HasColumnType("double precision");

                    b.Property<bool>("HasScoring")
                        .HasColumnType("boolean");

                    b.Property<string>("Image")
                        .HasColumnType("text");

                    b.Property<int?>("MaxPlayTime")
                        .HasColumnType("integer");

                    b.Property<int?>("MaxPlayers")
                        .HasColumnType("integer");

                    b.Property<int?>("MinAge")
                        .HasColumnType("integer");

                    b.Property<int?>("MinPlayTime")
                        .HasColumnType("integer");

                    b.Property<int?>("MinPlayers")
                        .HasColumnType("integer");

                    b.Property<double?>("Rating")
                        .HasColumnType("double precision");

                    b.Property<double?>("SoldPrice")
                        .HasColumnType("double precision");

                    b.Property<int>("State")
                        .HasColumnType("integer");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<double?>("Weight")
                        .HasColumnType("double precision");

                    b.Property<int?>("YearPublished")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.ToTable("Games");
                });

            modelBuilder.Entity("BoardGameTracker.Common.Entities.GameAccessory", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("GameId")
                        .HasColumnType("integer");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("GameId");

                    b.ToTable("GameAccessories");
                });

            modelBuilder.Entity("BoardGameTracker.Common.Entities.GameCategory", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("GameCategories");
                });

            modelBuilder.Entity("BoardGameTracker.Common.Entities.GameMechanic", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("GameMechanics");
                });

            modelBuilder.Entity("BoardGameTracker.Common.Entities.Helpers.PlayerPlay", b =>
                {
                    b.Property<int?>("PlayerId")
                        .HasColumnType("integer");

                    b.Property<int>("PlayId")
                        .HasColumnType("integer");

                    b.Property<bool>("FirstPlay")
                        .HasColumnType("boolean");

                    b.Property<bool>("IsBot")
                        .HasColumnType("boolean");

                    b.Property<double?>("Score")
                        .HasColumnType("double precision");

                    b.Property<bool>("Won")
                        .HasColumnType("boolean");

                    b.HasKey("PlayerId", "PlayId");

                    b.HasIndex("PlayId");

                    b.ToTable("PlayerPlay");
                });

            modelBuilder.Entity("BoardGameTracker.Common.Entities.Image", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int>("GameId")
                        .HasColumnType("integer");

                    b.Property<int?>("GamePlayId")
                        .HasColumnType("integer");

                    b.Property<string>("Path")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int?>("PlayId")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("GameId");

                    b.HasIndex("PlayId");

                    b.ToTable("Image");
                });

            modelBuilder.Entity("BoardGameTracker.Common.Entities.Location", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("Locations");
                });

            modelBuilder.Entity("BoardGameTracker.Common.Entities.Person", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("Type")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.ToTable("People");
                });

            modelBuilder.Entity("BoardGameTracker.Common.Entities.Play", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Comment")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime>("End")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("GameId")
                        .HasColumnType("integer");

                    b.Property<int?>("LocationId")
                        .HasColumnType("integer");

                    b.Property<DateTime>("Start")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("Id");

                    b.HasIndex("GameId");

                    b.HasIndex("LocationId");

                    b.ToTable("Plays");
                });

            modelBuilder.Entity("BoardGameTracker.Common.Entities.Player", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Image")
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("Players");
                });

            modelBuilder.Entity("ExpansionPlay", b =>
                {
                    b.Property<int>("ExpansionsId")
                        .HasColumnType("integer");

                    b.Property<int>("PlaysId")
                        .HasColumnType("integer");

                    b.HasKey("ExpansionsId", "PlaysId");

                    b.HasIndex("PlaysId");

                    b.ToTable("ExpansionPlay");
                });

            modelBuilder.Entity("GameGameCategory", b =>
                {
                    b.Property<int>("CategoriesId")
                        .HasColumnType("integer");

                    b.Property<int>("GamesId")
                        .HasColumnType("integer");

                    b.HasKey("CategoriesId", "GamesId");

                    b.HasIndex("GamesId");

                    b.ToTable("GameGameCategory");
                });

            modelBuilder.Entity("GameGameMechanic", b =>
                {
                    b.Property<int>("GamesId")
                        .HasColumnType("integer");

                    b.Property<int>("MechanicsId")
                        .HasColumnType("integer");

                    b.HasKey("GamesId", "MechanicsId");

                    b.HasIndex("MechanicsId");

                    b.ToTable("GameGameMechanic");
                });

            modelBuilder.Entity("GamePerson", b =>
                {
                    b.Property<int>("GamesId")
                        .HasColumnType("integer");

                    b.Property<int>("PeopleId")
                        .HasColumnType("integer");

                    b.HasKey("GamesId", "PeopleId");

                    b.HasIndex("PeopleId");

                    b.ToTable("GamePerson");
                });

            modelBuilder.Entity("BoardGameTracker.Common.Entities.Expansion", b =>
                {
                    b.HasOne("BoardGameTracker.Common.Entities.Game", "BaseGame")
                        .WithMany("Expansions")
                        .HasForeignKey("BaseGameId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.Navigation("BaseGame");
                });

            modelBuilder.Entity("BoardGameTracker.Common.Entities.GameAccessory", b =>
                {
                    b.HasOne("BoardGameTracker.Common.Entities.Game", "Game")
                        .WithMany("Accessories")
                        .HasForeignKey("GameId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Game");
                });

            modelBuilder.Entity("BoardGameTracker.Common.Entities.Helpers.PlayerPlay", b =>
                {
                    b.HasOne("BoardGameTracker.Common.Entities.Play", "Play")
                        .WithMany("Players")
                        .HasForeignKey("PlayId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("BoardGameTracker.Common.Entities.Player", "Player")
                        .WithMany("Plays")
                        .HasForeignKey("PlayerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Play");

                    b.Navigation("Player");
                });

            modelBuilder.Entity("BoardGameTracker.Common.Entities.Image", b =>
                {
                    b.HasOne("BoardGameTracker.Common.Entities.Game", "Game")
                        .WithMany()
                        .HasForeignKey("GameId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("BoardGameTracker.Common.Entities.Play", "Play")
                        .WithMany("ExtraImages")
                        .HasForeignKey("PlayId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.Navigation("Game");

                    b.Navigation("Play");
                });

            modelBuilder.Entity("BoardGameTracker.Common.Entities.Play", b =>
                {
                    b.HasOne("BoardGameTracker.Common.Entities.Game", "Game")
                        .WithMany("Plays")
                        .HasForeignKey("GameId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("BoardGameTracker.Common.Entities.Location", "Location")
                        .WithMany("Plays")
                        .HasForeignKey("LocationId")
                        .OnDelete(DeleteBehavior.SetNull);

                    b.Navigation("Game");

                    b.Navigation("Location");
                });

            modelBuilder.Entity("ExpansionPlay", b =>
                {
                    b.HasOne("BoardGameTracker.Common.Entities.Expansion", null)
                        .WithMany()
                        .HasForeignKey("ExpansionsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("BoardGameTracker.Common.Entities.Play", null)
                        .WithMany()
                        .HasForeignKey("PlaysId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("GameGameCategory", b =>
                {
                    b.HasOne("BoardGameTracker.Common.Entities.GameCategory", null)
                        .WithMany()
                        .HasForeignKey("CategoriesId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("BoardGameTracker.Common.Entities.Game", null)
                        .WithMany()
                        .HasForeignKey("GamesId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("GameGameMechanic", b =>
                {
                    b.HasOne("BoardGameTracker.Common.Entities.Game", null)
                        .WithMany()
                        .HasForeignKey("GamesId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("BoardGameTracker.Common.Entities.GameMechanic", null)
                        .WithMany()
                        .HasForeignKey("MechanicsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("GamePerson", b =>
                {
                    b.HasOne("BoardGameTracker.Common.Entities.Game", null)
                        .WithMany()
                        .HasForeignKey("GamesId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("BoardGameTracker.Common.Entities.Person", null)
                        .WithMany()
                        .HasForeignKey("PeopleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("BoardGameTracker.Common.Entities.Game", b =>
                {
                    b.Navigation("Accessories");

                    b.Navigation("Expansions");

                    b.Navigation("Plays");
                });

            modelBuilder.Entity("BoardGameTracker.Common.Entities.Location", b =>
                {
                    b.Navigation("Plays");
                });

            modelBuilder.Entity("BoardGameTracker.Common.Entities.Play", b =>
                {
                    b.Navigation("ExtraImages");

                    b.Navigation("Players");
                });

            modelBuilder.Entity("BoardGameTracker.Common.Entities.Player", b =>
                {
                    b.Navigation("Plays");
                });
#pragma warning restore 612, 618
        }
    }
}
