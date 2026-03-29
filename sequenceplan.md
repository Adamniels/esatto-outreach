# Sequence Feature - Build Plan

## Overview

The Sequence feature is an orchestrator that automates multi-step outreach to prospects. It comes in two modes:

- **FocusedSequence** -- targets a single prospect. Allows enrichment steps (company + contact) so every outreach step can use deeply personalized data. Ideal for high-value targets.
- **MultiSequence** -- targets many prospects. Execution is throttled (N active at a time) to avoid bans/flagging. AI finds commonalities across prospects to use in outreach. Ideal for campaigns.

Both modes share the same `Sequence` entity with a `SequenceMode` enum to distinguish them. Steps are defined once on the Sequence and executed per prospect.

---

## 1. Domain Model

### 1.1 Sequence (root aggregate)

```
Sequence : Entity
├── SequenceMode           (enum: Focused | Multi)
├── Name                   (string)
├── Status                 (enum: Draft | Active | Paused | Completed)
├── OwnerId / Owner        (ApplicationUser)
├── Settings               (JSON or child entity -- placeholder, TBD)
├── SequenceSteps          (ordered list of step definitions)
└── SequenceProspects      (join: which prospects are enrolled)
```

### 1.2 SequenceStep (step definition, belongs to Sequence)

A step is a **template** -- it defines *what* to do at a given position in the sequence. When executed for a prospect, a `SequenceProspectStep` is created to track that execution.

```
SequenceStep : Entity
├── SequenceId             (FK → Sequence)
├── OrderIndex             (int, determines execution order)
├── StepType               (enum -- see below)
├── DayOffset              (int, days after previous step to wait)
├── TimeOfDay              (TimeSpan, preferred send time)
└── Config                 (JSON or child -- type-specific settings, extensible)
```

**SequenceStepType enum** (easy to extend):

| Value | Available in | Description |
|-------|-------------|-------------|
| `EnrichCompany` | Focused only | Run company enrichment pipeline |
| `EnrichContact` | Focused only | Run contact enrichment pipeline |
| `Email` | Both | Send an AI-generated email |
| `LinkedInMessage` | Both | Send a LinkedIn message |
| `LinkedInConnectionRequest` | Both | Send a connection request with message |
| `LinkedInInteraction` | Both | Like/comment on prospect's content |

Adding a new step type = add enum value + implement an `IStepExecutor` for it (see section 3).

### 1.3 SequenceProspect (enrollment)

Tracks each prospect's participation in the sequence.

```
SequenceProspect : Entity
├── SequenceId             (FK → Sequence)
├── ProspectId             (FK → Prospect)
├── ContactPersonId        (FK → ContactPerson, the target contact)
├── Status                 (enum: Pending | Active | Completed | Failed | Paused)
├── CurrentStepIndex       (int, which step they're on)
├── ActivatedAt            (DateTime?, when this prospect started)
└── CompletedAt            (DateTime?, when finished all steps)
```

### 1.4 SequenceProspectStep (per-prospect step execution)

Created when a prospect reaches a step. Tracks individual execution state and generated content.

```
SequenceProspectStep : Entity
├── SequenceProspectId     (FK → SequenceProspect)
├── SequenceStepId         (FK → SequenceStep)
├── Status                 (enum: Pending | Generating | Ready | Executing | Completed | Failed)
├── ScheduledAt            (DateTime?, when it should run)
├── ExecutedAt             (DateTime?, when it actually ran)
├── GeneratedSubject       (string?, for email steps)
├── GeneratedBody          (string?, generated content)
├── FailureReason          (string?)
├── RetryCount             (int)
└── RowVersion             (byte[], concurrency token)
```

### 1.5 Enum: SequenceMode

```csharp
public enum SequenceMode
{
    Focused,  // single prospect, enrichment steps available
    Multi     // multiple prospects, throttled execution
}
```

### Entity Relationship Diagram

```
Sequence (1) ──── (*) SequenceStep
    │
    └──── (*) SequenceProspect (1) ──── (*) SequenceProspectStep
                                              │
                                              └──── references SequenceStep
```

