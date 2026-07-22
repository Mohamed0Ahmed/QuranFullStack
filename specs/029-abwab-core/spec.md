# Feature Specification: Abwab Core — Sections, Categories, Tree, and Protection

**Feature Branch**: `029-abwab-core`

**Created**: 2026-07-23

**Status**: Draft

**Input**: User description: "Create the Spec Kit for 029-abwab-core using ONLY section §18.3 of docs/feature-abwab-management/MASTER_PLAN.md (sections, categories, tree, protection). Generation only — preserve its exact scope, entry/exit gates, mandatory internal order, and acceptance criteria; introduce no new decisions; include nothing owned by 027–028 or 030–034; and do not implement code."

> **Canonical source**: `docs/feature-abwab-management/MASTER_PLAN.md` is the sole
> canonical product and architecture source for Abwab Spec Kits `027`–`034`. This
> specification is derived **only from Master Plan §18.3** (`029-abwab-core` — sections,
> categories, tree, and protection) plus its entry/exit gates and the §17 portfolio exit
> condition for `029`. It introduces no new product or architecture decision and
> reinterprets nothing. Section references (e.g. §5.1, §6.3, §7.1, §9, §14.1) point to the
> Master Plan and are recorded here as pointers only; their content is owned by those
> sections, not re-decided here. Where a conflict is perceived, the Master Plan governs and
> a genuine change returns to an independent amendment/re-review of that document, never a
> local decision here.

## Overview *(context)*

`029-abwab-core` is the third of eight top-level Abwab Spec Kits and the **first domain
vertical slice**. Its single purpose is to build the Abwab category domain in one strict
order: **category schema/read model precedes protection, protection precedes writers, and
the domain vertical slice plus all core restore adapters are accepted** (§17, §18.3).

The feature is delivered as four stages that MUST be built in the exact **mandatory
internal order** given by §18.3. Each stage is independently testable at its own exit
gate, but the stages are **not freely reorderable**: a later stage may only begin once the
earlier stage's guarantees hold — no protection storage before the read-only tree exists,
no tracked writer before the ManualProtection adapter is accepted, no frontend slice
before the writers exist. The feature is complete only when every exit/acceptance
criterion in §18.3 passes in CI.

**Entry condition (§18.3):** `028-abwab-safety-foundations` is accepted, including the
audit / write / concurrency / time kernel and the shared frontend infrastructure. Every
Abwab writer in this feature is built on that substrate (tracked ChangeSet UoW,
`AbwabWriteBarrier`, `ExpectedTimelineGeneration`, server clock).

**Scope boundaries carried from §18.3:**

- This feature builds the **sections / categories / tree / protection** domain only. It
  does **not** build category relationships or templates (`030`), attribution / Quran-link
  aggregates or sources (`031`), the personal workspace / review / notification surfaces
  (`032`), the audit-restore read model / planner / execution (`033`), or realtime
  hardening and release proof (`034`).
- Category deletion exposes an **integration seam for a reservation checker only**. Because
  request storage does not yet exist, `032` must install and test the Pending-aware checker
  before Submit is activated; `029` does not build request storage.
- There is **no drag-and-drop**. All ordering and moves are explicit actions.
- `029` has **no forward schema dependency** on relationships or links: subtree
  delete/restore uses a generic RESTRICT / no-cascade and dependent-visibility seam proven
  with a core fixture. Real relationship/link dormant integration belongs to `030`/`031`.
- `RepresentativeQuranExcerpt` is an **optional plain string** — it has **no Quran foreign
  key** and **no full-ayah validation**; the first Abwab Quran FK remains owned by later
  Kits.

## User Scenarios & Testing *(mandatory)*

<!--
  The four user stories below are the four mandatory-order stages of §18.3. Priority order
  reflects the mandatory build order: schema/read model before protection, protection
  before writers, writers before the frontend slice. Each story is independently testable
  at its own §18.3 exit gate, but the build order is fixed by §18.3 and is not a free
  MVP-reordering choice.
-->

### User Story 1 - Schema and read-only tree (Priority: P1)

