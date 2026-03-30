# Sequence Feature - Build Plan

## Overview

The Sequence feature is an orchestrator that automates multi-step outreach to prospects. It comes in two modes:

- **Focused** -- targets a single prospect. Enrichment (company + contact) can be enabled in settings so generated content uses deeply personalized data. Ideal for high-value targets.
- **Multi** -- targets many prospects. Execution is throttled (N active at a time) to avoid bans/flagging. AI can research similarities across enrolled prospects (enabled in settings) and generates content that applies to all of them. Ideal for campaigns.

Both modes share the same `Sequence` entity with a `SequenceMode` enum to distinguish them.

**Key design principles:**
- Steps belong to the Sequence, not to individual prospects. Generated content lives on `SequenceStep`.
- Enrichment/research is a **setting** on the Sequence, not a step type. When enabled, enrichment runs before content generation -- it feeds into generation, not into the step pipeline.
- For Multi mode the same generated content is sent to every enrolled prospect.
- The Sequence orchestrates *which* prospect to send each step to and *when*, tracking progress on `SequenceProspect`.

**User flow:**
1. Create a Sequence (choose Focused or Multi, configure settings)
2. Add outreach steps (email, LinkedIn, etc.)
3. Enroll prospect(s)
4. Call "Generate" (per step or all at once) -- runs enrichment/research if enabled, then AI creates content on each step
5. Review / edit the generated content
6. Activate -- the Sequence starts sending steps to prospects on schedule

---

## 1. Domain Model

### 1.1 Sequence (root aggregate)

```
Sequence : Entity
├── Title                  (string, required)
├── Description            (string?, optional)
├── Mode                   (enum: Focused | Multi)
├── Status                 (enum: Draft | Active | Paused | Completed | Archived | Failed)
├── OwnerId / Owner        (ApplicationUser)
├── Settings               (SequenceSettings, owned entity -- embedded columns)
├── SequenceSteps          (ordered list of outreach steps with generated content)
└── SequenceProspects      (enrolled prospects with progress tracking)
```

### 1.2 SequenceSettings (owned entity, embedded in Sequences table)

Settings control pre-generation behavior. Which flags are available depends on the mode.

```
SequenceSettings
├── EnrichCompany          (bool?, Focused only -- enrich company before generation)
├── EnrichContact          (bool?, Focused only -- enrich contact before generation)
└── ResearchSimilarities   (bool?, Multi only -- AI finds commonalities across prospects)
```

When generation is triggered:
- If `EnrichCompany` / `EnrichContact` is true → run enrichment pipelines first, then generate content using the collected data
- If `ResearchSimilarities` is true → AI analyzes all enrolled prospects to find shared traits, then uses those in content generation

### 1.3 SequenceStep (belongs to Sequence)

A step defines *what* to do at a given position in the sequence. Generated content is stored directly on the step.

```
SequenceStep : Entity
├── SequenceId             (FK → Sequence)
├── OrderIndex             (int, execution order)
├── StepType               (enum: Email | LinkedInMessage | LinkedInConnectionRequest | LinkedInInteraction)
├── DelayInDays            (int, days to wait after previous step)
├── TimeOfDayToRun         (enum?, preferred send window)
├── GeneratedSubject       (string?, for email steps)
├── GeneratedBody          (string?, generated content)
└── UseCollectedData       (bool?, controls whether generation uses collected/enriched data)
```

**SequenceStepType enum:**

| Value | Description |
|-------|-------------|
| `Email` | Send an email |
| `LinkedInMessage` | Send a LinkedIn message |
| `LinkedInConnectionRequest` | Send a connection request with message |
| `LinkedInInteraction` | Like/comment on prospect's content |

Adding a new step type = add enum value + implement an `IStepExecutor` for it.

**TimeOfDay enum:**

| Value | Window |
|-------|--------|
| `EarlyMorning` | 5am - 8am |
| `LateMorning` | 8am - 12pm |
| `EarlyAfternoon` | 12pm - 3pm |
| `LateAfternoon` | 3pm - 6pm |
| `Evening` | 6pm - 9pm |

