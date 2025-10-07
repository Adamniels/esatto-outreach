using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Esatto.Outreach.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "prospects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CompanyName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Domain = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ContactName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ContactEmail = table.Column<string>(type: "TEXT", maxLength: 320, nullable: true),
                    LinkedinUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prospects", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_prospects_CompanyName",
                table: "prospects",
                column: "CompanyName");

            migrationBuilder.CreateIndex(
                name: "IX_prospects_Domain",
                table: "prospects",
                column: "Domain");

            migrationBuilder.CreateIndex(
                name: "IX_prospects_Status_CreatedUtc",
                table: "prospects",
                columns: new[] { "Status", "CreatedUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "prospects");
        }
    }
}
