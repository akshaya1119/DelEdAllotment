using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DelEdAllotment.Migrations
{
    /// <inheritdoc />
    public partial class AddedColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SignaturePath",
                table: "Registration",
                newName: "Remarks");

            migrationBuilder.RenameColumn(
                name: "RollNumber",
                table: "Registration",
                newName: "TransactionId");

            migrationBuilder.RenameColumn(
                name: "ImagePath",
                table: "Registration",
                newName: "PhType");

            migrationBuilder.RenameColumn(
                name: "AssignedCentre",
                table: "Registration",
                newName: "FeeAmount");

            migrationBuilder.AddColumn<DateTime>(
                name: "RetirementDate",
                table: "Registration",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RetirementDate",
                table: "Registration");

            migrationBuilder.RenameColumn(
                name: "TransactionId",
                table: "Registration",
                newName: "RollNumber");

            migrationBuilder.RenameColumn(
                name: "Remarks",
                table: "Registration",
                newName: "SignaturePath");

            migrationBuilder.RenameColumn(
                name: "PhType",
                table: "Registration",
                newName: "ImagePath");

            migrationBuilder.RenameColumn(
                name: "FeeAmount",
                table: "Registration",
                newName: "AssignedCentre");
        }
    }
}
