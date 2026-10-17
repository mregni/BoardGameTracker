using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BoardGameTracker.Core.Datastore.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class OidcIssuerKeyedLoginsAndSessions : Migration
    {
        public const string BackfillIssuers = """
            UPDATE auth."ExternalLogins" AS e
            SET "Issuer" = rtrim(btrim(p."Authority"), '/')
            FROM auth."OidcProviders" AS p
            WHERE e."Provider" = p."Name" AND e."Issuer" IS NULL
            """;

        public const string BackfillRoleProvenance = """
            UPDATE auth."AspNetUsers" AS u
            SET "RoleManagedByIdentityProvider" = true
            WHERE u."PasswordHash" IS NULL
              AND EXISTS (SELECT 1 FROM auth."ExternalLogins" AS e WHERE e."UserId" = u."Id")
            """;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ExternalLogins_Provider_ProviderKey",
                schema: "auth",
                table: "ExternalLogins");

            migrationBuilder.AddColumn<string>(
                name: "DefaultRole",
                schema: "auth",
                table: "OidcProviders",
                type: "text",
                nullable: false,
                defaultValue: "User");

            migrationBuilder.AddColumn<string>(
                name: "Issuer",
                schema: "auth",
                table: "ExternalLogins",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RoleManagedByIdentityProvider",
                schema: "auth",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(BackfillIssuers);
            migrationBuilder.Sql(BackfillRoleProvenance);

            migrationBuilder.CreateTable(
                name: "OidcSessions",
                schema: "auth",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    KeyHash = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ProviderId = table.Column<int>(type: "integer", nullable: false),
                    IdToken = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OidcSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OidcSessions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "auth",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExternalLogins_Issuer_ProviderKey",
                schema: "auth",
                table: "ExternalLogins",
                columns: new[] { "Issuer", "ProviderKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OidcSessions_KeyHash",
                schema: "auth",
                table: "OidcSessions",
                column: "KeyHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OidcSessions_UserId",
                schema: "auth",
                table: "OidcSessions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OidcSessions",
                schema: "auth");

            migrationBuilder.DropIndex(
                name: "IX_ExternalLogins_Issuer_ProviderKey",
                schema: "auth",
                table: "ExternalLogins");

            migrationBuilder.DropColumn(
                name: "DefaultRole",
                schema: "auth",
                table: "OidcProviders");

            migrationBuilder.DropColumn(
                name: "Issuer",
                schema: "auth",
                table: "ExternalLogins");

            migrationBuilder.DropColumn(
                name: "RoleManagedByIdentityProvider",
                schema: "auth",
                table: "AspNetUsers");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalLogins_Provider_ProviderKey",
                schema: "auth",
                table: "ExternalLogins",
                columns: new[] { "Provider", "ProviderKey" },
                unique: true);
        }
    }
}
