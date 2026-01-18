using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Esatto.Outreach.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactorEntityIntelligenceStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompanyEnrichmentJson",
                table: "entity_intelligence");

            migrationBuilder.DropColumn(
                name: "CompanyHooksJson",
                table: "entity_intelligence");

            migrationBuilder.DropColumn(
                name: "PersonalHooksJson",
                table: "entity_intelligence");

            migrationBuilder.DropColumn(
                name: "SourcesJson",
                table: "entity_intelligence");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:hstore", ",,");

            migrationBuilder.AddColumn<string>(
                name: "EnrichedData",
                table: "entity_intelligence",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EnrichedData",
                table: "entity_intelligence");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:hstore", ",,");

            migrationBuilder.AddColumn<string>(
                name: "CompanyEnrichmentJson",
                table: "entity_intelligence",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyHooksJson",
                table: "entity_intelligence",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PersonalHooksJson",
                table: "entity_intelligence",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourcesJson",
                table: "entity_intelligence",
                type: "text",
                nullable: true);
        }
    }
}
