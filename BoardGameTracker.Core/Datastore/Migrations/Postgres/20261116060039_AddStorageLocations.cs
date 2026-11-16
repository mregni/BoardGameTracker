using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BoardGameTracker.Core.Datastore.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddStorageLocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BoxOrientation",
                table: "Games",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StorageLocationId",
                table: "Games",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StoragePosition",
                table: "Games",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StoredAtLocationId",
                table: "Games",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BoxOrientation",
                table: "Expansions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StorageLocationId",
                table: "Expansions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StoragePosition",
                table: "Expansions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StoredAtLocationId",
                table: "Expansions",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StorageLocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Layout = table.Column<int>(type: "integer", nullable: false),
                    Rows = table.Column<int>(type: "integer", nullable: true),
                    Columns = table.Column<int>(type: "integer", nullable: true),
                    Row = table.Column<int>(type: "integer", nullable: true),
                    Column = table.Column<int>(type: "integer", nullable: true),
                    LocationId = table.Column<int>(type: "integer", nullable: false),
                    ParentId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorageLocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StorageLocations_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StorageLocations_StorageLocations_ParentId",
                        column: x => x.ParentId,
                        principalTable: "StorageLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Games_StorageLocationId",
                table: "Games",
                column: "StorageLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Games_StoredAtLocationId",
                table: "Games",
                column: "StoredAtLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Expansions_StorageLocationId",
                table: "Expansions",
                column: "StorageLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Expansions_StoredAtLocationId",
                table: "Expansions",
                column: "StoredAtLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_StorageLocations_LocationId",
                table: "StorageLocations",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_StorageLocations_ParentId",
                table: "StorageLocations",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Expansions_Locations_StoredAtLocationId",
                table: "Expansions",
                column: "StoredAtLocationId",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Expansions_StorageLocations_StorageLocationId",
                table: "Expansions",
                column: "StorageLocationId",
                principalTable: "StorageLocations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Games_Locations_StoredAtLocationId",
                table: "Games",
                column: "StoredAtLocationId",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Games_StorageLocations_StorageLocationId",
                table: "Games",
                column: "StorageLocationId",
                principalTable: "StorageLocations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Expansions_Locations_StoredAtLocationId",
                table: "Expansions");

            migrationBuilder.DropForeignKey(
                name: "FK_Expansions_StorageLocations_StorageLocationId",
                table: "Expansions");

            migrationBuilder.DropForeignKey(
                name: "FK_Games_Locations_StoredAtLocationId",
                table: "Games");

            migrationBuilder.DropForeignKey(
                name: "FK_Games_StorageLocations_StorageLocationId",
                table: "Games");

            migrationBuilder.DropTable(
                name: "StorageLocations");

            migrationBuilder.DropIndex(
                name: "IX_Games_StorageLocationId",
                table: "Games");

            migrationBuilder.DropIndex(
                name: "IX_Games_StoredAtLocationId",
                table: "Games");

            migrationBuilder.DropIndex(
                name: "IX_Expansions_StorageLocationId",
                table: "Expansions");

            migrationBuilder.DropIndex(
                name: "IX_Expansions_StoredAtLocationId",
                table: "Expansions");

            migrationBuilder.DropColumn(
                name: "BoxOrientation",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "StorageLocationId",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "StoragePosition",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "StoredAtLocationId",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "BoxOrientation",
                table: "Expansions");

            migrationBuilder.DropColumn(
                name: "StorageLocationId",
                table: "Expansions");

            migrationBuilder.DropColumn(
                name: "StoragePosition",
                table: "Expansions");

            migrationBuilder.DropColumn(
                name: "StoredAtLocationId",
                table: "Expansions");
        }
    }
}
