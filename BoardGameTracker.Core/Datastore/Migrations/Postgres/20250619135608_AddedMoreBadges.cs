using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BoardGameTracker.Core.DataStore.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddedMoreBadges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Badges",
                columns: new[] { "Id", "DescriptionKey", "Image", "Level", "TitleKey", "Type" },
                values: new object[,]
                {
                    { 5, "sessions.green.description", "sessions-green.png", "Green", "sessions.green.title", "Sessions" },
                    { 6, "sessions.blue.description", "sessions-blue.png", "Blue", "sessions.blue.title", "Sessions" },
                    { 7, "sessions.red.description", "sessions-red.png", "Red", "sessions.red.title", "Sessions" },
                    { 8, "sessions.gold.description", "sessions-gold.png", "Gold", "sessions.gold.title", "Sessions" },
                    { 9, "wins.green.description", "wins-green.png", "Green", "wins.green.title", "Wins" },
                    { 10, "wins.blue.description", "wins-blue.png", "Blue", "wins.blue.title", "Wins" },
                    { 11, "wins.red.description", "wins-red.png", "Red", "wins.red.title", "Wins" },
                    { 12, "wins.gold.description", "wins-gold.png", "Gold", "wins.gold.title", "Wins" },
                    { 13, "duration.green.description", "duration-green.png", "Green", "duration.green.title", "Duration" },
                    { 14, "duration.blue.description", "duration-blue.png", "Blue", "duration.blue.title", "Duration" },
                    { 15, "duration.red.description", "duration-red.png", "Red", "duration.red.title", "Duration" },
                    { 16, "duration.gold.description", "duration-gold.png", "Gold", "duration.gold.title", "Duration" },
                    { 17, "win-percentage.green.description", "win-percentage-green.png", "Green", "win-percentage.green.title", "WinPercentage" },
                    { 18, "win-percentage.blue.description", "win-percentage-blue.png", "Blue", "win-percentage.blue.title", "WinPercentage" },
                    { 19, "win-percentage.red.description", "win-percentage-red.png", "Red", "win-percentage.red.title", "WinPercentage" },
                    { 20, "win-percentage.gold.description", "win-percentage-gold.png", "Gold", "win-percentage.gold.title", "WinPercentage" },
                    { 21, "solo-specialist.green.description", "solo-specialist-green.png", "Green", "solo-specialist.green.title", "SoloSpecialist" },
                    { 22, "solo-specialist.blue.description", "solo-specialist-blue.png", "Blue", "solo-specialist.blue.title", "SoloSpecialist" },
                    { 23, "solo-specialist.red.description", "solo-specialist-red.png", "Red", "solo-specialist.red.title", "SoloSpecialist" },
                    { 24, "solo-specialist.gold.description", "solo-specialist-gold.png", "Gold", "solo-specialist.gold.title", "SoloSpecialist" },
                    { 25, "winning-streak.green.description", "winning-streak-green.png", "Green", "winning-streak.green.title", "WinningStreak" },
                    { 26, "winning-streak.blue.description", "winning-streak-blue.png", "Blue", "winning-streak.blue.title", "WinningStreak" },
                    { 27, "winning-streak.red.description", "winning-streak-red.png", "Red", "winning-streak.red.title", "WinningStreak" },
                    { 28, "winning-streak.gold.description", "winning-streak-gold.png", "Gold", "winning-streak.gold.title", "WinningStreak" },
                    { 29, "social-player.green.description", "social-player-green.png", "Green", "social-player.green.title", "SocialPlayer" },
                    { 30, "social-player.blue.description", "social-player-blue.png", "Blue", "social-player.blue.title", "SocialPlayer" },
                    { 31, "social-player.red.description", "social-player-red.png", "Red", "social-player.red.title", "SocialPlayer" },
                    { 32, "social-player.gold.description", "social-player-gold.png", "Gold", "social-player.gold.title", "SocialPlayer" },
                    { 33, "close-win.description", "close-win.png", null, "close-win.title", "CloseWin" },
                    { 34, "close-loss.description", "close-loss.png", null, "close-loss.title", "CLoseLoss" },
                    { 35, "marathon-runner.description", "close-loss.png", null, "marathon-runner.title", "MarathonRunner" },
                    { 36, "first-try.description", "first-try.png", null, "first-try.title", "FirstTry" },
                    { 37, "learning-curve.description", "learning-curve.png", null, "learning-curve.title", "LearningCurve" },
                    { 38, "monthly-goal.description", "monthly-goal.png", null, "monthly-goal.title", "MonthlyGoal" },
                    { 39, "consistent-schedule.description", "consistent-schedule.png", null, "consistent-schedule.title", "ConsistentSchedule" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 25);

            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 26);

            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 27);

            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 28);

            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 29);

            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 30);

            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 31);

            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 32);

            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 33);

            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 34);

            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 35);

            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 36);

            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 37);

            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 38);

            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 39);
        }
    }
}
