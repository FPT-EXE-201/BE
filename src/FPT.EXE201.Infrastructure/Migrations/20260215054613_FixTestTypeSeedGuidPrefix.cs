using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FPT.EXE201.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixTestTypeSeedGuidPrefix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Step 1: Update TestType GUIDs (b0000001 → b0000002) using raw SQL ──
            // Disable FK checks to allow PK/FK GUID updates atomically
            migrationBuilder.Sql("SET FOREIGN_KEY_CHECKS = 0;");

            // Update ref_test_type_translations FK
            for (int i = 1; i <= 9; i++)
            {
                var suffix = i.ToString("x12");  // 000000000001 ... 000000000009
                migrationBuilder.Sql(
                    $"UPDATE ref_test_type_translations SET test_type_id = 'b0000002-0000-0000-0000-{suffix}' WHERE test_type_id = 'b0000001-0000-0000-0000-{suffix}';");
            }
            migrationBuilder.Sql(
                "UPDATE ref_test_type_translations SET test_type_id = 'b0000002-0000-0000-0000-00000000000a' WHERE test_type_id = 'b0000001-0000-0000-0000-00000000000a';");

            // Update prenatal_tests FK (if any rows reference old GUIDs)
            for (int i = 1; i <= 9; i++)
            {
                var suffix = i.ToString("x12");
                migrationBuilder.Sql(
                    $"UPDATE prenatal_tests SET test_type_id = 'b0000002-0000-0000-0000-{suffix}' WHERE test_type_id = 'b0000001-0000-0000-0000-{suffix}';");
            }
            migrationBuilder.Sql(
                "UPDATE prenatal_tests SET test_type_id = 'b0000002-0000-0000-0000-00000000000a' WHERE test_type_id = 'b0000001-0000-0000-0000-00000000000a';");

            // Update ref_test_types PK
            for (int i = 1; i <= 9; i++)
            {
                var suffix = i.ToString("x12");
                migrationBuilder.Sql(
                    $"UPDATE ref_test_types SET id = 'b0000002-0000-0000-0000-{suffix}' WHERE id = 'b0000001-0000-0000-0000-{suffix}';");
            }
            migrationBuilder.Sql(
                "UPDATE ref_test_types SET id = 'b0000002-0000-0000-0000-00000000000a' WHERE id = 'b0000001-0000-0000-0000-00000000000a';");

            migrationBuilder.Sql("SET FOREIGN_KEY_CHECKS = 1;");

            // ── Step 2: Insert 6 new DocumentTypes (Week 5.5) ──
            migrationBuilder.InsertData(
                table: "ref_document_types",
                columns: new[] { "id", "code", "created_at", "deleted_at", "is_active", "updated_at" },
                values: new object[,]
                {
                    { new Guid("b0000001-0000-0000-0000-000000000009"), "HIV_TEST", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("b0000001-0000-0000-0000-00000000000a"), "HEPATITIS_B_TEST", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("b0000001-0000-0000-0000-00000000000b"), "THYROID_TEST", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("b0000001-0000-0000-0000-00000000000c"), "GLUCOSE_TEST", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("b0000001-0000-0000-0000-00000000000d"), "CBC_TEST", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("b0000001-0000-0000-0000-00000000000e"), "NT_SCAN", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "ref_document_type_translations",
                columns: new[] { "document_type_id", "language_code", "description", "display_name" },
                values: new object[,]
                {
                    { new Guid("b0000001-0000-0000-0000-000000000009"), "en", "HIV screening test result", "HIV Test" },
                    { new Guid("b0000001-0000-0000-0000-000000000009"), "vi", "Kết quả xét nghiệm HIV", "Xét nghiệm HIV" },
                    { new Guid("b0000001-0000-0000-0000-00000000000a"), "en", "Hepatitis B (HBsAg) test result", "Hepatitis B Test" },
                    { new Guid("b0000001-0000-0000-0000-00000000000a"), "vi", "Kết quả xét nghiệm viêm gan B (HBsAg)", "Xét nghiệm viêm gan B" },
                    { new Guid("b0000001-0000-0000-0000-00000000000b"), "en", "TSH/thyroid function test result", "Thyroid Test" },
                    { new Guid("b0000001-0000-0000-0000-00000000000b"), "vi", "Kết quả xét nghiệm TSH/tuyến giáp", "Xét nghiệm tuyến giáp" },
                    { new Guid("b0000001-0000-0000-0000-00000000000c"), "en", "Oral glucose tolerance test (OGTT) result", "Glucose Test" },
                    { new Guid("b0000001-0000-0000-0000-00000000000c"), "vi", "Kết quả nghiệm pháp dung nạp glucose (OGTT)", "Xét nghiệm đường huyết" },
                    { new Guid("b0000001-0000-0000-0000-00000000000d"), "en", "Complete blood count (CBC) test result", "CBC Test" },
                    { new Guid("b0000001-0000-0000-0000-00000000000d"), "vi", "Kết quả xét nghiệm công thức máu toàn phần (CBC)", "Xét nghiệm công thức máu" },
                    { new Guid("b0000001-0000-0000-0000-00000000000e"), "en", "Nuchal translucency scan result", "NT Scan" },
                    { new Guid("b0000001-0000-0000-0000-00000000000e"), "vi", "Kết quả siêu âm đo độ mờ da gáy (NT scan)", "Đo độ mờ da gáy" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ── Step 1: Remove 6 new DocumentType translations + types ──
            migrationBuilder.DeleteData(
                table: "ref_document_type_translations",
                keyColumns: new[] { "document_type_id", "language_code" },
                keyValues: new object[] { new Guid("b0000001-0000-0000-0000-000000000009"), "en" });

            migrationBuilder.DeleteData(
                table: "ref_document_type_translations",
                keyColumns: new[] { "document_type_id", "language_code" },
                keyValues: new object[] { new Guid("b0000001-0000-0000-0000-000000000009"), "vi" });

            migrationBuilder.DeleteData(
                table: "ref_document_type_translations",
                keyColumns: new[] { "document_type_id", "language_code" },
                keyValues: new object[] { new Guid("b0000001-0000-0000-0000-00000000000a"), "en" });

            migrationBuilder.DeleteData(
                table: "ref_document_type_translations",
                keyColumns: new[] { "document_type_id", "language_code" },
                keyValues: new object[] { new Guid("b0000001-0000-0000-0000-00000000000a"), "vi" });

            migrationBuilder.DeleteData(
                table: "ref_document_type_translations",
                keyColumns: new[] { "document_type_id", "language_code" },
                keyValues: new object[] { new Guid("b0000001-0000-0000-0000-00000000000b"), "en" });

            migrationBuilder.DeleteData(
                table: "ref_document_type_translations",
                keyColumns: new[] { "document_type_id", "language_code" },
                keyValues: new object[] { new Guid("b0000001-0000-0000-0000-00000000000b"), "vi" });

            migrationBuilder.DeleteData(
                table: "ref_document_type_translations",
                keyColumns: new[] { "document_type_id", "language_code" },
                keyValues: new object[] { new Guid("b0000001-0000-0000-0000-00000000000c"), "en" });

            migrationBuilder.DeleteData(
                table: "ref_document_type_translations",
                keyColumns: new[] { "document_type_id", "language_code" },
                keyValues: new object[] { new Guid("b0000001-0000-0000-0000-00000000000c"), "vi" });

            migrationBuilder.DeleteData(
                table: "ref_document_type_translations",
                keyColumns: new[] { "document_type_id", "language_code" },
                keyValues: new object[] { new Guid("b0000001-0000-0000-0000-00000000000d"), "en" });

            migrationBuilder.DeleteData(
                table: "ref_document_type_translations",
                keyColumns: new[] { "document_type_id", "language_code" },
                keyValues: new object[] { new Guid("b0000001-0000-0000-0000-00000000000d"), "vi" });

            migrationBuilder.DeleteData(
                table: "ref_document_type_translations",
                keyColumns: new[] { "document_type_id", "language_code" },
                keyValues: new object[] { new Guid("b0000001-0000-0000-0000-00000000000e"), "en" });

            migrationBuilder.DeleteData(
                table: "ref_document_type_translations",
                keyColumns: new[] { "document_type_id", "language_code" },
                keyValues: new object[] { new Guid("b0000001-0000-0000-0000-00000000000e"), "vi" });

            migrationBuilder.DeleteData(
                table: "ref_document_types",
                keyColumn: "id",
                keyValue: new Guid("b0000001-0000-0000-0000-000000000009"));

            migrationBuilder.DeleteData(
                table: "ref_document_types",
                keyColumn: "id",
                keyValue: new Guid("b0000001-0000-0000-0000-00000000000a"));

            migrationBuilder.DeleteData(
                table: "ref_document_types",
                keyColumn: "id",
                keyValue: new Guid("b0000001-0000-0000-0000-00000000000b"));

            migrationBuilder.DeleteData(
                table: "ref_document_types",
                keyColumn: "id",
                keyValue: new Guid("b0000001-0000-0000-0000-00000000000c"));

            migrationBuilder.DeleteData(
                table: "ref_document_types",
                keyColumn: "id",
                keyValue: new Guid("b0000001-0000-0000-0000-00000000000d"));

            migrationBuilder.DeleteData(
                table: "ref_document_types",
                keyColumn: "id",
                keyValue: new Guid("b0000001-0000-0000-0000-00000000000e"));

            // ── Step 2: Revert TestType GUIDs (b0000002 → b0000001) ──
            migrationBuilder.Sql("SET FOREIGN_KEY_CHECKS = 0;");

            for (int i = 1; i <= 9; i++)
            {
                var suffix = i.ToString("x12");
                migrationBuilder.Sql(
                    $"UPDATE ref_test_type_translations SET test_type_id = 'b0000001-0000-0000-0000-{suffix}' WHERE test_type_id = 'b0000002-0000-0000-0000-{suffix}';");
            }
            migrationBuilder.Sql(
                "UPDATE ref_test_type_translations SET test_type_id = 'b0000001-0000-0000-0000-00000000000a' WHERE test_type_id = 'b0000002-0000-0000-0000-00000000000a';");

            for (int i = 1; i <= 9; i++)
            {
                var suffix = i.ToString("x12");
                migrationBuilder.Sql(
                    $"UPDATE prenatal_tests SET test_type_id = 'b0000001-0000-0000-0000-{suffix}' WHERE test_type_id = 'b0000002-0000-0000-0000-{suffix}';");
            }
            migrationBuilder.Sql(
                "UPDATE prenatal_tests SET test_type_id = 'b0000001-0000-0000-0000-00000000000a' WHERE test_type_id = 'b0000002-0000-0000-0000-00000000000a';");

            for (int i = 1; i <= 9; i++)
            {
                var suffix = i.ToString("x12");
                migrationBuilder.Sql(
                    $"UPDATE ref_test_types SET id = 'b0000001-0000-0000-0000-{suffix}' WHERE id = 'b0000002-0000-0000-0000-{suffix}';");
            }
            migrationBuilder.Sql(
                "UPDATE ref_test_types SET id = 'b0000001-0000-0000-0000-00000000000a' WHERE id = 'b0000002-0000-0000-0000-00000000000a';");

            migrationBuilder.Sql("SET FOREIGN_KEY_CHECKS = 1;");
        }
    }
}
