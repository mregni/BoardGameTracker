using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoardGameTracker.Core.DataStore.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddScoringFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasScoring",
                table: "Games",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasScoring",
                table: "Games");
        }
    }
}
