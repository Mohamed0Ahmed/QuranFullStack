# Feature Specification: Abwab Relationships and Templates — Category Adjuncts

**Feature Branch**: `030-abwab-relationships-templates`

**Created**: 2026-07-25

**Status**: Draft

**Input**: User description: "Create the Spec Kit for 030-abwab-relationships-templates using ONLY section §18.4 of docs/feature-abwab-management/MASTER_PLAN.md (category relationships + door templates). Generation only — preserve its exact scope, entry/exit gates, mandatory internal order, and acceptance criteria; introduce no new decisions; include nothing owned by 027–029 or 031–034; and do not implement code."

> **Canonical source**: `docs/feature-abwab-management/MASTER_PLAN.md` is the sole
> canonical product and architecture source for Abwab Spec Kits `027`–`034`. This
> specification is derived **only from Master Plan §18.4**
> (`030-abwab-relationships-templates` — category adjuncts) plus its entry/exit gates and
> the §17 portfolio exit condition for `030`. It introduces no new product or architecture
> decision and reinterprets nothing. Section references (e.g. §5.2, §6.3, §7.3, §7.4, §8,
> §9, §11, §14.1) point to the Master Plan and are recorded here as pointers only; their
> content is owned by those sections, not re-decided here. Where a conflict is perceived,
> the Master Plan governs and a genuine change returns to an independent amendment/
> re-review of that document, never a local decision here.

## Overview *(context)*

`030-abwab-relationships-templates` is the fourth of eight top-level Abwab Spec Kits and
the first **category-adjunct** domain. Its single purpose is to build the two adjuncts that
hang off the accepted `029` category tree — **category relationships** and **door
templates** — as complete domain vertical slices whose reversible adapters are accepted
(§17, §18.4).

**Entry condition (§18.4):** `028-abwab-safety-foundations` **and** `029-abwab-core` are
accepted. Every writer in this feature is built on the `028` audit / write-gate /
concurrency / stabilization substrate and on the `029` category identity, protection
resolver, and Category adapter.

**Mandatory internal order (§18.4), stated exactly:** the Relationship and Template
workstreams **may proceed in parallel** inside this Spec Kit, but **each must finish its
own adapter and vertical slice before the Spec Kit exits**. §18.4 fixes **no** step
ordering between or inside the two workstreams beyond that rule; none is invented here.

**Scope boundaries carried from §18.4:**

- This feature builds **category relationships** and **door templates** only. It does not
  build the audit/write/concurrency/time kernel, CI gates, identity/security, notification
  storage, or the shared frontend foundation (`028`); sections, categories, tree, ordinary/
  manual protection, or the category writer itself (`029`); Quran-link aggregates, sources,
  or link-check (`031`); personal workspace, requests, review, or notification surfaces
  (`032`); the audit-restore read model, preview, planner, or restore execution (`033`); or
  realtime hints, reconciliation, and release proof (`034`).
- Template application **writes real categories only through the accepted `029` category
  writer**; this feature adds no second category writer and no second category adapter.
- The versioned **application-event interpreter** delegates real-category inversion to the
  **single `029` Category adapter**. It is **not** another inverse adapter and must not
  produce a duplicate registry entry (§8).
- Templates copy **structure and basic data only**. There is no create-from-real-door path,
  no cross-door copy, and no copying of links, highlights, notes, requests, sources,
  workflow, audit, or technical state.
- Relationship mutations are outside the ordinary 24-hour protection layer: they neither
  start nor are blocked by it (§18.4, §9, §2.1).

## User Scenarios & Testing *(mandatory)*

<!--
  The two user stories below are exactly the two workstreams of §18.4. §18.4 permits them
  to run in parallel, so both carry the same priority; neither is a prerequisite of the
  other. Each is independently testable at its own §18.4 acceptance bullets, and the Spec
  Kit exits only when BOTH have finished their own adapter and vertical slice.
-->

### User Story 1 - Category relationships workstream (Priority: P1)

