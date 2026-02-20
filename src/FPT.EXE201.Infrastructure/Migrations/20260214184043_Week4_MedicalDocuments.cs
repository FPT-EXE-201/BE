using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FPT.EXE201.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Week4_MedicalDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ref_document_types",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "CHAR(36)", nullable: false, collation: "ascii_general_ci"),
                    code = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_active = table.Column<bool>(type: "TINYINT(1)", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ref_document_types", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "storage_files",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "CHAR(36)", nullable: false, collation: "ascii_general_ci"),
                    owner_user_id = table.Column<Guid>(type: "CHAR(36)", nullable: true, collation: "ascii_general_ci"),
                    storage_provider = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false, defaultValue: "stub")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    bucket_name = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    object_key = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    public_url = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    original_file_name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    mime_type = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    checksum_sha256 = table.Column<byte[]>(type: "BINARY(32)", nullable: true),
                    uploaded_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: false),
                    created_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_storage_files", x => x.id);
                    table.ForeignKey(
                        name: "FK_storage_files_users_owner_user_id",
                        column: x => x.owner_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ref_document_type_translations",
                columns: table => new
                {
                    document_type_id = table.Column<Guid>(type: "CHAR(36)", nullable: false, collation: "ascii_general_ci"),
                    language_code = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    display_name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description = table.Column<string>(type: "TEXT", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ref_document_type_translations", x => new { x.document_type_id, x.language_code });
                    table.ForeignKey(
                        name: "FK_ref_document_type_translations_languages_language_code",
                        column: x => x.language_code,
                        principalTable: "languages",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ref_document_type_translations_ref_document_types_document_t~",
                        column: x => x.document_type_id,
                        principalTable: "ref_document_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "medical_documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "CHAR(36)", nullable: false, collation: "ascii_general_ci"),
                    pregnancy_id = table.Column<Guid>(type: "CHAR(36)", nullable: false, collation: "ascii_general_ci"),
                    visit_id = table.Column<Guid>(type: "CHAR(36)", nullable: true, collation: "ascii_general_ci"),
                    document_type_id = table.Column<Guid>(type: "CHAR(36)", nullable: true, collation: "ascii_general_ci"),
                    storage_file_id = table.Column<Guid>(type: "CHAR(36)", nullable: false, collation: "ascii_general_ci"),
                    title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    document_date = table.Column<DateOnly>(type: "DATE", nullable: true),
                    captured_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: false),
                    source = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    notes = table.Column<string>(type: "TEXT", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_favorite = table.Column<bool>(type: "TINYINT(1)", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_medical_documents", x => x.id);
                    table.ForeignKey(
                        name: "FK_medical_documents_pregnancies_pregnancy_id",
                        column: x => x.pregnancy_id,
                        principalTable: "pregnancies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_medical_documents_prenatal_visits_visit_id",
                        column: x => x.visit_id,
                        principalTable: "prenatal_visits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_medical_documents_ref_document_types_document_type_id",
                        column: x => x.document_type_id,
                        principalTable: "ref_document_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_medical_documents_storage_files_storage_file_id",
                        column: x => x.storage_file_id,
                        principalTable: "storage_files",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ocr_results",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "CHAR(36)", nullable: false, collation: "ascii_general_ci"),
                    document_id = table.Column<Guid>(type: "CHAR(36)", nullable: false, collation: "ascii_general_ci"),
                    ocr_run_no = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    engine = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    language_hint = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    raw_text = table.Column<string>(type: "LONGTEXT", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    structured_json = table.Column<string>(type: "JSON", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    confidence = table.Column<decimal>(type: "DECIMAL(5,2)", nullable: true),
                    error_message = table.Column<string>(type: "TEXT", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ocr_results", x => x.id);
                    table.ForeignKey(
                        name: "FK_ocr_results_medical_documents_document_id",
                        column: x => x.document_id,
                        principalTable: "medical_documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "ref_document_types",
                columns: new[] { "id", "code", "created_at", "deleted_at", "is_active", "updated_at" },
                values: new object[,]
                {
                    { new Guid("b0000001-0000-0000-0000-000000000001"), "PRENATAL_CHECKUP", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("b0000001-0000-0000-0000-000000000002"), "ULTRASOUND", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("b0000001-0000-0000-0000-000000000003"), "BLOOD_TEST", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("b0000001-0000-0000-0000-000000000004"), "URINE_TEST", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("b0000001-0000-0000-0000-000000000005"), "PRESCRIPTION", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("b0000001-0000-0000-0000-000000000006"), "VACCINATION_RECORD", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("b0000001-0000-0000-0000-000000000007"), "MEDICAL_REPORT", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("b0000001-0000-0000-0000-000000000008"), "OTHER", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "ref_document_type_translations",
                columns: new[] { "document_type_id", "language_code", "description", "display_name" },
                values: new object[,]
                {
                    { new Guid("b0000001-0000-0000-0000-000000000001"), "en", "Routine prenatal examination report", "Prenatal Checkup" },
                    { new Guid("b0000001-0000-0000-0000-000000000001"), "vi", "Phiếu khám thai định kỳ", "Khám thai" },
                    { new Guid("b0000001-0000-0000-0000-000000000002"), "en", "Prenatal ultrasound result", "Ultrasound" },
                    { new Guid("b0000001-0000-0000-0000-000000000002"), "vi", "Kết quả siêu âm thai", "Siêu âm" },
                    { new Guid("b0000001-0000-0000-0000-000000000003"), "en", "Blood test result", "Blood Test" },
                    { new Guid("b0000001-0000-0000-0000-000000000003"), "vi", "Kết quả xét nghiệm máu", "Xét nghiệm máu" },
                    { new Guid("b0000001-0000-0000-0000-000000000004"), "en", "Urine test result", "Urine Test" },
                    { new Guid("b0000001-0000-0000-0000-000000000004"), "vi", "Kết quả xét nghiệm nước tiểu", "Xét nghiệm nước tiểu" },
                    { new Guid("b0000001-0000-0000-0000-000000000005"), "en", "Doctor's prescription", "Prescription" },
                    { new Guid("b0000001-0000-0000-0000-000000000005"), "vi", "Đơn thuốc từ bác sĩ", "Đơn thuốc" },
                    { new Guid("b0000001-0000-0000-0000-000000000006"), "en", "Vaccination record", "Vaccination Record" },
                    { new Guid("b0000001-0000-0000-0000-000000000006"), "vi", "Ghi nhận tiêm chủng", "Sổ tiêm chủng" },
                    { new Guid("b0000001-0000-0000-0000-000000000007"), "en", "Comprehensive medical report", "Medical Report" },
                    { new Guid("b0000001-0000-0000-0000-000000000007"), "vi", "Báo cáo y tế tổng hợp", "Báo cáo y tế" },
                    { new Guid("b0000001-0000-0000-0000-000000000008"), "en", "Other medical documents", "Other" },
                    { new Guid("b0000001-0000-0000-0000-000000000008"), "vi", "Tài liệu y tế khác", "Khác" }
                });

            migrationBuilder.CreateIndex(
                name: "idx_medical_docs_pregnancy",
                table: "medical_documents",
                columns: new[] { "pregnancy_id", "captured_at" });

            migrationBuilder.CreateIndex(
                name: "idx_medical_docs_type",
                table: "medical_documents",
                columns: new[] { "pregnancy_id", "document_type_id", "captured_at" });

            migrationBuilder.CreateIndex(
                name: "idx_medical_docs_visit",
                table: "medical_documents",
                column: "visit_id");

            migrationBuilder.CreateIndex(
                name: "IX_medical_documents_document_type_id",
                table: "medical_documents",
                column: "document_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_medical_documents_storage_file_id",
                table: "medical_documents",
                column: "storage_file_id");

            migrationBuilder.CreateIndex(
                name: "idx_ocr_results_status",
                table: "ocr_results",
                columns: new[] { "document_id", "status" });

            migrationBuilder.CreateIndex(
                name: "uk_ocr_results_doc_run",
                table: "ocr_results",
                columns: new[] { "document_id", "ocr_run_no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ref_document_type_translations_language_code",
                table: "ref_document_type_translations",
                column: "language_code");

            migrationBuilder.CreateIndex(
                name: "uk_ref_doc_types_code",
                table: "ref_document_types",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_storage_files_object",
                table: "storage_files",
                columns: new[] { "storage_provider", "object_key" });

            migrationBuilder.CreateIndex(
                name: "idx_storage_files_owner",
                table: "storage_files",
                column: "owner_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ocr_results");

            migrationBuilder.DropTable(
                name: "ref_document_type_translations");

            migrationBuilder.DropTable(
                name: "medical_documents");

            migrationBuilder.DropTable(
                name: "ref_document_types");

            migrationBuilder.DropTable(
                name: "storage_files");
        }
    }
}
