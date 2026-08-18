# Phase 0 Research: Abwab Door Inclusions

**Date**: 2026-08-17
**Spec**: [spec.md](./spec.md)

No unresolved technical-context clarification remains. Product behavior comes from the active spec;
the pre-Spec plan supplies approved implementation detail where it does not conflict with the
target-first clarification.

## R1 — Inclusion representation

**Decision**: Store inclusion as a dedicated directed edge plus a per-source-occurrence sync ledger.
Do not extend `AbwabDoorRelation` or `AbwabRelationType`.

**Rationale**: Inclusion owns synchronization, lifecycle, concurrency, and target records; semantic
relations are descriptive metadata and have none of those responsibilities.

**Alternatives considered**: Semantic relation type (conflates domains); read-time union (breaks
existing link edit/delete/copy/count behavior); manual copy (not durable synchronization).

## R2 — Materialized target content

**Decision**: Materialize synchronized content as normal target `LinkingUnit` records owned by one
internal linking contribution per active inclusion. Add `DoorInclusion` to the persisted Domain
source-kind enum, make `LinkingSourceContribution.OperationId` nullable only for that internal kind,
and require internal contributions to have no `LinkingOperation` row.

**Rationale**: Existing link readers, grouped/independent rendering, ayah/word projections, counts,
editing, deletion, and copying already operate on live units and contribution mappings. An inclusion
is not a user linking confirmation, so a synthetic `LinkingOperation` would create false audit and
public-projection semantics; conditional nullability keeps every authored contribution unchanged.

**Alternatives considered**: A second public record model or effective-content endpoint (duplicates
the content stack and leaks origin); merging clones with direct/other-edge units (destroys ownership);
creating a synthetic `LinkingOperation` per inclusion (misrepresents the inclusion as authored link
confirmation and creates another filtering/lifecycle surface).

## R3 — Stable clone and occurrence identities

**Decision**: Use `LinkingUnit.Id` as source-occurrence identity. Salt each target clone identity by
inclusion ID and source unit ID; use a separate fingerprint for grouped flag, ordered ayahs,
selected words, and ordered descriptions.

**Rationale**: Target identities stay stable across content edits while never colliding with direct
or other-edge units. Fingerprints detect source-shape drift without redefining occurrence identity.

**Alternatives considered**: Content hash as occurrence identity (edits manufacture occurrences);
ayah ID alone (cannot distinguish record lifetimes); unsalted target identity (silent merges).

## R4 — Physical replacement reconciliation

**Decision**: Every writer reports added, edited, deleted, and identity-preserving replacement unit
IDs. Same-occurrence edits preserve the unit ID when possible; otherwise the complete replacement
set must form a deterministic bijection across logical occurrences and transfer each ledger before
the old rows are removed. A true split/merge that cannot preserve every occurrence is fail-closed:
the implementation may not infer lineage from content or treat an edit as delete/relink.

**Rationale**: This is the only design that prevents an internal writer optimization from cancelling
a target suppression or override. The current direct word replacement already preserves unit ID;
prepared confirmation is the path that needs explicit reconciliation before orphan cleanup.

**Alternatives considered**: Treat every replacement as a new occurrence (violates suppression);
match by ayah/content overlap (ambiguous); silently choose one successor (loses state).

## R5 — Synchronization state machine

**Decision**: Persist `Active`, `Overridden`, and `Suppressed`. `Active` has a target unit and follows
source edits; `Overridden` has a target unit but ignores source edits; `Suppressed` has no target
unit and ignores source edits. Source-occurrence deletion or inclusion detach retires the mapping.

**Rationale**: The ledger makes target-local intent durable while keeping source deletion and edge
lifetime authoritative.

**Alternatives considered**: Permanent ayah blacklist (incorrect across later occurrences);
converting overrides to direct records (loses source-owned lifetime); deleting ledger on suppression
(source edits would recreate content).

## R6 — Transaction and lock order

**Decision**: Perform topology and link propagation in the initiating transaction under one
transaction-scoped PostgreSQL advisory lock: existing job/idempotency locks, inclusion lock, doors
ascending, then units/mappings ascending.

**Rationale**: Both current writer families lock doors today. A shared earlier lock prevents
opposite-edge cycles, graph/link drift, and reverse-order deadlocks while preserving job serialization.

**Alternatives considered**: Background/eventual queue (accepts drift); door locks before graph lock
(deadlock/stale validation risk); per-edge locks (cannot protect whole-graph cycle checks).

## R7 — Synchronizer boundary

**Decision**: Add one focused `IAbwabDoorInclusionSynchronizer` contract and a scoped EF
implementation that shares the caller's DbContext/transaction, receives exact mutation sets, batches
consumer traversal, and reports changed targets. Build the no-fixed-depth traversal before initial
sync so inclusion creation and later source mutations use the same graph engine. Keep EF transaction
types out of the abstraction.

**Rationale**: Prepared confirmation and direct edit/delete are the two current writer families;
central orchestration prevents duplicated propagation SQL and preserves layer boundaries.

**Alternatives considered**: Independent synchronization in each writer (behavior drift); controller
or Application EF logic (layer violation); synchronizer-owned transaction (cannot roll back source).

## R8 — Door ayah/word projection

**Decision**: Extract/reuse the existing affected-ayah rebuild logic from confirmation and direct
door-link writers. Recompute distinct membership from every surviving direct and synchronized unit.

**Rationale**: Existing projections already encode the required union semantics. Rebuilding only
affected ayahs avoids parallel truth and unnecessary whole-door work.

**Alternatives considered**: Incremental reference counters (new consistency surface); a second
effective projection (changes metric meaning); per-unit queries (N+1).

## R9 — Topology, versions, and cache

