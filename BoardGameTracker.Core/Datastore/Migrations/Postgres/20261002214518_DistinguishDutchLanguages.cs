using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoardGameTracker.Core.Datastore.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class DistinguishDutchLanguages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Languages",
                keyColumn: "Id",
                keyValue: 2,
                column: "TranslationKey",
                value: "dutch-belgium");

            migrationBuilder.UpdateData(
                table: "Languages",
                keyColumn: "Id",
                keyValue: 3,
                column: "TranslationKey",
                value: "dutch-netherlands");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Languages",
                keyColumn: "Id",
                keyValue: 2,
                column: "TranslationKey",
                value: "dutch");

            migrationBuilder.UpdateData(
                table: "Languages",
                keyColumn: "Id",
                keyValue: 3,
                column: "TranslationKey",
                value: "dutch");
        }
    }
}
