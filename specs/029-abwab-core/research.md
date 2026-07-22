# Research: Abwab Core — Sections, Categories, Tree, and Protection

**Feature**: `029-abwab-core` | **Date**: 2026-07-23 | **Source**: Master Plan §18.3

The spec is clarification-free (0 `[NEEDS CLARIFICATION]` markers). This document records the
technical decisions that realize §18.3 against the existing stack and the accepted `028`
substrate. Each decision is constrained by §18.3 and the repository; none introduces new product
scope. The stages follow §18.3's **mandatory internal order**: schema/read → protection → writers
→ frontend slice.

## Stage 1 — Schema and read-only tree

- **Decision**: One EF Core migration creates **Section**, **Category**, **CategorySearchAlias**,
  and the revision plumbing, and **seeds exactly one permanent default section** (`أبواب غير
  مصنفة`, `IsPermanentDefault`). Encode root vs descendant shape and the order/ancestry columns
  per §7.1 (`SectionOrder`/`GlobalOrder` for roots; `SiblingOrder`/`AncestorIds`/`Depth` for
  descendants). Enforce uniqueness with **filtered unique indexes over normalized values**: active
  root normalized names unique **globally across sections**, active sibling normalized names
  unique per parent, active section normalized names unique, and active normalized aliases unique
  **per category**. Ship **read/search/snapshot only** (the versioned `AbwabTreeSnapshot`, the
  `كل الأبواب` projection, category search over normalized name + aliases) and the versioned
  **Section/Category/Order** restore snapshots. **No** mutation endpoint or editable UI at this
  checkpoint.
- **Rationale**: Protection attaches to categories and writers mutate them, so a correct,
  uniqueness-constrained, restorable read model must exist first (§18.3 step 1). Normalized-value
  DB constraints are "the final race-safe guard" per §5.1; the domain writes normalized values via
  the one §5.1 algorithm and publishes the shared fixture corpus so backend/db/API/frontend agree.
- **Alternatives considered**: Application-only uniqueness (rejected — races; §5.1 requires the DB
  constraint as final guard); a paged hierarchy read (rejected — §11 mandates a versioned complete
  snapshot); enabling a "harmless" read-only edit stub early (rejected — §18.3 forbids any mutation
  surface at this checkpoint); computing the `كل الأبواب` set as a stored row (rejected — it is a
  projection over independent root orders, not a persisted section).

## Stage 2 — Protection storage and resolver

- **Decision**: Add the **ManualProtection** table (`CategoryId`, typed `ProtectionType` ∈
  {`CategoryData`, `InternalStructure`, `QuranContent`, `Deletion`, `Relationship`}, typed
  `ProtectionScope` ∈ {`CategoryOnly`, `Subtree`}, applied/lifted actor+timestamps, active/
  soft-delete, `Version`) with a **filtered unique index of one active record per
  `(CategoryId, ProtectionType)`**, plus the ordinary-protection actor/time fields on Category.
  Implement the **direct/inherited resolver**: inheritance evaluated from **current `AncestorIds`**
  (no descendant snapshot), returning type/scope, the resolving **source ancestor**, and
  **server-clock-derived expiry** via server-clock DTOs, with action classification. Measure a
  **deep-tree resolution query budget** against real PostgreSQL. **Accept the ManualProtection
  adapter before any protected category writer exists.**
- **Rationale**: Writers must consult resolved protection to authorize an action, so storage +
  resolver + accepted adapter must precede the writers (§18.3 step 2). Evaluating inheritance from
  live `AncestorIds` keeps protection correct across moves without a stored descendant snapshot
  (§7.2). Effective reads/lifts address soft-deleted categories by **immutable `CategoryId`** so a
  deletion cannot hide or strand a protection.
- **Alternatives considered**: Storing a materialized descendant protection snapshot (rejected —
  §7.2 evaluates from current `AncestorIds`); one row per (category, type, scope) (rejected — the
  filtered unique index permits exactly one active record per `(CategoryId, type)`, scope is a
  column on it); client-derived expiry (rejected — server clock is authoritative); deferring
  adapter acceptance until writers exist (rejected — §18.3 mandates acceptance first).

## Stage 3 — Activate tracked writers

