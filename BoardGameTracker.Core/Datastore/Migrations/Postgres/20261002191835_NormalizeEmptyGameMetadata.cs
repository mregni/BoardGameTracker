using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoardGameTracker.Core.Datastore.Migrations.Postgres;

/// <inheritdoc />
public partial class NormalizeEmptyGameMetadata : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE "Games" SET "MinPlayTime" = NULL, "MaxPlayTime" = NULL
            WHERE "MinPlayTime" <= 0 OR "MaxPlayTime" <= 0;

            UPDATE "Games" SET "MinAge" = NULL
            WHERE "MinAge" <= 0;
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
