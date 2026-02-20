using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FPT.EXE201.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class IncreaseMaxOutputTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "ai_prompt_templates",
                keyColumn: "id",
                keyValue: new Guid("a1000001-0000-0000-0000-000000000001"),
                column: "max_output_tokens",
                value: 16384);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "ai_prompt_templates",
                keyColumn: "id",
                keyValue: new Guid("a1000001-0000-0000-0000-000000000001"),
                column: "max_output_tokens",
                value: 8192);
        }
    }
}
