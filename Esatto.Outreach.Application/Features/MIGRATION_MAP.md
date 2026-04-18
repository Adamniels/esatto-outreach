# Migration Map

This map defines the target use-flow names and locations before moving files.

## Auth

- `Accept` -> `AcceptInvitation`
- `Create` -> `InviteUser`
- `Login` -> `Login`
- `Refresh` -> `RefreshToken`
- `Register` -> `RegisterUser`
- `Validate` -> `ValidateInvitation`
- `Common` -> `Shared`

## OutreachGeneration

- `Generate` split into:
  - `GenerateEmailDraft`
  - `GenerateLinkedInDraft`
- `Contracts` -> operation-local response models where possible, otherwise `Shared`

## OutreachPrompts

- `Activate` -> `ActivateOutreachPrompt`
- `Create` -> `CreateOutreachPrompt`
- `Delete` -> `DeleteOutreachPrompt`
- `Get` split into:
  - `GetActiveOutreachPrompt`
  - `GetOutreachPromptById`
- `List` -> `ListOutreachPrompts`
- `Update` -> `UpdateOutreachPrompt`
- `Common` -> `Shared`

## ProjectCases

- `Create` -> `CreateProjectCase`
- `Delete` -> `DeleteProjectCase`
- `Get` split into:
  - `GetProjectCase`
  - `ListProjectCases`
- `Update` -> `UpdateProjectCase`

## Webhooks

- `Callback` -> `HandleCapsuleWebhook`
- `Create` -> `CreateOrUpdateProspectFromCapsule`
- `Claim` -> `ClaimPendingProspect`
- `Reject` -> `RejectPendingProspect`
- `List` -> `ListPendingProspects`
- `Contracts` -> `Shared` (Capsule payload contracts shared by multiple flows)

## Intelligence

- `Chat` -> `ChatWithProspect`
- `Enrich` -> `EnrichContactPerson`
- `Generate` -> `GenerateEntityIntelligence`
- `Get` -> `GetCompanyInfo`
- `Reset` -> `ResetProspectChat`
- `Update` -> `UpdateCompanyInfo`
- `Contracts` -> `Shared`

## Prospects

- `Create` -> `CreateProspect`
- `Update` split into:
  - `UpdateProspect`
  - `UpdateContactPerson`
- `Add` -> `AddContactPerson`
- `Delete` split into:
  - `DeleteProspect`
  - `DeleteContactPerson`
- `Set` -> `SetActiveContact`
- `Clear` -> `ClearActiveContact`
- `Get` split into:
  - `GetProspectById`
  - `GetActiveContact`
- `List` -> `ListProspects`
- `Contracts` -> operation-local first, minimal `Shared`

## Sequences

- `Create` -> `CreateSequence`
- `Update` split into:
  - `UpdateSequence`
  - `UpdateSequenceStep`
  - `UpdateSequenceStepContent`
- `Delete` split into:
  - `DeleteSequence`
  - `DeleteSequenceStep`
- `Add` -> `AddSequenceStep`
- `ReorderSequenceSteps` -> `ReorderSequenceSteps`
- `Enroll` -> `EnrollProspectInSequence`
- `Remove` -> `RemoveProspectFromSequence`
- `Generate` -> `GenerateSequenceStepContent`
- `Activate` -> `ActivateSequence`
- `Pause` -> `PauseSequence`
- `Cancel` -> `CancelSequence`
- `Save` -> `SaveSequenceBuilderProgress`
- `Complete` -> `CompleteSequenceSetup`
- `Get` -> `GetSequence`
- `List` -> `ListSequences`
- `Orchestration` -> `RunSequenceOrchestration`
- `Common` -> `Shared`
- `Contracts` -> minimal `Shared`
