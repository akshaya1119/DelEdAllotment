using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DelEdAllotment.Migrations
{
    /// <inheritdoc />
    public partial class reinsertingreg : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssignedCentre",
                table: "Registration");

            migrationBuilder.DropColumn(
                name: "RollNumber",
                table: "Registration");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AssignedCentre",
                table: "Registration",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RollNumber",
                table: "Registration",
                type: "int",
                nullable: true);
        }
    }
}
