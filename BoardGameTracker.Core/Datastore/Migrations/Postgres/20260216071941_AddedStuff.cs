using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoardGameTracker.Core.DataStore.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddedStuff : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 34,
                column: "Type",
                value: "CloseLoss");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 34,
                column: "Type",
                value: "CLoseLoss");
        }
    }
}
