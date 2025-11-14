using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Esatto.Outreach.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftCompanyDataWithProspectRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "HardCompanyDataId",
                table: "prospects",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SoftCompanyDataId",
                table: "prospects",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "hard_company_data",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CompanyOverview = table.Column<string>(type: "TEXT", nullable: true),
                    ServicesJson = table.Column<string>(type: "TEXT", nullable: true),
                    CasesJson = table.Column<string>(type: "TEXT", nullable: true),
                    IndustriesJson = table.Column<string>(type: "TEXT", nullable: true),
                    KeyFactsJson = table.Column<string>(type: "TEXT", nullable: true),
                    SourcesJson = table.Column<string>(type: "TEXT", nullable: true),
                    ResearchedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hard_company_data", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "soft_company_data",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProspectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    HooksJson = table.Column<string>(type: "TEXT", nullable: true),
                    RecentEventsJson = table.Column<string>(type: "TEXT", nullable: true),
                    NewsItemsJson = table.Column<string>(type: "TEXT", nullable: true),
                    SocialActivityJson = table.Column<string>(type: "TEXT", nullable: true),
                    SourcesJson = table.Column<string>(type: "TEXT", nullable: true),
                    ResearchedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
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
                name: "IX_prospects_HardCompanyDataId",
                table: "prospects",
                column: "HardCompanyDataId");

            migrationBuilder.CreateIndex(
                name: "IX_prospects_SoftCompanyDataId",
                table: "prospects",
                column: "SoftCompanyDataId");

            migrationBuilder.CreateIndex(
                name: "IX_hard_company_data_ResearchedAt",
                table: "hard_company_data",
                column: "ResearchedAt");

            migrationBuilder.CreateIndex(
                name: "IX_soft_company_data_ProspectId",
                table: "soft_company_data",
                column: "ProspectId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_soft_company_data_ResearchedAt",
                table: "soft_company_data",
                column: "ResearchedAt");

            migrationBuilder.AddForeignKey(
                name: "FK_prospects_hard_company_data_HardCompanyDataId",
                table: "prospects",
                column: "HardCompanyDataId",
                principalTable: "hard_company_data",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_prospects_hard_company_data_HardCompanyDataId",
                table: "prospects");

            migrationBuilder.DropTable(
                name: "hard_company_data");

            migrationBuilder.DropTable(
                name: "soft_company_data");

            migrationBuilder.DropIndex(
                name: "IX_prospects_HardCompanyDataId",
                table: "prospects");

            migrationBuilder.DropIndex(
                name: "IX_prospects_SoftCompanyDataId",
                table: "prospects");

            migrationBuilder.DropColumn(
                name: "HardCompanyDataId",
                table: "prospects");

            migrationBuilder.DropColumn(
                name: "SoftCompanyDataId",
                table: "prospects");
        }
    }
}
