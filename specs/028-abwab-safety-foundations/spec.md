# Feature Specification: Abwab Safety Foundations — Fail-Closed Substrate

**Feature Branch**: `028-abwab-safety-foundations`

**Created**: 2026-07-22

**Status**: Draft

**Input**: User description: "Create the Spec Kit for 028-abwab-safety-foundations using only section §18.2 of docs/feature-abwab-management/MASTER_PLAN.md"

> **Canonical source**: `docs/feature-abwab-management/MASTER_PLAN.md` is the sole
> canonical product and architecture source for Abwab Spec Kits `027`–`034`. This
> specification is derived **only from Master Plan §18.2** (`028-abwab-safety-foundations`
> — fail-closed substrate) plus its entry/exit gates. It introduces no new product or
> architecture decision and reinterprets nothing. Section references (e.g. §15, §14.1,
> §16.3) point to the Master Plan and are recorded here as pointers only; their content
> is owned by those sections, not re-decided here. Where a conflict is perceived, the
> Master Plan governs and a genuine change returns to an independent amendment/re-review
> of that document, never a local decision here.

## Overview *(context)*

`028-abwab-safety-foundations` is the second of eight top-level Abwab Spec Kits and the
**first that produces code**. Its single purpose is to build a **fail-closed safety
substrate** so that no Abwab domain writer and no first Quran foreign key can exist until
every safety, audit, concurrency, ownership, and notification-storage guarantee is proven
in CI against real infrastructure (§18.2).

The feature is delivered as six stages that MUST be built in the exact **mandatory
internal order** given by §18.2. Each stage is independently testable at its own exit
gate, but the stages are **not freely reorderable**: a later stage may only begin once the
earlier stage's guarantees hold. The feature is complete only when every exit/acceptance
criterion in §18.2 passes in CI.

**Entry condition (§18.2):** `027-abwab-preflight` is accepted, and **no Abwab domain
writer or Quran foreign key exists yet**. The first Abwab Quran FK remains prohibited
until this feature's exit is accepted.

**Scope boundaries carried from §18.2:**

- This feature builds *foundations and one security vertical slice only*. It does **not**
  build the Abwab domain (sections, categories, tree, relationships, attribution links,
  workspace/review/notifications surfaces, audit/restore read model, or realtime
  hardening) — those are owned by `029`–`034`.
- The shared frontend foundation installs generic primitives only; it does **not** install
  Forms as preparation, create domain mocks/HTTP adapters, or freeze any domain DTO.
- The notification capability delivers **durable storage only**; it exposes no public
  port, endpoint, mock, HTTP adapter, or UI. `032` owns notification surfaces and the
  normal event matrix; `033` calls the storage writer for restore events.

## User Scenarios & Testing *(mandatory)*

<!--
  The six user stories below are the six mandatory-order stages of §18.2. Priority order
  reflects the mandatory build order: the substrate must exist before the domain can be
  built on it. Each story is independently testable at its own §18.2 exit gate, but the
  build order is fixed by §18.2 and is not a free MVP-reordering choice.
-->

### User Story 1 - CI and migration-safety pipeline (Priority: P1)

A platform engineer establishes the CI and migration-safety pipeline before any Abwab
schema or writer exists, so every later safety guarantee is proven automatically against
real infrastructure on every change (§18.2 step 1).

**Why this priority**: Nothing else in the substrate can be trusted without the pipeline
that runs it. Migration-based Testcontainers, the schema-compatibility assertion, the
contract-drift gate, and the preserved test-concurrency cap are the harness every other
stage's tests depend on.

**Independent Test**: Run the pipeline on a clean checkout; it is valid when
migration-based Testcontainers spin up, the schema-compatibility assertion passes, the
contract-drift gate is active, the reusable Playwright harness runs, and the Vitest
fork-concurrency cap (`VITEST_MIN_FORKS`/`VITEST_MAX_FORKS` in `package.json`) is present
and enforced.

**Acceptance Scenarios**:

1. **Given** a clean checkout, **When** CI runs, **Then** the Section 15 pipeline stands
   up migration-based Testcontainers and asserts schema compatibility.
2. **Given** the frontend test configuration, **When** CI runs unit tests, **Then** the
   fork-concurrency cap is read from `package.json` (because `vitest.config.ts` is ignored
   by the `@angular/build:unit-test` builder) and is enforced.
