# Esatto.Outreach - Codebase Standards & Rules Document

This document defines the architectural standards, design patterns, and coding conventions for the Esatto.Outreach backend. It serves as a practical guide for human developers and AI agents to ensure long-term maintainability and consistency.

These rules are grounded in the actual state of the codebase following the transition to a modern Clean Architecture pattern.

---

## 1. Project Standards Assessment

The project follows a **strict Layered Clean Architecture** combined with a **Command-Pattern Use Case** design.
Historically, the project contained monolithic service classes and inconsistent error handling. Following recent refactoring, the core patterns have been standardized:
- Business logic is strictly isolated in single-responsibility Use Cases.
- Endpoints are extremely thin.
- Dependencies flow inwards towards the Domain.

---

## 2. Architecture & Dependency Rules

### The Layers
1. **Domain (`Esatto.Outreach.Domain`)**: The absolute core. Contains Entities, Enums, Value Objects, and Domain Exceptions. **Must not depend on any other project.**
2. **Application (`Esatto.Outreach.Application`)**: Contains Business Logic (Use Cases), DTOs, Application Services, and Abstractions (Interfaces) for external concerns. **Depends ONLY on Domain.**
3. **Infrastructure (`Esatto.Outreach.Infrastructure`)**: Implements the Application's abstractions (Repositories, AI Clients, Web Scrapers, Email Senders, DbContext). **Depends on Application and Domain.**
4. **API (`Esatto.Outreach.Api`)**: The entry point. Contains HTTP Endpoints, Web Workers, Configuration, and DI Registration. **Depends on Infrastructure, Application, and Domain.**

### Dependency Injection Rules
- **Use Cases & Application Services**: Must be strictly registered in `Esatto.Outreach.Api/Program.cs`. This provides a single source of truth for all system capabilities.
- **Infrastructure Implementations**: Must be registered in `Esatto.Outreach.Infrastructure/DependencyInjection.cs`.
- **Controllers/Endpoints**: The API uses Minimal APIs. Endpoints must be defined as extension methods on `WebApplication` mapped inside `Endpoints/` folders (e.g., `app.MapProspectEndpoints()`).

---

## 3. Directory & File Organization

### Application Layer
The Application layer is organized around **features** for Use Cases, but uses **technical grouping** for other primitives.
- `Abstractions/`: Contains interfaces categorized by responsibility (`Clients/`, `Repositories/`, `Services/`).
- `DTOs/`: Organized by feature area (e.g., `DTOs/Auth/`, `DTOs/Prospects/`). DTOs are defined as C# `record` types. Keep related Request and Response records in a single file per feature domain (e.g., `WorkflowDtos.cs`).
- `Services/`: Contains **internal business logic services** (e.g., `WorkflowDraftGenerator`, `WorkflowStepExecutor`).
  - **RULE**: Services in this directory are strictly for *Internal Use Only*. They exist to share complex logic between Use Cases or Background Workers. They **MUST NEVER** be directly injected into or called by an API Endpoint.
- `UseCases/`: Organized by feature area (`Prospects/`, `Workflows/`, `Auth/`, etc.).

### Domain Layer
- `Common/`: Contains base classes like `Entity.cs`.
- `Entities/`: The aggregate roots and child entities (e.g., `Prospect`, `WorkflowInstance`).
- `Enums/`: Domain-level enumerations.
- `Exceptions/`: Custom domain exceptions (e.g., `AuthenticationFailedException`, `DomainConcurrencyException`).
- `ValueObjects/`: Immutable data structures.

---

## 4. Coding Conventions: Use Cases & Endpoints

### Single-Responsibility Use Cases
We do not use monolithic `XYZService` classes (like `ProspectService` or `WorkflowInstanceService`).
- **RULE**: Every action the system can perform is represented by a single class in the `UseCases` folder.
- **Naming**: The class name is an active verb phrase representing the action (e.g., `CreateWorkflowInstance`, `UpdateProspect`, `AcceptInvitation`).
- **Method Signature**: The class MUST expose exactly one public method named `Handle()`. This method must contain the actual business logic. (Do not use `ExecuteAsync()` or other variations).

### Thin Minimal API Endpoints
- **RULE**: Endpoints are strictly responsible for HTTP translation. They parse requests, inject the correct Use Case, execute `Handle()`, catch exceptions, and return HTTP `200/201/204/400/404` results.
- Endpoints **MUST NOT** contain business logic, data mapping, or database calls.

---