A backend engineer generates the Section / Category / Alias / revision schema, seeds
exactly one permanent default section, and exposes **read / search / snapshot only**, so a
correct, uniqueness-constrained category tree exists and is restorable before any mutation
surface is enabled (§18.3 step 1).

**Why this priority**: Nothing else in the domain can be built without the schema and its
uniqueness / order constraints. Protection attaches to categories, writers mutate them, and
the frontend renders them — all of that depends on this read model existing first, with no
mutation endpoint or editable UI enabled at this checkpoint.

**Independent Test**: Run against real PostgreSQL with read/search/snapshot only; the stage
is valid when the migration creates Section/Category/Alias/revision, exactly one permanent
default section is seeded, root/descendant shape and normalized uniqueness/order
constraints hold, the `كل الأبواب` projection and independent root orders read correctly,
Section/Category/Order versioned restore snapshots round-trip, and **no** category/section
mutation endpoint or editable UI exists yet.

**Acceptance Scenarios**:

1. **Given** a fresh migration, **When** it runs, **Then** Section/Category/Alias/revision
   tables are created and **exactly one permanent default section** is seeded.
2. **Given** the seeded tree, **When** read/search/snapshot APIs are exercised, **Then** the
   default section, the `كل الأبواب` projection, independent root orders, explicit child
   order, and ancestry/depth read correctly against real PostgreSQL.
3. **Given** the uniqueness constraints, **When** names are compared, **Then** root names are
   globally unique across sections, sibling names are unique by the exact §5.1 normalization
   contract, and aliases follow their **separate owned-row** uniqueness/search rules.
4. **Given** the Section/Category/Order restore snapshots, **When** they are versioned and
   round-tripped, **Then** they reconstruct the tree exactly.
5. **Given** this checkpoint, **When** the surface is inspected, **Then** **no**
   category/section mutation endpoint or editable UI is enabled.

---

### User Story 2 - Protection storage and resolver (Priority: P1)

A backend engineer adds ManualProtection storage plus ordinary-protection actor/time
fields, and a direct/inherited source resolver with server-clock DTOs and action
classification, so protection can be resolved for any category **before** any protected
category writer exists (§18.3 step 2).

**Why this priority**: Writers must consult resolved protection to decide whether an action
is allowed, so the protection storage and resolver — and the accepted ManualProtection
adapter — must exist before the tracked writers in Story 3. This ordering is mandatory:
"Accept the ManualProtection adapter before protected category writers exist."

**Independent Test**: Exercise the resolver against a deep real-PostgreSQL tree; the stage
is valid when a single active ManualProtection record exists per CategoryId/type, direct and
inherited source ancestors resolve correctly, server-derived expiry is returned via
server-clock DTOs, actions are classified, the deep-tree query stays within its measured
budget, and the ManualProtection adapter is accepted **before** any protected writer.

**Acceptance Scenarios**:

1. **Given** a category with direct or inherited protection, **When** protection is resolved,
   **Then** the resolver returns the correct type/scope and the direct or inherited **source
   ancestor**, plus **server-derived expiry** via server-clock DTOs.
2. **Given** a deep tree, **When** protection resolution runs against real PostgreSQL,
   **Then** it stays within the measured deep-tree query budget.
3. **Given** ManualProtection storage, **When** records are checked, **Then** there is exactly
   **one active record per CategoryId/type**.
4. **Given** the ManualProtection adapter, **When** the stage exits, **Then** the adapter is
   accepted **before** any protected category writer is implemented.

---

### User Story 3 - Activate tracked writers (Priority: P1)

A backend engineer implements the explicit section/category actions of the Section 9 matrix
on one audited unit of work — with expected TimelineGeneration/xmin/TreeRevision,
revalidated names, atomic ordering/ancestry, move cycle guards, atomic subtree
deletion/operation-restore, protection resolution, and safe 409s — so all core domain
mutations are tracked, concurrency-safe, protection-gated, and restorable (§18.3 step 3).