A dashboard administrator relates two existing categories — as a mutual `Similar` /
`Opposite` pair or as a directional `Broader` / `Narrower` edge — and can edit, delete, and
restore those relationships, while the system refuses self-links, duplicates, cycles, and
mutations touching any protected endpoint, and keeps relationship rows intact and dormant
when a category subtree is deleted.

**Why this priority**: §18.4 permits the relationship and template workstreams to run in
parallel; neither blocks the other. This story is the complete relationship vertical slice
and its versioned inverse adapter, which `033` consumes directly (`030→033`, §8).

**Independent Test**: Run the relationship slice alone against real PostgreSQL and a real
browser with the accepted `029` tree in place; the story is valid when canonical mutual/
directional shapes and their checks/indexes hold, Broader/Narrower cycle validation runs
under the transaction while an explicit direct A→C is still allowed, soft-delete/restore is
tracked, either-endpoint `Relationship` protection blocks the whole mutation, every named
negative/race case fails correctly, category subtree deletion leaves relationship rows
dormant with no cascade or history loss, and the versioned Relationship inverse adapter
round-trips.

**Acceptance Scenarios**:

1. **Given** two active categories, **When** a mutual `Similar` or `Opposite` relationship
   is added, **Then** it is stored in the canonical mutual shape with the directional
   columns null and the canonical lower/higher ordering enforced (§7.3).
2. **Given** two active categories, **When** a `BroaderNarrower` relationship is added,
   **Then** it is stored in the canonical directional shape with the mutual columns null,
   and the inverse label is derived for display only (§7.3).
3. **Given** an existing chain A→B→C, **When** an explicit direct A→C `BroaderNarrower`
   edge is added, **Then** it is **allowed**; **When** an edge would close a cycle, **Then**
   the write is rejected under the transaction (§11 owns the exact code
   `abwab.relationship_cycle`).
4. **Given** an active relationship, **When** it is deleted and later restored, **Then**
   both operations are **tracked soft delete / restore** with no physical delete and no
   history loss.
5. **Given** applicable direct or inherited `Relationship` manual protection on **any**
   target — the union of current and proposed endpoints for edit, the stored endpoints for
   delete/restore, and the proposed endpoints for add — **When** the mutation is attempted,
   **Then** the **entire** mutation is blocked (§7.3, §9; §11 owns `abwab.manual_protection`).
6. **Given** an edit that would replace a **protected old endpoint** with an unprotected
   new one, **When** it is attempted, **Then** it is blocked — protection cannot be escaped
   by replacing the protected endpoint.
7. **Given** a relationship mutation of any kind, **When** it succeeds, **Then** it neither
   starts nor is blocked by the ordinary 24-hour category window (§9, §2.1).
8. **Given** a category subtree is soft-deleted, **When** its relationship rows are
   inspected, **Then** they remain **intact and dormant** with no cascade and no history
   loss; **When** the category operation is restored, **Then** the same rows become visible
   again and stored-endpoint protection is enforced on that path.
9. **Given** the finished slice, **When** the registry is checked, **Then** the versioned
   **Relationship** inverse adapter round-trips and is registered exactly once (§8).

---

### User Story 2 - Door templates workstream (Priority: P1)

A dashboard administrator builds a door template by hand in a dedicated template editor —
creating the aggregate, its nodes, aliases, and order — and applies one template to one
real category, producing independent real categories that carry only structure and basic
data, with the template's own history kept in its separate view.

**Why this priority**: §18.4 permits the relationship and template workstreams to run in
parallel; neither blocks the other. This story is the complete template vertical slice, the
single DoorTemplate aggregate inverse adapter, and the application-event interpreter that
`033` consumes directly (`030→033`, §8).

**Independent Test**: Run the template slice alone against real PostgreSQL, the API, and a
real browser with the accepted `029` category writer in place; the story is valid when
manual editor CRUD over nodes/aliases/order works, one-target application goes through the
`029` category writer as one ChangeSet with one `TreeRevision` bump, the one versioned
Template aggregate adapter round-trips, the application-event interpreter reuses the single
`029` Category adapter with **no** duplicate registration, every negative-copy and
cycle/stale case fails correctly, alias remove/restore is tracked soft delete, and the
frozen permission ownership holds at the handler.

