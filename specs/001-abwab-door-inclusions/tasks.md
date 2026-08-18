# Tasks: Abwab Door Inclusions

**Input**: Design documents from `specs/001-abwab-door-inclusions/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/`,
`quickstart.md`

**Testing Decision**: The Test Freeze is active. Do not create a Backend test class or method,
Angular `*.spec.ts`, or Playwright journey/file. Tasks below may minimally update existing retained
exact-contract protection and later run approved existing gates/manual validation.

**Organization**: Tasks are dependency-ordered and grouped by the five prioritized user stories.
Every story ends with an independent manual/runtime checkpoint.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel after its stated phase prerequisites because it owns different files.
- **[Story]**: Maps implementation work to `US1`–`US5` in `spec.md`.
- Migration generation, database application, tests, and Git delivery remain separately authorized.

## Phase 1: Setup and Scope Confirmation

**Purpose**: Establish the exact implementation boundary before production-source edits.

- [X] T001 Confirm the active branch, feature decisions, authorization stops, and no-cap/target-first contracts in `specs/001-abwab-door-inclusions/spec.md`, `specs/001-abwab-door-inclusions/plan.md`, and `specs/001-abwab-door-inclusions/contracts/`
- [X] T002 [P] Inventory every live-unit mutation and current lock/transaction boundary in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Linking/EfLinkingConfirmationWriter*.cs` and `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Linking/EfDoorLinkRecordsWriter*.cs` before changing either writer family
- [X] T003 [P] Inventory the target context-menu, modal-host, URL-restoration, picker, and hard file-size boundaries in `Frontend/quran-dashboard-ui/src/app/features/abwab/pages/abwab-page/`, `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-tree/`, and `Frontend/quran-dashboard-ui/src/app/features/abwab/state/abwab-page-overlays.controller.ts`
- [X] T004 Confirm the Test Freeze, migration-generation stop, separate database-update stop, and no-Git-delivery boundary in `TESTING_CONSTITUTION.md`, `Backend/README.md`, and `specs/001-abwab-door-inclusions/quickstart.md`

**Checkpoint**: The implementation manifest covers every current writer and UI entry surface, and
no out-of-scope action has started.

---

## Phase 2: Foundational Persistence and Synchronization Primitives

**Purpose**: Create the shared model, constraints, mutation contract, lock, projection, and
registration foundation required by every user story.

**⚠️ CRITICAL**: No user-story phase begins until this foundation is complete. T018 is blocked until
the owner explicitly authorizes migration generation; it never authorizes database application.

- [X] T005 [P] Add the directed edge and sync-state enum in `Backend/domain/QuranDashboard.Domain/Abwab/AbwabDoorInclusion.cs` and `Backend/domain/QuranDashboard.Domain/Abwab/AbwabDoorInclusionSyncState.cs`
- [X] T006 [P] Add the per-occurrence active/overridden/suppressed ledger entity in `Backend/domain/QuranDashboard.Domain/Abwab/AbwabDoorInclusionUnitSync.cs`
- [X] T007 Add internal `DoorInclusion` enum ownership and make `LinkingSourceContribution.OperationId` nullable for that kind only in `Backend/domain/QuranDashboard.Domain/Linking/LinkingSourceKind.cs` and `Backend/domain/QuranDashboard.Domain/Linking/LinkingSourceContribution.cs`, without adding a public descriptor subtype or synthetic `LinkingOperation`
- [X] T008 Configure the inclusion edge, restrictive door foreign keys, soft-delete uniqueness, reverse traversal indexes, and `xmin` in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Configurations/Abwab/AbwabDoorInclusionConfiguration.cs`
- [X] T009 Configure the sync ledger, restrictive unit foreign keys, unique ownership, state/target coherence, and source lookup index in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Configurations/Abwab/AbwabDoorInclusionUnitSyncConfiguration.cs`
- [X] T010 Add the persisted `door_inclusion` kind, nullable inclusion reference, conditionally nullable operation foreign key, active uniqueness, and kind/reference/operation coherence constraints in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Configurations/Linking/LinkingSourceKindColumn.cs`, `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Configurations/Linking/LinkingSourceContributionConfiguration.cs`, and `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Configurations/Linking/LinkingDescriptorCheckConstraints.cs`
- [X] T011 Register inclusion DbSets and navigation ownership in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/QuranDashboardDbContext.cs`
- [X] T012 Define exact added/edited/deleted mutations plus identity-preserving replacement pairs for every logical occurrence, rejecting any physical reshape that lacks a deterministic bijection, in `Backend/application/QuranDashboard.Application.Abstractions/Abwab/Inclusions/AbwabDoorInclusionMutationSet.cs` and `Backend/application/QuranDashboard.Application.Abstractions/Abwab/Inclusions/IAbwabDoorInclusionSynchronizer.cs`
- [X] T013 [P] Implement canonical source-unit snapshots and fingerprints for grouped shape, ordered ayahs, selected words, and descriptions in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/Inclusions/AbwabDoorInclusionSourceSnapshot.cs` and `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/Inclusions/AbwabDoorInclusionFingerprint.cs`
- [X] T014 Implement the dedicated transaction-scoped inclusion advisory lock with the global job/idempotency → inclusion → ordered doors → ordered units contract in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/Inclusions/AbwabDoorInclusionSyncLock.cs`
- [X] T015 Extract one affected-ayah distinct-union rebuild owner from the existing confirmation/direct algorithms into `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Linking/RelationalDoorStateRebuilder.cs` and delegate from `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Linking/EfLinkingConfirmationWriter.RelationalDoorState.cs` and `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Linking/EfDoorLinkRecordsWriter.DoorState.cs`
- [X] T016 Create the scoped synchronizer shell, implement its batched no-fixed-depth active-consumer traversal, and register its abstraction, lock, and supporting dependencies in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/Inclusions/EfAbwabDoorInclusionSynchronizer.cs`, `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/Inclusions/EfAbwabDoorInclusionSynchronizer.Traversal.cs`, `Backend/infrastructure/QuranDashboard.Infrastructure/DependencyInjection/AbwabDependencyInjection.cs`, and `Backend/infrastructure/QuranDashboard.Infrastructure/DependencyInjection/LinkingDependencyInjection.cs`
- [X] T017 Audit the complete Abwab-to-Linking cascade closure, update literal reset ownership, and extend the existing schema assertion method only for the two inclusion tables, their foreign keys/indexes, and reset ownership in `Backend/scripts/wipe-abwab`, `Backend/scripts/README.md`, `Backend/tests/QuranDashboard.Tests/Abwab/AbwabSchemaFixture.cs`, `Backend/tests/QuranDashboard.Tests/Abwab/AbwabSchemaTests.cs`, and `Backend/tests/QuranDashboard.Tests/Smoke/SmokeApiFixture.cs` without executing a wipe/reset, adding a test method, or claiming retained ownership of linking-contribution coherence checks
- [X] T018 Obtain explicit migration-generation authorization, then generate `AddAbwabDoorInclusionSynchronization` only through `Backend/scripts/add-mig` into `Backend/infrastructure/QuranDashboard.Infrastructure/Migrations/`; stop without generating when authorization is absent and never run `Backend/scripts/update-db`
- [X] T019 Establish internal-contribution isolation before the first inclusion can be created: load `DoorInclusion` contributions into internal confirmed state for door-word impact, exclude them from the authored-source identity index and overlapping-source labels before token conversion, and keep public tokens/descriptors/request parsing unable to accept or emit the kind in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Linking/EfLinkingConfirmedStateReader.cs`, `Backend/application/QuranDashboard.Application/Linking/LinkingOperationClassifier.cs`, `Backend/application/QuranDashboard.Application.Abstractions/Linking/LinkingSourceTokens.cs`, and `Backend/api/QuranDashboard.Api/Contracts/Linking/LinkingSourceDescriptorBodyMapper.cs`

**Checkpoint**: The shared persistence model is exact, public contributions retain their operation
owner, internal contributions have no synthetic operation or public representation, one shared graph
traversal is available before initial sync, occurrence transfer fails closed, and the schema is
tooling-generated only under authorization.

---

## Phase 3: User Story 1 — Build an Aggregate Door (Priority: P1) 🎯 MVP

**Goal**: From one live aggregate target's context menu, an authorized curator selects one or many
live sources from the existing tree/list and creates all inclusions plus initial clones atomically.

**Independent Test**: Right-click one live target, open `تضمين الأبواب`, select two unrelated live
sources in the existing multi-select tree, submit once, and verify both direct edges and matching
ordinary target records appear while hierarchy and semantic relations remain unchanged; an invalid
member rejects the entire batch.

- [X] T020 [US1] Define topology, inclusion item, atomic add request/result, controlled outcome, reader, and writer contracts in `Backend/application/QuranDashboard.Application.Abstractions/Abwab/Inclusions/IAbwabDoorInclusionsReader.cs`, `Backend/application/QuranDashboard.Application.Abstractions/Abwab/Inclusions/IAbwabDoorInclusionsWriter.cs`, `Backend/application/QuranDashboard.Application.Abstractions/Abwab/Inclusions/AbwabDoorInclusionOutcomes.cs`, and `Backend/application/QuranDashboard.Application.Abstractions/Abwab/Responses/AbwabDoorInclusionDtos.cs`
- [X] T021 [P] [US1] Implement the public direct-source/direct-consumer query in `Backend/application/QuranDashboard.Application/Abwab/Queries/GetDoorInclusions/GetDoorInclusionsQuery.cs`, `Backend/application/QuranDashboard.Application/Abwab/Queries/GetDoorInclusions/GetDoorInclusionsOutcome.cs`, and `Backend/application/QuranDashboard.Application/Abwab/Queries/GetDoorInclusions/GetDoorInclusionsHandler.cs`
- [X] T022 [P] [US1] Implement the atomic multi-source add command validation/orchestration in `Backend/application/QuranDashboard.Application/Abwab/Commands/AddDoorInclusions/AddDoorInclusionsCommand.cs`, `Backend/application/QuranDashboard.Application/Abwab/Commands/AddDoorInclusions/AddDoorInclusionsOutcome.cs`, and `Backend/application/QuranDashboard.Application/Abwab/Commands/AddDoorInclusions/AddDoorInclusionsHandler.cs`
- [X] T023 [US1] Implement active direct topology reads including archived participants and the current target version in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Abwab/EfAbwabDoorInclusionsReader.cs`
- [X] T024 [US1] Implement batch normalization, lifecycle/version locking, active-duplicate rejection, and whole-proposed-graph cycle validation in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/Inclusions/EfAbwabDoorInclusionsWriter.Add.cs` and `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/Inclusions/AbwabDoorInclusionGraph.cs`
- [X] T025 [US1] Implement initial clone creation through the foundational traversal, internal salted identities, contribution-unit mappings with no `LinkingOperation`, Active ledger rows, affected projection rebuild, and downstream propagation in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/Inclusions/EfAbwabDoorInclusionSynchronizer.Add.cs`
- [X] T026 [US1] Complete one-transaction add orchestration, changed-target version advancement, rollback mapping, and one post-commit tree invalidation in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/Inclusions/EfAbwabDoorInclusionsWriter.cs` and `Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Abwab/InvalidatingAbwabDoorInclusionsWriter.cs`
- [X] T027 [P] [US1] Add `InclusionSourceCount` and `InclusionConsumerCount` without changing existing metric meanings in `Backend/application/QuranDashboard.Application.Abstractions/Abwab/Responses/AbwabTreeDto.cs`
- [X] T028 [US1] Populate direct active inclusion counts without excluding archived participants in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Abwab/EfAbwabTreeReader.cs`
- [X] T029 [P] [US1] Add `abwab.inclusions.create` and `abwab.inclusions.delete` under the sixth `تضمين الأبواب` group in `Backend/application/QuranDashboard.Application.Abstractions/Security/Permissions/AbwabPermissions.cs` and `Backend/application/QuranDashboard.Application.Abstractions/Security/Permissions/AbwabPermissionCatalogue.cs`
- [X] T030 [US1] Add localized inclusion messages, GET/POST request bodies, thin controller actions, `201/400/404/409/503` mapping, public GET, and one create permission classification in `Backend/api/QuranDashboard.Api/Common/ApiMessages.cs`, `Backend/api/QuranDashboard.Api/Contracts/Abwab/AbwabDoorInclusionBodies.cs`, and `Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabDoorInclusionsController.cs`
- [X] T031 [US1] Register Get/Add handlers and topology reader/writer decorators in `Backend/application/QuranDashboard.Application/DependencyInjection.cs` and `Backend/infrastructure/QuranDashboard.Infrastructure/DependencyInjection/AbwabDependencyInjection.cs`
- [X] T032 [US1] Minimally update existing permission catalogue and GET/POST route/authorization protection without adding tests in `Backend/tests/QuranDashboard.Tests/Api/Access/AbwabPermissionCatalogueTests.cs`, `Backend/tests/QuranDashboard.Tests/Smoke/SmokeRouteCatalog.cs`, `Backend/tests/QuranDashboard.Tests/Smoke/SmokeCoverageParityTests.cs`, and `Backend/tests/QuranDashboard.Tests/Smoke/SmokeAbwabWriteAuthorizationTests.cs`
- [X] T033 [US1] Export the implemented GET/POST contract and regenerate retained frontend DTO models via `Backend/scripts/check-api-contract`, reviewing only `Frontend/quran-dashboard-ui/openapi/swagger.json` and `Frontend/quran-dashboard-ui/src/app/core/api/generated/`
- [X] T034 [US1] Regenerate and validate permission constants through `Frontend/quran-dashboard-ui/package.json` scripts `generate:permission-codes` and `check:permission-catalogue`, changing only `Frontend/quran-dashboard-ui/src/app/core/auth/permission-codes.generated.ts`
- [X] T035 [P] [US1] Extract existing modal-host composition from the hard-boundary Abwab page into `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-overlays-host/abwab-overlays-host.component.ts`, `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-overlays-host/abwab-overlays-host.component.html`, and `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-overlays-host/abwab-overlays-host.component.scss` while preserving every current overlay and shrinking `Frontend/quran-dashboard-ui/src/app/features/abwab/pages/abwab-page/abwab-page.component.ts` and `Frontend/quran-dashboard-ui/src/app/features/abwab/pages/abwab-page/abwab-page.component.html`
- [X] T036 [P] [US1] Extract context-menu action construction from `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-tree/abwab-tree.component.ts` into `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-tree/abwab-tree-context-menu.controller.ts` while preserving right-click, `ContextMenu`, and `Shift+F10`
- [X] T037 [US1] Add typed public topology and atomic multi-source POST calls only in `Frontend/quran-dashboard-ui/src/app/features/abwab/data-access/abwab-inclusions.api.ts`
- [X] T038 [US1] Add held topology/version, request cancellation/generation, multi-source draft, initial/refresh/error/notice, and atomic-add orchestration in `Frontend/quran-dashboard-ui/src/app/features/abwab/state/abwab-inclusions.controller.ts`
- [X] T039 [US1] Build the wide Arabic target-first modal and reuse `abwab-door-picker` with `single=false`, main-page `liveRoots`, target exclusion, current-source disabling, multi-section selection, and one submit in `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-inclusions-modal/abwab-inclusions-modal.component.ts`, `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-inclusions-modal/abwab-inclusions-modal.component.html`, and `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-inclusions-modal/abwab-inclusions-modal.component.scss`
- [X] T040 [US1] Wire the public context-menu entry, live-target modal URL state, inclusion create permission, topology counts, Arabic labels, focus return, and modal host in `Frontend/quran-dashboard-ui/src/app/features/abwab/state/abwab-modal-url.controller.ts`, `Frontend/quran-dashboard-ui/src/app/features/abwab/state/abwab-permissions.controller.ts`, `Frontend/quran-dashboard-ui/src/app/features/abwab/state/abwab-tree.builder.ts`, `Frontend/quran-dashboard-ui/src/app/features/abwab/models/abwab.labels.ts`, `Frontend/quran-dashboard-ui/src/app/features/abwab/pages/abwab-page/abwab-page.component.ts`, and `Frontend/quran-dashboard-ui/src/app/features/abwab/pages/abwab-page/abwab-page.component.html`
- [X] T041 [US1] Validate the complete target-first MVP, invalid-batch atomicity, no-cap behavior, hierarchy preservation, permission visibility, and initial clone fidelity using sections 6–7 of `specs/001-abwab-door-inclusions/quickstart.md`

