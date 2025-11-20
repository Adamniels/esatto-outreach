using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Esatto.Outreach.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIdToEmailPrompts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_generate_email_prompts_IsActive",
                table: "generate_email_prompts");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "generate_email_prompts",
                type: "TEXT",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_generate_email_prompts_UserId",
                table: "generate_email_prompts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_generate_email_prompts_UserId_IsActive",
                table: "generate_email_prompts",
                columns: new[] { "UserId", "IsActive" });

            migrationBuilder.AddForeignKey(
                name: "FK_generate_email_prompts_AspNetUsers_UserId",
                table: "generate_email_prompts",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_generate_email_prompts_AspNetUsers_UserId",
                table: "generate_email_prompts");

            migrationBuilder.DropIndex(
                name: "IX_generate_email_prompts_UserId",
                table: "generate_email_prompts");

            migrationBuilder.DropIndex(
                name: "IX_generate_email_prompts_UserId_IsActive",
                table: "generate_email_prompts");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "generate_email_prompts");

            migrationBuilder.CreateIndex(
                name: "IX_generate_email_prompts_IsActive",
                table: "generate_email_prompts",
                column: "IsActive");
        }
    }
}