3. **Given** a contract change, **When** it drifts from the recorded contract, **Then** the
   contract-drift gate fails the build.
4. **Given** the reusable Playwright harness, **When** a browser test is added, **Then** it
   runs on the shared harness rather than a one-off setup.

---

### User Story 2 - Quran import safety and destructive-path lockdown (Priority: P1)

A data-safety owner inventories and neutralizes every destructive, force, and importer
path so that Abwab data can never be truncated or corrupted by a Quran import, even under
concurrent creation of dependent rows (§18.2 step 2).

**Why this priority**: The first Abwab Quran FK is prohibited until these paths fail
closed. A single un-guarded `TRUNCATE ... CASCADE` or force path could silently destroy
curated Abwab data; this must be proven safe before any later Kit is allowed to add the
first FK.

**Independent Test**: Enumerate every destructive/force/importer path; the stage is valid
when each is removed or prevented from affecting Abwab, a race-safe dependent
lock/preflight blocks import while dependents are being created, environment and DB
privileges are restricted, canonical source identity/stable IDs are verified, and
real-PostgreSQL refusal tests pass.

**Acceptance Scenarios**:

1. **Given** the destructive-path inventory, **When** it is reviewed, **Then** every
   destructive/force/importer path is enumerated and each `TRUNCATE ... CASCADE` effect on
   Abwab is removed or prevented.
2. **Given** a concurrent dependent-creation, **When** a destructive import runs, **Then**
   the race-safe dependent lock/preflight blocks it and it fails closed.
3. **Given** a forbidden or wrong-identity source package, **When** an import is attempted,
   **Then** pinned canonical source-identity/stable-ID verification refuses it (proven by
   real-PostgreSQL refusal tests with actual forbidden-source fixtures).
4. **Given** the deployment environment and DB role, **When** an import path runs, **Then**
   environment restrictions and restricted DB privileges apply.

---

### User Story 3 - Audit / timeline / write / concurrency / time kernel (Priority: P1)

A backend architect implements the write kernel that makes every future Abwab mutation
tracked, append-only, serialized, generation-stamped, and gated, so no domain writer can
ever mutate state without an audit trail and the global write barrier (§18.2 step 3).

**Why this priority**: This kernel is the contract every `029`–`034` writer is built on.
The tracked ChangeSet, append-only events, the `AbwabWriteBarrier` global gate, the
timeline-generation contract, and the server clock are the primitives that make the whole
portfolio's concurrency and audit guarantees enforceable.

**Independent Test**: Exercise the kernel with foundation-only fixtures; it is valid when a
write without a ChangeSet is rejected, a physical delete is rejected, a soft-delete is
enforced, cache publication happens only on commit, a stale `ExpectedTimelineGeneration`
fails with the exact 409 before any row mutation, and a registry test fails if any writer
lacks the global gate.

**Acceptance Scenarios**:

1. **Given** an attempted write with no tracked ChangeSet, **When** it is committed,
   **Then** it is rejected.
2. **Given** an attempted physical/hard delete, **When** it is issued, **Then** it is
   rejected and soft-delete is enforced; a sealed, default-deny personal-delete exception
   is proven with foundation-only fixture descriptors, without depending on future
   workspace types.
3. **Given** a command carrying a stale `ExpectedTimelineGeneration`, **When** the
   generation has advanced, **Then** it fails with the exact 409 **before any row
   mutation** — including representative security/personal commands and a domain fixture
   whose target row/revision was untouched.
4. **Given** the singleton timeline state, **When** the migration runs, **Then** exactly
   one immutable generation-zero `TimelineGenerationBoundary` root is seeded, ChangeSet
   generation stamping is immutable, and forbidden root edit/delete/duplicate attempts
   fail; only `033` may insert non-root boundaries through the sealed restore transaction.
5. **Given** any Abwab writer, **When** the stabilization registry test runs, **Then** it
   fails if that writer lacks the global `AbwabWriteBarrier` gate; the barrier starts in a
   Writable state and publishes caches only post-commit, with provider retries locked off
   for Abwab manual transactions.

---

### User Story 4 - Shared frontend foundation (Priority: P2)

A frontend architect implements only the shared ownership defined in §14.1 — stable DI and
form conventions, generic cache/store/action/conflict primitives, IndexedDB, the Playwright
harness, and a bounded synthetic-tree spike — so later domain UI Kits inherit a consistent
substrate without any domain coupling introduced early (§18.2 step 4).

