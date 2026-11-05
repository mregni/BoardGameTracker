using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoardGameTracker.Core.Datastore.Migrations.Postgres;

/// <inheritdoc />
public partial class MovePricedPreviouslyOwnedGamesToSold : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE "Games" SET "State" = 4
            WHERE "State" = 2 AND "SoldPrice" IS NOT NULL;
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE "Games" SET "State" = 2
            WHERE "State" = 4;
            """);
    }
}
