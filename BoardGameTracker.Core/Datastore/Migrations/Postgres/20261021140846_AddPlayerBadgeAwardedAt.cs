using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoardGameTracker.Core.Datastore.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddPlayerBadgeAwardedAt : Migration
    {
        public const string BackfillAwardedAt = """
            WITH thresholds ("BadgeId", "Kind", "Threshold") AS (
                VALUES
                    (1, 'games', 3), (2, 'games', 10), (3, 'games', 20), (4, 'games', 50),
                    (5, 'sessions', 5), (6, 'sessions', 10), (7, 'sessions', 50), (8, 'sessions', 100),
                    (9, 'wins', 3), (10, 'wins', 10), (11, 'wins', 25), (12, 'wins', 50)
            ),
            played AS (
                SELECT ps."PlayerId", ps."Won", s."GameId", s."Start", s."Id"
                FROM "PlayerSessions" ps
                JOIN "Sessions" s ON s."Id" = ps."SessionId"
            ),
            first_plays AS (
                SELECT DISTINCT ON ("PlayerId", "GameId") "PlayerId", "Start", "Id"
                FROM played
                ORDER BY "PlayerId", "GameId", "Start", "Id"
            ),
            milestones AS (
                SELECT 'sessions' AS "Kind", "PlayerId", "Start",
                       ROW_NUMBER() OVER (PARTITION BY "PlayerId" ORDER BY "Start", "Id") AS "Position"
                FROM played
                UNION ALL
                SELECT 'wins', "PlayerId", "Start",
                       ROW_NUMBER() OVER (PARTITION BY "PlayerId" ORDER BY "Start", "Id")
                FROM played
                WHERE "Won"
                UNION ALL
                SELECT 'games', "PlayerId", "Start",
                       ROW_NUMBER() OVER (PARTITION BY "PlayerId" ORDER BY "Start", "Id")
                FROM first_plays
            )
            UPDATE "BadgePlayer" bp
            SET "AwardedAt" = m."Start"
            FROM thresholds t
            JOIN milestones m ON m."Kind" = t."Kind" AND m."Position" = t."Threshold"
            WHERE bp."BadgesId" = t."BadgeId" AND bp."PlayersId" = m."PlayerId" AND bp."AwardedAt" IS NULL
            """;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AwardedAt",
                table: "BadgePlayer",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql(BackfillAwardedAt);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AwardedAt",
                table: "BadgePlayer");
        }
    }
}
