# Contract: Audit render payloads (relationships and templates)

**Feature**: `030-abwab-relationships-templates` | **Source**: Master Plan §6.3, §8, §18.4. `030`
defines and publishes the **payload shapes**; the main audit page, pagination, filters, and the audit
read model are `033`'s. Fixture data is synthetic Arabic only — never real Quran text.

## 1. Specialized relationship payload (main product audit)

Relationship add/edit/delete/restore are **direct audited domain operations** and are main-log
eligible. The payload renders:

- relationship **type** and **shape** (mutual pair vs directional edge), with the Broader/Narrower
  **inverse label derived for display** — never a stored second row,
- both endpoints by identity with their **historical section/path at operation time** (immutable) plus
  the live current door name/path/deleted state fetched on open,
- **before/after** product state for an edit — current state on the right, proposed/result on the left,
  changed values marked with colour **plus** a non-colour marker,
- the manual-protection blockers that were effective, where applicable.

Direct structure actions show reviewer **«غير مطلوب»** (§6.3). Actor and action time appear together.

## 2. Template application payload (main product audit)

Template application is main-log eligible and renders (§6.3, §7.4):

- the **template identity** and the **frozen template snapshot at application time**,
- **target and full path**,
- the **complete created tree**,
- **all copied basic fields** (name, representative excerpt, description, aliases, order),
- **counts by level**.

**Later template edits cannot change this rendering** — the snapshot is frozen at application time.

## 3. Separate template-history view (NOT the main log)

Template **CRUD** — manual create, edit, delete, restore — renders in its **separate template-history
view** over the same append-only audit engine, showing actor/time, action, **complete before/after
template trees**, and changed nodes/fields. It produces **no** main product-audit row (§6.3).

## 4. Relationship dormancy inside the `029` subtree payload

A category subtree delete/operation-restore ChangeSet (owned by `029`) renders **dormant
attached-state counts**; `030` supplies the real relationship counts via a read-side
relationship-count projection feeding the generic `dormantDependentCounts` seam of the `029` render
model (`abwab-audit-render.models.ts`) — the stored `029` event payload and the subtree handler are
unchanged. This is a **data contribution
to a `029`-owned payload** required by §6.3 — `030` takes no ownership of that render component and
changes none of its `029` behaviour (spec FR-008). Attached relationships are
labelled **dormant**, never falsely shown as deleted. A category subtree delete writes no relationship
row and therefore produces **no** relationship audit event
([`relationship-dormancy-contract.md`](./relationship-dormancy-contract.md)).

## Rules

- Every event stores `SnapshotSchemaVersion`, entity/aggregate identity, restore class, full product
  before/after state, and historical section/path where applicable; snapshots exclude `xmin`, logical
  revision counters, cache state, and realtime cursors (§6.3).
- Every main-list row exposes the locked columns (sequence, domain, action, actor, status, reviewer,
  details summary, notes summary); an inapplicable value is rendered explicitly, never silently dropped.
- Render components are **presentational only** — no fetching, no audit page, no pagination here.

## Tests

- Payload-shape tests for the relationship payload (all four operations, both shapes, inverse label
  derived) and the template-application payload (frozen snapshot, copied fields, level counts).
- Freeze test: editing the template after application leaves the rendered application detail unchanged.
- Separation test: template CRUD appears **only** in the template-history view — **0** main
  product-audit rows.
- Dormancy test: the `029` subtree payload reports relationship dormant counts and **0** relationship
  audit events for the delete.
