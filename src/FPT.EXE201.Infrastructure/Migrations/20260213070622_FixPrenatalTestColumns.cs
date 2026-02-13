using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FPT.EXE201.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixPrenatalTestColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_prenatal_tests_date",
                table: "prenatal_tests");

            // Rename test_at → test_date (preserve data)
            migrationBuilder.RenameColumn(
                name: "test_at",
                table: "prenatal_tests",
                newName: "test_date");

            // Change type from DATETIME → DATE
            migrationBuilder.AlterColumn<DateOnly>(
                name: "test_date",
                table: "prenatal_tests",
                type: "DATE",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "DATETIME");

            // Rename result_text → notes (preserve data)
            migrationBuilder.RenameColumn(
                name: "result_text",
                table: "prenatal_tests",
                newName: "notes");

            // Rename result_json → image_urls (preserve data, type already JSON)
            migrationBuilder.RenameColumn(
                name: "result_json",
                table: "prenatal_tests",
                newName: "image_urls");

            migrationBuilder.UpdateData(
                table: "ref_test_type_translations",
                keyColumns: new[] { "lang_code", "test_type_id" },
                keyValues: new object[] { "en", new Guid("b0000001-0000-0000-0000-000000000001") },
                columns: new[] { "description", "name" },
                values: new object[] { "Comprehensive blood chemistry (glucose, lipids, liver, kidney, electrolytes)", "Blood Biochemistry Panel" });

            migrationBuilder.UpdateData(
                table: "ref_test_type_translations",
                keyColumns: new[] { "lang_code", "test_type_id" },
                keyValues: new object[] { "vi", new Guid("b0000001-0000-0000-0000-000000000001") },
                columns: new[] { "description", "name" },
                values: new object[] { "Kiểm tra các chỉ số hoá sinh trong máu (đường, mỡ, gan, thận, điện giải)", "Xét nghiệm hoá sinh máu" });

            migrationBuilder.UpdateData(
                table: "ref_test_types",
                keyColumn: "id",
                keyValue: new Guid("b0000001-0000-0000-0000-000000000001"),
                column: "code",
                value: "BIOCHEMISTRY");

            migrationBuilder.CreateIndex(
                name: "idx_prenatal_tests_date",
                table: "prenatal_tests",
                column: "test_date");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_prenatal_tests_date",
                table: "prenatal_tests");

            migrationBuilder.RenameColumn(
                name: "notes",
                table: "prenatal_tests",
                newName: "result_text");

            migrationBuilder.RenameColumn(
                name: "image_urls",
                table: "prenatal_tests",
                newName: "result_json");

            migrationBuilder.AlterColumn<DateTime>(
                name: "test_date",
                table: "prenatal_tests",
                type: "DATETIME",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "DATE");

            migrationBuilder.RenameColumn(
                name: "test_date",
                table: "prenatal_tests",
                newName: "test_at");

            migrationBuilder.UpdateData(
                table: "ref_test_type_translations",
                keyColumns: new[] { "lang_code", "test_type_id" },
                keyValues: new object[] { "en", new Guid("b0000001-0000-0000-0000-000000000001") },
                columns: new[] { "description", "name" },
                values: new object[] { "Measures glucose level in blood", "Blood Glucose Test" });

            migrationBuilder.UpdateData(
                table: "ref_test_type_translations",
                keyColumns: new[] { "lang_code", "test_type_id" },
                keyValues: new object[] { "vi", new Guid("b0000001-0000-0000-0000-000000000001") },
                columns: new[] { "description", "name" },
                values: new object[] { "Kiểm tra nồng độ glucose trong máu", "Xét nghiệm đường huyết" });

            migrationBuilder.UpdateData(
                table: "ref_test_types",
                keyColumn: "id",
                keyValue: new Guid("b0000001-0000-0000-0000-000000000001"),
                column: "code",
                value: "BLOOD_GLUCOSE");

            migrationBuilder.CreateIndex(
                name: "idx_prenatal_tests_date",
                table: "prenatal_tests",
                column: "test_at");
        }
    }
}