**Acceptance Scenarios**:

1. **Given** the template editor, **When** an administrator creates a template and its
   nodes, aliases, and order, **Then** all of it is created **manually in the editor
   only** — there is no endpoint, command, UI action, or backend service that reads real
   categories into a template (§7.4).
2. **Given** one template and one target real category, **When** the template is applied,
   **Then** every template root is created as a **direct child** of the target, uniqueness
   and protection are revalidated **under the transaction**, and `TreeRevision` is
   incremented **once** for the whole application (one ChangeSet).
3. **Given** an application, **When** the created tree is inspected, **Then** it contains
   only the copied name, representative excerpt, description, aliases, order, and
   structure, and **no** link, highlight, note, request, source, workflow, audit, or
   technical state was copied; the created categories are independent real categories.
4. **Given** an attempt to create a template from a real door or to copy across doors,
   **When** it is made, **Then** **no such path exists** and the attempt fails (negative
   tests).
5. **Given** a template node reparent, **When** the destination is the node itself or lies
   inside the moved node's descendant tree, **Then** it is rejected; **When** the reparent
   is valid, **Then** sibling order is updated **atomically**, `TemplateRevision` bumps
   **once**, and the change round-trips through the **one** Template adapter.
6. **Given** stale or concurrent reparent/reorder commands, **When** they are submitted,
   **Then** they are rejected (§11 owns `abwab.template_revision_stale` / `abwab.row_stale`);
   **Given** a restore that would produce a cyclic template, **When** it is attempted,
   **Then** it is rejected (§11 owns `abwab.template_cycle`).
7. **Given** a `TemplateNodeSearchAlias`, **When** it is removed and later restored,
   **Then** both use **tracked soft delete**, physical delete is rejected, and adapter tests
   prove no alias history is lost.
8. **Given** partial permission grants, **When** commands are issued, **Then** the handler
   enforces the frozen ownership exactly: `template.add` creates **only** the aggregate;
   **every** node/alias add/edit/reparent/reorder/internal remove requires `template.edit`;
   aggregate lifecycle delete/restore uses **only** `template.delete` / `template.restore`;
   and real-category application uses **only** `template.apply`. No verb is borrowed and no
   grant relies on frontend hiding (§5.2).
9. **Given** an applied template, **When** the audit is rendered, **Then** the frozen
   template snapshot at application time is stored and rendered, and template CRUD appears
   in its **separate template-history view**, not as a main product-audit row (§6.3).
10. **Given** the finished slice, **When** the registry is checked, **Then** the **one**
    DoorTemplate aggregate inverse adapter round-trips, and the versioned application-event
    interpreter delegates real-category inversion to the **single `029` Category adapter**
    with **no** duplicate registry entry (§8).

---

### Edge Cases

- A relationship whose two endpoints are the **same category** (self-link) is rejected by
  the canonical shape rules (§7.3); §18.4 requires this to be proven, and §11 owns whatever
  exact response class applies — no new code is defined here.
- A **reverse duplicate** of an existing mutual pair (same pair, endpoints swapped) and a
  **direct duplicate** of an existing active directional edge both fail (§11 owns
  `abwab.relationship_duplicate`).
- Two concurrent Broader/Narrower writes that individually pass validation but together
  close a cycle — the **race-created cycle** — must still fail under the transaction.
- An edit that keeps one endpoint and swaps the other, where the **removed** endpoint is
  protected and the **new** one is not, is blocked; the protected-target union is current
  **plus** proposed for edit (§7.3).
- A relationship row that has become **stale** (expected row version no longer matches) is
  rejected rather than merged (§11 owns `abwab.row_stale`).
- Restoring a soft-deleted relationship whose canonical pair/edge is **active again** is a
  restore collision and must fail rather than create a duplicate active row.
- A category subtree deletion followed by category operation restore must leave relationship
  rows **dormant then visible again** — no cascade delete, no history loss, and
  stored-endpoint protection still enforced.
