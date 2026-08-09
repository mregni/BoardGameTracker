using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoardGameTracker.Core.DataStore.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddPlayerLinkAndEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_PlayerId",
                schema: "auth",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Players",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_PlayerId",
                schema: "auth",
                table: "AspNetUsers",
                column: "PlayerId",
                unique: true,
                filter: "\"PlayerId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_PlayerId",
                schema: "auth",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Players");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_PlayerId",
                schema: "auth",
                table: "AspNetUsers",
                column: "PlayerId");
        }
    }
}