**Why this priority**: The writers are the domain's core behavior and depend on both the
read model (Story 1) and the protection resolver/adapter (Story 2). They must exist before
the frontend slice can drive them. There is **no drag-and-drop**.

**Independent Test**: Exercise the writers against real PostgreSQL/API; the stage is valid
when every action runs through one audited UoW carrying expected
TimelineGeneration/xmin/TreeRevision, name/uniqueness checks are revalidated on
create/move/restore, ordering/ancestry updates are atomic with a single TreeRevision bump,
self/descendant/overlapping bulk moves are rejected, subtree deletion and operation-restore
are all-row tracked and atomic, protection gating matches Section 9, and conflicts return
the exact `abwab.*` codes as safe 409s.

**Acceptance Scenarios**:

1. **Given** a section rename/delete race, **When** it hits a normalized-name or non-empty
   condition, **Then** it maps exactly to `abwab.section_name_conflict` and
   `abwab.section_not_empty` (with the **separate** code for permanent-default violations),
   identically across API, core mock/HTTP, frontend, and contract tests.
2. **Given** create/promote-root and section-move, **When** they run, **Then** root defaulting
   applies, global order is preserved on section move, and independent root orders and
   explicit child order hold; one atomic reorder bumps TreeRevision **once** (real-PG).
3. **Given** a single or bulk move, **When** it targets self, a descendant, or overlapping
   ranges, **Then** it is rejected; a valid move rewrites descendant ancestry, and concurrent
   move/reorder conflicts return safe 409s (real-PG/API).
4. **Given** an alias add/edit/remove, **When** it is authorized, **Then** it requires
   `category.edit` as **category direct-content mutation** (never borrowing child
   `add`/`delete` verbs); removal is **tracked soft delete**, physical delete is rejected, and
   the versioned CategorySearchAlias adapter round-trips.
5. **Given** `RepresentativeQuranExcerpt`, **When** it is set, **Then** it is an optional
   audited/restorable **plain string** with **no Quran FK or full-ayah validation** and it
   activates ordinary protection as direct content.
6. **Given** ordinary 24-hour protection, **When** a direct-content edit/move occurs, **Then**
   only direct-content edits/moves are gated and start the window; original-editor / SystemOwner
   behavior and stronger manual/stabilization denial match Section 9.
7. **Given** ManualProtection apply/lift/preset, **When** commands run, **Then** same-scope
   apply is idempotent with **no audit no-op**, scope change is expected-version audited,
   a conflicting scope returns `abwab.manual_protection_scope_conflict`, apply/lift/preset are
   atomic, preview blocker identity is stable, and adapters round-trip (real-PG/race).
8. **Given** a full five-type preset, **When** it is applied, **Then** none/some/all
   pre-existing types, mixed pre-existing scopes, one selected scope applied to all five,
   required Expected Versions for every changed scope, an all-matching no-op, a per-type later
   lift, and a concurrent stale scope edit **rolling back the entire five-type command** all
   pass (real-PG/API/mock/HTTP).
9. **Given** an atomic subtree deletion or operation-restore, **When** it runs, **Then**
   child/parent order, all-row tracked atomicity, protection on **every** affected category, a
   generic RESTRICT/no-cascade and dependent-visibility seam using a **core fixture**, conflict
   rollback, and versioned adapter round-trips all hold (no forward relationship/link schema
   dependency).
10. **Given** category deletion, **When** the reservation seam is checked, **Then** an
    integration seam for a reservation checker exists, and because request storage does not yet
    exist, `032` must install and test the Pending-aware checker before Submit is activated.
11. **Given** the **three** registered restore adapters — Section, Category (incl. all three orders
    and subtree delete/operation-restore), and ManualProtection — **When** the stage exits, **Then**
    they are versioned, round-trip tested, and **marked accepted for `033`**, with the order
    round-trip verified as a **facet within** the Category (and Section-order within the Section)
    adapter and **no** standalone Order registration (§8 keeps exactly one adapter per persisted
    type; a duplicate registration fails CI).

---

### User Story 4 - Domain frontend vertical slice (Priority: P2)

