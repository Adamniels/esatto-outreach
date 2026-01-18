using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Esatto.Outreach.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyEnrichmentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CompanyEnrichmentJson",
                table: "entity_intelligence",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EnrichmentVersion",
                table: "entity_intelligence",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompanyEnrichmentJson",
                table: "entity_intelligence");

            migrationBuilder.DropColumn(
                name: "EnrichmentVersion",
                table: "entity_intelligence");
        }
    }
}
