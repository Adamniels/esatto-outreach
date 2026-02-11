using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Esatto.Outreach.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveHardCompanyData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_prospects_hard_company_data_HardCompanyDataId",
                table: "prospects");

            migrationBuilder.DropIndex(
                name: "IX_prospects_HardCompanyDataId",
                table: "prospects");

            migrationBuilder.DropColumn(
                name: "HardCompanyDataId",
                table: "prospects");

            migrationBuilder.DropTable(
                name: "hard_company_data");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "hard_company_data",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CasesJson = table.Column<string>(type: "text", nullable: true),
                    CompanyOverview = table.Column<string>(type: "text", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IndustriesJson = table.Column<string>(type: "text", nullable: true),
                    KeyFactsJson = table.Column<string>(type: "text", nullable: true),
                    ResearchedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ServicesJson = table.Column<string>(type: "text", nullable: true),
                    SourcesJson = table.Column<string>(type: "text", nullable: true),
                    UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hard_company_data", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_hard_company_data_ResearchedAt",
                table: "hard_company_data",
                column: "ResearchedAt");

            migrationBuilder.AddColumn<Guid>(
                name: "HardCompanyDataId",
                table: "prospects",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_prospects_HardCompanyDataId",
                table: "prospects",
                column: "HardCompanyDataId");

            migrationBuilder.AddForeignKey(
                name: "FK_prospects_hard_company_data_HardCompanyDataId",
                table: "prospects",
                column: "HardCompanyDataId",
                principalTable: "hard_company_data",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
