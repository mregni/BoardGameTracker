using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoardGameTracker.Core.DataStore.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddAuth2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdminGroupValue",
                schema: "auth",
                table: "OidcProviders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RolesClaimType",
                schema: "auth",
                table: "OidcProviders",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdminGroupValue",
                schema: "auth",
                table: "OidcProviders");

            migrationBuilder.DropColumn(
                name: "RolesClaimType",
                schema: "auth",
                table: "OidcProviders");
        }
    }
}
