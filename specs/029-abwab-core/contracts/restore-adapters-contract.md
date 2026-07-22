# Contract: Versioned restore adapters (accepted for `033`)

**Feature**: `029-abwab-core` | **Source**: Master Plan §8 (registry — governs), §18.3 (steps 1–3 +
exit), §6.3, §7.1. This feature **produces and accepts** the adapters; `033` **consumes** them for
its restore read model, planner, and execution. `029` builds no restore preview/planner/execution
surface.

## Registered adapters — exactly THREE (§8 governs)

The §8 registry is **keyed by persisted aggregate/type**, and "**duplicate as well as missing
registrations fail CI**". `029` therefore registers **exactly three** reversible adapters. Order is
**not** a fourth registration — it is a **tested facet inside the Category adapter** (`SiblingOrder`/
`SectionOrder`/`GlobalOrder`), with **section order tested inside the Section adapter**. A standalone
"Order" adapter would be a §8 **duplicate registration** and must fail CI at `033` entry.

> Where §18.3-exit and the §8 entry checklist enumerate "Section/Category/**Order**/ManualProtection",
> "Order" names the **round-trip that must pass**, not a separate registration. §8's registry table —
> "**One Category aggregate/deletion/order adapter**" — governs the count.

| Registered adapter | Accepted in | Round-trips (incl. facets) |
|---|---|---|
| **Section** | Story 1 | Section snapshot (name/normalized/`SortOrder`/permanent-default/soft-delete) — **section-order facet** included |
| **Category** (aggregate: Category, CategorySearchAlias, content, hierarchy/ancestry, **all three orders**, subtree soft-delete/operation-restore, ordinary-protection actor/time) | Story 1 | Category snapshot (name/excerpt/description/parent/section, `AncestorIds`/`Depth`, ordinary-protection fields, soft-delete); **order facet** (`SiblingOrder`/`SectionOrder`/`GlobalOrder` + one-`TreeRevision` semantics); subtree delete/operation-restore round-trips |
| **ManualProtection** | Story 2 (**before protected writers**) | one-active-per-`(CategoryId,type)`, scope, actor/time, active/soft-delete |

## Rules

- Each adapter is **versioned** and **round-trip tested** (write → snapshot → reconstruct →
  equality on product state). **Exactly one adapter per persisted type** (§8); no standalone Order
  registration.
- Snapshots store `SnapshotSchemaVersion`, entity/aggregate identity, restore class, full product
  before/after state, and historical section/path where applicable; they **exclude** `xmin`, logical
  revision counters (`TreeRevision`, `CategoryContentRevision`), cache state, and realtime cursors
  (§6.3, §6.4, §8).
- Technical counters (`TreeRevision`/`CategoryContentRevision`/`AuditHeadSequence`/`TimelineGeneration`)
  are **current state, not inverse-restored** (§6.4, §7.1); a rollback leaves them unchanged.
- At the feature exit, **all three adapters are marked accepted for `033`**; `033` may add non-root
  timeline boundaries and the restore transaction, which are **not** built here.

## Tests

- Versioned round-trip for each adapter; the **order facet** (all three orders + one-`TreeRevision`
  semantics) and subtree delete/operation-restore round-trip **within the Category adapter** (and
  section order within the Section adapter), with conflict rollback.
- Registration gate: a **static metadata/registry test maps every persisted `029` type to exactly
  one restore class and one adapter — a standalone Order (duplicate) or a missing registration fails
  CI** (§8).
- Order gate: the ManualProtection adapter acceptance **precedes** any protected category writer.
- Acceptance marker present for all **three** adapters at exit.