### 1.4 SequenceProspect (enrollment + progress tracking)

Tracks each prospect's enrollment and how far through the steps they've progressed. The orchestrator uses `CurrentStepIndex` + `NextStepScheduledAt` to decide who gets the next step.

```
SequenceProspect : Entity
├── SequenceId             (FK → Sequence)
├── ProspectId             (FK → Prospect)
├── ContactPersonId        (FK → ContactPerson, the target contact)
├── Status                 (enum: Pending | Active | Completed | Failed)
├── CurrentStepIndex       (int, index into the Sequence's SequenceSteps)
├── NextStepScheduledAt    (DateTime?, when the next step should execute)
├── LastStepExecutedAt     (DateTime?, when the last step ran)
├── ActivatedAt            (DateTime?, when this prospect started)
├── CompletedAt            (DateTime?, when finished all steps)
├── FailureReason          (string?, if status is Failed)
└── RowVersion             (byte[], concurrency token)
```

### 1.5 Enums

```csharp
public enum SequenceMode
{
    Focused,    // single prospect, enrichment settings available
    Multi       // multiple prospects, throttled, same content for all
}

public enum SequenceStatus
{
    Draft,      // being built -- steps, prospects, settings can be edited
    Active,     // executing -- worker is sending steps on schedule
    Paused,     // temporarily stopped
    Completed,  // all prospects finished
    Archived,   // manually archived by user
    Failed      // sequence-level failure
}

public enum SequenceProspectStatus
{
    Pending,    // enrolled but not yet started (waiting for throttle slot)
    Active,     // currently progressing through steps
    Completed,  // all steps sent successfully
    Failed      // a step failed after retries
}

public enum SequenceStepType
{
    Email,
    LinkedInMessage,
    LinkedInConnectionRequest,
    LinkedInInteraction
}

public enum TimeOfDay
{
    EarlyMorning,       // 5am - 8am
    LateMorning,        // 8am - 12pm
    EarlyAfternoon,     // 12pm - 3pm
    LateAfternoon,      // 3pm - 6pm
    Evening             // 6pm - 9pm
}
```

### Entity Relationship Diagram

```
Sequence (1) ──── (*) SequenceStep       [outreach step definitions + generated content]
    │
    ├──────── (1) SequenceSettings       [owned entity, embedded columns]
    │
    └──────── (*) SequenceProspect        [enrollment + progress per prospect]
```

---

## 2. Application Layer

### 2.1 Use Cases

**Sequence management:**
- `CreateSequence` -- create with title, description, mode, initial settings
- `UpdateSequence` -- update title, description, settings
- `DeleteSequence` -- delete a Draft sequence (prevent deleting active ones)
- `GetSequence` -- load with steps and prospect enrollment summary
- `ListSequences` -- list for the current user

**Step management:**
- `AddSequenceStep` -- add a step (validate step type exists)
- `UpdateSequenceStep` -- change timing, type, or edit generated content
- `DeleteSequenceStep` -- remove a step (only on Draft sequences)
- `ReorderSequenceSteps` -- change step ordering

**Content generation:**
- `GenerateStepContent` -- generate content for a single step
  1. If enrichment settings are enabled and not yet run → run enrichment first
  2. Build outreach context → call AI generator → store result on the step
- `GenerateAllStepContent` -- generate content for all outreach steps in sequence

**Prospect enrollment:**
- `EnrollProspect` -- add a prospect (+ contact person) to a sequence
- `EnrollMultipleProspects` -- bulk enroll for Multi mode
- `RemoveProspect` -- remove from sequence

**Execution control:**
- `ActivateSequence` -- validate (all steps have generated content, prospects enrolled, Focused has exactly 1 prospect) → move from Draft → Active
- `PauseSequence` -- pause execution
- `ResumeSequence` -- resume
- `CancelSequence` -- stop and mark as completed/archived

