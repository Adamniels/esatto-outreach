using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Esatto.Outreach.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSchedulingSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DelayOffset",
                table: "WorkflowTemplateSteps",
                newName: "TimeOfDay");

            migrationBuilder.RenameColumn(
                name: "DelayOffset",
                table: "WorkflowSteps",
                newName: "TimeOfDay");

            migrationBuilder.AddColumn<int>(
                name: "DayOffset",
                table: "WorkflowTemplateSteps",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DayOffset",
                table: "WorkflowSteps",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DayOffset",
                table: "WorkflowTemplateSteps");

            migrationBuilder.DropColumn(
                name: "DayOffset",
                table: "WorkflowSteps");

            migrationBuilder.RenameColumn(
                name: "TimeOfDay",
                table: "WorkflowTemplateSteps",
                newName: "DelayOffset");

            migrationBuilder.RenameColumn(
                name: "TimeOfDay",
                table: "WorkflowSteps",
                newName: "DelayOffset");
        }
    }
}