- Template application into a destination where a produced root name **collides** with an
  existing sibling/root name is rejected under the transaction (§11 owns
  `abwab.category_name_conflict`); application into a target carrying applicable manual
  protection is blocked (§9 target `InternalStructure`; §11 owns `abwab.manual_protection`).
- A template node reparent onto **itself** or into its **own descendant tree** is rejected;
  no cyclic template can be saved, applied, rendered, or restored (§7.4).
- Physical deletion of a `TemplateNodeSearchAlias` is rejected; only tracked soft delete /
  restore is permitted.
- A user holding only `template.add` cannot add nodes or aliases; a user holding only
  `template.edit` cannot delete/restore the aggregate or apply it to a real category.

## Requirements *(mandatory)*

### Functional Requirements

**Relationship workstream (Story 1)**

- **FR-001**: The system MUST implement the canonical **mutual** (`Similar` / `Opposite`)
  and **directional** (`BroaderNarrower`) relationship shapes together with their CHECK
  constraints and filtered unique indexes as defined in §7.3.
- **FR-002**: The system MUST perform **cycle-safe** Broader/Narrower validation **under the
  transaction**, while explicitly allowing a direct A→C edge even when A→B→C already exists.
- **FR-003**: Relationship delete and restore MUST be **tracked soft delete / restore**;
  physical delete MUST be rejected.
- **FR-004**: The system MUST enforce **either-endpoint `Relationship` manual protection**:
  the protected-target set is the **union of current and proposed endpoints** for edit, the
  **stored endpoints** for delete/restore, and the **proposed endpoints** for add;
  applicable direct or inherited protection on **any** target blocks the **entire** mutation
  (§7.3, §9).
- **FR-005**: The system MUST own the relationship **port, mock adapter, backend/HTTP
  mapping, UI/actions, cache keys, parity tests, specialized relationship audit payload, and
  versioned inverse adapter** (§14.1).
- **FR-006**: The system MUST prove, with tests, the following failures: **self**,
  **reverse-duplicate**, **direct-duplicate**, **race-created cycle**, **protected
  endpoint**, **protected-old-to-unprotected-new edit**, **stale row**, and **restore
  collision**.
- **FR-007**: Relationship mutations MUST **neither start nor be blocked by** the ordinary
  24-hour category protection window, and this MUST be proven (§9, §2.1).
- **FR-008**: Category subtree deletion MUST leave **real Relationship rows intact and
  dormant**, and category **operation restore** MUST make them visible again;
  real-PostgreSQL tests MUST prove **no cascade and no history loss** and MUST enforce
  **stored-endpoint protection**. The relationship **dormant attached-state counts** MUST be
  supplied through a `030`-owned **read projection** (dormant relationship counts for the affected
  category set) consumed by the generic `dormantDependentCounts` seam of the `029` subtree
  delete/operation-restore **render payload**, where attached state is labelled **dormant** rather
  than falsely shown as deleted (§6.3). The `029` subtree handler and its stored event payload are
  **not modified**; this is a data contribution to a `029`-owned render surface, not new `029`
  ownership.

**Template workstream (Story 2)**

- **FR-009**: The system MUST implement **manual template-editor CRUD** over the DoorTemplate
  aggregate, its **nodes**, **aliases**, and **order**.
- **FR-010**: The system MUST implement **one-target application** of one template to one
  real category **through the `029` category writer**; no second category writer is added
  here.
- **FR-011**: The system MUST provide **one** versioned **DoorTemplate aggregate** inverse
  adapter covering DoorTemplate, TemplateNode, and TemplateNodeSearchAlias (§8).
- **FR-012**: The system MUST provide a **versioned application-event interpreter** that
  delegates real-category inversion to the **single `029` Category adapter** **without
  duplicate registration**; the interpreter is explicitly **not** another inverse adapter
  (§8).
- **FR-013**: The system MUST own the template **ports, mocks, backend/HTTP mappings,
  editor/application UI, cache rules, parity tests, frozen template/history audit payloads**,
  and the one DoorTemplate aggregate inverse adapter (§6.3, §14.1).
