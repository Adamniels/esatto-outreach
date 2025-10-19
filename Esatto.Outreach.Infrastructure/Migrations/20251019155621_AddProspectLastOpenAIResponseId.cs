using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Esatto.Outreach.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProspectLastOpenAIResponseId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LastOpenAIResponseId",
                table: "prospects",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastOpenAIResponseId",
                table: "prospects");
        }
    }
}
