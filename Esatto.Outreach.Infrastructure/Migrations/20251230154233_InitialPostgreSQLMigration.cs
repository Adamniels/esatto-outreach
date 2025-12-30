using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Esatto.Outreach.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgreSQLMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop all existing tables to start fresh with correct PostgreSQL types
            migrationBuilder.Sql(@"
                DROP TABLE IF EXISTS ""AspNetRoleClaims"" CASCADE;
                DROP TABLE IF EXISTS ""AspNetUserClaims"" CASCADE;
                DROP TABLE IF EXISTS ""AspNetUserLogins"" CASCADE;
                DROP TABLE IF EXISTS ""AspNetUserRoles"" CASCADE;
                DROP TABLE IF EXISTS ""AspNetUserTokens"" CASCADE;
                DROP TABLE IF EXISTS refresh_tokens CASCADE;
                DROP TABLE IF EXISTS generate_email_prompts CASCADE;
                DROP TABLE IF EXISTS soft_company_data CASCADE;
                DROP TABLE IF EXISTS hard_company_data CASCADE;
                DROP TABLE IF EXISTS prospects CASCADE;
                DROP TABLE IF EXISTS ""AspNetUsers"" CASCADE;
                DROP TABLE IF EXISTS ""AspNetRoles"" CASCADE;
            ");

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    FullName = table.Column<string>(type: "text", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastLoginUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "hard_company_data",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyOverview = table.Column<string>(type: "text", nullable: true),
                    ServicesJson = table.Column<string>(type: "text", nullable: true),
                    CasesJson = table.Column<string>(type: "text", nullable: true),
                    IndustriesJson = table.Column<string>(type: "text", nullable: true),
                    KeyFactsJson = table.Column<string>(type: "text", nullable: true),
                    SourcesJson = table.Column<string>(type: "text", nullable: true),
                    ResearchedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hard_company_data", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    RoleId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "generate_email_prompts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Instructions = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsRevoked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_tokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_refresh_tokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "prospects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CapsuleId = table.Column<long>(type: "bigint", nullable: true),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    About = table.Column<string>(type: "text", nullable: true),
                    CapsuleCreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CapsuleUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastContactedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PictureURL = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Websites = table.Column<string>(type: "jsonb", nullable: false),
                    EmailAddresses = table.Column<string>(type: "jsonb", nullable: false),
                    PhoneNumbers = table.Column<string>(type: "jsonb", nullable: false),
                    Addresses = table.Column<string>(type: "jsonb", nullable: false),
                    Tags = table.Column<string>(type: "jsonb", nullable: false),
                    CustomFields = table.Column<string>(type: "jsonb", nullable: false),
                    IsPending = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    MailTitle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    MailBodyPlain = table.Column<string>(type: "text", nullable: true),
                    MailBodyHTML = table.Column<string>(type: "text", nullable: true),
                    LastOpenAIResponseId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    HardCompanyDataId = table.Column<Guid>(type: "uuid", nullable: true),
                    SoftCompanyDataId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OwnerId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prospects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_prospects_AspNetUsers_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_prospects_hard_company_data_HardCompanyDataId",
                        column: x => x.HardCompanyDataId,
                        principalTable: "hard_company_data",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "soft_company_data",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProspectId = table.Column<Guid>(type: "uuid", nullable: false),
                    HooksJson = table.Column<string>(type: "text", nullable: true),
                    RecentEventsJson = table.Column<string>(type: "text", nullable: true),
                    NewsItemsJson = table.Column<string>(type: "text", nullable: true),
                    SocialActivityJson = table.Column<string>(type: "text", nullable: true),
                    SourcesJson = table.Column<string>(type: "text", nullable: true),
                    ResearchedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_soft_company_data", x => x.Id);
                    table.ForeignKey(
                        name: "FK_soft_company_data_prospects_ProspectId",
                        column: x => x.ProspectId,
                        principalTable: "prospects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_generate_email_prompts_UserId",
                table: "generate_email_prompts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_generate_email_prompts_UserId_IsActive",
                table: "generate_email_prompts",
                columns: new[] { "UserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_hard_company_data_ResearchedAt",
                table: "hard_company_data",
                column: "ResearchedAt");

            migrationBuilder.CreateIndex(
                name: "IX_prospects_CapsuleId",
                table: "prospects",
                column: "CapsuleId",
                unique: true,
                filter: "\"CapsuleId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_prospects_HardCompanyDataId",
                table: "prospects",
                column: "HardCompanyDataId");

            migrationBuilder.CreateIndex(
                name: "IX_prospects_IsPending",
                table: "prospects",
                column: "IsPending");

            migrationBuilder.CreateIndex(
                name: "IX_prospects_IsPending_CreatedUtc",
                table: "prospects",
                columns: new[] { "IsPending", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_prospects_Name",
                table: "prospects",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_prospects_OwnerId",
                table: "prospects",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_prospects_OwnerId_Status",
                table: "prospects",
                columns: new[] { "OwnerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_prospects_SoftCompanyDataId",
                table: "prospects",
                column: "SoftCompanyDataId");

            migrationBuilder.CreateIndex(
                name: "IX_prospects_Status_CreatedUtc",
                table: "prospects",
                columns: new[] { "Status", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_ExpiresAt",
                table: "refresh_tokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_Token",
                table: "refresh_tokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_UserId",
                table: "refresh_tokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_soft_company_data_ProspectId",
                table: "soft_company_data",
                column: "ProspectId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_soft_company_data_ResearchedAt",
                table: "soft_company_data",
                column: "ResearchedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "generate_email_prompts");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "soft_company_data");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "prospects");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "hard_company_data");
        }
    }
}
