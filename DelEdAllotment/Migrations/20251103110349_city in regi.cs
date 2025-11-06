using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DelEdAllotment.Migrations
{
    /// <inheritdoc />
    public partial class cityinregi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_seat_allotments",
                table: "seat_allotments");

            migrationBuilder.RenameTable(
                name: "seat_allotments",
                newName: "Seat_allotments");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Seat_allotments",
                table: "Seat_allotments",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Seat_allotments",
                table: "Seat_allotments");

            migrationBuilder.RenameTable(
                name: "Seat_allotments",
                newName: "seat_allotments");

            migrationBuilder.AddPrimaryKey(
                name: "PK_seat_allotments",
                table: "seat_allotments",
                column: "Id");
        }
    }
}
