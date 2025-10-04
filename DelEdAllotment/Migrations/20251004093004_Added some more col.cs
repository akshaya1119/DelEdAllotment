using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DelEdAllotment.Migrations
{
    /// <inheritdoc />
    public partial class Addedsomemorecol : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Registration",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "AssignedCentre",
                table: "Registration",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "Registration",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "RollNumber",
                table: "Registration",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignaturePath",
                table: "Registration",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "Registration");

            migrationBuilder.DropColumn(
                name: "AssignedCentre",
                table: "Registration");

            migrationBuilder.DropColumn(
                name: "ImagePath",
                table: "Registration");

            migrationBuilder.DropColumn(
                name: "RollNumber",
                table: "Registration");

            migrationBuilder.DropColumn(
                name: "SignaturePath",
                table: "Registration");
        }
    }
}
