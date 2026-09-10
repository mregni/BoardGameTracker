using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoardGameTracker.Core.Datastore.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddConfigKeyUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM "Config" older
                USING "Config" newer
                WHERE older."Key" = newer."Key" AND older."Id" < newer."Id";
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Config_Key",
                table: "Config",
                column: "Key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Config_Key",
                table: "Config");
        }
    }
}
