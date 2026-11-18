using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matricula.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnrollmentConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Enrollments_StudentId",
                table: "Enrollments");

            migrationBuilder.DropIndex(
                name: "IX_EnrollmentDetails_EnrollmentId",
                table: "EnrollmentDetails");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_StudentId",
                table: "Enrollments",
                column: "StudentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EnrollmentDetails_EnrollmentId_ClassRoomCourseId",
                table: "EnrollmentDetails",
                columns: new[] { "EnrollmentId", "ClassRoomCourseId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Enrollments_StudentId",
                table: "Enrollments");

            migrationBuilder.DropIndex(
                name: "IX_EnrollmentDetails_EnrollmentId_ClassRoomCourseId",
                table: "EnrollmentDetails");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_StudentId",
                table: "Enrollments",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_EnrollmentDetails_EnrollmentId",
                table: "EnrollmentDetails",
                column: "EnrollmentId");
        }
    }
}
