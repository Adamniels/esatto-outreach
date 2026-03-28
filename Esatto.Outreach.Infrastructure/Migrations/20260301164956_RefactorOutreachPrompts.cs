using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Esatto.Outreach.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactorOutreachPrompts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "generate_email_prompts");

            migrationBuilder.AddColumn<string>(
                name: "LinkedInMessage",
                table: "prospects",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "outreach_prompts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Instructions = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outreach_prompts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_outreach_prompts_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_outreach_prompts_UserId",
                table: "outreach_prompts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_outreach_prompts_UserId_Type_IsActive",
                table: "outreach_prompts",
                columns: new[] { "UserId", "Type", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "outreach_prompts");

            migrationBuilder.DropColumn(
                name: "LinkedInMessage",
                table: "prospects");

            migrationBuilder.CreateTable(
                name: "generate_email_prompts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Instructions = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_generate_email_prompts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_generate_email_prompts_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_generate_email_prompts_UserId",
                table: "generate_email_prompts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_generate_email_prompts_UserId_IsActive",
                table: "generate_email_prompts",
                columns: new[] { "UserId", "IsActive" });
        }
    }
}
