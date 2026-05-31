using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyWebApp.Migrations
{
    /// <inheritdoc />
    public partial class AddCalendarAiFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AiAnalyzed",
                table: "CalendarEvents",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AiCategory",
                table: "CalendarEvents",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiClient",
                table: "CalendarEvents",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiProjectName",
                table: "CalendarEvents",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiRecommendations",
                table: "CalendarEvents",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiStatus",
                table: "CalendarEvents",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiSummary",
                table: "CalendarEvents",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiAnalyzed",
                table: "CalendarEvents");

            migrationBuilder.DropColumn(
                name: "AiCategory",
                table: "CalendarEvents");

            migrationBuilder.DropColumn(
                name: "AiClient",
                table: "CalendarEvents");

            migrationBuilder.DropColumn(
                name: "AiProjectName",
                table: "CalendarEvents");

            migrationBuilder.DropColumn(
                name: "AiRecommendations",
                table: "CalendarEvents");

            migrationBuilder.DropColumn(
                name: "AiStatus",
                table: "CalendarEvents");

            migrationBuilder.DropColumn(
                name: "AiSummary",
                table: "CalendarEvents");
        }
    }
}
