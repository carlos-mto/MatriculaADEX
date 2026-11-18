using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matricula.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdminPanel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClassRoomCourses_ClassRooms_ClassRoomId",
                table: "ClassRoomCourses");

            migrationBuilder.DropForeignKey(
                name: "FK_ClassRoomCourses_Courses_CourseId",
                table: "ClassRoomCourses");

            migrationBuilder.DropForeignKey(
                name: "FK_ClassRoomCourses_Shifts_ShiftId",
                table: "ClassRoomCourses");

            migrationBuilder.DropForeignKey(
                name: "FK_EnrollmentDetails_ClassRoomCourses_ClassRoomCourseId",
                table: "EnrollmentDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_Enrollments_Shifts_ShiftId",
                table: "Enrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_Enrollments_Students_StudentId",
                table: "Enrollments");

            migrationBuilder.AddColumn<Guid>(
                name: "TeacherId",
                table: "ClassRoomCourses",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AppSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Teachers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Specialty = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teachers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClassRoomCourses_TeacherId",
                table: "ClassRoomCourses",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_AppSettings_Key",
                table: "AppSettings",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Teachers_Email",
                table: "Teachers",
                column: "Email",
                unique: true,
                filter: "\"Email\" IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_ClassRoomCourses_ClassRooms_ClassRoomId",
                table: "ClassRoomCourses",
                column: "ClassRoomId",
                principalTable: "ClassRooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClassRoomCourses_Courses_CourseId",
                table: "ClassRoomCourses",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClassRoomCourses_Shifts_ShiftId",
                table: "ClassRoomCourses",
                column: "ShiftId",
                principalTable: "Shifts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClassRoomCourses_Teachers_TeacherId",
                table: "ClassRoomCourses",
                column: "TeacherId",
                principalTable: "Teachers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EnrollmentDetails_ClassRoomCourses_ClassRoomCourseId",
                table: "EnrollmentDetails",
                column: "ClassRoomCourseId",
                principalTable: "ClassRoomCourses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollments_Shifts_ShiftId",
                table: "Enrollments",
                column: "ShiftId",
                principalTable: "Shifts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollments_Students_StudentId",
                table: "Enrollments",
                column: "StudentId",
                principalTable: "Students",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClassRoomCourses_ClassRooms_ClassRoomId",
                table: "ClassRoomCourses");

            migrationBuilder.DropForeignKey(
                name: "FK_ClassRoomCourses_Courses_CourseId",
                table: "ClassRoomCourses");

            migrationBuilder.DropForeignKey(
                name: "FK_ClassRoomCourses_Shifts_ShiftId",
                table: "ClassRoomCourses");

            migrationBuilder.DropForeignKey(
                name: "FK_ClassRoomCourses_Teachers_TeacherId",
                table: "ClassRoomCourses");

            migrationBuilder.DropForeignKey(
                name: "FK_EnrollmentDetails_ClassRoomCourses_ClassRoomCourseId",
                table: "EnrollmentDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_Enrollments_Shifts_ShiftId",
                table: "Enrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_Enrollments_Students_StudentId",
                table: "Enrollments");

            migrationBuilder.DropTable(
                name: "AppSettings");

            migrationBuilder.DropTable(
                name: "Teachers");

            migrationBuilder.DropIndex(
                name: "IX_ClassRoomCourses_TeacherId",
                table: "ClassRoomCourses");

            migrationBuilder.DropColumn(
                name: "TeacherId",
                table: "ClassRoomCourses");

            migrationBuilder.AddForeignKey(
                name: "FK_ClassRoomCourses_ClassRooms_ClassRoomId",
                table: "ClassRoomCourses",
                column: "ClassRoomId",
                principalTable: "ClassRooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ClassRoomCourses_Courses_CourseId",
                table: "ClassRoomCourses",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ClassRoomCourses_Shifts_ShiftId",
                table: "ClassRoomCourses",
                column: "ShiftId",
                principalTable: "Shifts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EnrollmentDetails_ClassRoomCourses_ClassRoomCourseId",
                table: "EnrollmentDetails",
                column: "ClassRoomCourseId",
                principalTable: "ClassRoomCourses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollments_Shifts_ShiftId",
                table: "Enrollments",
                column: "ShiftId",
                principalTable: "Shifts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollments_Students_StudentId",
                table: "Enrollments",
                column: "StudentId",
                principalTable: "Students",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