---

## 2. Application Layer

### 2.1 Use Cases

**Sequence management:**
- `CreateSequence` -- create a new sequence (Focused or Multi), name, initial steps
- `UpdateSequence` -- rename, update settings
- `DeleteSequence` -- delete a draft sequence (prevent deleting active ones)
- `GetSequence` -- load a sequence with its steps and prospect enrollment summary
- `ListSequences` -- list sequences for the current user

**Step management:**
- `AddSequenceStep` -- add a step to a sequence (validate step type is allowed for the mode)
- `UpdateSequenceStep` -- change order, timing, type, config
- `DeleteSequenceStep` -- remove a step (only on Draft sequences)
- `ReorderSequenceSteps` -- change the ordering of steps

**Prospect enrollment:**
- `EnrollProspect` -- add a prospect (+ contact person) to a sequence
- `EnrollMultipleProspects` -- bulk enroll for MultiSequence
- `RemoveProspect` -- remove from sequence (cancel pending steps)

**Execution control:**
- `ActivateSequence` -- validate and start execution (move from Draft → Active)
- `PauseSequence` -- pause execution
- `ResumeSequence` -- resume
- `CancelSequence` -- stop and mark as completed

### 2.2 Application Services

**`SequenceOrchestrator`** -- core service called by the background worker:
1. Find sequences that are `Active`
2. For each, find `SequenceProspect` entries that are `Active` and have a `SequenceProspectStep` with `ScheduledAt <= now` and `Status == Ready`
3. Execute the step via the appropriate `IStepExecutor`
4. On success: advance the prospect to the next step (create next `SequenceProspectStep`, calculate `ScheduledAt`)
5. On failure: increment retry count or mark as failed

**`SequenceActivator`** -- handles activation logic:
- For FocusedSequence: validate exactly 1 prospect enrolled, create first `SequenceProspectStep`
- For MultiSequence: validate at least 1 prospect enrolled, activate the first N prospects (throttle), create their first `SequenceProspectStep`

**`SequenceContentGenerator`** -- generates content for outreach steps:
- Uses `IOutreachContextBuilder` to build context
- Uses `IOutreachGeneratorFactory` to get the right generator
- For FocusedSequence: uses enriched data (UseCollectedData strategy)
- For MultiSequence: uses web search or collected data depending on what's available

### 2.3 Step Executors (Strategy Pattern)

```
IStepExecutor
├── EmailStepExecutor          (sends email via IEmailSender)
├── LinkedInMessageExecutor    (sends via ILinkedInActionsClient)
├── LinkedInConnectionExecutor (sends connection request)
├── LinkedInInteractionExecutor(performs interaction)
├── EnrichCompanyExecutor      (runs ICompanyEnrichmentService)
└── EnrichContactExecutor      (runs IContactDiscoveryProvider)
```

Each executor implements:
```csharp
public interface IStepExecutor
{
    SequenceStepType StepType { get; }
    Task ExecuteAsync(StepExecutionContext context, CancellationToken ct);
}
```

Adding a new step type = add a new class implementing `IStepExecutor` + register it in DI.

---

## 3. Infrastructure

### 3.1 Repository

`ISequenceRepository` / `SequenceRepository`:
- CRUD for Sequence, SequenceStep, SequenceProspect, SequenceProspectStep
- Query methods for the orchestrator (find due steps, find prospects needing activation)

### 3.2 EF Configuration

`SequenceConfiguration.cs`:
- Table names, indexes, relationships
- Composite index on `SequenceProspectStep(Status, ScheduledAt)` for efficient worker queries
- Value converter for `SequenceStepType` enum → string
- RowVersion concurrency token on `SequenceProspectStep`

### 3.3 Migration

Single migration to create all 4 tables: `Sequences`, `SequenceSteps`, `SequenceProspects`, `SequenceProspectSteps`.

---

## 4. API Layer

### 4.1 Endpoints (`SequenceEndpoints.cs`)

