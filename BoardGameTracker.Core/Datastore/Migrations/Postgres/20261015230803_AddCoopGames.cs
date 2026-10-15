using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoardGameTracker.Core.Datastore.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddCoopGames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCoop",
                table: "Sessions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "TeamScore",
                table: "Sessions",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TeamWon",
                table: "Sessions",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CoopByDefault",
                table: "Games",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasCoopMode",
                table: "Games",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCoop",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "TeamScore",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "TeamWon",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "CoopByDefault",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "HasCoopMode",
                table: "Games");
        }
    }
}
