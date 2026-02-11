using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Esatto.Outreach.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixRowVersionConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add trigger function to auto-update RowVersion on UPDATE
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION update_workflow_step_rowversion()
                RETURNS TRIGGER AS $$
                BEGIN
                    NEW.""RowVersion"" = gen_random_bytes(16);
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;
            ");

            // Add trigger to WorkflowSteps table
            migrationBuilder.Sql(@"
                CREATE TRIGGER trigger_update_workflow_step_rowversion
                BEFORE UPDATE ON ""WorkflowSteps""
                FOR EACH ROW
                EXECUTE FUNCTION update_workflow_step_rowversion();
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop trigger
            migrationBuilder.Sql(@"DROP TRIGGER IF EXISTS trigger_update_workflow_step_rowversion ON ""WorkflowSteps"";");
            
            // Drop function
            migrationBuilder.Sql(@"DROP FUNCTION IF EXISTS update_workflow_step_rowversion();");
        }
    }
}
