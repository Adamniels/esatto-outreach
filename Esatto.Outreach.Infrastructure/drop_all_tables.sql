-- Drop all foreign key constraints first
ALTER TABLE IF EXISTS prospects DROP CONSTRAINT IF EXISTS "FK_prospects_AspNetUsers_OwnerId";
ALTER TABLE IF EXISTS prospects DROP CONSTRAINT IF EXISTS "FK_prospects_hard_company_data_HardCompanyDataId";
ALTER TABLE IF EXISTS soft_company_data DROP CONSTRAINT IF EXISTS "FK_soft_company_data_prospects_ProspectId";
ALTER TABLE IF EXISTS generate_email_prompts DROP CONSTRAINT IF EXISTS "FK_generate_email_prompts_AspNetUsers_UserId";
ALTER TABLE IF EXISTS refresh_tokens DROP CONSTRAINT IF EXISTS "FK_refresh_tokens_AspNetUsers_UserId";

-- Drop all tables
DROP TABLE IF EXISTS "AspNetRoleClaims" CASCADE;
DROP TABLE IF EXISTS "AspNetUserClaims" CASCADE;
DROP TABLE IF EXISTS "AspNetUserLogins" CASCADE;
DROP TABLE IF EXISTS "AspNetUserRoles" CASCADE;
DROP TABLE IF EXISTS "AspNetUserTokens" CASCADE;
DROP TABLE IF EXISTS refresh_tokens CASCADE;
DROP TABLE IF EXISTS generate_email_prompts CASCADE;
DROP TABLE IF EXISTS soft_company_data CASCADE;
DROP TABLE IF EXISTS hard_company_data CASCADE;
DROP TABLE IF EXISTS prospects CASCADE;
DROP TABLE IF EXISTS "AspNetUsers" CASCADE;
DROP TABLE IF EXISTS "AspNetRoles" CASCADE;
DROP TABLE IF EXISTS "__EFMigrationsHistory" CASCADE;
