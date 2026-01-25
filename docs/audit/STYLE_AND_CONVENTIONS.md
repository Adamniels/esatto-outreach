# TODO: might want to add this to a better folder for AI agents to use??
# Style and Conventions

## Architecture
- **Clean Architecture**:
    - `Api`: Minimal logic, maps HTTP -> Use Cases.
    - `Application`: Orchestration, DTOs, interfaces.
    - `Domain`: Pure C# logic, Entities, Enums. No external deps.
    - `Infrastructure`: Implementation of interfaces (EF, Emails, AI).
- **Services**: Registered in `DependencyInjection.cs` per layer.

## Coding Standards

### Backend (C#)
- **Namespaces**: `Esatto.Outreach.<Layer>.<Feature>`
- **Naming**:
    - Classes: PascalCase
    - Interfaces: `I` prefix (e.g., `IProspectRepository`)
    - Async Methods: Suffix `Async` (e.g., `HandleAsync`, `GetByIdAsync`) - *Note: Use Cases currently use `Handle` without async suffix, inconsistent with Repos.*
    - Private fields: `_camelCase`
- **Nullable Reference Types**: Enabled (`<Nullable>enable</Nullable>`).
- **Entity Framework**:
    - Use `Guid` for IDs.
    - Inherit from `Entity` base class.
    - Use private setters + public factory/update methods (Domain-Driven Design).
- **Controllers/Endpoints**: Use Minimal API (`app.MapGet` etc) organized in `Endpoints/*.cs` static classes.

### Frontend (Vue)
- **Framework**: Vue 3 + Vite.
- **Language**: TypeScript.
- **Components**: Composition API (`<script setup lang="ts">`).
- **Styling**: Vanilla CSS imported globally or scoped.
- **State**: Composables (e.g. `useAuth.ts`) for shared state.

## Recommended Standardizations
1.  **Async Suffix**: Adopt strictly for all async methods, including Use Cases (`HandleAsync`).
2.  **Validation**: Adopt a standard result pattern or exception-filter pattern for validation failures instead of checking in Endpoints.
3.  **DateTime**: Always use `DateTime.UtcNow`. Never `DateTime.Now`. Enforce via architecture test or linter.