A frontend engineer owns the core port, core mock, backend contract, HTTP mapping, parity
suite, tree/search/editor/protection UI, and core cache rules — reusing the Reactive Forms
package already used by the real `028` permission-administration form — and publishes the
core audit render payloads, so the domain is usable end-to-end with backend/frontend parity
(§18.3 step 4).

**Why this priority**: The frontend slice depends on the completed writers and is last in
§18.3's mandatory order. It proves the whole domain end-to-end (parity, cache, RTL, no-drag,
context preservation) but is not on the backend critical path.

**Independent Test**: Drive the UI against the core mock and the real HTTP adapter; the
stage is valid when mock/HTTP parity holds, composite-read permission gating matches the
backend with no partial leak, category editors reuse the `028` Reactive Forms package, the
core audit render payloads (category create/edit, bulk-move, subtree-deletion,
manual-protection) publish per §6.3 — with ordering data rendered **within** the bulk-move and
category-edit payloads, not as a standalone component — and the browser suite (stale-cache,
rollback, RTL keyboard/focus, large-tree, explicit action, no-edit-session-lock, no-drag,
post-mutation context preservation) passes.

**Acceptance Scenarios**:

1. **Given** the core port/mock and HTTP adapter, **When** the parity suite runs, **Then**
   backend DTO, core mock, HTTP mapping, and UI action visibility remain in parity.
2. **Given** composite-read policy, **When** every grant combination of `category.view`,
   `section.view`, and `protection.view` is tested, **Then** tree/search requires the first
   two, dedicated/full manual metadata requires all applicable permissions, and **no** partial
   response leaks type/scope/actor/source-ancestor data.
3. **Given** category editors, **When** they are implemented, **Then** they reuse the Reactive
   Forms package already used by the real `028` permission-administration form.
4. **Given** the audit render payloads, **When** a mutation is rendered, **Then** the complete
   category (create/edit), bulk-move, subtree-deletion, and manual-protection payloads defined in
   §6.3 are published, with **ordering data folded into** the bulk-move payload (sibling-order side
   effects grouped by affected parent/order scope) and the category-edit payload (order fields) —
   **no** standalone ordering payload (§6.3 defines none).
5. **Given** the browser/source suite, **When** it runs, **Then** mock/HTTP parity,
   stale-cache, rollback, RTL keyboard/focus, large-tree, explicit action, no-edit-session-lock,
   **no-drag**, and post-mutation context-preservation tests pass.

---

### Edge Cases

- **Root name collision across sections** — a root name that already exists in **any** section
  must be rejected as globally unique (Story 1/3).
- **Sibling name collision under normalization** — two sibling names that collide under the
  §5.1 normalization contract must be rejected on create, move, and restore (Story 1/3).
- **Non-empty section delete** — deleting a section that still contains categories must map to
  `abwab.section_not_empty`; deleting/altering the permanent default section uses its separate
  code (Story 3).
- **Self / descendant / overlapping bulk move** — a move onto self, a descendant, or an
  overlapping range must be rejected before any ancestry rewrite (Story 3).
- **Concurrent move vs reorder** — a concurrent move/reorder conflict must return a safe 409
  and not corrupt order or ancestry (Story 3).
- **Same-scope idempotent apply** — re-applying manual protection at the same scope must be an
  idempotent no-op with **no audit event** (Story 3).
- **Conflicting-scope apply** — a conflicting-scope apply must return
  `abwab.manual_protection_scope_conflict` (Story 3).
- **Stale scope in a five-type preset** — a concurrent stale scope edit inside a full preset
  must roll back the **entire** five-type command (Story 3).
- **Protection view/lift on a soft-deleted target** — authorized protection view/lift by
  immutable ID must succeed even when the target category is soft-deleted (Story 2/3).
- **Subtree delete with dependents** — subtree deletion must apply the generic RESTRICT /
  no-cascade and dependent-visibility seam via a core fixture, preserving dependents as dormant
  without a forward schema dependency (Story 3).
- **Category deletion before request storage exists** — the reservation seam must be present but
  inert; `032` installs and tests the Pending-aware checker before Submit (Story 3).