- **FR-014**: Negative tests MUST prove there is **no create-from-real-door path** and **no
  cross-door-copy path**, and that **no** link, highlight, note, request, source, workflow,
  audit, or technical state is copied by application (§7.4).
- **FR-015**: Application MUST create every template root as a **direct child** of the target
  category, **revalidate uniqueness and protection under the transaction**, and increment
  `TreeRevision` **once** (one ChangeSet, §7.4).
- **FR-016**: Real-PostgreSQL/API tests MUST reject **template self/descendant reparent**,
  **stale/concurrent reparent/reorder**, and **cyclic restore**; a **valid reparent** MUST
  update sibling order **atomically**, bump `TemplateRevision` **once**, and round-trip
  through the **one** Template adapter.
- **FR-017**: `TemplateNodeSearchAlias` remove/restore MUST use **tracked soft delete**;
  physical-delete tests and adapter tests MUST prohibit losing alias history.
- **FR-018**: Handler, source, and parity tests MUST **freeze aggregate permission
  ownership**: `template.add` creates **only** the aggregate; **every** node/alias
  add/edit/reparent/reorder/internal remove requires `template.edit`; lifecycle
  delete/restore use **only** `template.delete` / `template.restore`; real-category
  application uses **only** `template.apply`. Partial grants MUST NOT borrow another verb or
  rely on frontend hiding (§5.2).

**Exit / acceptance (both workstreams)**

- **FR-019**: The **Relationship** adapter, the **DoorTemplate aggregate** adapter, the
  **application event interpreter**, and its **verified reuse** of the `029` Category adapter
  MUST all be accepted **with no duplicate registry entry**; a duplicate or missing
  registration MUST fail CI (§8).
- **FR-020**: **No** relationship or template writer may bypass the **audit / protection /
  concurrency / stabilization** foundation established by `028`–`029`.
- **FR-021**: Both workstreams MAY be developed **in parallel**, but the Spec Kit MUST NOT
  exit until **each** workstream has finished **its own adapter and its own vertical slice**.
- **FR-022**: Template editor ordering and reparent actions MUST be **explicit actions/forms**;
  drag-and-drop remains absent everywhere in the application under the existing global
  invariant and CI source gate (§14.2, §15.2) — this feature introduces no exception.

### Key Entities *(include if feature involves data)*

- **CategoryRelationship**: One typed row that is either a **mutual** pair (`Similar` /
  `Opposite`, non-null lower/higher category IDs in canonical order, directional columns
  null) or a **directional** edge (`BroaderNarrower`, non-null source/target, mutual columns
  null); carries its identity, relationship type, soft-delete metadata, and version (§7.3).
- **`Relationship` manual-protection type**: One of the five manual protection types; for
  relationships the protected targets are the current **plus** proposed endpoints on edit,
  the stored endpoints on delete/restore, and the proposed endpoints on add (§6.6, §7.3).
- **DoorTemplate**: The template aggregate root holding identity, name/normalized name,
  optional description, `TemplateRevision`, soft-delete metadata, and version (§7.4).
- **TemplateNode**: A node inside one template holding template ownership, parent node,
  name/normalized name, optional plain-string representative excerpt, optional description,
  explicit sibling order, soft-delete metadata, and version (§7.4).
- **TemplateNodeSearchAlias**: A template-node alias mirroring the category alias
  value/normalization/soft-delete contract; remove/restore is tracked soft delete (§7.4).
- **`TemplateRevision`**: The template aggregate's logical counter, bumped **exactly once**
  per grouped template operation and carried as an expected value on node commands (§6.4).
- **Template application event**: The audited application of one template to one real
  category — one ChangeSet, one `TreeRevision` bump — storing and rendering the **frozen
  template snapshot** at application time (§6.3, §7.4).
- **Versioned inverse adapters owned here**: the **Relationship** adapter and the **one**
  DoorTemplate aggregate adapter, both reversible product state consumed directly by `033`
  (§8).
