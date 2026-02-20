using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FPT.EXE201.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentIdToPrenatalTest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "document_id",
                table: "prenatal_tests",
                type: "CHAR(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateIndex(
                name: "idx_prenatal_tests_document",
                table: "prenatal_tests",
                column: "document_id");

            migrationBuilder.AddForeignKey(
                name: "FK_prenatal_tests_medical_documents_document_id",
                table: "prenatal_tests",
                column: "document_id",
                principalTable: "medical_documents",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_prenatal_tests_medical_documents_document_id",
                table: "prenatal_tests");

            migrationBuilder.DropIndex(
                name: "idx_prenatal_tests_document",
                table: "prenatal_tests");

            migrationBuilder.DropColumn(
                name: "document_id",
                table: "prenatal_tests");
        }
    }
}
