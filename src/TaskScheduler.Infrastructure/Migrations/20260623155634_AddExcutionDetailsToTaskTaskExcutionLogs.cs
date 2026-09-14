using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskScheduler.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExcutionDetailsToTaskTaskExcutionLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ExitCode",
                table: "TaskExecutionLogs",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExitCode",
                table: "TaskExecutionLogs");
        }
    }
}
