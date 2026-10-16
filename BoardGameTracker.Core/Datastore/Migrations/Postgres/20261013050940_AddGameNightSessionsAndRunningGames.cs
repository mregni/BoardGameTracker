using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BoardGameTracker.Core.Datastore.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddGameNightSessionsAndRunningGames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GameNightId",
                table: "Sessions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndedAt",
                table: "GameNights",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RunningGames",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GameNightId = table.Column<int>(type: "integer", nullable: false),
                    GameId = table.Column<int>(type: "integer", nullable: false),
                    Start = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RunningGames", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RunningGames_GameNights_GameNightId",
                        column: x => x.GameNightId,
                        principalTable: "GameNights",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RunningGames_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RunningGamePlayer",
                columns: table => new
                {
                    RunningGameId = table.Column<int>(type: "integer", nullable: false),
                    PlayerId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RunningGamePlayer", x => new { x.RunningGameId, x.PlayerId });
                    table.ForeignKey(
                        name: "FK_RunningGamePlayer_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RunningGamePlayer_RunningGames_RunningGameId",
                        column: x => x.RunningGameId,
                        principalTable: "RunningGames",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_GameNightId",
                table: "Sessions",
                column: "GameNightId");

            migrationBuilder.CreateIndex(
                name: "IX_RunningGamePlayer_PlayerId",
                table: "RunningGamePlayer",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_RunningGames_GameId",
                table: "RunningGames",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_RunningGames_GameNightId",
                table: "RunningGames",
                column: "GameNightId");

            migrationBuilder.AddForeignKey(
                name: "FK_Sessions_GameNights_GameNightId",
                table: "Sessions",
                column: "GameNightId",
                principalTable: "GameNights",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sessions_GameNights_GameNightId",
                table: "Sessions");

            migrationBuilder.DropTable(
                name: "RunningGamePlayer");

            migrationBuilder.DropTable(
                name: "RunningGames");

            migrationBuilder.DropIndex(
                name: "IX_Sessions_GameNightId",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "GameNightId",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "EndedAt",
                table: "GameNights");
        }
    }
}
