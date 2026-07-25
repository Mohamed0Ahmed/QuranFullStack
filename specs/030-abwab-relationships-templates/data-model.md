# Data Model: Abwab Relationships and Templates — Category Adjuncts

**Feature**: `030-abwab-relationships-templates` | **Date**: 2026-07-25 | **Source**: Master Plan
§7.3 (relationships), §7.4 (templates and application), §8 (registry), §6.3/§6.4, §9, §11 — realized
per §18.4 only.

All entities are `IAbwabAuditable` and mutate **only** through the `028` audited-commit executor
behind the `AbwabWriteBarrier`; the `SavingChanges` guard rejects any physical delete and any
mutation without a tracked ChangeSet. Every mutable row carries `uint Version` mapped explicitly to
`xmin` (the `029` convention). Story tags refer to the two §18.4 workstreams in
[`spec.md`](./spec.md); they run in **parallel** — the tag says which workstream owns the entity, not
a build order.

## Domain entities

### CategoryRelationship (Story 1)

One typed row per relationship (§7.3):

| Field | Notes |
|---|---|
| `CategoryRelationshipId` | identity |
| `RelationshipType` | `Similar` \| `Opposite` \| `BroaderNarrower` |
| `LowerCategoryId`, `HigherCategoryId` | **mutual shape only**; non-null with `LowerCategoryId < HigherCategoryId`; null for directional |
| `SourceCategoryId`, `TargetCategoryId` | **directional shape only**; source = broader, target = narrower; null for mutual |
| soft-delete metadata | tracked soft delete/restore; physical delete rejected |
| `Version` | `xmin` concurrency token → `abwab.row_stale` |

**Constraints and rules**

- CHECK: **exactly one shape** per row (mutual pair non-null ⇔ directional pair null).
- CHECK: **canonical ordering** `LowerCategoryId < HigherCategoryId` for mutual rows.
- CHECK: **no self-link** in either shape.
- Filtered unique index over **active** rows: one mutual pair **per type**; one directional edge per
  ordered pair. Both map to `abwab.relationship_duplicate` — a *reverse* duplicate collapses onto the
  same key because of the canonical ordering.
- The Broader/Narrower **inverse label is derived for display**; it is never a second stored row.
- Directional writes reject **cycles under the transaction** (`abwab.relationship_cycle`); an explicit
  direct **A→C is allowed** even when A→B→C exists.
- Category endpoints use **RESTRICT / no-cascade** FKs; relationship rows are never deleted by a
  category subtree deletion.
- **No ordinary 24-hour window** is read, started, or restarted by any relationship mutation (§9, §2.1).

### Relationship protection targets (Story 1)

Not a table — the resolved target set the writer checks via the accepted `029`
`ManualProtectionResolution` (direct + inherited from current `AncestorIds`), for the `Relationship`
manual-protection type (§6.6, §7.3, §9):

| Operation | Protected targets |
|---|---|
| add | **proposed** endpoints |
| edit | **union of current and proposed** endpoints |
| delete / restore | **stored** endpoints |

Applicable direct **or** inherited protection on **any** target blocks the **entire** mutation
(`abwab.manual_protection`) — an edit cannot escape protection by replacing a protected endpoint.

### DoorTemplate (Story 2)

| Field | Notes |
|---|---|
| `DoorTemplateId` | identity |
| `Name` / `NormalizedName` | §5.1 `ArabicNameNormalizer` |
| `Description` | optional |
| `TemplateRevision` | aggregate logical counter; **one bump per grouped operation** (§6.4) |
| soft-delete metadata | aggregate lifecycle via `template.delete` / `template.restore` |
| `Version` | `xmin` |

Created and edited **only** in the template editor — no endpoint, command, UI action, or backend
service reads real categories into a template (§7.4).

### TemplateNode (Story 2)

| Field | Notes |
|---|---|
| `TemplateNodeId` | identity |
| `DoorTemplateId` | owning template |
| `ParentTemplateNodeId` | null for a template root |
| `Name` / `NormalizedName` | §5.1 normalization |
| `RepresentativeQuranExcerpt` | optional **plain string — no Quran FK, no ayah validation** |
| `Description` | optional |
| `SiblingOrder` | explicit; sibling orders updated atomically on reparent/reorder |
| soft-delete metadata | tracked |
| `Version` | `xmin` |

**Structure rules (§7.4)**: create/reparent/reorder carry expected `TemplateRevision`, run on tracked
rows, reject **self-parenting** and a destination **inside the moved node's descendant tree**, validate
the parent chain under the transaction, update affected sibling orders **atomically**, and bump
`TemplateRevision` **once**. No cyclic template can be **saved, applied, rendered, or restored**
(`abwab.template_cycle`); stale expectations map to `abwab.template_revision_stale` / `abwab.row_stale`.

### TemplateNodeSearchAlias (Story 2)

Mirrors the `CategorySearchAlias` value / normalization / soft-delete contract (§7.4): `Value`,
`NormalizedValue`, owning `TemplateNodeId`, soft-delete metadata, `Version`. Remove and restore are
**tracked soft delete/restore**; physical delete is rejected and adapter round-trips prove **no alias
history is lost**. Alias mutations are `template.edit` work — never a borrowed child verb (§5.2).

### Template application (Story 2)

Not a table — one audited operation that writes **real categories through the accepted `029` category
writer** (§7.4, §18.4):

- Creates **every template root as a direct child** of the one target category.
- Recursively copies **only**: name, representative excerpt, description, aliases, order, structure.
- Copies **nothing** else — no links, ayah members, highlights, notes, requests, sources, decisions,
  notifications, audit/workflow history, or technical revisions. Produced categories are **independent
  real categories** with their own fresh technical state.
