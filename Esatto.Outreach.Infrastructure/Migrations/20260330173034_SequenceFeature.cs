using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Esatto.Outreach.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SequenceFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Sequences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Mode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OwnerId = table.Column<string>(type: "text", nullable: true),
                    Setting_EnrichCompany = table.Column<bool>(type: "boolean", nullable: true),
                    Setting_EnrichContact = table.Column<bool>(type: "boolean", nullable: true),
                    Setting_ResearchSimilarities = table.Column<bool>(type: "boolean", nullable: true),
                    Setting_MaxActiveProspectsPerDay = table.Column<int>(type: "integer", nullable: true),
                    MultiEnrichment = table.Column<string>(type: "text", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sequences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sequences_AspNetUsers_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SequenceProspects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SequenceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProspectId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContactPersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CurrentStepIndex = table.Column<int>(type: "integer", nullable: false),
                    NextStepScheduledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastStepExecutedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ActivatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SequenceProspects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SequenceProspects_ContactPersons_ContactPersonId",
                        column: x => x.ContactPersonId,
                        principalTable: "ContactPersons",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SequenceProspects_Sequences_SequenceId",
                        column: x => x.SequenceId,
                        principalTable: "Sequences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SequenceProspects_prospects_ProspectId",
                        column: x => x.ProspectId,
                        principalTable: "prospects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SequenceSteps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SequenceId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false),
                    StepType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DelayInDays = table.Column<int>(type: "integer", nullable: false),
                    TimeOfDayToRun = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    GeneratedSubject = table.Column<string>(type: "text", nullable: true),
                    GeneratedBody = table.Column<string>(type: "text", nullable: true),
                    UseCollectedData = table.Column<bool>(type: "boolean", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SequenceSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SequenceSteps_Sequences_SequenceId",
                        column: x => x.SequenceId,
                        principalTable: "Sequences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SequenceProspects_ContactPersonId",
                table: "SequenceProspects",
                column: "ContactPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_SequenceProspects_ProspectId",
                table: "SequenceProspects",
                column: "ProspectId");

            migrationBuilder.CreateIndex(
                name: "IX_SequenceProspects_SequenceId",
                table: "SequenceProspects",
                column: "SequenceId");

            migrationBuilder.CreateIndex(
                name: "IX_SequenceProspects_WorkerQueue",
                table: "SequenceProspects",
                columns: new[] { "Status", "NextStepScheduledAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Sequences_OwnerId",
                table: "Sequences",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_SequenceSteps_SequenceId",
                table: "SequenceSteps",
                column: "SequenceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SequenceProspects");

            migrationBuilder.DropTable(
                name: "SequenceSteps");

            migrationBuilder.DropTable(
                name: "Sequences");
        }
    }
}