**Checkpoint**: User Story 1 is independently usable as the MVP: one target-first atomic add flow,
public topology, matching initial/transitive content, no internal contribution exposure or token
failure, and no source-first/multi-target route or UI.

---

## Phase 4: User Story 2 — Keep Included Content Synchronized (Priority: P2)

**Goal**: Every supported source add/edit/delete reaches all consumer doors in the same transaction,
including transitive paths and distinct ayah/word union maintenance.

**Independent Test**: Configure A includes B and B includes C, then add, edit, and delete a C record;
B and A change before source success, duplicate word suppliers remain distinct at record level, and
a forced failure rolls back the complete source/target mutation.

- [X] T042 [US2] Dispatch freshly added source units through the foundational traversal with per-edge clone/mapping creation, affected projection rebuild, and recursive target version tracking in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/Inclusions/EfAbwabDoorInclusionSynchronizer.Add.cs`
- [X] T043 [US2] Dispatch source edits across every mapping state in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/Inclusions/EfAbwabDoorInclusionSynchronizer.Edit.cs`: replace and propagate `Active` clones, but advance observed fingerprint/audit only for `Overridden` and `Suppressed` without overwriting or recreating target content
- [X] T044 [US2] Implement Active source-delete dependent cleanup before restrictive FK deletion and downstream removal propagation in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/Inclusions/EfAbwabDoorInclusionSynchronizer.Delete.cs`
- [X] T045 [US2] Preserve every logical source occurrence ID across physical reshaping or transfer each ledger row through a deterministic bijection before orphan cleanup; reject a true one-to-many or many-to-one reshape that cannot preserve all occurrence identities and states in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Linking/EfLinkingConfirmationWriter.RelationalUnits.cs` and `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/Inclusions/EfAbwabDoorInclusionSynchronizer.Reconcile.cs`
- [X] T046 [US2] Insert the inclusion advisory lock after existing job/idempotency/revision locks and before door/unit locks in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Linking/EfLinkingConfirmationWriter.Prepared.cs` and `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Linking/EfLinkingConfirmationJobStore.Execution.cs`
- [X] T047 [US2] Emit precise source add/edit/delete/replacement mutation sets from prepared/background confirmation before commit in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Linking/EfLinkingConfirmationWriter.RelationalWorksets.cs`, `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Linking/EfLinkingConfirmationWriter.RelationalLinks.cs`, and `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Linking/EfLinkingConfirmationWriter.RelationalUnits.cs`
- [X] T048 [US2] Integrate source-side direct record changes with the shared lock and synchronizer without altering direct behavior in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Linking/EfDoorLinkRecordsWriter.cs` and `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Linking/EfDoorLinkRecordsWriter.DoorState.cs`
- [X] T049 [US2] Ensure recursive target versions advance and only outer committed confirmation/direct writers invalidate once in `Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Linking/InvalidatingLinkingConfirmationWriter.cs`, `Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Linking/InvalidatingDoorLinkRecordsWriter.cs`, and `Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Abwab/CachedAbwabTreeReader.cs`
- [X] T050 [US2] Map expected synchronization conflicts and safe-completion failures to controlled rollback-safe Application outcomes in `Backend/application/QuranDashboard.Application/Linking/ConfirmationJobs/ProcessLinkingConfirmationJobHandler.cs`; reserve `Backend/api/QuranDashboard.Api/Middleware/GlobalExceptionHandler.cs` for sanitized unexpected faults and emit at most one safe ID/count/state diagnostic at the owning boundary
- [X] T051 [US2] Validate transitive active add/edit/delete, duplicate ayah/word union, occurrence replacement, concurrency, controlled-versus-unexpected failure handling, no hard graph cap, and pre-success completion using projection/propagation cases in `specs/001-abwab-door-inclusions/quickstart.md`

**Checkpoint**: User Story 2 is independently verifiable through source mutations and a three-door
chain, with no partial state, no N+1 traversal, and no eventual catch-up.

---

## Phase 5: User Story 3 — Curate an Aggregate Locally (Priority: P3)

**Goal**: Existing target link edit/delete tools create durable local override/suppression without
writing to the source, and source-occurrence deletion still retires target-local state.

**Independent Test**: Override one synchronized target record and suppress another, edit both source
occurrences, then delete/relink them; override and suppression survive same-occurrence edits, source
data never changes, and only a later new occurrence returns.

- [ ] T052 [US3] Resolve synchronized target ownership before direct mutation while leaving unsynchronized units on the existing path in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Linking/EfDoorLinkRecordsWriter.cs`
- [ ] T053 [US3] Dispatch selected-word replacement on a synchronized target clone to target-only content update plus `Overridden` state and downstream propagation in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Linking/EfDoorLinkRecordsWriter.cs` and `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/Inclusions/EfAbwabDoorInclusionSynchronizer.Override.cs`
- [ ] T054 [US3] Dispatch synchronized target deletion to recursive downstream clone cleanup followed by durable local `Suppressed` state with no source mutation in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Linking/EfDoorLinkRecordsWriter.Deletion.cs` and `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/Inclusions/EfAbwabDoorInclusionSynchronizer.Suppress.cs`
- [ ] T055 [US3] Partition mixed bulk selections into ordinary direct deletions and per-occurrence synchronized suppressions, propagate every target-visible removal through consumer doors, and commit the complete set in one transaction in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Linking/EfDoorLinkRecordsWriter.Deletion.cs` and `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/Inclusions/EfAbwabDoorInclusionSynchronizer.Suppress.cs`
- [ ] T056 [US3] Extend source deletion reconciliation so Overridden clones are removed, Suppressed mappings retire, and downstream target state is cleaned before source-unit deletion in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/Inclusions/EfAbwabDoorInclusionSynchronizer.Delete.cs`
- [ ] T057 [US3] Preserve existing Owner-only link route authorization and unchanged request/response shapes while mapping target-local outcomes in `Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabDoorLinksController.cs` and `Backend/application/QuranDashboard.Application.Abstractions/Linking/DoorLinks/DoorLinkRecordDtos.cs`
- [ ] T058 [US3] Validate transitive override/suppression, mixed bulk downstream removal, fingerprint-only edits for `Overridden`/`Suppressed`, source isolation, source delete, same-occurrence edit, and later delete-then-relink behavior using target-local cases in `specs/001-abwab-door-inclusions/quickstart.md`