- **Partial permission read** — a caller missing a required view permission must receive **no**
  partial response leaking type/scope/actor/source-ancestor data (Story 4).

## Requirements *(mandatory)*

### Functional Requirements

**Schema and read-only tree (Story 1)**

- **FR-001**: The system MUST generate the Section / Category / Alias / revision migration and
  seed **exactly one permanent default section**.
- **FR-002**: The system MUST enforce root/descendant shape and normalized uniqueness/order
  constraints: root names globally unique across sections, sibling names unique by the exact
  §5.1 normalization contract, and aliases governed by their **separate owned-row**
  uniqueness/search rules.
- **FR-003**: The system MUST implement **read / search / snapshot only** at this checkpoint and
  MUST NOT enable any category/section mutation endpoint or editable UI.
- **FR-004**: The system MUST support the default section, the `كل الأبواب` projection,
  independent root orders, explicit child order, and ancestry/depth as read-model behavior.
- **FR-005**: The system MUST accept versioned Section / Category / Order restore snapshots that
  round-trip the tree.

**Protection storage and resolver (Story 2)**

- **FR-006**: The system MUST add ManualProtection storage plus ordinary-protection actor/time
  fields, with **exactly one active record per CategoryId/type**.
- **FR-007**: The system MUST resolve direct and inherited protection, returning the direct or
  inherited **source ancestor** and **server-derived expiry** via server-clock DTOs, with action
  classification.
- **FR-008**: The system MUST keep protection resolution within a measured deep-tree query
  budget against real PostgreSQL.
- **FR-009**: The system MUST accept the ManualProtection adapter **before** any protected
  category writer exists.

**Activate tracked writers (Story 3)**

- **FR-010**: The system MUST implement the explicit section/category actions of the Section 9
  matrix on **one audited unit of work**, each carrying expected
  TimelineGeneration/xmin/TreeRevision.
- **FR-011**: The system MUST revalidate destination names on create/move/restore using the same
  uniqueness/normalization checks; moves and restore MUST use the same checks as create.
- **FR-012**: The system MUST perform tracked atomic order changes with root order rules,
  create/promote-root defaulting, and global-order preservation on section move, bumping
  TreeRevision **once** per atomic reorder.
- **FR-013**: The system MUST maintain ancestry, rewriting descendant ancestry on a valid move.
- **FR-014**: The system MUST guard single and bulk moves against self/descendant/overlapping
  targets and MUST return safe 409s for concurrent move/reorder conflicts.
- **FR-015**: The system MUST perform atomic subtree deletion and operation-restore with
  child/parent order, all-row tracked atomicity, protection on **every** affected category,
  conflict rollback, and versioned adapter round-trips.
- **FR-016**: The system MUST apply dormant-dependent filtering through a generic RESTRICT /
  no-cascade and dependent-visibility seam using a **core fixture**, with **no forward
  relationship/link schema dependency** (`030`/`031` own real integration).
- **FR-017**: The system MUST map section normalized-name and non-empty-delete database races
  exactly to `abwab.section_name_conflict` and `abwab.section_not_empty`, with a **separate
  code** for permanent-default violations, identically across API, core mock/HTTP, frontend, and
  contract tests.
- **FR-018**: The system MUST treat alias add/edit/remove as **category direct-content mutation**
  authorized by `category.edit` (never borrowing child `add`/`delete` verbs); alias removal MUST
  be **tracked soft delete**, physical delete MUST be rejected, and the versioned
  CategorySearchAlias adapter MUST round-trip.
- **FR-019**: The system MUST treat `RepresentativeQuranExcerpt` as an **optional
  audited/restorable plain string** with **no Quran FK and no full-ayah validation**, activating
  ordinary protection as direct content.
- **FR-020**: The ordinary 24-hour protection window MUST gate **only** direct-content
  edits/moves and start the window; original-editor / SystemOwner behavior and stronger
  manual/stabilization denial MUST match Section 9.
