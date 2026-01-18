#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BoardGameTracker.Core.Datastore.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddedBadges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Badges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DescriptionKey = table.Column<string>(type: "text", nullable: false),
                    TitleKey = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Level = table.Column<string>(type: "text", nullable: true),
                    Image = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Badges", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BadgePlayer",
                columns: table => new
                {
                    BadgesId = table.Column<int>(type: "integer", nullable: false),
                    PlayersId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BadgePlayer", x => new { x.BadgesId, x.PlayersId });
                    table.ForeignKey(
                        name: "FK_BadgePlayer_Badges_BadgesId",
                        column: x => x.BadgesId,
                        principalTable: "Badges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BadgePlayer_Players_PlayersId",
                        column: x => x.PlayersId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Badges",
                columns: new[] { "Id", "DescriptionKey", "Image", "Level", "TitleKey", "Type" },
                values: new object[,]
                {
                    { 1, "different-games.green.description", "different-games-green.png", "Green", "different-games.green.title", "DifferentGames" },
                    { 2, "different-games.blue.description", "different-games-blue.png", "Blue", "different-games.blue.title", "DifferentGames" },
                    { 3, "different-games.red.description", "different-games-red.png", "Red", "different-games.red.title", "DifferentGames" },
                    { 4, "different-games.gold.description", "different-games-gold.png", "Gold", "different-games.gold.title", "DifferentGames" }
                });

            migrationBuilder.InsertData(
                table: "Languages",
                columns: new[] { "Id", "Key", "TranslationKey" },
                values: new object[,]
                {
                    { 1, "en-us", "english" },
                    { 2, "nl-be", "dutch" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_BadgePlayer_PlayersId",
                table: "BadgePlayer",
                column: "PlayersId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BadgePlayer");

            migrationBuilder.DropTable(
                name: "Badges");

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "Id",
                keyValue: 2);
        }
    }
}