- **Application-event interpreter**: A versioned interpreter that maps template-application
  events to real-category inversion performed by the **single `029` Category adapter**; it
  is **not** a second adapter and adds no registry entry (§8).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: **100%** of stored relationships satisfy exactly one canonical shape — mutual
  with canonical lower/higher ordering and null directional columns, or directional with
  null mutual columns — and **0** self-links are storable, verified by real-PostgreSQL CHECK
  and index tests.
- **SC-002**: Duplicate active mutual pairs (including the **reverse** ordering) and
  duplicate active directional edges are rejected in **100%** of cases; a legitimate direct
  A→C edge alongside an existing A→B→C chain succeeds in **100%** of cases.
- **SC-003**: Broader/Narrower cycle validation runs **under the transaction** and rejects
  **100%** of cycle-closing writes, including **concurrent race-created** cycles, with **0**
  cycles persisted.
- **SC-004**: Relationship delete/restore is tracked soft delete with **0** physical deletes
  succeeding and **0** history rows lost; restore collisions against a now-active canonical
  pair/edge fail in **100%** of cases.
- **SC-005**: Either-endpoint `Relationship` protection blocks the **entire** mutation in
  **100%** of applicable direct/inherited cases across add (proposed endpoints), edit
  (current **∪** proposed), and delete/restore (stored endpoints), including the
  protected-old-to-unprotected-new edit.
- **SC-006**: Relationship mutations start the ordinary 24-hour window **0** times and are
  blocked by it **0** times, proven by real-PostgreSQL/API tests.
- **SC-007**: After a category subtree deletion, **100%** of that subtree's relationship rows
  remain present and dormant with **0** cascade deletions and **0** lost history; after
  category operation restore, **100%** of them are visible again with the same identities,
  and stored-endpoint protection is enforced on that path.
- **SC-008**: Template creation and editing occur **only** in the template editor: **0**
  endpoints, commands, UI actions, or backend services read real categories into a template,
  and **0** cross-door copy paths exist, proven by negative and source tests.
- **SC-009**: One template application produces **exactly 1** ChangeSet and **exactly 1**
  `TreeRevision` bump, creates **every** template root as a **direct child** of the target,
  and revalidates destination uniqueness and protection **within** the same transaction in
  **100%** of cases.
- **SC-010**: An applied tree carries **only** name, representative excerpt, description,
  aliases, order, and structure: **0** links, highlights, notes, requests, sources,
  decisions, notifications, workflow/audit history, or technical revisions are copied.
- **SC-011**: Template self-reparent, descendant-reparent, stale/concurrent reparent/reorder,
  and cyclic restore are rejected in **100%** of real-PostgreSQL/API cases, with **0** cyclic
  templates saved, applied, rendered, or restored; a valid reparent updates sibling order
  atomically and bumps `TemplateRevision` **exactly once**.
- **SC-012**: `TemplateNodeSearchAlias` remove/restore round-trips through the Template
  adapter with **0** physical deletes succeeding and **0** alias-history rows lost.
- **SC-013**: Permission-ownership tests cover **every** template verb: `template.add` grants
  aggregate creation **only**; **100%** of node/alias add/edit/reparent/reorder/internal-remove
  commands require `template.edit`; delete/restore require `template.delete` /
  `template.restore`; application requires `template.apply`; **0** commands succeed by
  borrowing another verb or by relying on frontend hiding.
- **SC-014**: The audit surfaces are exact: template application stores and renders the
  **frozen** template snapshot at application time and is unaffected by later template edits;
  template CRUD appears **only** in the separate template-history view and produces **0** main
  product-audit rows; the specialized relationship audit payload renders for **100%** of
  relationship operations (§6.3).
- **SC-015**: Mock and HTTP adapters are in **parity** for **100%** of relationship and
  template operations, and relationship/template cache keys/invalidation publish **only**
  after commit, with **0** publications on rollback (§14.1, §14.3).
- **SC-016**: The restore registry contains **exactly 1** Relationship adapter and **exactly
  1** DoorTemplate aggregate adapter; the application-event interpreter adds **0** registry
  entries and is proven to delegate real-category inversion to the **single `029` Category
  adapter**; a duplicate or missing registration **fails CI** (§8).
