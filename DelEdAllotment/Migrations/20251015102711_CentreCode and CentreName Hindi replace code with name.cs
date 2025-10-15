using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DelEdAllotment.Migrations
{
    /// <inheritdoc />
    public partial class CentreCodeandCentreNameHindireplacecodewithname : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CityCodeHindi",
                table: "Centre",
                newName: "CityNameHindi");

            migrationBuilder.RenameColumn(
                name: "CentreCodeHindi",
                table: "Centre",
                newName: "CentreNameHindi");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CityNameHindi",
                table: "Centre",
                newName: "CityCodeHindi");

            migrationBuilder.RenameColumn(
                name: "CentreNameHindi",
                table: "Centre",
                newName: "CentreCodeHindi");
        }
    }
}
