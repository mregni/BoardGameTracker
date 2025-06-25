using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoardGameTracker.Core.DataStore.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class UpdatingExpansions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Expansion_Games_BaseGameId",
                table: "Expansion");

            migrationBuilder.DropIndex(
                name: "IX_Expansion_BaseGameId",
                table: "Expansion");

            migrationBuilder.DropColumn(
                name: "AdditionDate",
                table: "Expansion");

            migrationBuilder.DropColumn(
                name: "BaseGameId",
                table: "Expansion");

            migrationBuilder.DropColumn(
                name: "BuyingPrice",
                table: "Expansion");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Expansion");

            migrationBuilder.DropColumn(
                name: "Image",
                table: "Expansion");

            migrationBuilder.DropColumn(
                name: "MaxPlayTime",
                table: "Expansion");

            migrationBuilder.DropColumn(
                name: "MaxPlayers",
                table: "Expansion");

            migrationBuilder.DropColumn(
                name: "MinAge",
                table: "Expansion");

            migrationBuilder.DropColumn(
                name: "MinPlayTime",
                table: "Expansion");

            migrationBuilder.DropColumn(
                name: "MinPlayers",
                table: "Expansion");

            migrationBuilder.DropColumn(
                name: "Rating",
                table: "Expansion");

            migrationBuilder.DropColumn(
                name: "SoldPrice",
                table: "Expansion");

            migrationBuilder.DropColumn(
                name: "State",
                table: "Expansion");

            migrationBuilder.DropColumn(
                name: "Weight",
                table: "Expansion");

            migrationBuilder.RenameColumn(
                name: "YearPublished",
                table: "Expansion",
                newName: "GameId");

            migrationBuilder.AlterColumn<int>(
                name: "BggId",
                table: "Expansion",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Expansion_GameId",
                table: "Expansion",
                column: "GameId");

            migrationBuilder.AddForeignKey(
                name: "FK_Expansion_Games_GameId",
                table: "Expansion",
                column: "GameId",
                principalTable: "Games",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Expansion_Games_GameId",
                table: "Expansion");

            migrationBuilder.DropIndex(
                name: "IX_Expansion_GameId",
                table: "Expansion");

            migrationBuilder.RenameColumn(
                name: "GameId",
                table: "Expansion",
                newName: "YearPublished");

            migrationBuilder.AlterColumn<int>(
                name: "BggId",
                table: "Expansion",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<DateTime>(
                name: "AdditionDate",
                table: "Expansion",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BaseGameId",
                table: "Expansion",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "BuyingPrice",
                table: "Expansion",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Expansion",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Image",
                table: "Expansion",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxPlayTime",
                table: "Expansion",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxPlayers",
                table: "Expansion",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinAge",
                table: "Expansion",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinPlayTime",
                table: "Expansion",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinPlayers",
                table: "Expansion",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Rating",
                table: "Expansion",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "SoldPrice",
                table: "Expansion",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "State",
                table: "Expansion",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "Weight",
                table: "Expansion",
                type: "double precision",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Expansion_BaseGameId",
                table: "Expansion",
                column: "BaseGameId");

            migrationBuilder.AddForeignKey(
                name: "FK_Expansion_Games_BaseGameId",
                table: "Expansion",
                column: "BaseGameId",
                principalTable: "Games",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