**Checkpoint**: User Story 3 works through the existing link UI and contracts; target-local intent is
durable for one occurrence and never flows backward.

---

## Phase 6: User Story 4 — Manage Inclusion Lifecycles Safely (Priority: P4)

**Goal**: Readers inspect both topology directions, curators detach direct sources safely, archived
doors retain correct read/sync behavior, and reattachment creates fresh state.

**Independent Test**: Archive/restore source and target, detach an edge containing active,
overridden, and suppressed mappings, then reattach; archive preserves current content, detach removes
only edge-owned state, and reattach does not revive retired local choices.

- [ ] T059 [P] [US4] Implement and register detach validation, outcomes, and handler in `Backend/application/QuranDashboard.Application/Abwab/Commands/DeleteDoorInclusion/DeleteDoorInclusionCommand.cs`, `Backend/application/QuranDashboard.Application/Abwab/Commands/DeleteDoorInclusion/DeleteDoorInclusionOutcome.cs`, `Backend/application/QuranDashboard.Application/Abwab/Commands/DeleteDoorInclusion/DeleteDoorInclusionHandler.cs`, `Backend/application/QuranDashboard.Application.Abstractions/Abwab/Inclusions/AbwabDoorInclusionOutcomes.cs`, and `Backend/application/QuranDashboard.Application/DependencyInjection.cs`
- [ ] T060 [US4] Implement lock/version validation, recursive edge-owned clone cleanup, suppressed-ledger removal, internal contribution retirement, projection rebuild, version advancement, and one invalidation in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/Inclusions/EfAbwabDoorInclusionsWriter.Detach.cs`
- [ ] T061 [US4] Add the permission-classified DELETE body/action, `200` removal summary, and `400/404/409/503` mapping in `Backend/api/QuranDashboard.Api/Contracts/Abwab/AbwabDoorInclusionBodies.cs`, `Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabDoorInclusionsController.cs`, and `Backend/api/QuranDashboard.Api/Common/ApiMessages.cs`
- [ ] T062 [US4] Export the DELETE contract and regenerate retained frontend inclusion result models through `Backend/scripts/check-api-contract` into `Frontend/quran-dashboard-ui/openapi/swagger.json` and `Frontend/quran-dashboard-ui/src/app/core/api/generated/`
- [ ] T063 [US4] Add typed detach calls, read-only consumers, latest-version conflict refresh, and detach state to `Frontend/quran-dashboard-ui/src/app/features/abwab/data-access/abwab-inclusions.api.ts` and `Frontend/quran-dashboard-ui/src/app/features/abwab/state/abwab-inclusions.controller.ts`
- [ ] T064 [US4] Add direct `مصادر الباب`, read-only `يُستخدم في أبواب جامعة`, archived labels/explanation, permission-gated detach rows, and nested source-unchanged confirmation in `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-inclusions-modal/abwab-inclusions-modal.component.ts`, `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-inclusions-modal/abwab-inclusions-modal.component.html`, and `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-inclusions-modal/abwab-inclusions-modal.component.scss`
- [ ] T065 [US4] Preserve edges/clones through source archive, continue traversal into archived targets, block archived-target user writes, and avoid restore duplication without treating archive as source deletion in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/Inclusions/EfAbwabDoorInclusionSynchronizer.Traversal.cs` and `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/EfAbwabDoorsWriter.cs`
- [ ] T066 [P] [US4] Add an archived-door read-only inclusion topology action/count with text/icon status and no mutation controls in `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-archive-view/abwab-archive-view.component.ts`, `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-archive-view/abwab-archive-view.component.html`, and `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-archive-view/abwab-archive-view.component.scss`
- [ ] T067 [P] [US4] Encode and restore the inclusion target as `modal=inclusions-<doorId>` for live and archived subjects in `Frontend/quran-dashboard-ui/src/app/features/abwab/state/abwab-modal-url.controller.ts`, `Frontend/quran-dashboard-ui/src/app/features/abwab/state/abwab-url-sync.ts`, and `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-modal-restore/`
- [ ] T068 [US4] Make reattachment create a new edge/contribution and fresh initial sync without retired mappings in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/Inclusions/EfAbwabDoorInclusionsWriter.Add.cs`
- [ ] T069 [US4] Extend existing retained DELETE route/authorization/reset matrices without adding tests in `Backend/tests/QuranDashboard.Tests/Smoke/SmokeRouteCatalog.cs`, `Backend/tests/QuranDashboard.Tests/Smoke/SmokeCoverageParityTests.cs`, `Backend/tests/QuranDashboard.Tests/Smoke/SmokeAbwabWriteAuthorizationTests.cs`, and `Backend/tests/QuranDashboard.Tests/Smoke/SmokeApiFixture.cs`
- [ ] T070 [US4] Validate archive retention, archived-target read-only behavior, detach isolation, fresh reattachment, topology directions/counts, version refresh, and focus restoration using lifecycle cases in `specs/001-abwab-door-inclusions/quickstart.md`

**Checkpoint**: User Story 4 is independently verifiable without modifying source content or
unrelated target records, including archived audit/restore context.

---

## Phase 7: User Story 5 — Use Included Records Through Existing Link Experiences (Priority: P5)

**Goal**: Readers and Owners consume, edit, delete, and copy synchronized records through the exact
existing link UI and DTOs without any origin/synchronization attribution.

**Independent Test**: Open the current target link panel, render and copy a synchronized record,
exercise stale refresh, and confirm the destination is direct while no content response or UI names
the source, inclusion, internal contribution, or sync state.

- [ ] T071 [P] [US5] Audit the foundational internal-contribution isolation across `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Linking/EfLinkingConfirmedStateReader.cs`, `Backend/application/QuranDashboard.Application/Linking/LinkingOperationClassifier.cs`, `Backend/application/QuranDashboard.Application.Abstractions/Linking/LinkingSourceTokens.cs`, and `Backend/api/QuranDashboard.Api/Contracts/Linking/LinkingSourceDescriptorBodyMapper.cs`, confirming public preflight/overlap output remains attribution-free while internal units still affect door state
- [ ] T072 [US5] Preserve unchanged link record/snapshot shapes and ordinary synchronized-unit reads in `Backend/application/QuranDashboard.Application.Abstractions/Linking/DoorLinks/DoorLinkRecordDtos.cs`, `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Linking/EfDoorLinkRecordsReader.cs`, and `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Linking/EfDoorLinkRecordsReader.Snapshot.cs`
- [ ] T073 [P] [US5] Preserve copy-as-direct behavior without inclusion metadata in `Frontend/quran-dashboard-ui/src/app/features/abwab/state/abwab-door-link-copy.controller.ts`, `Frontend/quran-dashboard-ui/src/app/features/abwab/state/abwab-door-link-copy.loader.ts`, `Frontend/quran-dashboard-ui/src/app/features/abwab/state/abwab-door-link-copy.mapper.ts`, and `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-door-link-copy/`
- [ ] T074 [US5] Refresh stale open target panels after propagated version changes without origin branching in `Frontend/quran-dashboard-ui/src/app/features/abwab/state/abwab-door-links.facade.ts`, `Frontend/quran-dashboard-ui/src/app/features/abwab/state/abwab-door-links.store.ts`, and `Frontend/quran-dashboard-ui/src/app/features/abwab/state/abwab-door-link-edit.controller.ts`
- [ ] T075 [US5] Audit the current link panel/list/record/editor components for zero source badges, sync fields, alternate tabs, or new Quran presentation in `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-door-links-panel/`, `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-door-links-list/`, `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-door-link-record/`, and `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-door-link-editor/`
- [ ] T076 [US5] Validate existing counts, list/ayah/highlight/edit/delete/copy UI, direct-copy ownership, source-attribution absence, and stale refresh using contract/UI cases in `specs/001-abwab-door-inclusions/quickstart.md`

**Checkpoint**: User Story 5 proves inclusion is invisible inside Quran-content presentation and
does not create a second link model or mode.

---

## Phase 8: Polish and Cross-Cutting Verification

**Purpose**: Reconcile generated contracts, retained exact protection, build/static gates, manual
acceptance, architecture thresholds, and authorization boundaries across all stories.

- [ ] T077 Re-run the final API contract drift workflow in `Backend/scripts/check-api-contract` and review only sanctioned changes in `Frontend/quran-dashboard-ui/openapi/swagger.json` and `Frontend/quran-dashboard-ui/src/app/core/api/generated/`
- [ ] T078 Re-run permission generation/catalogue validation through `Frontend/quran-dashboard-ui/package.json` and verify only `Frontend/quran-dashboard-ui/src/app/core/auth/permission-codes.generated.ts` changed as generated permission output
- [ ] T079 Run the Backend build and pending-model check exactly as documented in `specs/001-abwab-door-inclusions/quickstart.md`, confirming the authorized generated migration matches `Backend/infrastructure/QuranDashboard.Infrastructure/Migrations/` and never applying it here
- [ ] T080 Run the existing migration and gate-contract lanes without adding tests via `Backend/scripts/test-backend` and reconcile only failures owned by the schema/constraint changes listed in `Backend/tests/QuranDashboard.Tests/TestSupport/Execution/test-gates.tsv`
- [ ] T081 Run the existing tier-b and smoke lanes via `Backend/scripts/test-backend` and reconcile only retained Abwab/catalogue/route protection in `Backend/tests/QuranDashboard.Tests/`
- [ ] T082 Run `check:no-unit-specs`, `typecheck:app`, `build:verify`, and `check:golden-ui` independently and in order through `Frontend/quran-dashboard-ui/package.json`
- [ ] T083 Minimally extend the existing hidden-control/anonymous-write assertions for the inclusion action only if its protected subject changed, without adding a journey or test file, in `Frontend/quran-dashboard-ui/e2e/abwab-permissions.e2e.ts`
- [ ] T084 If T083 changed the retained journey, run only its existing Abwab Playwright project command from `Frontend/quran-dashboard-ui/e2e/README.md`; otherwise record it as not selected in the `specs/001-abwab-door-inclusions/quickstart.md` acceptance record
- [ ] T085 Execute and record all target-first, graph, propagation, override/suppression, archive/detach, permissions, compatibility, responsive, RTL, focus, and Quran-safety manual cases in `specs/001-abwab-door-inclusions/quickstart.md`
- [ ] T086 Perform the pre-delivery scope, comment-policy, layer, file-size, generated-file, and focused-change self-check against `CODING_PRINCIPLES.md`, `Backend/.architecture/`, `Frontend/quran-dashboard-ui/FRONTEND_UI_RULES.md`, and `Frontend/quran-dashboard-ui/.architecture/`
- [ ] T087 Confirm the final diff against `specs/001-abwab-door-inclusions/spec.md`, `specs/001-abwab-door-inclusions/plan.md`, `specs/001-abwab-door-inclusions/data-model.md`, and `specs/001-abwab-door-inclusions/contracts/` contains no Quran data mutation, hierarchy/semantic-relation change, source attribution, hard graph cap, timing SLA, background propagation, database application, deployment change, or unauthorized new test

**Checkpoint**: Every selected gate and manual case has an explicit result, no unauthorized action
occurred, and implementation is ready for the separately requested engineering-review/delivery flow.

---

## Dependencies and Execution Order

### Phase Dependencies

```text
Phase 1 Setup
    ↓
