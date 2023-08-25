using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BoardGameTracker.Core.DataStore.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlayerPlays_Players_PlayerId",
                table: "PlayerPlays");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayerPlays_Plays_PlayId",
                table: "PlayerPlays");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PlayerPlays",
                table: "PlayerPlays");

            migrationBuilder.DropIndex(
                name: "IX_PlayerPlays_PlayerId",
                table: "PlayerPlays");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "PlayerPlays");

            migrationBuilder.RenameTable(
                name: "PlayerPlays",
                newName: "PlayerPlay");

            migrationBuilder.RenameIndex(
                name: "IX_PlayerPlays_PlayId",
                table: "PlayerPlay",
                newName: "IX_PlayerPlay_PlayId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PlayerPlay",
                table: "PlayerPlay",
                columns: new[] { "PlayerId", "PlayId" });

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerPlay_Players_PlayerId",
                table: "PlayerPlay",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerPlay_Plays_PlayId",
                table: "PlayerPlay",
                column: "PlayId",
                principalTable: "Plays",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlayerPlay_Players_PlayerId",
                table: "PlayerPlay");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayerPlay_Plays_PlayId",
                table: "PlayerPlay");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PlayerPlay",
                table: "PlayerPlay");

            migrationBuilder.RenameTable(
                name: "PlayerPlay",
                newName: "PlayerPlays");

            migrationBuilder.RenameIndex(
                name: "IX_PlayerPlay_PlayId",
                table: "PlayerPlays",
                newName: "IX_PlayerPlays_PlayId");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "PlayerPlays",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddPrimaryKey(
                name: "PK_PlayerPlays",
                table: "PlayerPlays",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerPlays_PlayerId",
                table: "PlayerPlays",
                column: "PlayerId");

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerPlays_Players_PlayerId",
                table: "PlayerPlays",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerPlays_Plays_PlayId",
                table: "PlayerPlays",
                column: "PlayId",
                principalTable: "Plays",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