- **FR-021**: ManualProtection apply/lift/preset MUST guarantee one active record per
  CategoryId/type, an idempotent same-scope apply with **no audit no-op**, an expected-version
  audited scope change, a conflicting-scope `abwab.manual_protection_scope_conflict`,
  apply/lift/preset atomicity, stable preview blocker identity, and adapter round-trips.
- **FR-022**: A full five-type preset MUST correctly handle none/some/all pre-existing types,
  mixed pre-existing scopes, one selected scope applied to all five, required Expected Versions
  for **every** changed scope, an all-matching no-op, a per-type later lift, and a concurrent
  stale scope edit that **rolls back the entire five-type command**.
- **FR-023**: Authorized protection view/lift by immutable ID MUST succeed even when the target
  category is **soft-deleted**, showing direct/inherited source ancestor and server-derived
  expiry.
- **FR-024**: Category deletion MUST expose an **integration seam for a reservation checker**;
  because request storage does not yet exist, `032` MUST install and test the Pending-aware
  checker before Submit is activated (`029` builds no request storage).
- **FR-025**: The **three** registered restore adapters — **Section**, **Category** (aggregate:
  Category + CategorySearchAlias + content + hierarchy/ancestry + all three orders + subtree
  delete/operation-restore + ordinary-protection actor/time), and **ManualProtection** — MUST be
  versioned, round-trip tested, and **marked accepted for `033`**. Order MUST be verified as a
  **facet within** the Category adapter (section order within the Section adapter), **not** as a
  standalone registration; per §8 there is exactly **one adapter per persisted type**, and a
  duplicate registration (e.g. a standalone Order adapter) MUST fail CI.
- **FR-026**: There MUST be **no drag-and-drop**; all ordering and moves are explicit actions.
- **FR-032**: A category **direct-content** mutation (Name, Description, `RepresentativeQuranExcerpt`,
  and CategorySearchAlias add/edit/remove) MUST bump its owning Category's `CategoryContentRevision`
  **exactly once** per audited operation (§6.4, §8). `CategoryContentRevision` is a
  reconciliation/logical counter **distinct from `TreeRevision`** (structural) — a pure move/reorder
  bumps `TreeRevision`, not `CategoryContentRevision` — and has **no dedicated §11 stale code** (do
  not invent one; content concurrency is enforced by `xmin` → `abwab.row_stale` and
  `ExpectedTimelineGeneration`).

**Domain frontend vertical slice (Story 4)**

- **FR-027**: The system MUST own the core port, core mock, backend contract, HTTP mapping,
  parity suite, tree/search/editor/protection UI, and core cache rules, with backend DTO, core
  mock, HTTP mapping, and UI action visibility in parity.
- **FR-028**: Composite-read policy MUST cover every grant combination of `category.view`,
  `section.view`, and `protection.view`: tree/search requires the first two, dedicated/full
  manual metadata requires all applicable permissions, and **no** partial response leaks
  type/scope/actor/source-ancestor data.
- **FR-029**: Category editors MUST reuse the Reactive Forms package already used by the real
  `028` permission-administration form.
- **FR-030**: The system MUST publish the audit render payloads defined in §6.3: complete category
  (create/edit), bulk-move, subtree-deletion, and manual-protection. **Ordering data MUST be
  rendered within** the bulk-move payload (sibling-order side effects grouped by affected
  parent/order scope) and the category-edit payload (order fields); there is **no** standalone
  "ordering" render component (§6.3 defines none).
- **FR-031**: The browser/source suite MUST prove mock/HTTP parity, stale-cache, rollback, RTL
  keyboard/focus, large-tree, explicit action, no-edit-session-lock, **no-drag**, and
  post-mutation context-preservation.

### Key Entities *(include if feature involves data)*

- **Section**: A top-level grouping with an independent global root order; exactly one
  **permanent default section** is seeded and protected by its own violation code.
- **Category**: A node in the tree (root or descendant) with a normalized name, explicit child
  order, ancestry/depth, an optional `RepresentativeQuranExcerpt` plain string, and a
  `CategoryContentRevision` counter bumped **once** per direct-content mutation; root names are
  globally unique, sibling names unique by the §5.1 normalization contract.
