using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Esatto.Outreach.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MigrateToCapsuleCrmIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_prospects_Domain",
                table: "prospects");

            migrationBuilder.DropIndex(
                name: "IX_prospects_MailTitle",
                table: "prospects");

            migrationBuilder.DropColumn(
                name: "ContactEmail",
                table: "prospects");

            migrationBuilder.DropColumn(
                name: "ContactName",
                table: "prospects");

            migrationBuilder.DropColumn(
                name: "Domain",
                table: "prospects");

            migrationBuilder.RenameColumn(
                name: "LinkedinUrl",
                table: "prospects",
                newName: "PictureURL");

            migrationBuilder.RenameColumn(
                name: "CompanyName",
                table: "prospects",
                newName: "Name");

            migrationBuilder.RenameIndex(
                name: "IX_prospects_CompanyName",
                table: "prospects",
                newName: "IX_prospects_Name");

            migrationBuilder.AlterColumn<string>(
                name: "OwnerId",
                table: "prospects",
                type: "TEXT",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "prospects",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "MailBodyPlain",
                table: "prospects",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "About",
                table: "prospects",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Addresses",
                table: "prospects",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CapsuleCreatedAt",
                table: "prospects",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CapsuleId",
                table: "prospects",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CapsuleUpdatedAt",
                table: "prospects",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailAddresses",
                table: "prospects",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsPending",
                table: "prospects",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastContactedAt",
                table: "prospects",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumbers",
                table: "prospects",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Websites",
                table: "prospects",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_prospects_CapsuleId",
                table: "prospects",
                column: "CapsuleId",
                unique: true,
                filter: "\"CapsuleId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_prospects_IsPending",
                table: "prospects",
                column: "IsPending");

            migrationBuilder.CreateIndex(
                name: "IX_prospects_IsPending_CreatedUtc",
                table: "prospects",
                columns: new[] { "IsPending", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_prospects_OwnerId_Status",
                table: "prospects",
                columns: new[] { "OwnerId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_prospects_CapsuleId",
                table: "prospects");

            migrationBuilder.DropIndex(
                name: "IX_prospects_IsPending",
                table: "prospects");

            migrationBuilder.DropIndex(
                name: "IX_prospects_IsPending_CreatedUtc",
                table: "prospects");

            migrationBuilder.DropIndex(
                name: "IX_prospects_OwnerId_Status",
                table: "prospects");

            migrationBuilder.DropColumn(
                name: "About",
                table: "prospects");

            migrationBuilder.DropColumn(
                name: "Addresses",
                table: "prospects");

            migrationBuilder.DropColumn(
                name: "CapsuleCreatedAt",
                table: "prospects");

            migrationBuilder.DropColumn(
                name: "CapsuleId",
                table: "prospects");

            migrationBuilder.DropColumn(
                name: "CapsuleUpdatedAt",
                table: "prospects");

            migrationBuilder.DropColumn(
                name: "EmailAddresses",
                table: "prospects");

            migrationBuilder.DropColumn(
                name: "IsPending",
                table: "prospects");

            migrationBuilder.DropColumn(
                name: "LastContactedAt",
                table: "prospects");

            migrationBuilder.DropColumn(
                name: "PhoneNumbers",
                table: "prospects");

            migrationBuilder.DropColumn(
                name: "Websites",
                table: "prospects");

            migrationBuilder.RenameColumn(
                name: "PictureURL",
                table: "prospects",
                newName: "LinkedinUrl");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "prospects",
                newName: "CompanyName");

            migrationBuilder.RenameIndex(
                name: "IX_prospects_Name",
                table: "prospects",
                newName: "IX_prospects_CompanyName");

            migrationBuilder.AlterColumn<string>(
                name: "OwnerId",
                table: "prospects",
                type: "TEXT",
                maxLength: 450,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "prospects",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "MailBodyPlain",
                table: "prospects",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactEmail",
                table: "prospects",
                type: "TEXT",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactName",
                table: "prospects",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Domain",
                table: "prospects",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_prospects_Domain",
                table: "prospects",
                column: "Domain");

            migrationBuilder.CreateIndex(
                name: "IX_prospects_MailTitle",
                table: "prospects",
                column: "MailTitle");
        }
    }
}
