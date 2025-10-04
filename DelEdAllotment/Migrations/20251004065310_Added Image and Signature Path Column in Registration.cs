using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DelEdAllotment.Migrations
{
    /// <inheritdoc />
    public partial class AddedImageandSignaturePathColumninRegistration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "Registration",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

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
                name: "ImagePath",
                table: "Registration");

            migrationBuilder.DropColumn(
                name: "SignaturePath",
                table: "Registration");
        }
    }
}
