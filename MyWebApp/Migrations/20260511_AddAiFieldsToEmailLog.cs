using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyWebApp.Migrations
{
    /// <inheritdoc />
    public partial class AddAiFieldsToEmailLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AiCategory",
                table: "EmailLogs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AiAnalyzed",
                table: "EmailLogs",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AiRecommendations",
                table: "EmailLogs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiSummary",
                table: "EmailLogs",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiCategory",
                table: "EmailLogs");

            migrationBuilder.DropColumn(
                name: "AiAnalyzed",
                table: "EmailLogs");

            migrationBuilder.DropColumn(
                name: "AiRecommendations",
                table: "EmailLogs");

            migrationBuilder.DropColumn(
                name: "AiSummary",
                table: "EmailLogs");
        }
    }
}