- Revalidates destination **uniqueness** (§5.1), manual **`InternalStructure`** protection on the
  target, current category state, concurrency, and order allocation **inside one transaction**.
- Produces **one ChangeSet** and **one `TreeRevision`** bump.
- Requires `template.apply` **alone** (§5.2).
- Conflicts reuse existing codes: `abwab.category_name_conflict`, `abwab.manual_protection`,
  `abwab.category_unavailable`, `abwab.tree_revision_stale`, `abwab.timeline_generation_stale`,
  `abwab.stabilization_active`.

## Reused technical / core state (not owned here)

- **`AbwabRevisionState`** (`028` singleton): `AuditHeadSequence`, `TimelineGeneration`,
  `TreeRevision`. Template application bumps `TreeRevision` once; relationship mutations do not touch
  it. Neither workstream defines a new counter beyond `TemplateRevision`.
- **`029` Category / Section / ManualProtection**: identity, `AncestorIds`-based inheritance, the
  category writer, and the protection resolver are consumed, never re-implemented or forked.
- **Dependent-visibility seam (`029`)**: `030` supplies the **real** dependent for the relationship
  half — dormancy is a **read projection** over untouched rows, not a written flag, so a category
  operation-restore needs no relationship-side write to reverse.

## Versioned restore adapters (accepted for `033`)

`030` registers **exactly two** new adapters; the application-event interpreter registers **none**
(§8 — one adapter per persisted type; duplicate **and** missing registrations fail CI):

| Registered adapter | Covers | Notes |
|---|---|---|
| **Relationship** | `CategoryRelationship` | shape/type, both endpoint pairs, soft-delete state |
| **DoorTemplate aggregate** | `DoorTemplate` + `TemplateNode` + `TemplateNodeSearchAlias` | **one** adapter for the whole aggregate; alias history round-trips |
| *(not an adapter)* **application-event interpreter** | template-application events | versioned; delegates real-category inversion to the **single `029` `CategoryRestoreAdapter`**; adds **no** registry entry |

Snapshots hold product state only. They exclude `xmin`, logical revision counters (`TemplateRevision`,
`TreeRevision`), cache state, and realtime cursors (§6.3, §6.4, §8).

## Invariant summary (verification anchors)

| Invariant | Entity | Enforced by | Story |
|---|---|---|---|
| Exactly one shape per row; canonical lower/higher ordering; no self-link | CategoryRelationship | CHECK constraints (real-PG) | 1 |
| Duplicate active mutual pair (incl. reverse) / directional edge rejected | CategoryRelationship | filtered unique indexes → `abwab.relationship_duplicate` | 1 |
| Cycle rejected under the transaction; direct A→C allowed; race-created cycle rejected | CategoryRelationship | in-transaction validation under the `028` locks → `abwab.relationship_cycle` | 1 |
| Tracked soft delete/restore; physical delete rejected | CategoryRelationship | `028` `SavingChanges` guard + adapter round-trip | 1 |
| Restore collision against a now-active pair/edge rejected | CategoryRelationship | in-transaction index revalidation | 1 |
| Protection targets = proposed / current ∪ proposed / stored; any target blocks all | CategoryRelationship | `029` resolver + endpoint-protection gate → `abwab.manual_protection` | 1 |
| Relationship mutations neither start nor are blocked by the ordinary 24h window | CategoryRelationship | §9 gate tests (0 window reads/writes) | 1 |
| Subtree deletion leaves rows intact/dormant; operation-restore re-exposes them | CategoryRelationship | RESTRICT/no-cascade + read projection (real-PG) | 1 |
| Templates/nodes/aliases created only in the editor; no create-from-real-door, no cross-door copy | DoorTemplate aggregate | absence gate (source/architecture) + negative API tests | 2 |
| Self/descendant reparent rejected; parent chain validated under the transaction | TemplateNode | tree guards → `abwab.template_cycle` | 2 |
| Valid reparent updates sibling order atomically and bumps `TemplateRevision` exactly once | TemplateNode | tracked atomic rewrite + bump-count test | 2 |
| Stale/concurrent reparent/reorder rejected | TemplateNode | expected `TemplateRevision`/`xmin` → `abwab.template_revision_stale` / `abwab.row_stale` | 2 |
| Cyclic template cannot be saved, applied, rendered, or restored | DoorTemplate aggregate | validation + cyclic-restore test | 2 |
| Alias remove/restore tracked; alias history never lost | TemplateNodeSearchAlias | soft-delete + adapter round-trip | 2 |
| Application creates roots as direct children; revalidates uniqueness/protection under the tx | template application | `029` writer inside one transaction | 2 |
| Application = one ChangeSet + one `TreeRevision` bump | template application | audited-operation + bump-count test | 2 |
| Copy allowlist only; no link/highlight/note/request/source/workflow/audit/technical copying | template application | allowlist + per-family negative tests | 2 |
| `template.add` \| `edit` \| `delete` \| `restore` \| `apply` ownership frozen; no borrowed verb | DoorTemplate aggregate | handler/source/parity partial-grant matrix | 2 |
| Frozen template snapshot at application time; template CRUD only in the separate history view | audit payloads | §6.3 render payload tests | 2 |
| Exactly 2 new adapters; interpreter adds 0 entries; duplicate/missing fails CI | restore registry | extended §8 registry test | 1 + 2 |
| No writer bypasses audit/protection/concurrency/stabilization | both | barrier/guard + stabilization tests | 1 + 2 |
| No Quran FK introduced | TemplateNode | `NoPrematureQuranFkTests` stays green | 2 |
| Mock/HTTP parity, explicit actions, no-drag, RTL, context preservation | frontend slices | Vitest parity + Playwright/source gates | 1 + 2 |
