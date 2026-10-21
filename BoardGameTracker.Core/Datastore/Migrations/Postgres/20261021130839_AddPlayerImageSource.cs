using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoardGameTracker.Core.Datastore.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddPlayerImageSource : Migration
    {
        public const string MarkUploadedImages = """
            UPDATE "Players" SET "ImageSource" = 'Upload'
            WHERE "Image" IS NOT NULL AND "Image" <> ''
            """;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageSource",
                table: "Players",
                type: "text",
                nullable: false,
                defaultValue: "None");

            migrationBuilder.Sql(MarkUploadedImages);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageSource",
                table: "Players");
        }
    }
}
