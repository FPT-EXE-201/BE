using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FPT.EXE201.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixAiLogResponsePayloadColumnType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "response_payload",
                table: "ai_request_logs",
                type: "LONGTEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "JSON",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "response_payload",
                table: "ai_request_logs",
                type: "JSON",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "LONGTEXT",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
