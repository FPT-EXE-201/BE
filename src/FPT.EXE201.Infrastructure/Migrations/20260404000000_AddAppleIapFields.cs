using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FPT.EXE201.Infrastructure.Migrations
{
    public partial class AddAppleIapFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "apple_original_transaction_id",
                table: "subscriptions",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "apple_product_id",
                table: "subscriptions",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ix_subscriptions_apple_original_transaction_id",
                table: "subscriptions",
                column: "apple_original_transaction_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_subscriptions_apple_original_transaction_id",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "apple_original_transaction_id",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "apple_product_id",
                table: "subscriptions");
        }
    }
}