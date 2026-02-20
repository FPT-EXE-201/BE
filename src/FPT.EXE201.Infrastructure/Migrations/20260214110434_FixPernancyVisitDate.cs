using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FPT.EXE201.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixPernancyVisitDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_prenatal_visits_date",
                table: "prenatal_visits");

            migrationBuilder.DropIndex(
                name: "uk_pregnancies_user_no",
                table: "pregnancies");

            migrationBuilder.DropColumn(
                name: "visit_at",
                table: "prenatal_visits");

            migrationBuilder.AddColumn<DateOnly>(
                name: "visit_date",
                table: "prenatal_visits",
                type: "DATE",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.CreateIndex(
                name: "idx_prenatal_visits_date",
                table: "prenatal_visits",
                column: "visit_date");

            migrationBuilder.CreateIndex(
                name: "uk_pregnancies_user_no",
                table: "pregnancies",
                columns: new[] { "user_id", "pregnancy_no", "deleted_at" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_prenatal_visits_date",
                table: "prenatal_visits");

            migrationBuilder.DropIndex(
                name: "uk_pregnancies_user_no",
                table: "pregnancies");

            migrationBuilder.DropColumn(
                name: "visit_date",
                table: "prenatal_visits");

            migrationBuilder.AddColumn<DateTime>(
                name: "visit_at",
                table: "prenatal_visits",
                type: "DATETIME",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "idx_prenatal_visits_date",
                table: "prenatal_visits",
                column: "visit_at");

            migrationBuilder.CreateIndex(
                name: "uk_pregnancies_user_no",
                table: "pregnancies",
                columns: new[] { "user_id", "pregnancy_no" },
                unique: true);
        }
    }
}
