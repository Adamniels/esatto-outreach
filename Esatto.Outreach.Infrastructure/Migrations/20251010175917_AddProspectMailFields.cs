using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Esatto.Outreach.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProspectMailFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MailBodyHTML",
                table: "prospects",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MailBodyPlain",
                table: "prospects",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MailTitle",
                table: "prospects",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_prospects_MailTitle",
                table: "prospects",
                column: "MailTitle");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_prospects_MailTitle",
                table: "prospects");

            migrationBuilder.DropColumn(
                name: "MailBodyHTML",
                table: "prospects");

            migrationBuilder.DropColumn(
                name: "MailBodyPlain",
                table: "prospects");

            migrationBuilder.DropColumn(
                name: "MailTitle",
                table: "prospects");
        }
    }
}
