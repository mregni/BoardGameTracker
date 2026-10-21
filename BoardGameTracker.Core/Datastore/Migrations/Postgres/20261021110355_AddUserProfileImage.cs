using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoardGameTracker.Core.Datastore.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddUserProfileImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PictureClaimType",
                schema: "auth",
                table: "OidcProviders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfileImage",
                schema: "auth",
                table: "AspNetUsers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfileImageSource",
                schema: "auth",
                table: "AspNetUsers",
                type: "text",
                nullable: false,
                defaultValue: "None");

            migrationBuilder.AddColumn<string>(
                name: "ProfileImageSourceUrl",
                schema: "auth",
                table: "AspNetUsers",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PictureClaimType",
                schema: "auth",
                table: "OidcProviders");

            migrationBuilder.DropColumn(
                name: "ProfileImage",
                schema: "auth",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ProfileImageSource",
                schema: "auth",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ProfileImageSourceUrl",
                schema: "auth",
                table: "AspNetUsers");
        }
    }
}