```
POST   /sequences                          → CreateSequence
GET    /sequences                          → ListSequences
GET    /sequences/{id}                     → GetSequence
PUT    /sequences/{id}                     → UpdateSequence
DELETE /sequences/{id}                     → DeleteSequence

POST   /sequences/{id}/steps              → AddSequenceStep
PUT    /sequences/{id}/steps/{stepId}     → UpdateSequenceStep
DELETE /sequences/{id}/steps/{stepId}     → DeleteSequenceStep
PUT    /sequences/{id}/steps/reorder      → ReorderSequenceSteps

POST   /sequences/{id}/prospects          → EnrollProspect / EnrollMultipleProspects
DELETE /sequences/{id}/prospects/{spId}   → RemoveProspect

POST   /sequences/{id}/activate           → ActivateSequence
POST   /sequences/{id}/pause              → PauseSequence
POST   /sequences/{id}/resume             → ResumeSequence
POST   /sequences/{id}/cancel             → CancelSequence
```

### 4.2 Background Workers

**`SequenceExecutionWorker`** -- runs on a timer (e.g. every 30s):
- Calls `SequenceOrchestrator` to process due steps
- Uses concurrency token (`RowVersion`) to prevent duplicate execution in multi-instance deployments

**`SequenceThrottleWorker`** -- runs on a timer (e.g. every 60s):
- For MultiSequences: checks if active prospect count < throttle limit
- Activates the next pending prospect(s) up to the limit

---

## 5. Frontend

### 5.1 Pages / Views

- **Sequences list page** (`/sequences`) -- shows all sequences with mode badge (Focused/Multi), status, prospect count
- **Sequence detail/builder page** (`/sequences/:id`) -- step editor (drag to reorder), prospect enrollment panel, activation controls
- **Sequence monitoring view** -- real-time progress of each prospect through steps (status badges, timestamps)

### 5.2 Components

- `SequenceBuilder.vue` -- visual step editor, add/remove/reorder steps
- `SequenceProspectList.vue` -- manage enrolled prospects
- `SequenceStepCard.vue` -- individual step display with type icon, timing config
- `SequenceProgress.vue` -- prospect-by-step execution matrix

### 5.3 API Service

- `sequenceService.ts` -- API client matching the backend endpoints
- `src/types/sequence.ts` -- TypeScript types for all DTOs

---

## 6. Build Order (incremental, each phase is shippable)

### Phase 1 -- Domain + Persistence (backend only)
- Define all entities in `SequenceFeature/`
- EF configuration + migration
- Repository implementation
- DbContext registration + DI

### Phase 2 -- CRUD Use Cases + API
- Create/Read/Update/Delete for Sequences, Steps, Prospects
- Wire up endpoints
- Register in Program.cs

### Phase 3 -- Execution Engine (backend)
- `IStepExecutor` interface + all executor implementations
- `SequenceContentGenerator` (wraps existing outreach generation)
- `SequenceOrchestrator` + `SequenceActivator`
- Background workers
- `IEmailSender` / `ILinkedInActionsClient` interfaces (re-introduce, with mocks for now)

### Phase 4 -- Frontend: Sequence Builder
- Types + API service
- Sequences list page
- Sequence detail page with step builder
- Prospect enrollment UI

### Phase 5 -- Frontend: Execution Monitoring
- Sequence progress view
- Real-time status updates
- Error/retry UI

### Phase 6 -- Settings & Polish
- Throttle settings for MultiSequence
- Timing/scheduling configuration
- Any additional settings that emerge

---

## Open Questions (to decide later)

- **Settings entity shape** -- what settings does a Sequence have? (throttle limit, timezone, send windows, retry policy)
- **Content review** -- should the user be able to review/edit AI-generated content before it sends, or is it fully automated?
- **Retry policy** -- how many retries on failure? Exponential backoff?
- **Notifications** -- should the user be notified when a sequence completes or a step fails?
- **Analytics** -- track open rates, response rates per sequence?