Phase 2 Foundation
    ↓
US1 Build Aggregate (MVP)
    ↓
US2 Durable Synchronization
    ↓
US3 Local Curation
    ├───────────────┐
    ↓               ↓
US4 Lifecycle     US5 Existing Link UX
    └───────┬───────┘
            ↓
Phase 8 Cross-Cutting Verification
```

- **Phase 1** has no dependency and starts immediately.
- **Phase 2** depends on Phase 1 and blocks all stories. Migration task T018 additionally blocks on
  explicit owner authorization and never implies database application.
- **US1 (P1)** depends on Phase 2 and is the MVP.
- **US2 (P2)** depends on US1's edge/initial-sync implementation, but its source-mutation checkpoint
  is independently testable through the HTTP-created graph.
- **US3 (P3)** depends on US2's durable ledger propagation and is independently testable through
  existing link edit/delete routes.
- **US4 (P4)** depends on US1 topology plus US3 state semantics so detach can reconcile every state.
- **US5 (P5)** depends on US2/US3 Backend semantics but may run in parallel with US4 because it owns
  public projection/link-UI files rather than topology lifecycle files.
- **Phase 8** depends on every story selected for delivery.

### Within Each User Story

1. Contracts/models before infrastructure implementations.
2. Infrastructure behavior before HTTP exposure.
3. Backend contract export before generated frontend model consumption.
4. Data access before state orchestration; state before modal/page composition.
5. Existing retained protection is updated only when its owned subject changes.
6. The independent manual checkpoint closes the story before the next dependent story begins.

## Parallel Opportunities

### Setup and Foundation

- T002 and T003 can run together because Backend and Frontend inspection do not overlap.
- T005 and T006 can be authored together in separate Domain files; T013 can proceed after their
  field/state contracts are stable while configuration work continues. T019 follows T007/T010 and
  must complete before any US1 inclusion materialization.

### User Story 1

- T021 and T022 can proceed together after T020.
- T027 and T029 own independent response/security files while T023–T026 implement topology writes.
- T035 and T036 are independent Frontend extractions and can proceed before generated inclusion
  models are ready.

### User Story 2

- T042 ongoing-add dispatch and T046 lock integration own different files after the foundational
  traversal/lock contracts are stable; merge them before T047 emits mutation sets.

### User Story 3

- T053 override and T054 suppression may be developed in separate synchronizer partials after T052
  establishes ownership classification; integrate them before T055 mixed bulk behavior.

### User Story 4

- T059 Backend detach contracts, T066 archived Frontend entry, and T067 URL restoration own separate
  files; integrate after T063/T064 establish the completed modal workflow.

### User Story 5

- T071 Backend public-isolation audit and T073 Frontend copy preservation can proceed together.
- US5 may proceed in parallel with US4 after US3 completes.

## Parallel Execution Examples

### User Story 1

```text
Task T021: Implement GetDoorInclusions query files.
Task T022: Implement AddDoorInclusions command files.
Task T035: Extract the Abwab overlays host.
Task T036: Extract the Abwab tree context-menu controller.
```

### User Story 2

```text
Task T042: Dispatch ongoing source additions through the foundational traversal.
Task T046: Insert the synchronization advisory lock into confirmation execution.
```

### User Story 3

```text
Task T053: Implement local override.
Task T054: Implement local suppression.
```

### User Story 4

```text
Task T059: Add Backend detach contracts and handler.
Task T066: Add archived read-only topology entry.
Task T067: Add modal target URL restoration.
```

### User Story 5

```text
Task T071: Audit internal inclusion ownership isolation from public projections.
Task T073: Preserve copy-as-direct Frontend behavior.
```

## Implementation Strategy

### MVP First

1. Complete Phase 1 setup and Phase 2 foundation.
2. Stop at T018 unless migration generation has been explicitly authorized; never apply it without a
   separate instruction.
3. Complete US1 tasks T020–T041.
4. Validate the target-first atomic add journey independently.
5. Stop for owner review before expanding beyond the MVP if implementation was authorized only for
   US1.

### Incremental Delivery

1. **US1**: target-first topology and atomic initial sync.
2. **US2**: ongoing/transitive source synchronization.
3. **US3**: target-local override and suppression.
4. **US4**: archive/detach/reattach lifecycle and two-direction topology management.
5. **US5**: explicit proof that existing link content remains unchanged and attribution-free.
6. **Phase 8**: generated-contract, retained-gate, frontend, manual, and scope verification.

### Parallel Team Strategy

After the foundation is complete, parallelize only tasks explicitly marked `[P]` or listed in the
parallel examples. Do not run Backend test lanes concurrently, do not let multiple workers edit the
same writer/controller/generated file, and merge no story before its independent checkpoint passes.

## Notes

- Every task includes an exact file or owning directory.
- `[P]` never overrides a dependency stated in this document.
- No task creates a new automated test; retained files are updated minimally and conditionally.
- No task applies a migration, wipes/resets data, changes Railway, or modifies Quran source data.
- No task stages, commits, pushes, opens a PR, deploys, or runs formal engineering review without a
  separate user request.
