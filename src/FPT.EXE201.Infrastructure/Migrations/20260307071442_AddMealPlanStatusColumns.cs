using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FPT.EXE201.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMealPlanStatusColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "completed_weeks",
                table: "meal_plans",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "error_message",
                table: "meal_plans",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "meal_plans",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Pending")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "total_weeks",
                table: "meal_plans",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "completed_weeks",
                table: "meal_plans");

            migrationBuilder.DropColumn(
                name: "error_message",
                table: "meal_plans");

            migrationBuilder.DropColumn(
                name: "status",
                table: "meal_plans");

            migrationBuilder.DropColumn(
                name: "total_weeks",
                table: "meal_plans");
        }
    }
}
