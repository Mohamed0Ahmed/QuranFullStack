# Implementation Plan: Abwab Door Inclusions

**Branch**: `feat/abwab-chapter-inclusion` | **Date**: 2026-08-17 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/001-abwab-door-inclusions/spec.md`

## Summary

Add door inclusion as an independent directed graph that materializes each source door's existing
link records into one aggregate target door and keeps those records synchronized in the initiating
transaction. Store each direct edge and each source-record occurrence mapping explicitly, reuse
normal `LinkingUnit` records for target content, and preserve target-local override/suppression
semantics without exposing origin metadata. Add a public topology read plus permission-classified
create/delete commands, then compose a target-first Arabic inclusion modal from the existing Abwab
context menu and live multi-select door picker. Existing link content contracts and components stay
unchanged.

## Technical Context

**Language/Version**: C# on .NET 10; TypeScript 5.9 with Angular 20.3

**Primary Dependencies**: ASP.NET Core 10, EF Core 10.0.8, Npgsql EF provider 10.0.0,
PostgreSQL, Angular CDK, RxJS 7.8, Tailwind CSS 3.4, generated OpenAPI models

**Storage**: PostgreSQL; two additive Abwab inclusion tables plus a nullable internal-inclusion
reference, conditionally nullable contribution `OperationId`, and coherence constraints that keep
every public linking contribution attached to its existing `LinkingOperation`

**Testing**: Test Freeze remains active. No new test class, test method, Angular unit spec, or
Playwright journey. Use the Backend build, pending-model check after an authorized migration,
existing migration/gate-contract/tier-b/smoke lanes, API contract drift check, existing frontend
generation/static/build/Golden gates, and the manual runtime matrix in `quickstart.md`.

**Target Platform**: Current single-instance Linux-hosted ASP.NET Core API and supported desktop,
tablet, and mobile browsers for the Angular application

**Project Type**: Full-stack web application in a Backend/Frontend monorepo

**Performance Goals**: No latency target or service-level objective is authorized. A mutation may
report success only after all reachable synchronization changes complete; graph/unit access must be
batched, affected ayahs only must be rebuilt, per-door/per-unit N+1 access is forbidden, and the
outer committed writer invalidates the tree once.

**Constraints**: No hard V1 cap on direct sources or graph depth; active graph must remain acyclic;
all propagation is transaction-bound and one-way; no background/eventual queue; no read-time union;
no source attribution in content contracts; public topology reads; independent inclusion-write
permissions; existing link mutations remain Owner-only; no Quran data changes; no horizontal
deployment change; migration generation and database application require separate authorizations.

**Scale/Scope**: One target may include any number of live sources in one atomic batch, sources may
be anywhere in the Abwab tree, and changes propagate through every reachable consumer in the active
DAG. Planning assumes no product maximum and therefore uses set-based traversal and deterministic
lock ordering rather than fixed-depth logic.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design.*

`.specify/memory/constitution.md` v1.0.0 is ratified and supplies the non-negotiable feature gates.
The repository router, native area routers, active specification, architecture authorities, and
Testing Constitution provide the more specific implementation rules.

| Gate | Pre-design result | Evidence |
| --- | --- | --- |
| I. Explicit scope and authorization | PASS | Work is on `feat/abwab-chapter-inclusion`, not `main`; this workflow changes planning/governance artifacts only and does not authorize implementation, migration, database update, Git delivery, review, or deployment. |
| II. Quran data integrity and provenance | PASS | No Quran table, source package, Quran text, identity, renderer, or content correction is changed. |
| III. Contract and layer ownership | PASS | Domain concepts stay in Domain; orchestration in Application; focused contracts in Application.Abstractions; EF and synchronization implementation in Infrastructure; controllers stay thin; public content contracts omit internal ownership. |
| IV. Atomic, authorized, controlled mutation | PASS | Inclusion/link propagation is transaction-bound; expected failures are controlled Application outcomes; public GET and permission-classified writes preserve existing authorization boundaries. |
| V. Testing Constitution | PASS | No new automated tests; only minimal updates to retained exact-contract protection and approved existing gates are planned. |
| Repository and generation constraints | PASS | The active feature is selected; EF/API/permission outputs use sanctioned generators under separate authorization; no generated file is hand-edited. |

No gate violation requires a complexity exception.

## Project Structure

### Documentation (this feature)

```text
specs/001-abwab-door-inclusions/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── checklists/
│   └── requirements.md
└── contracts/
    ├── http-api.md
    └── inclusion-management-ui.md