**Why this priority**: The frontend substrate is shared by all later UI Kits, but it is not
on the critical path for the backend safety gates; it can be proven independently of the
domain. Installing domain coupling or Forms here would leak `029`–`033` ownership forward.

**Independent Test**: Inspect the delivered frontend foundation; it is valid when only the
§14.1 generic primitives exist, the synthetic-tree spike records bounded performance and
browser behavior **without** freezing a domain DTO, and there is **no** all-domain adapter,
no domain mock/HTTP adapter, and no Forms package installed as preparation.

**Acceptance Scenarios**:

1. **Given** the shared frontend foundation, **When** it is reviewed, **Then** it contains
   only the §14.1 ownership: stable DI/form conventions, generic
   cache/store/action/conflict primitives, IndexedDB, and the Playwright harness.
2. **Given** the synthetic-tree spike, **When** it runs, **Then** it records bounded
   performance and browser behavior and does **not** freeze any domain DTO.
3. **Given** the foundation, **When** dependencies are checked, **Then** no domain
   mock/HTTP adapter exists and `@angular/forms` is **not** yet installed at this stage.

---

### User Story 5 - System Owner and permission foundation (Priority: P2)

A security engineer implements the System Owner membership model and the permission
foundation as a single security vertical slice — immutable membership, serialized
owner add/remove with a final-owner invariant, an operational bootstrap, the exact
permission catalogue, and an Owner-only grant/revoke surface — so authorization is
provably fail-closed and non-authoritative in the frontend (§18.2 step 5).

**Why this priority**: This is the first vertical slice that exercises the whole substrate
(audit, concurrency, `/me`, cache invalidation, real Reactive Forms). It proves the
permission primitives every later Kit's authorization depends on. It follows the kernel
because it relies on the audit/concurrency guarantees from Story 3.

**Independent Test**: Exercise the ownership and permission slice; it is valid when
concurrent owner removals always leave at least one active owner, zero-to-one bootstrap is
atomic/idempotent and permanently audited (with wrong-issuer/unverified-email/disabled-
account/duplicate-mismatch failures), permission codes are identical across
seed/policy/`/me`/frontend/test catalogues, list/grant/revoke parity holds, and frontend
hiding is demonstrably non-authoritative.

**Acceptance Scenarios**:

1. **Given** concurrent owner removals, **When** they race, **Then** at least one active
   owner is preserved; removal/disable is observed on the next sensitive request; no
   email/role/runtime fallback exists.
2. **Given** the zero-to-one bootstrap, **When** it runs, **Then** it is atomic/idempotent
   and permanently audited; wrong issuer, unverified bootstrap email, disabled account, and
   duplicate mismatched identity each fail.
3. **Given** the five catalogues (seed, policy, `/me`, frontend, test), **When** permission
   codes are compared, **Then** they are identical; list/grant/revoke parity,
   assignability/baseline denial, exact role/direct unique keys, idempotent no-audit
   no-ops, first-grant and grant-versus-revoke serialization, stale-version, unauthorized,
   permanent-audit, cache-invalidation, and stabilization tests all pass.
4. **Given** a committed grant/revoke, **When** `/me`, backend policy, cache, and UI are
   read, **Then** they converge on the committed winner and frontend hiding is demonstrably
   non-authoritative.
5. **Given** the grant/revoke UI, **When** it is implemented, **Then** `@angular/forms` is
   added here (real Reactive Forms first use) and Owner **membership administration** is
   never exposed in the dashboard.
6. **Given** the `attribution.view` baseline, **When** its metadata/policy is checked
   across seed/policy/`/me`/frontend, **Then** it is identical and attempts to remove the
   baseline are rejected; actual Pending list/detail/count behavior remains owned by `032`.

---

### User Story 6 - Durable notification storage capability (Priority: P3)

A backend engineer implements durable notification **storage only** — a
recipient/source/idempotency schema, read state, a transaction-capable persistence writer,
and a low-level recipient/read-state repository — so later Kits can persist notifications
inside a caller's transaction without duplicates, while all public surfaces stay out of
this feature (§18.2 step 6).

**Why this priority**: Storage must exist before `032` can bind notification surfaces and
`033` can emit restore events, but it carries no user-facing behavior itself, so it is the
last stage and the lowest priority within this feature.

