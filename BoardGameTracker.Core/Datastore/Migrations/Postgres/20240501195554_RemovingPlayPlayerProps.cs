using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoardGameTracker.Core.DataStore.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class RemovingPlayPlayerProps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Ended",
                table: "Plays");

            migrationBuilder.DropColumn(
                name: "CharacterName",
                table: "PlayerPlay");

            migrationBuilder.DropColumn(
                name: "Color",
                table: "PlayerPlay");

            migrationBuilder.DropColumn(
                name: "Team",
                table: "PlayerPlay");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Ended",
                table: "Plays",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "CharacterName",
                table: "PlayerPlay",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "PlayerPlay",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Team",
                table: "PlayerPlay",
                type: "text",
                nullable: true);
        }
    }
}
