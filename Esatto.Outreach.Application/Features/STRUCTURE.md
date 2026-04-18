# Feature Slice Structure

This project uses strict feature + use-flow vertical slices in `Esatto.Outreach.Application/Features`.

## Goals

- Keep files easy to find as the backend grows.
- Keep use-case code local to the business flow it implements.
- Preserve clean architecture boundaries.
- Prefer consistency over local style variations.

## Folder Layout

Pattern:

`Features/<Feature>/<UseFlow>/`

Example:

`Features/Auth/AcceptInvitation/AcceptInvitationCommandHandler.cs`

### Allowed Feature-Level Shared Area

Use `Features/<Feature>/Shared/` only when an artifact is reused by 2+ use-flows in the same feature and duplication is clearly harmful.

Do not move operation-local request/response models into `Shared`.

## Naming Conventions

- Commands: `<UseFlow>Command`
- Command handlers: `<UseFlow>CommandHandler`
- Queries: `<UseFlow>Query`
- Query handlers: `<UseFlow>QueryHandler`
- API request models: `<UseFlow>Request`
- API response models: `<UseFlow>Response`
- DTO suffix uses `Dto` (not `DTO`).

Use-flow names should be business-intent names where possible:

- Prefer `AcceptInvitation`, `ActivateSequence`, `ClaimPendingProspect`.
- Avoid generic buckets as the final shape (`Create`, `Update`, `Get`, `List`) when a clearer flow name exists.

## Namespace Policy

Namespaces must match folder path:

`Esatto.Outreach.Application.Features.<Feature>.<UseFlow>`

Feature-level shared namespace:

`Esatto.Outreach.Application.Features.<Feature>.Shared`

## Contract Locality Rules

- Default: operation-local request/response model in the same use-flow folder.
- If a model is reused across multiple use-flows in the same feature, move it to feature `Shared`.
- Avoid cross-feature DTO coupling; map at boundaries instead.

## Tests Mirror Production

`Esatto.Outreach.UnitTests/Application/Features` mirrors `Esatto.Outreach.Application/Features`.

Pattern:

`Application/Features/<Feature>/<UseFlow>/<UseFlow>Tests.cs`

## Refactor Safety Rules

- Structural change first, behavior change only when required to compile.
- Keep routes and observable behavior stable during migration.
- Build and run affected tests after each feature migration.
- Never skip unresolved compile/test failures to continue to next feature.