**Decision**: Read direct sources and consumers from active edges, including archived participants;
add only source/consumer counts to tree DTOs. Advance every changed target door's `xmin` and let the
outer committed writer invalidate the Abwab tree once.

**Rationale**: Existing stale-version handling and process-local generation cache already own client
refresh. Synchronizer-level invalidation would occur before commit or repeat recursively.

**Alternatives considered**: New ETag/effective cache (explicitly out of scope); per-hop invalidation
(wasteful and unsafe); changing existing link/count meanings (contract break).

## R10 — HTTP and authorization

**Decision**: Use one public topology GET and permission-classified atomic POST/DELETE under the
target door, all in `ApiResponse<T>`. Add independent create/delete permission codes; preserve
`[RequireOwner]` on existing link mutations.

**Rationale**: Resource-oriented target routes match the locked target-first product model. Public
topology and classified unsafe routes follow current API/security policy.

**Alternatives considered**: Relation permissions (wrong capability); Owner-only inclusion writes
(ignores independent catalogue); source-first or multi-target routes (contradict the spec).

## R11 — Target-first Frontend composition

**Decision**: Launch `تضمين الأبواب` from the selected aggregate target's existing tree context menu
for right-click and keyboard context-menu paths. Reuse `abwab-door-picker` with `liveRoots`,
multi-select, target exclusion, and current-direct-source disabling. Use a new wide modal, focused
controller, and data-access service.

**Rationale**: The existing picker already provides the same live hierarchy, search, keyboard
checkboxes, excluded/disabled IDs, and multi-selection required by the spec. The management picker
is single-target and would create the wrong source-first behavior.

**Alternatives considered**: New picker (duplicates interaction); `abwab-management-picker`
(wrong semantics/session state); adding inclusion to relations modal (conflates capabilities).

## R12 — Frontend ownership and restoration

**Decision**: Keep request orchestration and the latest topology `doorVersion` in
`abwab-inclusions.controller.ts`; encode the modal target in URL state (for example,
`modal=inclusions-<doorId>`); add archived-door read-only topology entry; use existing modal shell,
confirm dialog, announcer, and five separate async owners.

**Rationale**: Archived navigation clears the normal selected-door query state, and existing page/
tree/overlay files are already at or above review thresholds. Focused ownership prevents further
growth and restores the exact target safely.

**Alternatives considered**: Tree-node version for writes (may be stale); overlay controller
expansion (already oversized); local-only modal state (breaks refresh/restore); mutation controls on
archived targets (forbidden).

## R13 — Existing link UI boundary

**Decision**: Do not add link components, tabs, origin badges, or DTO fields. Before the first
inclusion can materialize, partition internal contributions from authored-source projections:
confirmed-state computation may retain their ayah/word impact, while source tokens, descriptors,
overlapping-source labels, request parsers, and public projections must exclude the internal kind.
Synchronized units continue through current link snapshot/list/editor/delete/copy state and UI.

**Rationale**: The Backend can dispatch direct versus synchronized mutation semantics internally;
the visible record shape and user tools do not need to know origin.

**Alternatives considered**: Frontend sync-state branching (leaks internal model); a new effective
content mode (explicit non-goal); disabling edits/deletes for synchronized records (violates spec).

Expected synchronization conflicts and safe-completion failures remain controlled Application
outcomes. `GlobalExceptionHandler` handles only unexpected faults, emits at most one safe diagnostic
at the owning boundary, and never becomes an alternate domain-outcome mapper.

## R14 — Generated contracts and permissions

**Decision**: Export Swagger and regenerate retained frontend models through
`Backend/scripts/check-api-contract`; generate permission constants with
`npm run generate:permission-codes`. Never hand-edit generated outputs.

**Rationale**: These are the repository's sanctioned sources of client/permission truth and their
drift checks expose contract mismatch.

**Alternatives considered**: Hand-authored frontend DTOs/constants (drift); committing generated
service functions (the pipeline intentionally prunes them).

## R15 — Migration and reset ownership

**Decision**: Add the EF model first; generate `AddAbwabDoorInclusionSynchronization` only after
explicit authorization, and apply it only under a separate database-update instruction. Audit the
complete `wipe-abwab` cascade closure and existing Abwab schema/smoke fixture ownership.

**Rationale**: New cross-feature foreign keys make the current six-table wipe allowlist unsafe to
change by count alone. Existing rows need only a nullable contribution reference; no data backfill.

**Alternatives considered**: Hand-written migration/snapshot (forbidden); automatic database update
(unauthorized); simple six-to-eight table list expansion (does not prove cascade safety).

## R16 — Performance and observability

**Decision**: Add no latency SLA, product cap, logging vendor, tracing stack, or deployment change.
Use set-based reads/writes, deterministic traversal, affected-ayah rebuilds, and one outer cache
invalidation. Unexpected failures use existing centralized exception handling; any targeted feature
log contains safe IDs/counts/state only and is emitted once at the owning boundary.

**Rationale**: The spec explicitly defers timing targets and forbids hard caps while requiring
atomic correctness. Existing single-instance deployment and logging policy remain sufficient scope.

**Alternatives considered**: Invented three-second target (removed from spec); source/depth cap
(clarification rejected); new telemetry stack or horizontal topology (out of scope).

## R17 — Testing Decision

**Decision**: Add no automated test. Minimally update only existing exact-contract schema,
permission catalogue, smoke route/parity/authorization, reset ownership, and—only if its protected
subject changes—the retained Abwab permission journey. Use existing gates plus manual/runtime proof.

**Rationale**: The repository Test Freeze permits retained protection updates but requires explicit
owner approval for any new Backend test method/class or Playwright journey; Angular unit specs are
prohibited.

**Alternatives considered**: New unit/integration/E2E coverage (not authorized); omitting changed
retained catalogues (would leave exact-contract protection stale).
