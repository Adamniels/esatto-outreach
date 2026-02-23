-- Rensar alla rader (behåller tabeller och schema).
--
-- Användarnamn och databas hittar du i:
--   Esatto.Outreach.Api/appsettings.Development.json  (lokal Docker)
--     → ConnectionStrings:Default har Host, Database, Username, Password
--   Esatto.Outreach.Api/appsettings.json  (t.ex. Azure)
--     → samma, men lösenord kan komma från miljövariabel (DB_PASSWORD).
--
-- Lokal Docker (Development): oftast Database=outreach_dev, Username=postgres.
-- Kör t.ex.: psql -h localhost -p 5432 -U postgres -d outreach_dev -f clear-all-data.sql
-- (Lösenord: står i appsettings.Development.json, t.ex. localdevpassword)

TRUNCATE TABLE
  "AspNetUserTokens",
  "AspNetUserRoles",
  "AspNetUserLogins",
  "AspNetUserClaims",
  "AspNetRoleClaims",
  "AspNetRoles",
  "AspNetUsers",
  "companies",
  "refresh_tokens",
  "generate_email_prompts",
  "WorkflowSteps",
  "WorkflowInstances",
  "WorkflowTemplateSteps",
  "WorkflowTemplates",
  "entity_intelligence",
  "ContactPersons",
  "prospects"
RESTART IDENTITY CASCADE;
