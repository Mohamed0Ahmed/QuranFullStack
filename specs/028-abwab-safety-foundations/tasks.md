---
description: "Task list for 028-abwab-safety-foundations"
---

# Tasks: Abwab Safety Foundations — Fail-Closed Substrate

**Input**: Design documents from `/specs/028-abwab-safety-foundations/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Source**: Master Plan §18.2 only. The six user stories are the six **mandatory-order**
stages of §18.2; unlike a typical Spec Kit, they are **not** freely parallel — a later
stage begins only after the earlier stage's §18.2 exit gate passes.

**Tests**: INCLUDED. §18.2's exit/acceptance criteria are themselves tests (refusal,
contract, registry, parity, concurrency). They are written **first** and must fail before
implementation.

## Path Conventions

- Backend (.NET 10, Clean Architecture): `Backend/domain/QuranDashboard.Domain/`,
  `Backend/application/QuranDashboard.Application(.Abstractions)/`,
  `Backend/infrastructure/QuranDashboard.Infrastructure/`, `Backend/api/QuranDashboard.Api/`,
  importer `Backend/tools/QuranDashboard.DataImporter/`, tests
  `Backend/tests/QuranDashboard.Tests/` (xUnit + Testcontainers).
- Frontend (Angular 20.3): `Frontend/quran-dashboard-ui/src/app/{core,shared,features}/`,
  `Frontend/quran-dashboard-ui/e2e/` (Playwright), `Frontend/quran-dashboard-ui/package.json`.
- EF migrations are **generated via EF tooling only** (never hand-written), per
  `Backend/CLAUDE.md`.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Feature scaffolding shared by all stages.

- [ ] T001 [P] Create the `Abwab` substrate folders across backend layers: `Backend/domain/QuranDashboard.Domain/Abwab/`, `Backend/application/QuranDashboard.Application.Abstractions/Abwab/`, `Backend/application/QuranDashboard.Application/Abwab/`, `Backend/infrastructure/QuranDashboard.Infrastructure/Abwab/`, `Backend/api/QuranDashboard.Api/Abwab/`
- [ ] T002 [P] Create backend foundation test folders in `Backend/tests/QuranDashboard.Tests/Abwab/` with subfolders `_Fixtures/`, `_Guards/`, `Ci/`, `ImportSafety/`, `Kernel/`, `Ownership/`, `Permissions/`, `Notifications/`
- [ ] T003 [P] Create the Playwright e2e scaffold directory `Frontend/quran-dashboard-ui/e2e/` (harness + spikes placeholders) without installing `@angular/forms`

---

## Phase 2: Foundational (Blocking Prerequisites)

**⚠️ CRITICAL**: Must complete before any user story. These are cross-cutting and not owned by a single stage.

- [ ] T004 Add the shared Testcontainers PostgreSQL fixture (applies EF migrations to a fresh real DB) in `Backend/tests/QuranDashboard.Tests/Abwab/_Fixtures/PostgresFixture.cs`
- [ ] T005 [P] Add the xUnit collection definition wiring the Postgres fixture in `Backend/tests/QuranDashboard.Tests/Abwab/_Fixtures/AbwabDbCollection.cs`
- [ ] T006 [P] Add the cross-cutting prohibition guard test — fails if any Abwab→Quran foreign key or any Abwab domain writer exists before this feature's exit (FR-009) — in `Backend/tests/QuranDashboard.Tests/Abwab/_Guards/NoPrematureQuranFkTests.cs`

**Checkpoint**: Shared real-PG harness ready; prohibition guard green.

---

## Phase 3: User Story 1 - CI & migration-safety pipeline (Priority: P1) 🎯 MVP

**Goal**: Stand up the Section 15 pipeline so every later safety guarantee is proven in CI against real infrastructure.

**Independent Test**: On a clean checkout, migration-based Testcontainers stand up, the schema-compatibility assertion passes, the contract-drift gate is active, the Playwright harness runs, and the Vitest fork cap is present and enforced.

### Tests for User Story 1 ⚠️ (write first, must fail)

- [ ] T007 [P] [US1] Schema-compatibility assertion test (migrations yield a model-compatible schema, no pending model diff) in `Backend/tests/QuranDashboard.Tests/Abwab/Ci/SchemaCompatibilityTests.cs`
- [ ] T008 [P] [US1] Contract-drift gate test (generated contracts match recorded snapshots) in `Backend/tests/QuranDashboard.Tests/Abwab/Ci/ContractDriftTests.cs`

### Implementation for User Story 1

- [ ] T009 [US1] Establish the CI pipeline with a migration-based Testcontainers backend job in the repo CI workflow (`.github/workflows/`), applying migrations before tests
- [ ] T010 [US1] Wire the schema-compatibility assertion into the CI workflow (`.github/workflows/`) so the build fails on schema/model drift
- [ ] T011 [US1] Wire the contract-drift gate into the CI workflow (`.github/workflows/`) so the build fails when contracts drift
- [ ] T012 [P] [US1] Add `@playwright/test` and a reusable Playwright harness covering the locked scenarios (RTL, keyboard navigation, focus restoration, ARIA basics, virtualization, critical dialogs — §15.2 gate 6) in `Frontend/quran-dashboard-ui/e2e/harness/` and `Frontend/quran-dashboard-ui/package.json`
- [ ] T013 [P] [US1] Verify and lock the Vitest fork cap (`VITEST_MIN_FORKS=1 VITEST_MAX_FORKS=2`) in `Frontend/quran-dashboard-ui/package.json`; add a CI check asserting the env vars are present (because `vitest.config.ts` is ignored by `@angular/build:unit-test`)
- [ ] T014 [US1] Enforce the staged source-package strategy in CI (imports read only from `resources/import-sources/`), documented in the CI workflow
- [ ] T076 [US1] Frontend no-drag **source gate** (FR-041): a CI source test that rejects drag/drop packages, directives, handles, and event wiring anywhere under `Frontend/quran-dashboard-ui/src/`, wired into `.github/workflows/` (§15.2 gate 3, §3.2)
- [ ] T077 [US1] Dependency/security-audit + secret/license CI check (FR-042): run a dependency/security audit and secret/license scan appropriate to the repo, wired into `.github/workflows/` (§15.2 gate 8)

**Checkpoint**: Pipeline green; all later stages' tests can hang off it.

---

## Phase 4: User Story 2 - Quran import safety & destructive-path lockdown (Priority: P1)

**Goal**: Every destructive/force/importer path fails closed against Abwab, even under concurrent dependent creation.

**Independent Test**: Real-PG tests: forbidden/wrong-identity sources are refused; a concurrent dependent creation makes a destructive import fail closed; no `TRUNCATE ... CASCADE` reaches Abwab. The first Abwab Quran FK stays prohibited.

> **Fixture note (mandatory-order constraint)**: US2 runs **before** the kernel/schema (US3, T038), so no real Abwab domain rows or Abwab→Quran FK exist yet. All US2 tests therefore prove the import paths **structurally** — no `TRUNCATE ... CASCADE` wiring, restricted privileges, and a race-safe preflight — using **foundation-only / synthetic dependent fixtures**, never real Abwab domain rows (mirrors FR-013's "foundation-only fixture descriptors, without depending on future workspace types"). This keeps US2 free of a forward dependency on US3.

### Tests for User Story 2 ⚠️ (write first, must fail)

- [ ] T015 [P] [US2] Forbidden-source refusal test with **actual forbidden-source fixtures** (real PG) in `Backend/tests/QuranDashboard.Tests/Abwab/ImportSafety/ForbiddenSourceRefusalTests.cs`
- [ ] T016 [P] [US2] Wrong source-identity / unstable-ID refusal test in `Backend/tests/QuranDashboard.Tests/Abwab/ImportSafety/SourceIdentityTests.cs`
- [ ] T017 [P] [US2] Concurrent-dependent-creation vs destructive-import fails-closed test (real PG), using **foundation-only / synthetic dependent fixtures** (no real Abwab domain rows — none exist until US3), in `Backend/tests/QuranDashboard.Tests/Abwab/ImportSafety/DestructiveRaceTests.cs`

### Implementation for User Story 2

- [ ] T018 [US2] Enumerate and document every destructive/force/importer path in `Backend/report/feature-028-abwab-safety-foundations/destructive-path-inventory.md`
- [ ] T019 [US2] Remove or prevent all `TRUNCATE ... CASCADE` effects that could reach Abwab (structural — no Abwab FK exists yet) in `Backend/tools/QuranDashboard.DataImporter/` and `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/DataPipelines/Quran/`
- [ ] T020 [US2] Add a race-safe dependent lock/preflight before any destructive step (advisory/row lock) in `Backend/tools/QuranDashboard.DataImporter/`
- [ ] T021 [P] [US2] Apply environment restrictions and restricted DB privileges to import paths (importer + `Backend/infrastructure/QuranDashboard.Infrastructure/` config)
- [ ] T022 [P] [US2] Pin canonical source identity and verify stable IDs against the staged package in `Backend/tools/QuranDashboard.DataImporter/` (source read from `resources/import-sources/`)
- [ ] T023 [US2] Confirm importer changes introduce no Abwab→Quran FK; keep the T006 prohibition guard green (`Backend/tests/QuranDashboard.Tests/Abwab/_Guards/NoPrematureQuranFkTests.cs`)

**Checkpoint**: All destructive paths fail closed; first Quran FK still prohibited.

---

## Phase 5: User Story 3 - Audit / timeline / write / concurrency / time kernel (Priority: P1)

**Goal**: Every future Abwab mutation is tracked, append-only, serialized, generation-stamped, gated, and server-clock-stamped.

**Independent Test**: No-ChangeSet write rejected; physical delete rejected (soft-delete enforced); exactly one immutable gen-zero boundary; stale `ExpectedTimelineGeneration` → exact 409 before any row mutation; registry test fails if any writer lacks the barrier.

### Tests for User Story 3 ⚠️ (write first, must fail)

- [ ] T024 [P] [US3] No-ChangeSet write rejection test (real PG) in `Backend/tests/QuranDashboard.Tests/Abwab/Kernel/ChangeSetRequiredTests.cs`
- [ ] T025 [P] [US3] Physical-delete rejection + soft-delete enforced test in `Backend/tests/QuranDashboard.Tests/Abwab/Kernel/SoftDeleteTests.cs`
- [ ] T026 [P] [US3] Sealed default-deny personal-delete exception test with foundation-only fixtures in `Backend/tests/QuranDashboard.Tests/Abwab/Kernel/PersonalDeleteExceptionTests.cs`
- [ ] T027 [P] [US3] Gen-zero boundary seed + forbidden root edit/delete/duplicate test in `Backend/tests/QuranDashboard.Tests/Abwab/Kernel/TimelineBoundarySeedTests.cs`
- [ ] T028 [P] [US3] Stale `ExpectedTimelineGeneration` → exact 409 before any mutation (incl. untouched-row fixture) in `Backend/tests/QuranDashboard.Tests/Abwab/Kernel/TimelineGenerationConflictTests.cs`
- [ ] T029 [P] [US3] Stabilization registry test (fails if any Abwab writer lacks the barrier) in `Backend/tests/QuranDashboard.Tests/Abwab/Kernel/WriteBarrierRegistryTests.cs`
- [ ] T030 [P] [US3] Append-only/TRUNCATE DB-defense + restricted-role test (real PG) in `Backend/tests/QuranDashboard.Tests/Abwab/Kernel/AppendOnlyDefenseTests.cs`
- [ ] T031 [P] [US3] Contract/source coverage test: every mutation port/command + actionable read declares `ExpectedTimelineGeneration` in `Backend/tests/QuranDashboard.Tests/Abwab/Kernel/GenerationContractCoverageTests.cs`
- [ ] T032 [P] [US3] Post-commit-only cache publication + provider-retries-off test in `Backend/tests/QuranDashboard.Tests/Abwab/Kernel/CachePublicationTests.cs`
- [ ] T078 [P] [US3] `AbwabRevisionState` singleton seed test — seeded **exactly once** (`AuditHeadSequence=0`, generation-0, `TreeRevision=0`) and increments **monotonically under row-lock** (real PG) in `Backend/tests/QuranDashboard.Tests/Abwab/Kernel/RevisionStateSeedTests.cs` (FR-014/§7.1/§7.9)
- [ ] T079 [P] [US3] Audit-atomicity rollback test (real PG) — an injected audit/event failure rolls back **all** domain rows with **no half-written ChangeSet** in `Backend/tests/QuranDashboard.Tests/Abwab/Kernel/AuditAtomicityTests.cs` (FR-036, §15.3 "Audit atomicity")
- [ ] T080 [P] [US3] Audit-head monotonicity test (real PG) — concurrent audited commits receive **one strictly increasing** `AuditHeadSequence`; rollback leaves head/generation/tree unchanged in `Backend/tests/QuranDashboard.Tests/Abwab/Kernel/AuditHeadMonotonicityTests.cs` (FR-037, §15.3 "Audit head")
- [ ] T081 [P] [US3] Forbidden-write-API **bypass source/architecture gate** test — fails when an Abwab writer namespace references `ExecuteUpdate`/`ExecuteDelete`/`ExecuteSqlRaw`/`ExecuteSqlInterpolated`/raw `DbCommand`/`NpgsqlConnection`/`NpgsqlCommand`/binary COPY; reviewed allowlist requires owner+reason; **distinct from the T037 interceptor-skip check** in `Backend/tests/QuranDashboard.Tests/Abwab/Kernel/ForbiddenWriteApiGateTests.cs` (FR-038, §6.1 layer 2, §15.2 gate 3, §15.3 "Bypass prevention")

### Implementation for User Story 3

- [ ] T033 [P] [US3] ChangeSet + append-only AuditEvent domain types in `Backend/domain/QuranDashboard.Domain/Abwab/Audit/`
- [ ] T034 [P] [US3] `TimelineGenerationBoundary` **plus the `AbwabRevisionState` singleton** (`AuditHeadSequence`, `TimelineGeneration`, `TreeRevision`, `Version`/xmin) domain state in `Backend/domain/QuranDashboard.Domain/Abwab/Timeline/`; `ChangeSetSequence` is assigned from `AuditHeadSequence` and `EventOrdinal` is per-operation (distinct coordinates, §6.1/§7.9)
- [ ] T035 [P] [US3] `AbwabWriteBarrier` singleton (initial Writable) in `Backend/domain/QuranDashboard.Domain/Abwab/Concurrency/` + port in `Backend/application/QuranDashboard.Application.Abstractions/Abwab/`
- [ ] T036 [P] [US3] Server clock abstraction + implementation (`IServerClock`) in `Backend/application/QuranDashboard.Application.Abstractions/Abwab/` and `Backend/infrastructure/QuranDashboard.Infrastructure/Abwab/`
- [ ] T037 [US3] Tracked ChangeSet unit of work + `SavingChanges` guard (reject no-ChangeSet, reject physical delete, enforce soft-delete, sealed personal-delete exception) **plus the CI bypass check guarding the audit interceptor** (fails CI if the interceptor can be skipped) in `Backend/infrastructure/QuranDashboard.Infrastructure/Abwab/Persistence/`
- [ ] T038 [US3] Generate (via EF tooling) the migration seeding exactly one immutable gen-zero boundary, **the `AbwabRevisionState` singleton (`AuditHeadSequence=0`, generation-0, `TreeRevision=0`)**, **the `AbwabWriteBarrier` singleton row (initial `Writable`)**, immutable ChangeSet generation stamping, append-only/TRUNCATE DB defense, and restricted application role in `Backend/infrastructure/QuranDashboard.Infrastructure/` (report migration name + files after)
- [ ] T039 [US3] `ExpectedTimelineGeneration` command/read contract + 409-before-mutation guard, mapped to the `abwab.*` 409 conflict codes at the API in `Backend/application/QuranDashboard.Application/Abwab/` and `Backend/api/QuranDashboard.Api/Abwab/`
- [ ] T040 [US3] Stabilization middleware/command guard registering every Abwab writer against the barrier in `Backend/application/QuranDashboard.Application/Abwab/` and `Backend/api/QuranDashboard.Api/Abwab/`
- [ ] T041 [US3] Post-commit cache publication + disable provider retries for Abwab manual transactions in `Backend/infrastructure/QuranDashboard.Infrastructure/Abwab/`

**Checkpoint**: Write kernel proven; every writer must pass the barrier and carry generation.

---

## Phase 6: User Story 4 - Shared frontend foundation (Priority: P2)

**Goal**: Only the §14.1 generic primitives, plus a bounded synthetic-tree spike — no domain coupling, no Forms yet.

**Independent Test**: Only §14.1 primitives exist; the spike records bounded perf/browser behavior and freezes no domain DTO; no domain mock/HTTP adapter, no all-domain adapter; `@angular/forms` not installed.

### Tests for User Story 4 ⚠️ (write first, must fail)

- [ ] T042 [P] [US4] Vitest unit tests for generic cache/store/action/conflict primitives in `Frontend/quran-dashboard-ui/src/app/core/data-access/*.spec.ts`
- [ ] T043 [P] [US4] Playwright bounded synthetic-tree spike of **2,000–3,000 nodes** (records perf/browser behavior, no domain DTO — §14.1/§15.3) in `Frontend/quran-dashboard-ui/e2e/spikes/synthetic-tree.spec.ts`

### Implementation for User Story 4

- [ ] T044 [P] [US4] Stable DI + form **conventions** (tokens/providers, no Forms package) in `Frontend/quran-dashboard-ui/src/app/core/`
- [ ] T045 [P] [US4] Generic cache primitive backed by IndexedDB in `Frontend/quran-dashboard-ui/src/app/core/caching/`
- [ ] T046 [P] [US4] Generic store/action/conflict primitives in `Frontend/quran-dashboard-ui/src/app/core/data-access/` and `Frontend/quran-dashboard-ui/src/app/shared/`
- [ ] T047 [US4] Bounded synthetic-tree spike implementation (perf harness, **2,000–3,000 nodes**) in `Frontend/quran-dashboard-ui/e2e/spikes/`
- [ ] T048 [US4] Boundary check: no domain mock/HTTP adapter, no all-domain adapter, `@angular/forms` absent (lint/check + note in `Frontend/quran-dashboard-ui/src/app/core/README.md`)

**Checkpoint**: Generic frontend substrate ready; zero domain leakage.

---

## Phase 7: User Story 5 - System Owner & permission foundation (Priority: P2)

**Goal**: Immutable Owner membership, serialized owner ops with a final-owner invariant, atomic bootstrap, the exact permission catalogue, and an Owner-only grant/revoke slice with non-authoritative frontend hiding.

**Independent Test**: Concurrent owner removals leave ≥1 active owner; bootstrap atomic/idempotent/audited with failure cases; permission codes identical across 5 catalogues; grant/revoke parity/serialization; frontend hiding non-authoritative.

### Tests for User Story 5 ⚠️ (write first, must fail)

- [ ] T049 [P] [US5] Concurrent owner-removal keeps ≥1 active owner + observed-on-next-request test + **explicit no-email/role/runtime-fallback rejection assertion** (real PG) in `Backend/tests/QuranDashboard.Tests/Abwab/Ownership/FinalOwnerInvariantTests.cs`
- [ ] T050 [P] [US5] Zero-to-one bootstrap atomic/idempotent/permanently-audited + wrong-issuer/unverified-email/disabled-account/duplicate-mismatch failure tests in `Backend/tests/QuranDashboard.Tests/Abwab/Ownership/BootstrapTests.cs`
- [ ] T051 [P] [US5] Permission-code parity across seed/policy/`/me`/frontend/test test in `Backend/tests/QuranDashboard.Tests/Abwab/Permissions/PermissionParityTests.cs`
- [ ] T052 [P] [US5] Grant/revoke serialization + stale-version + idempotent-no-audit + unauthorized + permanent-audit + cache-invalidation tests in `Backend/tests/QuranDashboard.Tests/Abwab/Permissions/GrantRevokeTests.cs`
- [ ] T053 [P] [US5] `attribution.view` baseline identical across layers + removal-rejected test in `Backend/tests/QuranDashboard.Tests/Abwab/Permissions/BaselinePermissionTests.cs`
- [ ] T054 [P] [US5] Frontend non-authoritative hiding test (hidden action still rejected by backend policy) in `Frontend/quran-dashboard-ui/e2e/permissions/non-authoritative.spec.ts`
- [ ] T082 [P] [US5] Security-audit vs product-head **separation** test (real PG) — grant/revoke/bootstrap produce permanent **security-audit** events but do **NOT** advance `AuditHeadSequence` and **never** appear as Product-Restore-head events, while still taking the barrier + `AbwabRevisionState` locks and carrying `ExpectedTimelineGeneration` in `Backend/tests/QuranDashboard.Tests/Abwab/Permissions/SecurityAuditSeparationTests.cs` (FR-039, §6.1/§6.2/§6.7/§7.7/§8)
- [ ] T083 [P] [US5] `SystemOwnerOnly` assignability-rejection test — granting a `SystemOwnerOnly` code (`permission.*`, `audit.restore`, `safetyPoint.*`) to an ordinary user is rejected with `abwab.permission_baseline_locked` in `Backend/tests/QuranDashboard.Tests/Abwab/Permissions/AssignabilityTests.cs` (SC-007, §5.2/§11)
- [ ] T084 [P] [US5] Rate-limiter startup/options + safe-429 test — safe **enabled** defaults load; stricter named policies for permission-administration and owner-bootstrap; quotas positive/bounded/documented in `Backend/tests/QuranDashboard.Tests/Abwab/Permissions/RateLimiterDefaultsTests.cs` (FR-040, §20.1/§4/§15)

### Implementation for User Story 5

- [ ] T055 [P] [US5] `SystemOwnerMembership` domain (immutable issuer/subject, enabled checks, final-owner invariant) in `Backend/domain/QuranDashboard.Domain/Security/Owners/`
- [ ] T056 [US5] Serialized owner add/remove + atomic/idempotent, permanently-audited zero-to-one bootstrap handler in `Backend/application/QuranDashboard.Application/Security/Owners/`
- [ ] T057 [P] [US5] Exact permission catalogue + seed (codes + metadata: `SystemOwnerOnly`, `DashboardAdminBaseline`) in `Backend/domain/QuranDashboard.Domain/Security/Permissions/` and Infrastructure seed
- [ ] T058 [US5] `PermissionAssignment` (role/direct unique keys) + serialized race semantics + idempotent no-audit no-ops in `Backend/application/QuranDashboard.Application/Security/Permissions/` and `Backend/infrastructure/QuranDashboard.Infrastructure/`
- [ ] T059 [US5] `/me` projection + cache invalidation + policy handlers in `Backend/application/QuranDashboard.Application/Security/` and `Backend/api/QuranDashboard.Api/Security/`
- [ ] T060 [P] [US5] Permission-administration port + mock in `Backend/application/QuranDashboard.Application.Abstractions/Security/` (+ mock in tests)
- [ ] T061 [US5] Owner-only list/grant/revoke backend + API returning the `ApiResponse` envelope and `abwab.*` 409 codes in `Backend/api/QuranDashboard.Api/Security/Permissions/`
- [ ] T062 [US5] Add `@angular/forms` and build the Owner-only Reactive-Forms grant/revoke UI in `Frontend/quran-dashboard-ui/src/app/features/permissions/` (+ `package.json`)
- [ ] T063 [US5] Frontend permission consumption from `/me` with non-authoritative hiding in `Frontend/quran-dashboard-ui/src/app/core/auth/`
- [ ] T064 [US5] Verify Owner **membership** administration is never exposed in the dashboard (only permission admin); note boundary in `Backend/api/QuranDashboard.Api/Security/README.md`
- [ ] T085 [US5] Rate-limiter safe-**enabled** defaults on the existing limiter infrastructure + **separate stricter named policies** for permission-administration and operational owner-bootstrap paths (positive/bounded/documented quotas) in `Backend/api/QuranDashboard.Api/` limiter configuration (FR-040, §20.1)

**Checkpoint**: Ownership + permission slice proven end-to-end; backend authoritative, frontend hiding non-authoritative.

---

## Phase 8: User Story 6 - Durable notification storage (Priority: P3)

**Goal**: Transaction-joining, duplicate-safe notification **storage only** — no public surface.

**Independent Test**: Writer joins a caller's transaction (rolled-back caller → no row); unique source identity prevents duplicates; read state is outside product audit/restore; no public port/endpoint/mock/HTTP/UI introduced.

### Tests for User Story 6 ⚠️ (write first, must fail)

- [ ] T065 [P] [US6] Storage-writer joins caller transaction (rolled-back caller → no row) test (real PG) in `Backend/tests/QuranDashboard.Tests/Abwab/Notifications/TransactionJoinTests.cs`
- [ ] T066 [P] [US6] Unique-source-identity dedup + read-state-outside-audit test in `Backend/tests/QuranDashboard.Tests/Abwab/Notifications/DedupAndReadStateTests.cs`

### Implementation for User Story 6

- [ ] T067 [US6] Generate (via EF tooling) the notification schema (recipient/source/idempotency) + read state migration in `Backend/infrastructure/QuranDashboard.Infrastructure/Abwab/Notifications/` (report migration name + files after)
- [ ] T068 [US6] Transaction-capable persistence writer joining the caller's unit of work in `Backend/infrastructure/QuranDashboard.Infrastructure/Abwab/Notifications/`
- [ ] T069 [P] [US6] Low-level recipient/read-state repository in `Backend/infrastructure/QuranDashboard.Infrastructure/Abwab/Notifications/`
- [ ] T070 [US6] Boundary guard: confirm no public notification port/endpoint/mock/HTTP adapter/UI is introduced by `028` (note in `Backend/infrastructure/QuranDashboard.Infrastructure/Abwab/Notifications/README.md`)

**Checkpoint**: Notification storage ready for `032` (surfaces) and `033` (restore events).

---

## Phase 9: Polish & Cross-Cutting Concerns

- [ ] T071 [P] Run `quickstart.md` end-to-end validation across all §18.2 exit gates (`specs/028-abwab-safety-foundations/quickstart.md`)
- [ ] T072 [P] Update nearest READMEs for every area touched (importer, persistence DataPipelines, frontend `core`/`shared`, API `Security`) per the repo README rule
- [ ] T073 Run the clean-code guard and test-guard self-checks against delivered code and fix findings
- [ ] T074 [P] Write the backend completion/validation report in `Backend/report/feature-028-abwab-safety-foundations/`
- [ ] T075 Confirm the first Abwab Quran FK is still prohibited at exit and record the acceptance handoff to `029`

---

## Dependencies & Execution Order

### Mandatory stage order (§18.2)

The stages are built **in order**; each begins only after the prior stage's §18.2 exit gate passes:

```
Setup (P1) → Foundational (P2) → US1 → US2 → US3 → US4 → US5 → US6 → Polish
```

- **US1 (CI & migration safety)** — first; establishes the pipeline all later tests need.
- **US2 (import safety)** — after US1 (needs real-PG CI). The first Abwab Quran FK is prohibited until US2's exit.
- **US3 (write kernel)** — after US1; provides ChangeSet/barrier/timeline/clock every writer depends on.
- **US4 (frontend foundation)** — mandated 4th. Its only real code dependency is the US1 Playwright harness; it has **no** dependency on US2/US3 backend, so in a staffed team it may proceed alongside US2/US3, but §18.2 fixes its acceptance position after the kernel.
- **US5 (owner + permission slice)** — after US3 (uses audit/concurrency/`/me`/cache) and US4 (frontend conventions/primitives); installs `@angular/forms`.
- **US6 (notification storage)** — after US3 (transaction-capable persistence/UoW); last.

### Within each story

- Tests are written **first** and must fail before implementation.
- Domain types → ports/abstractions → infrastructure/persistence → application handlers → API/UI.
- EF migrations (T038, T067) are generated by tooling after their entity types exist.

### Parallel opportunities

- **[P] within a story**: tasks on different files with no incomplete dependency (e.g. the test tasks T024–T032; domain types T033–T036; frontend primitives T044–T046).
- **Across stories**: limited by the mandatory order. The genuine cross-story parallel is **US4 frontend** alongside **US2/US3 backend** (different stacks, no shared files), provided the US1 Playwright harness exists.
- **Not parallel**: US2→US3 share the kernel/persistence surface conceptually but touch different files; US5 and US6 both depend on US3 and must follow it.

---

## Parallel Example: User Story 3 (kernel tests)

```bash
# Write all kernel tests together first (they must fail before implementation):
Task: "No-ChangeSet write rejection test (T024)"
Task: "Physical-delete rejection + soft-delete test (T025)"
Task: "Gen-zero boundary seed + forbidden-root test (T027)"
Task: "Stale generation → 409-before-mutation test (T028)"
Task: "Write-barrier registry test (T029)"
Task: "Append-only/TRUNCATE DB-defense test (T030)"

# Then the independent domain types together:
Task: "ChangeSet + AuditEvent domain types (T033)"
Task: "TimelineGenerationBoundary + generation state (T034)"
Task: "AbwabWriteBarrier singleton + port (T035)"
Task: "Server clock abstraction + impl (T036)"
```

---

## Implementation Strategy

### MVP scope

The MVP is **US1 + US2 + US3** (all P1): the CI pipeline, import-safety lockdown, and the
write kernel. That combination is the actual fail-closed substrate — it is what unblocks the
first Abwab Quran FK and every `029`–`034` writer. US4 (P2), US5 (P2), and US6 (P3) then
complete the shared frontend substrate, the security vertical slice, and notification storage.

### Incremental delivery

1. Setup + Foundational → shared real-PG harness and the prohibition guard.
2. US1 → pipeline green.
3. US2 → destructive paths fail closed (FK still prohibited).
4. US3 → write kernel proven.
5. US4 → generic frontend substrate.
6. US5 → owner + permission slice (installs `@angular/forms`).
7. US6 → notification storage.
8. Polish → quickstart validation, READMEs, guards, report, acceptance handoff to `029`.

### Exit

The feature is complete only when **every** §18.2 exit/acceptance criterion passes in CI and
the first Abwab Quran foreign key remains prohibited until that acceptance.
