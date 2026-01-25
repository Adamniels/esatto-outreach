using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Esatto.Outreach.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGenerationStrategyToWorkflowSteps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GenerationStrategy",
                table: "WorkflowTemplateSteps",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GenerationStrategy",
                table: "WorkflowSteps",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GenerationStrategy",
                table: "WorkflowTemplateSteps");

            migrationBuilder.DropColumn(
                name: "GenerationStrategy",
                table: "WorkflowSteps");
        }
    }
}
