using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FPT.EXE201.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGoogleAuthToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "auth_provider",
                table: "users",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "local")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "google_id",
                table: "users",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "uk_users_google_id",
                table: "users",
                column: "google_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uk_users_google_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "auth_provider",
                table: "users");

            migrationBuilder.DropColumn(
                name: "google_id",
                table: "users");
        }
    }
}