- **CategorySearchAlias**: A **separately owned** row with its own uniqueness/search rules;
  add/edit/remove is category direct-content mutation under `category.edit` (and bumps the owning
  Category's `CategoryContentRevision` once); removal is tracked soft delete.
- **Tree order / TreeRevision**: The ordering state and a **structural** tree revision bumped
  **once** per atomic reorder (distinct from `CategoryContentRevision`); the `كل الأبواب` projection
  and independent root orders read from it.
- **ManualProtection**: One active record per CategoryId/type carrying type/scope; resolved as
  direct or inherited with a source ancestor and server-derived expiry; supports apply / lift /
  preset (five types).
- **Ordinary protection (24-hour window)**: Actor/time fields on categories that gate **only**
  direct-content edits/moves and start the window, per Section 9.
- **Versioned restore adapters**: the **three** registered adapters — Section, Category (incl. all
  three orders + subtree delete/operation-restore), and ManualProtection — versioned, round-trip
  tested, and **accepted for `033`**; order is a tested **facet** within Category/Section, not a
  standalone registration (§8).
- **Reservation-checker seam**: An inert integration seam on category deletion; the Pending-aware
  checker is installed and tested by `032` before Submit.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Exactly **1** permanent default section exists after migration; root names are
  globally unique across sections and sibling names unique by the §5.1 normalization contract in
  **100%** of create/move/restore cases (real-PostgreSQL).
- **SC-002**: At the Story 1 checkpoint, **0** category/section mutation endpoints and **0**
  editable UI surfaces are enabled; read/search/snapshot and Section/Category/Order restore
  snapshots round-trip the tree with **0** discrepancies.
- **SC-003**: Protection resolves the correct type/scope, direct/inherited **source ancestor**,
  and server-derived expiry in **100%** of cases, stays within the measured deep-tree query
  budget, and there is exactly **1** active ManualProtection record per CategoryId/type; the
  ManualProtection adapter is accepted **before** any protected writer exists.
- **SC-004**: Section normalized-name and non-empty-delete races map to
  `abwab.section_name_conflict` / `abwab.section_not_empty` (and the separate permanent-default
  code) identically across API, core mock/HTTP, frontend, and contract tests, with **0** drift.
- **SC-005**: One atomic reorder bumps TreeRevision **exactly once**; global order is preserved
  on section move; self/descendant/overlapping bulk moves are rejected in **100%** of cases;
  descendant ancestry is rewritten on valid moves; concurrent move/reorder conflicts return safe
  409s (real-PG/API).
- **SC-006**: Alias removal is tracked soft delete with **0** physical deletes succeeding, the
  CategorySearchAlias adapter round-trips, and alias add/edit/remove is authorized **only** by
  `category.edit` (never a borrowed child verb).
- **SC-007**: `RepresentativeQuranExcerpt` is an optional plain string with **0** Quran FKs and
  **0** full-ayah validations, and it activates ordinary protection as direct content.
- **SC-008**: The ordinary 24-hour tests prove **only** direct-content edits/moves are gated and
  start the window, and original-editor / SystemOwner / stronger manual/stabilization behavior
  matches Section 9 in **100%** of cases.
- **SC-009**: Manual apply/lift/preset tests pass: idempotent same-scope apply with **0** audit
  no-ops, expected-version audited scope change, conflicting-scope
  `abwab.manual_protection_scope_conflict`, apply/lift/preset atomicity, stable preview blocker
  identity, and adapter round-trips.
- **SC-010**: The full five-type preset passes for none/some/all pre-existing types, mixed
  scopes, one scope applied to all five, required Expected Versions for every changed scope, an
  all-matching no-op, per-type later lift, and a concurrent stale scope edit that rolls back the
  **entire** five-type command (real-PG/API/mock/HTTP).
- **SC-011**: Subtree delete/operation-restore proves child/parent order, all-row tracked
  atomicity, protection on **every** affected category, the generic RESTRICT/no-cascade and
  dependent-visibility seam via a core fixture, conflict rollback, and versioned adapter
  round-trips — with **0** forward relationship/link schema dependencies.
- **SC-012**: The **3** registered adapters (Section, Category incl. all three orders + subtree
  delete/operation-restore, ManualProtection) are versioned, round-trip tested, and marked accepted
  for `033`; the order round-trip is verified as a **facet** within Category/Section; a static §8
  registry test proves **exactly one adapter per persisted type** and **fails CI** on a standalone
  Order (duplicate) or any missing registration.
- **SC-013**: Composite-read policy tests cover **every** grant combination of `category.view`,
  `section.view`, and `protection.view`; tree/search requires the first two; dedicated/full
  manual metadata requires all applicable permissions; **0** partial responses leak
  type/scope/actor/source-ancestor data; backend DTO, core mock, HTTP mapping, and UI action
  visibility stay in parity.
- **SC-014**: Category deletion exposes a reservation-checker seam with **0** request-storage
  built here; the Pending-aware checker is owned by `032` and must be installed and tested before
  Submit.
- **SC-015**: The browser/source suite passes mock/HTTP parity, stale-cache, rollback, RTL
  keyboard/focus, large-tree, explicit action, no-edit-session-lock, **no-drag**, and
  post-mutation context-preservation, and category editors reuse the `028` Reactive Forms
  package.
- **SC-016**: Every §18.3 exit/acceptance criterion passes in CI, in the mandatory internal
  order (schema/read → protection → writers → frontend slice).
- **SC-017**: A category direct-content mutation (Name / Description / `RepresentativeQuranExcerpt` /
  CategorySearchAlias add/edit/remove) bumps `CategoryContentRevision` **exactly once** per audited
  operation, while a pure move/reorder bumps `TreeRevision` and **0** times `CategoryContentRevision`
  (verified against real PostgreSQL); no dedicated §11 stale code exists for it (§6.4, §8).

## Assumptions

- This specification is derived **only from Master Plan §18.3** (and its entry/exit gates plus
  the §17 portfolio exit condition for `029`). Where §18.3 points to another section (§5.1, §6.3,
  §7.x, §9, §14.1, etc.), that section's content is owned there and recorded here only as a
  pointer; it is not re-decided in this spec.
- `028-abwab-safety-foundations` is accepted at entry, including the audit / write / concurrency
  / time kernel and the shared frontend infrastructure that every `029` writer and UI reuses.
- The Master Plan governs any perceived conflict; a genuine change returns to an independent
  amendment/re-review of the Master Plan, never a local decision here.
- The four stages are built in §18.3's mandatory internal order; each is independently testable
  at its own exit gate, but the order is fixed and not a free MVP reordering.
- "Real infrastructure" for verification means real PostgreSQL (Testcontainers) and a real
  browser (Playwright), consistent with §18.3's real-PG/API/browser acceptance tests.
- The Reactive Forms package reused by category editors is the one already installed and tested
  by the real `028` permission-administration form; `029` installs no new forms substrate.

## Dependencies

- **Predecessor**: `028-abwab-safety-foundations` (accepted) is the sole direct predecessor; this
  feature's only entry edge is `028 → 029` (§17).
- **Downstream ownership boundaries** (not built here):
  - `030-abwab-relationships-templates` and `031-abwab-attribution-links` depend on accepted
    `028` and `029`; they own relationships/templates and Quran-link aggregates/sources, and the
    real dormant integration behind `029`'s generic dependent-visibility seam.
  - `032-abwab-workspace-review-notifications` installs and tests the Pending-aware reservation
    checker at `029`'s deletion seam before Submit is activated, and owns notification surfaces.
  - `033-abwab-audit-restore` consumes the **three** versioned `029` restore adapters accepted here
    — Section, Category (incl. order + subtree delete/operation-restore), and ManualProtection — for
    its restore read model, planner, and execution. (The §8 entry checklist enumerates
    "Section/Category/Order/ManualProtection", where **Order is the Category adapter's order facet**,
    not a fourth registration.)
  - `034-abwab-realtime-hardening-release` owns realtime hardening, live reauthorization, and
    operational release proof.
