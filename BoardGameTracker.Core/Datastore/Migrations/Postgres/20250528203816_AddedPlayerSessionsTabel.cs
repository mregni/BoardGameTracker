#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace BoardGameTracker.Core.Datastore.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddedPlayerSessionsTabel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlayerSession_Players_PlayerId",
                table: "PlayerSession");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayerSession_Sessions_SessionId",
                table: "PlayerSession");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PlayerSession",
                table: "PlayerSession");

            migrationBuilder.RenameTable(
                name: "PlayerSession",
                newName: "PlayerSessions");

            migrationBuilder.RenameIndex(
                name: "IX_PlayerSession_SessionId",
                table: "PlayerSessions",
                newName: "IX_PlayerSessions_SessionId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PlayerSessions",
                table: "PlayerSessions",
                columns: new[] { "PlayerId", "SessionId" });

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerSessions_Players_PlayerId",
                table: "PlayerSessions",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerSessions_Sessions_SessionId",
                table: "PlayerSessions",
                column: "SessionId",
                principalTable: "Sessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlayerSessions_Players_PlayerId",
                table: "PlayerSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayerSessions_Sessions_SessionId",
                table: "PlayerSessions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PlayerSessions",
                table: "PlayerSessions");

            migrationBuilder.RenameTable(
                name: "PlayerSessions",
                newName: "PlayerSession");

            migrationBuilder.RenameIndex(
                name: "IX_PlayerSessions_SessionId",
                table: "PlayerSession",
                newName: "IX_PlayerSession_SessionId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PlayerSession",
                table: "PlayerSession",
                columns: new[] { "PlayerId", "SessionId" });

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerSession_Players_PlayerId",
                table: "PlayerSession",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerSession_Sessions_SessionId",
                table: "PlayerSession",
                column: "SessionId",
                principalTable: "Sessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
