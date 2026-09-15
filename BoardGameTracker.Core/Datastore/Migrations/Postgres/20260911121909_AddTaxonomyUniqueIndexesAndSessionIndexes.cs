using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoardGameTracker.Core.Datastore.Migrations.Postgres;

/// <inheritdoc />
public partial class AddTaxonomyUniqueIndexesAndSessionIndexes : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Sessions_GameId",
            table: "Sessions");

        migrationBuilder.DropIndex(
            name: "IX_GameNightRsvp_GameNightId",
            table: "GameNightRsvp");

        migrationBuilder.Sql("""
            INSERT INTO "GameGameCategory" ("CategoriesId", "GamesId")
            SELECT DISTINCT k."KeepId", j."GamesId"
            FROM "GameGameCategory" j
            JOIN "GameCategories" c ON c."Id" = j."CategoriesId"
            JOIN (SELECT "Name", MIN("Id") AS "KeepId" FROM "GameCategories" GROUP BY "Name") k ON k."Name" = c."Name"
            WHERE c."Id" <> k."KeepId"
            ON CONFLICT DO NOTHING;

            DELETE FROM "GameCategories" c
            USING (SELECT "Name", MIN("Id") AS "KeepId" FROM "GameCategories" GROUP BY "Name") k
            WHERE k."Name" = c."Name" AND c."Id" <> k."KeepId";

            INSERT INTO "GameGameMechanic" ("MechanicsId", "GamesId")
            SELECT DISTINCT k."KeepId", j."GamesId"
            FROM "GameGameMechanic" j
            JOIN "GameMechanics" m ON m."Id" = j."MechanicsId"
            JOIN (SELECT "Name", MIN("Id") AS "KeepId" FROM "GameMechanics" GROUP BY "Name") k ON k."Name" = m."Name"
            WHERE m."Id" <> k."KeepId"
            ON CONFLICT DO NOTHING;

            DELETE FROM "GameMechanics" m
            USING (SELECT "Name", MIN("Id") AS "KeepId" FROM "GameMechanics" GROUP BY "Name") k
            WHERE k."Name" = m."Name" AND m."Id" <> k."KeepId";

            INSERT INTO "GamePerson" ("PeopleId", "GamesId")
            SELECT DISTINCT k."KeepId", j."GamesId"
            FROM "GamePerson" j
            JOIN "People" p ON p."Id" = j."PeopleId"
            JOIN (SELECT "Name", "Type", MIN("Id") AS "KeepId" FROM "People" GROUP BY "Name", "Type") k ON k."Name" = p."Name" AND k."Type" = p."Type"
            WHERE p."Id" <> k."KeepId"
            ON CONFLICT DO NOTHING;

            DELETE FROM "People" p
            USING (SELECT "Name", "Type", MIN("Id") AS "KeepId" FROM "People" GROUP BY "Name", "Type") k
            WHERE k."Name" = p."Name" AND k."Type" = p."Type" AND p."Id" <> k."KeepId";

            DELETE FROM "GameNightRsvp" r
            USING (SELECT "GameNightId", "PlayerId", MAX("Id") AS "KeepId" FROM "GameNightRsvp" GROUP BY "GameNightId", "PlayerId") k
            WHERE k."GameNightId" = r."GameNightId" AND k."PlayerId" = r."PlayerId" AND r."Id" <> k."KeepId";
            """);

        migrationBuilder.CreateIndex(
            name: "IX_Sessions_GameId_Start",
            table: "Sessions",
            columns: new[] { "GameId", "Start" });

        migrationBuilder.CreateIndex(
            name: "IX_People_Name_Type",
            table: "People",
            columns: new[] { "Name", "Type" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_GameNightRsvp_GameNightId_PlayerId",
            table: "GameNightRsvp",
            columns: new[] { "GameNightId", "PlayerId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_GameMechanics_Name",
            table: "GameMechanics",
            column: "Name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_GameCategories_Name",
            table: "GameCategories",
            column: "Name",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Sessions_GameId_Start",
            table: "Sessions");

        migrationBuilder.DropIndex(
            name: "IX_People_Name_Type",
            table: "People");

        migrationBuilder.DropIndex(
            name: "IX_GameNightRsvp_GameNightId_PlayerId",
            table: "GameNightRsvp");

        migrationBuilder.DropIndex(
            name: "IX_GameMechanics_Name",
            table: "GameMechanics");

        migrationBuilder.DropIndex(
            name: "IX_GameCategories_Name",
            table: "GameCategories");

        migrationBuilder.CreateIndex(
            name: "IX_Sessions_GameId",
            table: "Sessions",
            column: "GameId");

        migrationBuilder.CreateIndex(
            name: "IX_GameNightRsvp_GameNightId",
            table: "GameNightRsvp",
            column: "GameNightId");
    }
}