- **Decision**: Implement the explicit §9 section/category actions on **one audited ChangeSet UoW**
  (the `028` kernel), each carrying `ExpectedTimelineGeneration`, expected `xmin`, and expected
  `TreeRevision`. Behaviors: destination-name **revalidation under the transaction** on
  create/move/restore using the §5.1 rule; **tracked atomic order changes** with root defaulting
  (a root created/promoted without an explicit `SectionId` lands in the permanent default section
  and appends both root orders), **global-order preservation on section move**, independent
  `SectionOrder`/`GlobalOrder`, explicit child `SiblingOrder`, and **one `TreeRevision` bump** per
  atomic reorder; **ancestry maintenance** rewriting `AncestorIds`/`Depth` for every descendant;
  **single/bulk move cycle guards** rejecting self/descendant/overlapping selections; **atomic
  subtree soft-delete/operation-restore** locking every affected row in deterministic ID order,
  recording one `DeletionOperationId`, checking `Deletion` on every affected category and
  `InternalStructure` on the surviving parent; **dormant-dependent filtering** via a generic
  RESTRICT/no-cascade + dependent-visibility **core fixture** (no relationship/link schema);
  protection resolution + the ordinary 24-hour window (gating only direct-content edits/moves);
  and **safe 409s** mapped to the exact §11 `abwab.*` codes. Aliases: add/edit/remove is
  **category direct-content mutation under `category.edit`** (tracked soft delete; physical delete
  rejected). Full-preset: one selected scope idempotently upserts all five typed records, requiring
  Expected Version for each changed scope and rolling back all five on any stale/constraint/
  protection failure. **No drag-and-drop.**
- **Rationale**: These are the domain's core mutations and depend on the read model (Stage 1) and
  the accepted resolver/adapter (Stage 2). Revalidating under the transaction and mapping named DB
  constraints to exact codes is the only race-safe path (§7.1, §7.2, §11); the five-type preset's
  all-or-nothing rollback and idempotent-no-ChangeSet no-op are stated verbatim in §7.2.
- **Alternatives considered**: Drag-and-drop reordering (rejected — §3.2/§18.3 forbid it; explicit
  actions only); per-order coupling of `SectionOrder` and `GlobalOrder` (rejected — §7.1 keeps them
  independent); cascade-deleting dependents (rejected — §7.1 makes them dormant, restored with the
  category); persisting "Full protection" as a sixth type (rejected — §7.2 upserts the five, never a
  sixth); a soft no-op that still writes an audit event on same-scope apply (rejected — §7.2
  requires no ChangeSet).

## Stage 4 — Domain frontend vertical slice

- **Decision**: Own the **core port + core mock**, the backend contract, the **HTTP mapping**, the
  **parity suite** (core mock ≡ real HTTP), the **tree/search/editor/protection UI**, and **core
  cache rules** on the reused `028` §14.1 primitives (cache/store/action/conflict, IndexedDB).
  Category editors **reuse the `028` `@angular/forms` Reactive Forms package** (installed and tested
  by the permission-administration form). Enforce **composite-read** UI action-visibility that
  mirrors the backend (`category.view` + `section.view` for tree/search; `protection.view` for full
  manual metadata) with **no partial leak** — redaction remains a **backend DTO projection**.
  Publish the **§6.3 audit render payloads**: complete category (create/edit), one-ChangeSet bulk
  move, one-ChangeSet subtree delete/operation-restore, and manual-protection — **ordering data
  folded into** the bulk-move and category-edit payloads (§6.3 defines no standalone reorder render).
  Run the
  browser/source suite: mock/HTTP parity, stale-cache, rollback, RTL keyboard/focus, large-tree,
  explicit action, **no-edit-session-lock**, **no-drag**, post-mutation context preservation.
- **Rationale**: The frontend proves the domain end-to-end and is last in §18.3's order; parity +
  backend-authoritative redaction guarantee the mock cannot drift from HTTP and the UI cannot leak
  protected metadata (§11, §14). Reusing the `028` Forms package avoids a second forms substrate.
- **Alternatives considered**: Frontend-enforced permission redaction (rejected — §11 requires
  backend DTO projection; frontend hiding is non-authoritative); an edit-session lock to serialize
  editors (rejected — §18.3 requires **no-edit-session-lock**; concurrency is handled by expected
  revisions and 409s); installing a fresh forms package (rejected — reuse the accepted `028` one);
  a bespoke cache instead of the §14.1 primitives (rejected — the shared substrate is reused).

## Cross-cutting

- **Real infrastructure where it matters**: uniqueness/normalization, tree shape/order/ancestry,
  the deep-tree protection budget, writer concurrency (move-vs-reorder, five-type stale rollback),
  subtree delete/restore atomicity, versioned adapter round-trips, and composite-read redaction are
  proven against **real PostgreSQL** (Testcontainers); UI parity/large-tree/no-drag/RTL are proven
  in a **real browser** (Playwright).
- **Order dependency**: Stage N+1 begins only after Stage N's §18.3 exit gate passes — no mutation
  surface before the read model, no protected writer before the accepted ManualProtection adapter,
  no frontend slice before the writers.
- **Adapters accepted for `033`** (§8 governs — keyed by persisted type, duplicate registrations
  fail CI): exactly **three** adapters — Section, Category (incl. all three orders + subtree
  delete/operation-restore), ManualProtection — versioned and round-trip tested so `033`'s restore
  read model/planner/execution can consume them without change. **Order is a tested facet inside
  Category/Section, not a fourth registration**; the §18.3-exit "Section/Category/Order/ManualProtection"
  wording names the round-trips that must pass, not four registrations.
- **No forward dependency**: relationship/link dormant integration is proven only through a generic
  dependent-visibility **core fixture**; real integration belongs to `030`/`031`.
