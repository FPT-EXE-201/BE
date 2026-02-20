using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FPT.EXE201.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAutoFillConfirmFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "auto_fill_result",
                table: "ocr_results",
                type: "JSON",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "confirmed_at",
                table: "ocr_results",
                type: "DATETIME(6)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "confirmed_by",
                table: "ocr_results",
                type: "CHAR(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<string>(
                name: "confirmed_json",
                table: "ocr_results",
                type: "JSON",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "auto_fill_result",
                table: "ocr_results");

            migrationBuilder.DropColumn(
                name: "confirmed_at",
                table: "ocr_results");

            migrationBuilder.DropColumn(
                name: "confirmed_by",
                table: "ocr_results");

            migrationBuilder.DropColumn(
                name: "confirmed_json",
                table: "ocr_results");
        }
    }
}