### 2.2 Application Services

**`SequenceOrchestrator`** -- core service called by the background worker:
1. Find sequences with `Status == Active`
2. For each, find `SequenceProspect` entries where `Status == Active` and `NextStepScheduledAt <= now`
3. Look up the step at `CurrentStepIndex`
4. Execute the step via the appropriate `IStepExecutor` (pass generated content + prospect + contact)
5. On success: increment `CurrentStepIndex`, calculate `NextStepScheduledAt` from the next step's `DelayInDays` + `TimeOfDayToRun`, or mark as `Completed` if last step
6. On failure: retry or mark prospect as `Failed`

**`SequenceActivator`** -- handles activation logic:
- Validate all steps have `GeneratedBody` (and `GeneratedSubject` for email steps)
- For Focused: validate exactly 1 prospect enrolled
- For Multi: validate at least 1 prospect enrolled
- Set `Sequence.Status = Active`
- Activate the first batch of prospects (throttled for Multi), calculate their `NextStepScheduledAt`

**`SequenceContentGenerator`** -- generates content for steps:
- Checks `SequenceSettings` -- runs enrichment/research if enabled and needed
- Uses `IOutreachContextBuilder` + `IOutreachGeneratorFactory`
- For Focused + `UseCollectedData`: builds context from enriched prospect data
- For Multi + `ResearchSimilarities`: builds context from commonalities across all prospects
- Stores result on `SequenceStep.GeneratedSubject` / `GeneratedBody`

### 2.3 Step Executors (Strategy Pattern)

```
IStepExecutor
├── EmailStepExecutor              (sends email via IEmailSender)
├── LinkedInMessageExecutor        (sends via ILinkedInActionsClient)
├── LinkedInConnectionExecutor     (sends connection request)
└── LinkedInInteractionExecutor    (performs interaction)
```

Each executor implements:
```csharp
public interface IStepExecutor
{
    SequenceStepType StepType { get; }
    Task ExecuteAsync(StepExecutionContext context, CancellationToken ct);
}
```

`StepExecutionContext` contains: the `SequenceStep` (with generated content), the `SequenceProspect`, the `Prospect`, and the `ContactPerson`.

Adding a new step type = add enum value + new `IStepExecutor` class + register in DI.

---

## 3. Infrastructure

### 3.1 Repository

`ISequenceRepository` / `SequenceRepository`:
- CRUD for Sequence, SequenceStep, SequenceProspect
- Query: find active sequences with prospects due for next step (`Status == Active`, `NextStepScheduledAt <= now`)
- Query: find pending prospects for throttle activation

### 3.2 EF Configuration

`SequenceConfiguration.cs`:
- Table names: `Sequences`, `SequenceSteps`, `SequenceProspects`
- `SequenceSettings` configured as an owned entity (embedded columns on `Sequences` table)
- Index on `SequenceProspect(Status, NextStepScheduledAt)` for efficient worker queries
- Value converters for enums → string
- RowVersion concurrency token on `SequenceProspect`
- `TimeOfDay` stored as string

### 3.3 Migration

Single migration to create 3 tables: `Sequences` (with embedded settings columns), `SequenceSteps`, `SequenceProspects`.

---

## 4. API Layer

### 4.1 Endpoints (`SequenceEndpoints.cs`)

```
POST   /sequences                              → CreateSequence
GET    /sequences                              → ListSequences
GET    /sequences/{id}                         → GetSequence
PUT    /sequences/{id}                         → UpdateSequence
DELETE /sequences/{id}                         → DeleteSequence

POST   /sequences/{id}/steps                  → AddSequenceStep
PUT    /sequences/{id}/steps/{stepId}         → UpdateSequenceStep
DELETE /sequences/{id}/steps/{stepId}         → DeleteSequenceStep
PUT    /sequences/{id}/steps/reorder          → ReorderSequenceSteps

POST   /sequences/{id}/steps/{stepId}/generate → GenerateStepContent
POST   /sequences/{id}/generate                → GenerateAllStepContent

POST   /sequences/{id}/prospects               → EnrollProspect(s)
DELETE /sequences/{id}/prospects/{spId}        → RemoveProspect

POST   /sequences/{id}/activate                → ActivateSequence
POST   /sequences/{id}/pause                   → PauseSequence
POST   /sequences/{id}/resume                  → ResumeSequence
POST   /sequences/{id}/cancel                  → CancelSequence
```