**Independent Test**: Exercise the storage writer; it is valid when it can join a caller's
domain transaction, unique source identity prevents duplicates, read state is kept outside
product audit/restore, and no notification UI/transport ownership is introduced.

**Acceptance Scenarios**:

1. **Given** a caller's domain transaction, **When** a notification is written, **Then** the
   storage writer joins that transaction.
2. **Given** two writes with the same source identity, **When** both are attempted, **Then**
   unique source identity prevents the duplicate.
3. **Given** the notification storage, **When** it is reviewed, **Then** read state is
   outside product audit/restore and **no** public port, endpoint, mock, HTTP adapter, or
   UI is introduced here (owned by `032`; `033` calls the writer for restore events).

---

### Edge Cases

- **Concurrent dependent creation during a destructive import** — the race-safe dependent
  lock/preflight must block the import and fail closed, not race it (Story 2).
- **Stale timeline generation on an untouched row** — a command with an outdated
  `ExpectedTimelineGeneration` must return the exact 409 before any row mutation even when
  its target row/revision was never touched (Story 3).
- **Last-owner removal under concurrency** — two simultaneous owner removals must not both
  succeed; at least one active owner always remains (Story 5).
- **Bootstrap with a wrong or unverified identity** — wrong issuer, unverified bootstrap
  email, disabled account, or duplicate mismatched identity must each fail the zero-to-one
  bootstrap (Story 5).
- **A new Abwab writer added without the global gate** — the stabilization registry test
  must fail (Story 3).
- **A mutation port/command or actionable read that omits TimelineGeneration** — the
  foundation contract/source test must fail (Story 3).
- **Personal-delete exception** — the sealed, default-deny personal-delete path must be
  proven with foundation-only fixture descriptors, without depending on future workspace
  types (Story 3).
- **Duplicate notification by source identity** — unique source identity must prevent the
  duplicate (Story 6).

## Requirements *(mandatory)*

### Functional Requirements

**CI and migration safety (Story 1)**

- **FR-001**: The system MUST establish the Section 15 CI pipeline using migration-based
  Testcontainers and a schema-compatibility assertion.
- **FR-002**: The system MUST enforce the source-package strategy and a contract-drift gate
  that fails the build on drift.
- **FR-003**: The system MUST preserve the Vitest fork-concurrency cap
  (`VITEST_MIN_FORKS`/`VITEST_MAX_FORKS` in `package.json`), because `vitest.config.ts` is
  ignored by the `@angular/build:unit-test` builder.
- **FR-004**: The system MUST provide a reusable Playwright harness for browser tests.

**Quran import safety (Story 2)**

- **FR-005**: The system MUST inventory every destructive/force/importer path and remove or
  prevent all `TRUNCATE ... CASCADE` effects on Abwab.
- **FR-006**: The system MUST add a race-safe dependent lock/preflight that blocks a
  destructive import while dependents are being created, failing closed under concurrency.
- **FR-007**: The system MUST apply environment restrictions and restricted DB privileges to
  import paths.
- **FR-008**: The system MUST verify pinned canonical source identity and stable IDs, and
  MUST refuse forbidden or wrong-identity sources (proven by real-PostgreSQL refusal tests
  with actual forbidden-source fixtures).
- **FR-009**: The first Abwab Quran foreign key MUST remain prohibited until this feature's
  exit is accepted.

**Audit / timeline / write / concurrency / time kernel (Story 3)**

- **FR-010**: The system MUST implement a tracked ChangeSet unit of work and MUST reject any
  write that has no ChangeSet.
- **FR-011**: The system MUST record append-only events with commit-correct sequencing and
  restricted persistence boundaries, with a SavingChanges guard and CI bypass checks.
- **FR-012**: The system MUST enforce soft-delete and reject physical/hard deletes, with a
  database-level append-only/TRUNCATE defense.
- **FR-013**: The system MUST provide a sealed, default-deny personal-delete exception proven
  with foundation-only fixture descriptors, without depending on future workspace types.
- **FR-014**: The system MUST maintain singleton monotonic audit-head/revision/generation
  state using the `uint`/xmin convention, with immutable ChangeSet generation stamping.
- **FR-015**: The migration MUST seed exactly one immutable generation-zero
  `TimelineGenerationBoundary` root; root edit/delete/duplicate MUST fail; only `033` may
  insert non-root boundaries through the sealed restore transaction.
