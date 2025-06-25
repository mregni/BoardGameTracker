using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoardGameTracker.Core.DataStore.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class MoveExpansionsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Expansion_Games_GameId",
                table: "Expansion");

            migrationBuilder.DropForeignKey(
                name: "FK_ExpansionSession_Expansion_ExpansionsId",
                table: "ExpansionSession");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Expansion",
                table: "Expansion");

            migrationBuilder.RenameTable(
                name: "Expansion",
                newName: "Expansions");

            migrationBuilder.RenameIndex(
                name: "IX_Expansion_GameId",
                table: "Expansions",
                newName: "IX_Expansions_GameId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Expansions",
                table: "Expansions",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ExpansionSession_Expansions_ExpansionsId",
                table: "ExpansionSession",
                column: "ExpansionsId",
                principalTable: "Expansions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Expansions_Games_GameId",
                table: "Expansions",
                column: "GameId",
                principalTable: "Games",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExpansionSession_Expansions_ExpansionsId",
                table: "ExpansionSession");

            migrationBuilder.DropForeignKey(
                name: "FK_Expansions_Games_GameId",
                table: "Expansions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Expansions",
                table: "Expansions");

            migrationBuilder.RenameTable(
                name: "Expansions",
                newName: "Expansion");

            migrationBuilder.RenameIndex(
                name: "IX_Expansions_GameId",
                table: "Expansion",
                newName: "IX_Expansion_GameId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Expansion",
                table: "Expansion",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Expansion_Games_GameId",
                table: "Expansion",
                column: "GameId",
                principalTable: "Games",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ExpansionSession_Expansion_ExpansionsId",
                table: "ExpansionSession",
                column: "ExpansionsId",
                principalTable: "Expansion",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
