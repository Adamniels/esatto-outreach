using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Esatto.Outreach.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveProspectContactInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailAddresses",
                table: "prospects");

            migrationBuilder.DropColumn(
                name: "PhoneNumbers",
                table: "prospects");

            // Manual adjustment for PersonalNewsJson
            migrationBuilder.Sql("UPDATE \"ContactPersons\" SET \"PersonalNewsJson\" = '[]' WHERE \"PersonalNewsJson\" IS NULL OR \"PersonalNewsJson\" = '';");
            migrationBuilder.Sql("ALTER TABLE \"ContactPersons\" ALTER COLUMN \"PersonalNewsJson\" TYPE jsonb USING \"PersonalNewsJson\"::jsonb");
            migrationBuilder.Sql("ALTER TABLE \"ContactPersons\" ALTER COLUMN \"PersonalNewsJson\" SET DEFAULT '[]'");
            migrationBuilder.Sql("ALTER TABLE \"ContactPersons\" ALTER COLUMN \"PersonalNewsJson\" SET NOT NULL");

            // Manual adjustment for PersonalHooksJson
            migrationBuilder.Sql("UPDATE \"ContactPersons\" SET \"PersonalHooksJson\" = '[]' WHERE \"PersonalHooksJson\" IS NULL OR \"PersonalHooksJson\" = '';");
            migrationBuilder.Sql("ALTER TABLE \"ContactPersons\" ALTER COLUMN \"PersonalHooksJson\" TYPE jsonb USING \"PersonalHooksJson\"::jsonb");
            migrationBuilder.Sql("ALTER TABLE \"ContactPersons\" ALTER COLUMN \"PersonalHooksJson\" SET DEFAULT '[]'");
            migrationBuilder.Sql("ALTER TABLE \"ContactPersons\" ALTER COLUMN \"PersonalHooksJson\" SET NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmailAddresses",
                table: "prospects",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumbers",
                table: "prospects",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "PersonalNewsJson",
                table: "ContactPersons",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<string>(
                name: "PersonalHooksJson",
                table: "ContactPersons",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb");
        }
    }
}