- **FR-016**: Every mutation port/command and every actionable read MUST carry an
  `ExpectedTimelineGeneration`/TimelineGeneration contract; a foundation contract/source
  test MUST fail if any omits it.
- **FR-017**: A command carrying a stale `ExpectedTimelineGeneration` MUST fail with the
  exact 409 **before any row mutation**, including representative security/personal commands
  and a domain fixture whose target row/revision was untouched.
- **FR-018**: The system MUST provide a server clock (server-authoritative time), not
  client time.
- **FR-019**: The system MUST enforce a global `AbwabWriteBarrier` singleton gate that starts
  in a Writable state; a stabilization registry test MUST fail when any Abwab writer lacks
  the gate.
- **FR-020**: Cache publication MUST occur only post-commit, and provider retries MUST be
  locked off for Abwab manual transactions.

**Shared frontend foundation (Story 4)**

- **FR-021**: The system MUST implement only the §14.1 shared frontend ownership: stable
  DI/form conventions, generic cache/store/action/conflict primitives, IndexedDB, and the
  Playwright harness.
- **FR-022**: The system MUST run a bounded synthetic-tree spike that records bounded
  performance and browser behavior **without** freezing a domain DTO.
- **FR-023**: This stage MUST NOT install Forms as preparation, MUST NOT create domain
  mocks/HTTP adapters, and MUST NOT create an all-domain adapter.

**System Owner and permission foundation (Story 5)**

- **FR-024**: The system MUST model immutable issuer/subject Owner membership with
  enabled-account checks and no email/role/runtime fallback.
- **FR-025**: The system MUST serialize Owner add/remove and enforce a final-owner
  invariant so concurrent removals always leave at least one active owner; removal/disable
  MUST be observed on the next sensitive request.
- **FR-026**: The system MUST provide an atomic, idempotent, permanently-audited zero-to-one
  Owner bootstrap that fails on wrong issuer, unverified bootstrap email, disabled account,
  or duplicate mismatched identity.
- **FR-027**: The system MUST implement the exact permission catalogue with codes identical
  across seed, policy, `/me`, frontend, and test catalogues, with retained uniquely-keyed
  role/subject assignment state and serialized race semantics.
- **FR-028**: The system MUST provide `/me` projection, cache invalidation, and policy
  handlers; `/me`, backend policy, cache, and UI MUST converge on the committed winner.
- **FR-029**: The system MUST implement an Owner-only permission-administration
  port/mock plus list/grant/revoke backend/API/HTTP/UI as a security vertical slice, with
  parity and cache tests, and MUST NOT expose Owner **membership** administration in the
  dashboard.
- **FR-030**: The system MUST add `@angular/forms` at this stage because the real
  grant/revoke form imports and tests Reactive Forms; frontend hiding MUST be demonstrably
  non-authoritative.
- **FR-031**: The `attribution.view` baseline metadata/policy MUST be identical across
  seed/policy/`/me`/frontend, and attempts to remove the baseline MUST be rejected; actual
  Pending list/detail/count behavior remains owned by `032`.

**Durable notification storage (Story 6)**

- **FR-032**: The system MUST implement a notification recipient/source/idempotency schema
  with read state and a transaction-capable persistence writer plus a low-level
  recipient/read-state repository.
- **FR-033**: The notification storage writer MUST be able to join a caller's domain
  transaction, and unique source identity MUST prevent duplicates.
- **FR-034**: Notification read state MUST be kept outside product audit/restore.
- **FR-035**: This stage MUST expose no public notification port, endpoint, mock, HTTP
  adapter, or UI; `032` owns those surfaces and the normal event matrix, and `033` calls the
  storage writer for restore events.

### Key Entities *(include if feature involves data)*

- **ChangeSet (tracked unit of work)**: The audited envelope for every Abwab mutation;
  carries immutable generation stamping; a write without one is rejected.
- **Audit event (append-only)**: Commit-sequenced, append-only record of a mutation;
  physically un-deletable, un-truncatable.
- **TimelineGenerationBoundary**: Singleton monotonic timeline state; exactly one immutable
  generation-zero root is seeded; non-root boundaries are insertable only by `033`'s sealed
  restore transaction.
- **AbwabWriteBarrier**: Global singleton gate (initial Writable state) that every Abwab
  writer MUST pass; enforced by a stabilization registry test.
