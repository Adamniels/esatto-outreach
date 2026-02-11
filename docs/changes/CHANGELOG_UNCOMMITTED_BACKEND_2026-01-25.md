# Uncommitted Change Log — Backend — 2026-01-25

## 1) Summary
- **Total files changed**: ~32 (12 modified, 20+ untracked/added)
- **Added**: Entire Workflow domain (Entities, API, Infrastructure), Tests project.
- **Modified**: Email generators, Prospect endpoints, Program.cs, Enrichment service.
- **Key themes**:
  - **Feature**: Implemented "Outreach Workflows" engine (Templates, Instances, Background Worker).
  - **Refactor**: Hardened API error handling in `ProspectEndpoints`.
  - **Enhancement**: Improved AI email generation prompts and fallbacks.
  - **Infrastructure**: Added Database Migrations for Workflows.
  - **Testing**: Added new Test project.

## 2) File-by-file breakdown

### `Esatto.Outreach.Api/Program.cs`
- **Change type**: Modified
- **Why**: To register new Workflow services and background workers, and improve JSON serialization.
- **What changed**:
  - Added `JsonStringEnumConverter` to JSON options (allows string enums in API).
  - Registered `WorkflowExecutionWorker` and `WorkflowCleanupWorker` hosted services.
  - Mapped `MapWorkflowEndpoints()`.
  - Added `partial class Program` for integration testing visibility.
- **Risk / Impact**: **Medium**. Background workers now run on startup. JSON Enum serialization changes apply globally.

### `Esatto.Outreach.Api/Endpoints/ProspectEndpoints.cs`
- **Change type**: Modified
- **Why**: To standarize error handling and delegate validation to the UseCase layer.
- **What changed**:
  - Removed formatting-check `if (string.IsNullOrWhiteSpace(dto.Name))` from the endpoint handler.
  - Wrapped calls in `try/catch (ArgumentException)` to return `400 Bad Request`.
  - Wrapped `try/catch (InvalidOperationException)` to return `409 Conflict`.
- **Risk / Impact**: **Low/Medium**. API Contract change: Validation errors might look slightly different (wrapped in JSON) depending on previous implementation.

### `Esatto.Outreach.Api/Endpoints/WorkflowEndpoints.cs` (Untracked/New)
- **Change type**: Added
- **Why**: To expose Workflow management to the frontend.
- **What changed**:
  - `POST /prospects/{id}/workflows`: Create instance from template.
  - `POST /workflow-instances/{id}/activate`: Schedule workflow.
  - `POST /workflow-instances/{id}/steps`: Add steps dynamically.
  - `GET`: Retrieve instances and validate activation.
- **Risk / Impact**: New API surface.

### `Esatto.Outreach.Infrastructure/OutreachDbContext.cs` & `Migrations/*`
- **Change type**: Modified & Added
- **Why**: To persist Workflow entities.
- **What changed**:
  - Added `DbSets` for `WorkflowTemplates`, `WorkflowTemplateSteps`, `WorkflowInstances`, `WorkflowSteps`.
  - Added multiple migration files (`AddWorkflowEntities`, `AddWorkflowTables`, `UpdateSchedulingSchema`, etc.).
- **Risk / Impact**: **High**. Database schema modification required.

### `Esatto.Outreach.Infrastructure/EmailGeneration/*.cs` (CollectedData & OpenAICustom)
- **Change type**: Modified
- **Why**: To better handle cases where no specific "Contact Person" is identified and improve signature formatting.
- **What changed**:
  - **CollectedDataEmailGenerator**: Added fallback logic: "If no contact person exists, write generically to the company. Do NOT use [Name] placeholders."
  - **OpenAICustomEmailGenerator**: Added explicit instruction to "Focus on how Esatto AB can help" and similar fallback logic for missing contacts.
- **Risk / Impact**: **Low**. Purely prompt/logic improvement. User-visible change in generated emails.

### `Esatto.Outreach.Infrastructure/Services/Enrichment/CompanyEnrichmentService.cs`
- **Change type**: Modified
- **Why**: To reduce log noise in production.
- **What changed**:
  - Changed `LogInformation` to `LogDebug` for large data dumps (internal nuggets, external nuggets).
- **Risk / Impact**: **None**.

### `Esatto.Outreach.Domain/Entities/*.cs` (Workflow*) (Untracked/New)
- **Change type**: Added
- **Why**: Core domain modeling for the new feature.
- **What changed**:
  - `WorkflowInstance`: Tracks status (Draft, Active, Paused, Completed).
  - `WorkflowStep`: Represents individual actions (Email, LinkedIn, etc.) with `GenerationStrategy`.
  - `WorkflowTemplate`: Blueprints for workflows.

### `Esatto.Outreach.Api/Workers/*.cs` (Untracked/New)
- **Change type**: Added
- **Why**: To execute workflow steps in the background.
- **What changed**:
  - `WorkflowExecutionWorker`: Polls for pending steps and executes them.
  - `WorkflowCleanupWorker`: Likely handles maintenance.
- **Risk / Impact**: **Medium**. New background processes affecting system resources.

## 3) Behavioral changes
- **User-visible (UI)**:
  - Users can now create, view, and activate "Workflows" for prospects.
  - Emails generated for prospects without a Contact Person will no longer look broken (no "[Namn]" placeholders).
- **API behavior**:
  - Global JSON serialization now treats Enums as Strings.
  - Prospect endpoints return 400/409 on specific exceptions.
- **Worker/Scheduler**:
  - New background worker polling database for due workflow steps.

## 4) Tests
- **Added**: `Esatto.Outreach.Tests` project (untracked).
- **Gaps**:
  - Verify if `WorkflowExecutionWorker` has integration tests.

## 5) Review checklist
- [ ] Verify `JsonStringEnumConverter` doesn't break existing endpoints expecting integers.
- [ ] Review `WorkflowExecutionWorker` polling interval and locking mechanism.
- [ ] Check migration order (multiple migrations are untracked; ensure they apply cleanly).
- [ ] Confirm `WorkflowMocks.cs` is not being used in production code (check DI registration).
