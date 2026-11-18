using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matricula.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnrollmentPeriod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Enrollments_StudentId",
                table: "Enrollments");

            migrationBuilder.AddColumn<string>(
                name: "Period",
                table: "Enrollments",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "2026-II");

            migrationBuilder.CreateTable(
                name: "AdminUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Salt = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminUsers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_StudentId_Period",
                table: "Enrollments",
                columns: new[] { "StudentId", "Period" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdminUsers_Username",
                table: "AdminUsers",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminUsers");

            migrationBuilder.DropIndex(
                name: "IX_Enrollments_StudentId_Period",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "Period",
                table: "Enrollments");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_StudentId",
                table: "Enrollments",
                column: "StudentId",
                unique: true);
        }
    }
}
