using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Esatto.Outreach.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangeStepGenerationType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UseCollectedData",
                table: "SequenceSteps");

            migrationBuilder.AddColumn<string>(
                name: "GenerationType",
                table: "SequenceSteps",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GenerationType",
                table: "SequenceSteps");

            migrationBuilder.AddColumn<bool>(
                name: "UseCollectedData",
                table: "SequenceSteps",
                type: "boolean",
                nullable: true);
        }
    }
}
