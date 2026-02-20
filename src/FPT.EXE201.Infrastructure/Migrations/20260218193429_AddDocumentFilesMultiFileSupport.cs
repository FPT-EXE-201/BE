using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FPT.EXE201.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentFilesMultiFileSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Create document_files table FIRST
            migrationBuilder.CreateTable(
                name: "document_files",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "CHAR(36)", nullable: false, collation: "ascii_general_ci"),
                    document_id = table.Column<Guid>(type: "CHAR(36)", nullable: false, collation: "ascii_general_ci"),
                    storage_file_id = table.Column<Guid>(type: "CHAR(36)", nullable: false, collation: "ascii_general_ci"),
                    sort_order = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    page_label = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_files", x => x.id);
                    table.ForeignKey(
                        name: "FK_document_files_medical_documents_document_id",
                        column: x => x.document_id,
                        principalTable: "medical_documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_document_files_storage_files_storage_file_id",
                        column: x => x.storage_file_id,
                        principalTable: "storage_files",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "idx_document_files_storage",
                table: "document_files",
                column: "storage_file_id");

            migrationBuilder.CreateIndex(
                name: "uk_document_files_sort",
                table: "document_files",
                columns: new[] { "document_id", "sort_order" },
                unique: true);

            // Step 2: Migrate existing data — create DocumentFile for each existing MedicalDocument
            migrationBuilder.Sql(@"
                INSERT INTO document_files (id, document_id, storage_file_id, sort_order, page_label, created_at, updated_at, deleted_at)
                SELECT UUID(), id, storage_file_id, 1, NULL, NOW(6), NOW(6), NULL
                FROM medical_documents
                WHERE storage_file_id IS NOT NULL;
            ");

            // Step 3: Now safe to remove the old column
            migrationBuilder.DropForeignKey(
                name: "FK_medical_documents_storage_files_storage_file_id",
                table: "medical_documents");

            migrationBuilder.DropIndex(
                name: "IX_medical_documents_storage_file_id",
                table: "medical_documents");

            migrationBuilder.DropColumn(
                name: "storage_file_id",
                table: "medical_documents");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "document_files");

            migrationBuilder.AddColumn<Guid>(
                name: "storage_file_id",
                table: "medical_documents",
                type: "CHAR(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_medical_documents_storage_file_id",
                table: "medical_documents",
                column: "storage_file_id");

            migrationBuilder.AddForeignKey(
                name: "FK_medical_documents_storage_files_storage_file_id",
                table: "medical_documents",
                column: "storage_file_id",
                principalTable: "storage_files",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
