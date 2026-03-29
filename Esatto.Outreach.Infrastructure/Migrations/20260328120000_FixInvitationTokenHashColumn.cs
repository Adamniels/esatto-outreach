using Esatto.Outreach.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Esatto.Outreach.Infrastructure.Migrations
{
    [DbContext(typeof(OutreachDbContext))]
    [Migration("20260328120000_FixInvitationTokenHashColumn")]
    public partial class FixInvitationTokenHashColumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = 'invitations'
                          AND column_name = 'Token'
                    ) AND NOT EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = 'invitations'
                          AND column_name = 'TokenHash'
                    ) THEN
                        ALTER TABLE invitations RENAME COLUMN "Token" TO "TokenHash";
                    END IF;
                END
                $$;
                """);

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM pg_indexes
                        WHERE schemaname = 'public'
                          AND tablename = 'invitations'
                          AND indexname = 'IX_invitations_Token'
                    ) AND NOT EXISTS (
                        SELECT 1
                        FROM pg_indexes
                        WHERE schemaname = 'public'
                          AND tablename = 'invitations'
                          AND indexname = 'IX_invitations_TokenHash'
                    ) THEN
                        ALTER INDEX "IX_invitations_Token" RENAME TO "IX_invitations_TokenHash";
                    END IF;
                END
                $$;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM pg_indexes
                        WHERE schemaname = 'public'
                          AND tablename = 'invitations'
                          AND indexname = 'IX_invitations_TokenHash'
                    ) AND NOT EXISTS (
                        SELECT 1
                        FROM pg_indexes
                        WHERE schemaname = 'public'
                          AND tablename = 'invitations'
                          AND indexname = 'IX_invitations_Token'
                    ) THEN
                        ALTER INDEX "IX_invitations_TokenHash" RENAME TO "IX_invitations_Token";
                    END IF;
                END
                $$;
                """);

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = 'invitations'
                          AND column_name = 'TokenHash'
                    ) AND NOT EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = 'invitations'
                          AND column_name = 'Token'
                    ) THEN
                        ALTER TABLE invitations RENAME COLUMN "TokenHash" TO "Token";
                    END IF;
                END
                $$;
                """);
        }
    }
}