### 4.2 Background Workers

**`SequenceExecutionWorker`** -- runs on a timer (e.g. every 30s):
- Calls `SequenceOrchestrator` to find and execute due steps
- Uses `RowVersion` on `SequenceProspect` to prevent duplicate execution

**`SequenceThrottleWorker`** -- runs on a timer (e.g. every 60s):
- For Multi mode: checks if active prospect count < throttle limit
- Activates next pending prospect(s) up to the limit, calculates their `NextStepScheduledAt`

---

## 5. Frontend

### 5.1 Pages / Views

- **Sequences list page** (`/sequences`) -- shows all sequences with mode badge (Focused/Multi), status, prospect count
- **Sequence detail/builder page** (`/sequences/:id`) -- settings panel, step editor (reorder, add, remove), prospect enrollment, generate + review content, activation controls
- **Sequence monitoring view** -- progress of each prospect through steps

### 5.2 Components

- `SequenceBuilder.vue` -- visual step editor, add/remove/reorder steps
- `SequenceStepCard.vue` -- individual step with type icon, timing config, generated content preview + edit
- `SequenceSettingsPanel.vue` -- mode-dependent settings (enrichment toggles for Focused, research toggle for Multi)
- `SequenceProspectList.vue` -- manage enrolled prospects
- `SequenceProgress.vue` -- shows each prospect's progress through the sequence steps

### 5.3 API Service

- `sequenceService.ts` -- API client matching the backend endpoints
- `src/types/sequence.ts` -- TypeScript types for all DTOs

---

## 6. Build Order (incremental, each phase is shippable)

### Phase 1 -- Domain + Persistence (backend only)
- Finalize all entities in `SequenceFeature/` (already started)
- Add factory methods, validation, domain logic
- EF configuration + migration
- Repository implementation
- DbContext registration + DI

### Phase 2 -- CRUD Use Cases + API
- Create/Read/Update/Delete for Sequences, Steps, Prospects
- Wire up endpoints in `SequenceEndpoints.cs`
- Register use cases in `Program.cs`

### Phase 3 -- Content Generation
- `SequenceContentGenerator` service
- `GenerateStepContent` + `GenerateAllStepContent` use cases
- Generation endpoints

### Phase 4 -- Execution Engine (backend)
- `IStepExecutor` interface + all executor implementations
- `SequenceOrchestrator` + `SequenceActivator`
- Background workers (`SequenceExecutionWorker`, `SequenceThrottleWorker`)
- `IEmailSender` / `ILinkedInActionsClient` interfaces (re-introduce with mocks)
- Activation / pause / resume / cancel use cases

### Phase 5 -- Frontend: Sequence Builder
- Types + API service
- Sequences list page + routing
- Sequence detail page with settings, step builder, prospect enrollment
- Generate + review content UI

### Phase 6 -- Frontend: Execution Monitoring
- Sequence progress view
- Status updates
- Error display

### Phase 7 -- Settings & Polish
- Throttle settings for Multi mode
- Timezone handling
- Any additional settings that emerge

---

## Open Questions (to decide later)

- **Throttle settings** -- how many prospects per day? (default 5, but should be configurable per sequence?)
- **Retry policy** -- how many retries on failure? Exponential backoff?
- **Notifications** -- should the user be notified when a sequence completes or a step fails?
- **Analytics** -- track open rates, response rates per sequence?
- **ResearchSimilarities implementation** -- exact approach for AI finding commonalities across prospects
- **Timezone** -- whose timezone for TimeOfDay? The owner's? Stored on Sequence or on Settings?