```

`tasks.md` is intentionally not created by this workflow.

### Source Code (repository root)

```text
Backend/
├── domain/QuranDashboard.Domain/
│   ├── Abwab/                         # inclusion edge, ledger, state enum
│   └── Linking/                       # internal contribution reference, enum, nullable operation owner
├── application/QuranDashboard.Application.Abstractions/
│   ├── Abwab/                         # topology DTOs, reader/writer/synchronizer contracts
│   ├── Abwab/Responses/               # tree and inclusion responses
│   └── Security/Permissions/          # inclusion codes and catalogue group
├── application/QuranDashboard.Application/Abwab/
│   ├── Queries/GetDoorInclusions/
│   └── Commands/{AddDoorInclusions,DeleteDoorInclusion}/
├── infrastructure/QuranDashboard.Infrastructure/
│   ├── Persistence/Configurations/{Abwab,Linking}/
│   ├── Persistence/Reads/Abwab/
│   ├── Persistence/Writes/Abwab/Inclusions/
│   ├── Persistence/Writes/Linking/    # existing writer integration
│   ├── Caching/{Abwab,Linking}/       # outer invalidation only
│   ├── DependencyInjection/
│   └── Migrations/                    # generated later, only when authorized
├── api/QuranDashboard.Api/
│   ├── Controllers/Abwab/AbwabDoorInclusionsController.cs
│   ├── Contracts/Abwab/
│   └── Common/ApiMessages.cs
├── scripts/{wipe-abwab,check-api-contract,check-pending-model}
└── tests/QuranDashboard.Tests/        # retained exact-contract updates only

Frontend/quran-dashboard-ui/
├── openapi/swagger.json               # generated
└── src/app/
    ├── core/api/generated/            # generated DTO models only
    ├── core/auth/permission-codes.generated.ts
    └── features/abwab/
        ├── components/
        │   ├── abwab-inclusions-modal/
        │   ├── abwab-door-picker/     # reused live multi-select tree/list
        │   ├── abwab-tree/            # target context-menu entry
        │   └── abwab-archive-view/    # read-only topology entry
        ├── data-access/abwab-inclusions.api.ts
        ├── state/abwab-inclusions.controller.ts
        ├── state/abwab-modal-url.controller.ts
        ├── state/abwab-permissions.controller.ts
        ├── state/abwab-tree.builder.ts
        └── pages/abwab-page/          # composition only
