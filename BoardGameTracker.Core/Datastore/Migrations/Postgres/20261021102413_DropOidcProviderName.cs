using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoardGameTracker.Core.Datastore.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class DropOidcProviderName : Migration
    {
        public const string BackfillIssuers = """
            UPDATE auth."ExternalLogins" AS e
            SET "Issuer" = rtrim(btrim(p."Authority"), '/')
            FROM auth."OidcProviders" AS p
            WHERE e."Provider" = p."Name" AND e."Issuer" IS NULL
            """;

        public const string RelabelLinks = """
            UPDATE auth."ExternalLogins" AS e
            SET "Provider" = p."DisplayName"
            FROM auth."OidcProviders" AS p
            WHERE e."Provider" = p."Name"
            """;

        public const string RestoreNames = """
            UPDATE auth."OidcProviders" SET "Name" = 'provider-' || "Id"
            """;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(BackfillIssuers);
            migrationBuilder.Sql(RelabelLinks);

            migrationBuilder.DropIndex(
                name: "IX_OidcProviders_Name",
                schema: "auth",
                table: "OidcProviders");

            migrationBuilder.DropColumn(
                name: "Name",
                schema: "auth",
                table: "OidcProviders");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Name",
                schema: "auth",
                table: "OidcProviders",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(RestoreNames);

            migrationBuilder.CreateIndex(
                name: "IX_OidcProviders_Name",
                schema: "auth",
                table: "OidcProviders",
                column: "Name",
                unique: true);
        }
    }
}
