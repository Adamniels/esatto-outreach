using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Esatto.Outreach.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class StabilityHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SequenceProspects_SequenceId",
                table: "SequenceProspects");

            migrationBuilder.RenameColumn(
                name: "Token",
                table: "refresh_tokens",
                newName: "TokenHash");

            migrationBuilder.RenameIndex(
                name: "IX_refresh_tokens_Token",
                table: "refresh_tokens",
                newName: "IX_refresh_tokens_TokenHash");

            migrationBuilder.AlterColumn<string>(
                name: "Websites",
                table: "prospects",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<string>(
                name: "Tags",
                table: "prospects",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<string>(
                name: "CustomFields",
                table: "prospects",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<string>(
                name: "EnrichedData",
                table: "entity_intelligence",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PersonalNewsJson",
                table: "ContactPersons",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<string>(
                name: "PersonalHooksJson",
                table: "ContactPersons",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.CreateIndex(
                name: "IX_SequenceProspects_SequenceId_ProspectId",
                table: "SequenceProspects",
                columns: new[] { "SequenceId", "ProspectId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SequenceProspects_SequenceId_ProspectId",
                table: "SequenceProspects");

            migrationBuilder.RenameColumn(
                name: "TokenHash",
                table: "refresh_tokens",
                newName: "Token");

            migrationBuilder.RenameIndex(
                name: "IX_refresh_tokens_TokenHash",
                table: "refresh_tokens",
                newName: "IX_refresh_tokens_Token");

            migrationBuilder.AlterColumn<string>(
                name: "Websites",
                table: "prospects",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Tags",
                table: "prospects",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "CustomFields",
                table: "prospects",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "EnrichedData",
                table: "entity_intelligence",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PersonalNewsJson",
                table: "ContactPersons",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "PersonalHooksJson",
                table: "ContactPersons",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateIndex(
                name: "IX_SequenceProspects_SequenceId",
                table: "SequenceProspects",
                column: "SequenceId");
        }
    }
}
