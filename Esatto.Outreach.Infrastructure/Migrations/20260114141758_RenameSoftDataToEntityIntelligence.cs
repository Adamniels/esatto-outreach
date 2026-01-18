using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Esatto.Outreach.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameSoftDataToEntityIntelligence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "soft_company_data");

            migrationBuilder.RenameColumn(
                name: "SoftCompanyDataId",
                table: "prospects",
                newName: "EntityIntelligenceId");

            migrationBuilder.RenameIndex(
                name: "IX_prospects_SoftCompanyDataId",
                table: "prospects",
                newName: "IX_prospects_EntityIntelligenceId");

            migrationBuilder.CreateTable(
                name: "entity_intelligence",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProspectId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyHooksJson = table.Column<string>(type: "text", nullable: true),
                    PersonalHooksJson = table.Column<string>(type: "text", nullable: true),
                    SummarizedContext = table.Column<string>(type: "text", nullable: true),
                    SourcesJson = table.Column<string>(type: "text", nullable: true),
                    ResearchedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_entity_intelligence", x => x.Id);
                    table.ForeignKey(
                        name: "FK_entity_intelligence_prospects_ProspectId",
                        column: x => x.ProspectId,
                        principalTable: "prospects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_entity_intelligence_ProspectId",
                table: "entity_intelligence",
                column: "ProspectId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_entity_intelligence_ResearchedAt",
                table: "entity_intelligence",
                column: "ResearchedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "entity_intelligence");

            migrationBuilder.RenameColumn(
                name: "EntityIntelligenceId",
                table: "prospects",
                newName: "SoftCompanyDataId");

            migrationBuilder.RenameIndex(
                name: "IX_prospects_EntityIntelligenceId",
                table: "prospects",
                newName: "IX_prospects_SoftCompanyDataId");

            migrationBuilder.CreateTable(
                name: "soft_company_data",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProspectId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    HooksJson = table.Column<string>(type: "text", nullable: true),
                    NewsItemsJson = table.Column<string>(type: "text", nullable: true),
                    RecentEventsJson = table.Column<string>(type: "text", nullable: true),
                    ResearchedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SocialActivityJson = table.Column<string>(type: "text", nullable: true),
                    SourcesJson = table.Column<string>(type: "text", nullable: true),
                    UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_soft_company_data", x => x.Id);
                    table.ForeignKey(
                        name: "FK_soft_company_data_prospects_ProspectId",
                        column: x => x.ProspectId,
                        principalTable: "prospects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_soft_company_data_ProspectId",
                table: "soft_company_data",
                column: "ProspectId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_soft_company_data_ResearchedAt",
                table: "soft_company_data",
                column: "ResearchedAt");
        }
    }
}
