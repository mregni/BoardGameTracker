using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoardGameTracker.Core.DataStore.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddPlayColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExpansionPlay_Play_PlaysId",
                table: "ExpansionPlay");

            migrationBuilder.DropForeignKey(
                name: "FK_GameSession_Play_PlayId",
                table: "GameSession");

            migrationBuilder.DropForeignKey(
                name: "FK_Image_Play_PlayId",
                table: "Image");

            migrationBuilder.DropForeignKey(
                name: "FK_Play_Games_GameId",
                table: "Play");

            migrationBuilder.DropForeignKey(
                name: "FK_Play_Location_LocationId",
                table: "Play");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayerPlay_Play_PlayId",
                table: "PlayerPlay");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Play",
                table: "Play");

            migrationBuilder.DropColumn(
                name: "Result",
                table: "PlayerPlay");

            migrationBuilder.RenameTable(
                name: "Play",
                newName: "Plays");

            migrationBuilder.RenameIndex(
                name: "IX_Play_LocationId",
                table: "Plays",
                newName: "IX_Plays_LocationId");

            migrationBuilder.RenameIndex(
                name: "IX_Play_GameId",
                table: "Plays",
                newName: "IX_Plays_GameId");

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

            migrationBuilder.AddColumn<bool>(
                name: "FirstPlay",
                table: "PlayerPlay",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Team",
                table: "PlayerPlay",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Won",
                table: "PlayerPlay",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Plays",
                table: "Plays",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ExpansionPlay_Plays_PlaysId",
                table: "ExpansionPlay",
                column: "PlaysId",
                principalTable: "Plays",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GameSession_Plays_PlayId",
                table: "GameSession",
                column: "PlayId",
                principalTable: "Plays",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Image_Plays_PlayId",
                table: "Image",
                column: "PlayId",
                principalTable: "Plays",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerPlay_Plays_PlayId",
                table: "PlayerPlay",
                column: "PlayId",
                principalTable: "Plays",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Plays_Games_GameId",
                table: "Plays",
                column: "GameId",
                principalTable: "Games",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Plays_Location_LocationId",
                table: "Plays",
                column: "LocationId",
                principalTable: "Location",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExpansionPlay_Plays_PlaysId",
                table: "ExpansionPlay");

            migrationBuilder.DropForeignKey(
                name: "FK_GameSession_Plays_PlayId",
                table: "GameSession");

            migrationBuilder.DropForeignKey(
                name: "FK_Image_Plays_PlayId",
                table: "Image");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayerPlay_Plays_PlayId",
                table: "PlayerPlay");

            migrationBuilder.DropForeignKey(
                name: "FK_Plays_Games_GameId",
                table: "Plays");

            migrationBuilder.DropForeignKey(
                name: "FK_Plays_Location_LocationId",
                table: "Plays");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Plays",
                table: "Plays");

            migrationBuilder.DropColumn(
                name: "CharacterName",
                table: "PlayerPlay");

            migrationBuilder.DropColumn(
                name: "Color",
                table: "PlayerPlay");

            migrationBuilder.DropColumn(
                name: "FirstPlay",
                table: "PlayerPlay");

            migrationBuilder.DropColumn(
                name: "Team",
                table: "PlayerPlay");

            migrationBuilder.DropColumn(
                name: "Won",
                table: "PlayerPlay");

            migrationBuilder.RenameTable(
                name: "Plays",
                newName: "Play");

            migrationBuilder.RenameIndex(
                name: "IX_Plays_LocationId",
                table: "Play",
                newName: "IX_Play_LocationId");

            migrationBuilder.RenameIndex(
                name: "IX_Plays_GameId",
                table: "Play",
                newName: "IX_Play_GameId");

            migrationBuilder.AddColumn<int>(
                name: "Result",
                table: "PlayerPlay",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Play",
                table: "Play",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ExpansionPlay_Play_PlaysId",
                table: "ExpansionPlay",
                column: "PlaysId",
                principalTable: "Play",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GameSession_Play_PlayId",
                table: "GameSession",
                column: "PlayId",
                principalTable: "Play",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Image_Play_PlayId",
                table: "Image",
                column: "PlayId",
                principalTable: "Play",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Play_Games_GameId",
                table: "Play",
                column: "GameId",
                principalTable: "Games",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Play_Location_LocationId",
                table: "Play",
                column: "LocationId",
                principalTable: "Location",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerPlay_Play_PlayId",
                table: "PlayerPlay",
                column: "PlayId",
                principalTable: "Play",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