## 5. Error Handling & Validation Patterns

The system relies heavily on the **Throw-or-Return** pattern instead of returning Tuples (no `return (false, null, "Error")`).

### "Not Found" / Missing Entities
- **RULE (Standardized Decision)**: If a requested entity does not exist, the Use Case **MUST throw a `KeyNotFoundException`**.
  - Example: `?? throw new KeyNotFoundException("Workflow instance not found");`
- The executing Endpoint must catch `KeyNotFoundException` and return `Results.NotFound()`.

### Business Rule Violations (Conflict, Invalid State)
- Use Cases should throw `InvalidOperationException` for business logic violations (e.g., "Workflow is not in draft state").
- Endpoints catch this and return `Results.BadRequest(new { error = ex.Message })` (or `Results.Conflict` where appropriate).

### Authentication & Authorization Failures
- Use `AuthenticationFailedException` (located in Domain/Exceptions) for credential failures, invalid tokens, or rejected invitations. Endpoints catch this and return `BadRequest` (for security obscurity) or `Unauthorized`.

---

## 6. Security & Ownership Authorization

This is a multi-tenant / multi-user system where prospects belong to specific users.

- **STRICT RULE**: All Use Cases that operate on user-specific entities (such as updating, deleting, or fetching a `Prospect` or its children) **MUST** receive the calling user's `userId` as an argument.
- **STRICT RULE**: The Use Case **MUST** explicitly verify ownership before performing operations.
  ```csharp
  var entity = await _repo.GetByIdAsync(id, ct);
  if (entity.OwnerId != userId)
      throw new UnauthorizedAccessException("You don't have permission to modify this prospect");
  ```
- *Note: This applies to all reads (`GetProspectById`) and writes.*

---

## 7. Data Mapping (DTOs)

- Use C# `record` types for all DTOs to ensure immutability.
- **Mapping Pattern**: Entities are translated to View/Response DTOs via a static factory method on the DTO itself.
  - **RULE**: Implement `public static MyViewDto FromEntity(MyEntity entity)` on the DTO. Do not use libraries like AutoMapper.
  - This keeps mapping logic predictable and tightly coupled to the data structure it produces.

---

## 8. Domain Entities & Persistence

- Entities inherit from `Entity` (providing `Id`, `CreatedUtc`, `UpdatedUtc`).
- **Encapsulation**: Entity properties should have `protected set;` or `private set;`.
- **State Changes**: State modifications must happen via explicit domain methods on the entity (e.g., `prospect.UpdateBasics(...)`, `prospect.AddNewContact(...)`, `template.AddStep(...)`), rather than ad-hoc property setters from the Application layer.
- Persistence is handled via the Repository pattern (`IProspectRepository`, `IWorkflowRepository`). Infrastructure implements these using Entity Framework Core (`OutreachDbContext`).

---

## 9. Testing Patterns

- Use **xUnit** as the test runner.
- Use **NSubstitute** for mocking infrastructure dependencies.
- Use **FluentAssertions** for readable assertions.
- **Test Factory**: Use `Esatto.Outreach.UnitTests.Helpers.TestFactory` for constructing valid, pre-configured Domain Entities required for tests. Do not construct monolithic test objects manually in every test file.
- **Exception Testing**: When testing the "Throw" pattern, use `act.Should().ThrowAsync<ExpectedException>()`.

---

## 10. Summary Checklist for New Features

When adding a new feature to the backend, an engineer or AI agent should follow this checklist:

1. [ ] Check if Domain Entities or Enums need updating. Add encapsulated state mutation methods.
2. [ ] Define necessary DTOs (Request/Response records) in `Application/DTOs/<FeatureName>/`. Implement `FromEntity()` if creating a response view.
3. [ ] Create a new class `<Action>Feature.cs` in `Application/UseCases/<FeatureName>/`. Provide a single `Handle(...)` method.
4. [ ] **Enforce Ownership Check** in the Use Case if operating on user data (`userId`).
5. [ ] **Enforce Error Handling** by throwing `KeyNotFoundException` for missing entities and `InvalidOperationException` for logic violations.
6. [ ] Register the new Use Case in `Api/Program.cs` as a scoped service.
7. [ ] Hook the Use Case to an HTTP Endpoint in `Api/Endpoints/<FeatureName>Endpoints.cs`. Handle exceptions and map them to standard HTTP status codes.
8. [ ] Write unit tests mirroring the Throw-or-Return behaviors using NSubstitute and FluentAssertions.
