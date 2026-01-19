using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Esatto.Outreach.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddContactPersonIsActive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ContactPersons_ProspectId",
                table: "ContactPersons");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "ContactPersons",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_ContactPersons_ProspectId_IsActive",
                table: "ContactPersons",
                columns: new[] { "ProspectId", "IsActive" },
                unique: true,
                filter: "\"IsActive\" = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ContactPersons_ProspectId_IsActive",
                table: "ContactPersons");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "ContactPersons");

            migrationBuilder.CreateIndex(
                name: "IX_ContactPersons_ProspectId",
                table: "ContactPersons",
                column: "ProspectId");
        }
    }
}