- **System Owner membership**: Immutable issuer/subject membership with enabled-account
  checks, serialized add/remove, and a final-owner invariant; administered outside the
  dashboard.
- **Permission catalogue / assignment**: The exact permission codes and uniquely-keyed
  role/subject assignments, identical across seed/policy/`/me`/frontend/test.
- **`attribution.view` baseline**: A baseline permission whose metadata/policy is identical
  across layers and cannot be removed.
- **Notification storage record**: Recipient/source/idempotency schema with read state,
  written inside a caller's transaction, deduplicated by unique source identity, kept
  outside product audit/restore.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of enumerated destructive/force/importer paths fail closed against Abwab
  under concurrent dependent creation, and no `TRUNCATE ... CASCADE` effect on Abwab
  survives (verified by real-PostgreSQL tests, including actual forbidden-source fixtures).
- **SC-002**: 0 Abwab writes succeed without a tracked ChangeSet, and 0 physical deletes
  succeed (soft-delete enforced), verified by no-ChangeSet-rejection and physical-delete-
  rejection tests plus the DB append-only/TRUNCATE defense.
- **SC-003**: A stale `ExpectedTimelineGeneration` command fails with the exact 409 in 100%
  of cases **before any row mutation**, and the foundation contract test fails whenever any
  mutation port/command or actionable read omits TimelineGeneration.
- **SC-004**: Exactly 1 immutable generation-zero timeline boundary exists after migration;
  root edit/delete/duplicate attempts fail in 100% of cases; non-root insertion is possible
  only via `033`'s sealed restore transaction.
- **SC-005**: Under concurrent Owner removals, at least 1 active owner always remains
  (0 zero-owner outcomes), and removal/disable is observed on the next sensitive request
  with no email/role/runtime fallback.
- **SC-006**: The zero-to-one bootstrap is atomic, idempotent, and permanently audited;
  wrong issuer, unverified email, disabled account, and duplicate mismatched identity each
  fail in 100% of cases.
- **SC-007**: Permission codes are identical (0 drift) across all 5 catalogues (seed,
  policy, `/me`, frontend, test); list/grant/revoke parity, serialization, stale-version,
  unauthorized, permanent-audit, and cache-invalidation tests all pass; frontend hiding is
  demonstrably non-authoritative.
- **SC-008**: The stabilization registry test fails whenever any Abwab writer lacks the
  global `AbwabWriteBarrier` gate, and cache publication is observed only post-commit.
- **SC-009**: Notification storage joins a caller's domain transaction and prevents
  duplicates by unique source identity; read state is outside product audit/restore; 0
  public notification port/endpoint/mock/HTTP/UI surfaces are introduced by this feature.
- **SC-010**: The frontend foundation contains only §14.1 primitives; the synthetic-tree
  spike records bounded performance/browser behavior with 0 frozen domain DTOs and 0
  all-domain adapters; `@angular/forms` is present only from Story 5 onward.
- **SC-011**: Every §18.2 exit/acceptance criterion passes in CI; the first Abwab Quran
  foreign key remains prohibited until this feature's exit is accepted.

## Assumptions

- This specification is derived **only from Master Plan §18.2** (and its entry/exit gates).
  Where §18.2 points to another section (§15, §14.1, §16.3, §5, §6.x, etc.), that section's
  content is owned there and recorded here only as a pointer; it is not re-decided in this
  spec.
- `027-abwab-preflight` is accepted, and no Abwab domain writer or Quran foreign key exists
  at entry (§18.2 Entry).
- The Master Plan governs any perceived conflict; a genuine change returns to an
  independent amendment/re-review of the Master Plan, never a local decision here.
- The six stages are built in §18.2's mandatory internal order; each is independently
  testable at its own exit gate, but the order is fixed and not a free MVP reordering.
- "Real infrastructure" for verification means real PostgreSQL (Testcontainers) and a real
  browser (Playwright), consistent with §18.2's stated foundation tests.

## Dependencies

- **Predecessor**: `027-abwab-preflight` (accepted) is the sole predecessor; this feature's
  only entry edge is `027 → 028`.
- **Downstream ownership boundaries** (not built here): `029`–`033` own the Abwab domain
  and its surfaces; `032` owns notification surfaces and the normal event matrix; `033`
  owns audit/restore, non-root timeline boundaries, and restore-event emission via the
  storage writer; `034` owns realtime hardening, live/bootstrap-readiness, and release
  proof.
