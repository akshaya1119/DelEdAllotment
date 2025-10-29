using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DelEdAllotment.Migrations
{
    /// <inheritdoc />
    public partial class AddedColsScribeUniversityandTransactionDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "HomeDistrict",
                table: "Registration",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Scribe",
                table: "Registration",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "TransactionDate",
                table: "Registration",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "University",
                table: "Registration",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Scribe",
                table: "Registration");

            migrationBuilder.DropColumn(
                name: "TransactionDate",
                table: "Registration");

            migrationBuilder.DropColumn(
                name: "University",
                table: "Registration");

            migrationBuilder.UpdateData(
                table: "Registration",
                keyColumn: "HomeDistrict",
                keyValue: null,
                column: "HomeDistrict",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "HomeDistrict",
                table: "Registration",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