```

**Structure Decision**: Keep inclusion within the existing Abwab bounded context and existing
Linking storage model. The internal contribution adds `DoorInclusion` to the Domain enum and uses a
null `OperationId` guarded by kind/reference coherence; it never creates a synthetic
`LinkingOperation`. New Backend types follow current feature-first layer placement; the complex
synchronizer is split under an Abwab/Inclusions writer folder rather than expanding either existing
link writer. Internal contribution isolation and the reusable graph traversal are foundational and
must exist before the first edge can materialize. Frontend API calls, workflow state, and modal
rendering receive separate focused owners. `abwab-page.component.ts`, its template, and
`abwab-tree.component.ts` are already at their hard review boundaries, so context-menu/composition
responsibilities must be extracted before new wiring is added. `abwab-page-overlays.controller.ts`
is already above its soft threshold and must not absorb inclusion state.

## Phase 0: Research

Research decisions and rejected alternatives are recorded in [research.md](./research.md). All
technical-context unknowns are resolved and no planning placeholder remains.

## Phase 1: Design and Contracts

- [data-model.md](./data-model.md) defines the inclusion edge, per-occurrence ledger, internal
  contribution extension, source fingerprint, relationships, constraints, and state transitions.
- [contracts/http-api.md](./contracts/http-api.md) defines the public topology read,
  permission-classified atomic add/detach commands, tree count additions, controlled outcomes, and
  unchanged link-record contract.
- [contracts/inclusion-management-ui.md](./contracts/inclusion-management-ui.md) locks the
  target-first context-menu flow, live-tree multi-selection, archived/read-only behavior,
  permissions, URL restoration, async ownership, and accessibility.
- [quickstart.md](./quickstart.md) supplies the later verification order and manual runtime matrix
  without executing it.

## Implementation Strategy

### Phase 1 — Domain, persistence, and internal contribution ownership

Add `AbwabDoorInclusion`, `AbwabDoorInclusionUnitSync`, and
`AbwabDoorInclusionSyncState`; add the internal Domain source kind; extend
`LinkingSourceContribution` with the inclusion reference and nullable `OperationId`; configure exact
restrictive foreign keys, filtered uniqueness, operation/kind/reference coherence, state/target
coherence, `xmin`, and indexes. Public kinds keep their required operation owner; the internal kind
requires no operation owner. Add DbSets and audit the full `wipe-abwab` cascade closure. Do not
generate the migration until separately authorized.

**Gate**: The internal enum value has no public token or descriptor; public contributions still
require `OperationId`, internal contributions require it null, no synthetic operation is created,
suppressed mappings survive source edits, source deletion is blocked until ledger reconciliation,
and an authorized generated migration leaves no pending model changes.

### Phase 2 — Shared synchronization and public-isolation foundation

Create the scoped synchronizer, advisory lock, canonical snapshots/fingerprints, affected-ayah
rebuilder, and batched no-fixed-depth consumer traversal before any inclusion is created. Partition
internal contributions from authored-source projections: retain their units for internal door-state
impact while excluding their kind, identity, and label from tokens, descriptors, request parsers,
overlap labels, and public confirmed-state output.

**Gate**: Initial sync and later source propagation share one traversal implementation; internal
contributions cannot be submitted or emitted publicly; existing linking preflight/classification
continues to compute correct ayah/word impact without attempting to tokenize the internal kind.

### Phase 3 — Inclusion topology and initial synchronization

Add focused topology reader/writer abstractions and Get/Add/Delete use cases. The add writer
normalizes the full source batch, acquires the synchronization lock, locks doors by ascending ID,
validates the target version/lifecycle and every source, evaluates the proposed graph as a whole,
then creates all edges/internal contributions and clones through the shared traversal or creates
none. Detach owns edge-specific cleanup.

Same-occurrence edits must preserve `LinkingUnit.Id` or supply a deterministic bijection across all
affected logical occurrences whose ledger transfers happen before orphan deletion. A physical
one-to-many or many-to-one reshape may proceed only by preserving every logical occurrence identity;
a true split/merge may not infer lineage from content, be treated as delete/relink, or revive
suppressed content, and must fail closed before any source or target change commits.

**Gate**: missing, archived, self, duplicate, stale, and cyclic cases are controlled; no partial
batch is visible; initial and transitive propagation complete before success; archived sources remain
visible in topology; no source attribution enters public reads.

### Phase 4 — Existing link-writer integration

Insert the synchronization advisory lock after existing confirmation job/idempotency locks and
before any door/unit locks in both existing writer families. Integrate prepared/background
confirmation and direct selected-word replacement/bulk deletion. Source edits dispatch across every
mapping state: `Active` replaces and propagates the clone, while `Overridden` and `Suppressed` update
only the observed fingerprint/audit state. Local suppression and mixed deletion recursively remove
downstream clones before deleting the target unit. Expected conflicts and safe-completion failures
return controlled Application outcomes; `GlobalExceptionHandler` handles only unexpected faults.
Extract/reuse the existing relational door ayah/word rebuild algorithm instead of adding parallel
projection SQL.

**Gate**: every supported source add/edit/delete path reports precise occurrence changes; source
edits cannot overwrite overrides or recreate suppressions; target suppression propagates to every
consumer without touching the source; direct target behavior remains unchanged; one cache
invalidation occurs after commit.

### Phase 5 — Tree, versions, and cache

Add direct inclusion source/consumer counts to the tree DTO/reader, leave existing link and selected
word count meanings intact, advance every changed target door's `xmin`, and invalidate the shared
tree once from the outer committed writer.

**Gate**: counts refresh without restart, archive keeps synchronized counts, detach removes only
edge-owned records, and stale open link panels use existing recovery.

### Phase 6 — HTTP and generated contracts

Add the thin inclusions controller, request bodies, localized outcomes, permission codes, and sixth
permission group. Export Swagger and regenerate API models; regenerate permission constants. Update
only sanctioned generated outputs and retained exact catalogues/inventories.

**Gate**: GET is public; POST/DELETE each have one inclusion permission; link routes remain
Owner-only; contract drift and permission catalogue checks are clean.

### Phase 7 — Target-first inclusion management

First extract context-menu/page composition responsibilities needed to keep current hard-boundary
files focused. Add the wide inclusion modal, focused controller, and data-access service. Launch
only from one aggregate target's live-tree context menu (`right-click`, `ContextMenu`, or
`Shift+F10`); reuse the main snapshot's `liveRoots` in the existing multi-select door picker; pass
the target as excluded and current direct sources as disabled; never add source-first or
multi-target entry. Consumers are read-only; detach exists only on direct-source rows. Encode the
target in inclusion-modal URL state so archived topology can reopen read-only.

**Gate**: the full selected source set is one atomic POST; Backend cycle validation remains
authoritative; archived/target/existing-source choices are unavailable; loading, refreshing, empty,
error, and notice owners remain distinct; focus and announcements meet the UI contract.

### Phase 8 — Existing link UI compatibility

Do not add or redesign link-content components. Verify synchronized units flow through the current
snapshot/list, ayah, highlighting, edit, delete, bulk-delete, and copy surfaces with no origin or
state fields. Adjust only existing stale-version refresh orchestration if required.

**Gate**: generated link DTOs and rendered content reveal no source attribution; copy produces a
direct record; existing Owner-only behavior and visual presentation remain intact.

## Testing Decision

No new automated test is authorized. Do not add a Backend test class/method, Angular `*.spec.ts`,
or Playwright journey. Minimally update only retained protection whose exact owned subject changes:

- existing Abwab schema/reset assertions and fixture ownership;
- existing Abwab permission catalogue count/order/group assertions;
- existing smoke route catalogue, route parity, and data-driven Abwab write authorization rows;
- existing smoke reset ownership for inclusion/linking state; and
- the retained `abwab-permissions.e2e.ts` journey only if its existing hidden-control/anonymous-write
  subject must include the new action, without adding a journey.

No retained test owns the linking-contribution check constraints. Protect those through the
authorized generated migration, pending-model check, existing migration/gate-contract lanes, build,
and manual runtime verification unless the owner later authorizes one focused permanent-test
exception.

## Authorization Boundaries

- This plan does not authorize implementation.
- `Backend/scripts/add-mig AddAbwabDoorInclusionSynchronization` requires explicit migration
  generation authorization.
- `Backend/scripts/update-db` requires a separate database-update instruction.
- Wipe/reset commands require separate destructive-data authorization and are not part of the
  quickstart sequence.
- No stage, commit, push, PR, deployment, or production-state change is authorized by this plan.

## Post-Design Constitution Check

Phase 1 design remains within every v1.0.0 constitution gate. The contracts do not expose internal
synchronization metadata, the UI contract does not introduce an alternate content screen, internal
contributions cannot become public linking operations, the data model changes no Quran table, the
quickstart adds no unauthorized test or destructive operation, and all generated/migration work
remains behind later authorization. No complexity exception is required.
