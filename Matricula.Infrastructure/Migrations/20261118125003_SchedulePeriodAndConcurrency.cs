using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matricula.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SchedulePeriodAndConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Period",
                table: "ClassRoomCourses",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "2026-II");

            // [Timestamp] uint maps to the xmin system column: it already exists, no column is added.

            migrationBuilder.AddColumn<string>(
                name: "SecurityStamp",
                table: "AdminUsers",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("UPDATE \"AdminUsers\" SET \"SecurityStamp\" = md5(random()::text);");

            migrationBuilder.CreateIndex(
                name: "IX_ClassRoomCourses_Period_ClassRoomId",
                table: "ClassRoomCourses",
                columns: new[] { "Period", "ClassRoomId" });

            migrationBuilder.CreateIndex(
                name: "IX_ClassRoomCourses_Period_TeacherId",
                table: "ClassRoomCourses",
                columns: new[] { "Period", "TeacherId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ClassRoomCourses_Period_ClassRoomId",
                table: "ClassRoomCourses");

            migrationBuilder.DropIndex(
                name: "IX_ClassRoomCourses_Period_TeacherId",
                table: "ClassRoomCourses");

            migrationBuilder.DropColumn(
                name: "Period",
                table: "ClassRoomCourses");

            migrationBuilder.DropColumn(
                name: "SecurityStamp",
                table: "AdminUsers");
        }
    }
}
