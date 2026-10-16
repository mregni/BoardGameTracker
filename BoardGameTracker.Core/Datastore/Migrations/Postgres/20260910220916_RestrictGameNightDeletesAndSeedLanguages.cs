using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BoardGameTracker.Core.Datastore.Migrations.Postgres;

/// <inheritdoc />
public partial class RestrictGameNightDeletesAndSeedLanguages : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_GameNights_Locations_LocationId",
            table: "GameNights");

        migrationBuilder.DropForeignKey(
            name: "FK_GameNights_Players_HostId",
            table: "GameNights");

        migrationBuilder.InsertData(
            table: "Languages",
            columns: new[] { "Id", "Key", "TranslationKey" },
            values: new object[,]
            {
                { 3, "nl-nl", "dutch" },
                { 4, "es-es", "spanish" }
            });

        migrationBuilder.AddForeignKey(
            name: "FK_GameNights_Locations_LocationId",
            table: "GameNights",
            column: "LocationId",
            principalTable: "Locations",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_GameNights_Players_HostId",
            table: "GameNights",
            column: "HostId",
            principalTable: "Players",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_GameNights_Locations_LocationId",
            table: "GameNights");

        migrationBuilder.DropForeignKey(
            name: "FK_GameNights_Players_HostId",
            table: "GameNights");

        migrationBuilder.DeleteData(
            table: "Languages",
            keyColumn: "Id",
            keyValue: 3);

        migrationBuilder.DeleteData(
            table: "Languages",
            keyColumn: "Id",
            keyValue: 4);

        migrationBuilder.AddForeignKey(
            name: "FK_GameNights_Locations_LocationId",
            table: "GameNights",
            column: "LocationId",
            principalTable: "Locations",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_GameNights_Players_HostId",
            table: "GameNights",
            column: "HostId",
            principalTable: "Players",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }
}