- **SC-017**: **0** relationship or template writers bypass the audit / protection /
  concurrency / stabilization foundation, proven by the architecture/source gates and by
  stabilization-blocked write tests.
- **SC-018**: The Spec Kit exits only when **both** workstreams have finished **their own**
  adapter and vertical slice; **0** exit is recorded with one workstream's adapter or slice
  incomplete.
- **SC-019**: Every §18.4 exit/acceptance criterion passes in CI, with **0** drag-and-drop
  packages, directives, handles, or event wiring introduced by the template or relationship
  UI (§15.2 source gate).

## Assumptions

- This specification is derived **only from Master Plan §18.4** (and its entry/exit gates plus
  the §17 portfolio exit condition for `030`). Where §18.4 points to another section (§5.2,
  §6.3, §6.6, §7.3, §7.4, §8, §9, §11, §14.x, §15.x), that section's content is owned there and
  recorded here only as a pointer; it is not re-decided in this spec.
- `028-abwab-safety-foundations` and `029-abwab-core` are both accepted at entry, including the
  audit / write / concurrency / time kernel, the CI gates, the shared frontend foundation, and
  the `029` category identity, protection resolver, category writer, and Category adapter.
- §18.4 defines **no numbered internal order**. Its only ordering rule — parallel workstreams,
  each finishing its own adapter and vertical slice before exit — is carried verbatim, and no
  additional sequencing is invented here.
- The Master Plan governs any perceived conflict; a genuine change returns to an independent
  amendment/re-review of the Master Plan, never a local decision here.
- "Real infrastructure" for verification means real PostgreSQL (Testcontainers) and a real
  browser (Playwright), consistent with §18.4's real-PG/API/browser acceptance tests and the
  `028` CI gates.
- Named conflict codes (`abwab.relationship_duplicate`, `abwab.relationship_cycle`,
  `abwab.template_cycle`, `abwab.template_revision_stale`, `abwab.row_stale`,
  `abwab.manual_protection`, `abwab.category_name_conflict`) and named permissions
  (`relationship.*`, `template.*`) are **frozen catalogue values owned by §5.2 and §11**,
  cited here as pointers; this spec adds, renames, or remaps **none** of them. The §5.2 permission
  values and four of the §11 conflict strings are frozen in the Master Plan but not yet present in
  the repository catalogue files; the tasks add those exact frozen strings to the existing catalogue
  code — an implementation step, not a new decision.
- The frontend ports, mocks, HTTP mappings, and cache behavior added here follow the existing
  `028` shared foundation and the installed Reactive Forms package; this feature installs no new
  frontend substrate.

## Dependencies

- **Predecessors**: `028-abwab-safety-foundations` and `029-abwab-core`, both accepted; the
  entry edges are `028 → 030` and `029 → 030` (§17, §15.1). Migration-specific predecessor
  condition: `029` category identity/schema accepted (§15.1).
- **Consumed from `029`**: the category writer used by template application, the Category
  aggregate inverse adapter reused by the application-event interpreter, category identity and
  the manual-protection resolver, and the subtree delete / operation-restore path whose generic
  dependent-visibility seam this feature fills with **real** relationship dormancy.
- **Downstream ownership boundaries** (not built here):
  - `031-abwab-attribution-links` owns Quran-link aggregates, sources, and link-check, and fills
    the same `029` dormancy seam for links; templates copy none of it.
  - `032-abwab-workspace-review-notifications` owns the personal workspace, requests, review
    decisions, and notification surfaces; templates copy none of it.
  - `033-abwab-audit-restore` consumes the versioned adapters accepted here — the
    **Relationship** adapter and the **one** DoorTemplate aggregate adapter — plus the
    application-event interpreter, over the direct `030 → 033` edges (§8, §16); it owns the
    audit read model, preview, planner, restore execution, and stabilization.
  - `034-abwab-realtime-hardening-release` owns realtime hints (including tree/template change
    hints), reconciliation, live reauthorization, and operational release proof.
