using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Esatto.Outreach.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactorProspectToCrmAgnostic : Migration
    {
        /// <inheritdoc />
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Add new columns
            migrationBuilder.AddColumn<int>(
                name: "CrmSource",
                table: "prospects",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ExternalCrmId",
                table: "prospects",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            // 2. Backfill data: Convert existing CapsuleId values to the new format
            // CrmSource = 1 (Capsule), ExternalCrmId = CapsuleId as string
            migrationBuilder.Sql(@"
                UPDATE prospects
                SET ""CrmSource"" = 1, ""ExternalCrmId"" = ""CapsuleId""::text
                WHERE ""CapsuleId"" IS NOT NULL;
            ");

            // 3. Drop old columns and indexes
            migrationBuilder.DropIndex(
                name: "IX_prospects_CapsuleId",
                table: "prospects");

            migrationBuilder.DropColumn(
                name: "CapsuleId",
                table: "prospects");

            migrationBuilder.DropColumn(
                name: "Addresses",
                table: "prospects");

            // 4. Renames
            migrationBuilder.RenameColumn(
                name: "CapsuleUpdatedAt",
                table: "prospects",
                newName: "CrmUpdatedAt");

            migrationBuilder.RenameColumn(
                name: "CapsuleCreatedAt",
                table: "prospects",
                newName: "CrmCreatedAt");

            // 5. Other schema changes (WorkflowSteps)
            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "WorkflowSteps",
                type: "bytea",
                rowVersion: true,
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "bytea",
                oldRowVersion: true);

            // 6. Create new index
            migrationBuilder.CreateIndex(
                name: "IX_prospects_CrmSource_ExternalCrmId",
                table: "prospects",
                columns: new[] { "CrmSource", "ExternalCrmId" },
                unique: true,
                filter: "\"ExternalCrmId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_prospects_CrmSource_ExternalCrmId",
                table: "prospects");

            migrationBuilder.RenameColumn(
                name: "CrmUpdatedAt",
                table: "prospects",
                newName: "CapsuleUpdatedAt");

            migrationBuilder.RenameColumn(
                name: "CrmCreatedAt",
                table: "prospects",
                newName: "CapsuleCreatedAt");

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "WorkflowSteps",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0],
                oldClrType: typeof(byte[]),
                oldType: "bytea",
                oldRowVersion: true,
                oldNullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CapsuleId",
                table: "prospects",
                type: "bigint",
                nullable: true);

             // Backfill CapsuleId from ExternalCrmId where CrmSource = 1 (Capsule)
            migrationBuilder.Sql(@"
                UPDATE prospects
                SET ""CapsuleId"" = ""ExternalCrmId""::bigint
                WHERE ""CrmSource"" = 1 AND ""ExternalCrmId"" IS NOT NULL;
            ");

            migrationBuilder.AddColumn<string>(
                name: "Addresses",
                table: "prospects",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.DropColumn(
                name: "CrmSource",
                table: "prospects");

            migrationBuilder.DropColumn(
                name: "ExternalCrmId",
                table: "prospects");

            migrationBuilder.CreateIndex(
                name: "IX_prospects_CapsuleId",
                table: "prospects",
                column: "CapsuleId",
                unique: true,
                filter: "\"CapsuleId\" IS NOT NULL");
        }
    }
}
