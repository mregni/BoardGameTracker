using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoardGameTracker.Core.Datastore.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddExpansionStatePriceAndImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AdditionDate",
                table: "Expansions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BuyingPrice",
                table: "Expansions",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Image",
                table: "Expansions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SoldPrice",
                table: "Expansions",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "State",
                table: "Expansions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                UPDATE "Expansions" SET "State" = 1;
                UPDATE "Expansions" AS e SET "AdditionDate" = g."AdditionDate"
                FROM "Games" AS g
                WHERE g."Id" = e."GameId";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdditionDate",
                table: "Expansions");

            migrationBuilder.DropColumn(
                name: "BuyingPrice",
                table: "Expansions");

            migrationBuilder.DropColumn(
                name: "Image",
                table: "Expansions");

            migrationBuilder.DropColumn(
                name: "SoldPrice",
                table: "Expansions");

            migrationBuilder.DropColumn(
                name: "State",
                table: "Expansions");
        }
    }
}
