using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matricula.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnrollmentShift : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ShiftId",
                table: "Enrollments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_ShiftId",
                table: "Enrollments",
                column: "ShiftId");

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollments_Shifts_ShiftId",
                table: "Enrollments",
                column: "ShiftId",
                principalTable: "Shifts",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Enrollments_Shifts_ShiftId",
                table: "Enrollments");

            migrationBuilder.DropIndex(
                name: "IX_Enrollments_ShiftId",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "ShiftId",
                table: "Enrollments");
        }
    }
}
