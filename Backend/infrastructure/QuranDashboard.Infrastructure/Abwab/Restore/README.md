# Abwab restore adapters

Versioned, round-trip-tested capture/reconstruct adapters for the `029` Abwab category
domain (Master Plan §8 registry). Each adapter is a pure mapping between a persisted
product type and an immutable, schema-versioned snapshot; `029` builds no restore
preview/planner/execution surface — that is `033`'s consumer.

## Registered adapters

- **`SectionRestoreAdapter`** — `Section` (name/normalized/`SortOrder`/permanent-default/
  soft-delete). Section order (`SortOrder`) round-trips as a plain field on this adapter,
  not a separate registration.
- **`CategoryRestoreAdapter`** — the whole Category aggregate (`Category` +
  `CategorySearchAlias`), via `CategoryAggregate`. `SiblingOrder`/`SectionOrder`/
  `GlobalOrder` round-trip as facets of this one adapter, never as a standalone "Order"
  adapter (§8: one adapter per persisted type; a standalone Order registration is a
  duplicate and must fail CI).
- **`ManualProtectionRestoreAdapter`** — `ManualProtection` (type/scope, applied/lifted
  actor+timestamps, soft-delete). This is the third and last adapter for `029`.
- **`RelationshipRestoreAdapter`** (`030`) — `CategoryRelationship` (type, the mutual pair in
  canonical order **or** the directional pair, soft-delete state). A snapshot carries **no dormancy
  state**: dormancy is a read-side projection over untouched rows, so restoring the endpoint category
  alone re-exposes the relationship. Reconstruct respects the writer's invariants — reactivating a
  row whose canonical pair/edge is active again fails on the filtered unique index rather than
  persisting a second active row.

## Snapshot exclusions

Snapshots hold product state only. They exclude `xmin`, logical revision counters
(`TreeRevision`, `CategoryContentRevision`), cache state, and realtime cursors — those are
current technical state, never inverse-restored (§6.3, §6.4, §8).

## Acceptance status

Section and Category adapters are versioned and round-trip tested as of Story 1
(`029-abwab-core`). The ManualProtection adapter is versioned and round-trip tested as of
Story 2, and is **accepted here — before any protected category writer exists** (the §18.3
order gate for `029` US2→US3).

**Story 3 (T062) acceptance note**: the Category adapter's snapshot now also round-trips
`DeletionOperationId` — the correlation field the US3 subtree-delete/operation-restore
handlers stamp on every category soft-deleted by the same atomic operation, cleared back to
`null` on restore (still excluded: `xmin`, `TreeRevision`, `CategoryContentRevision`, cache
state, realtime cursors). All **three** registered adapters — **Section**, **Category**
(aggregate: Category + CategorySearchAlias + content + hierarchy/ancestry + all three orders
+ subtree delete/operation-restore + ordinary-protection actor/time), and
**ManualProtection** — are versioned, round-trip tested, and **accepted for `033`**. The
static §8 registry test (`Backend/tests/QuranDashboard.Tests/Abwab/RestoreAdapters/RestoreRegistryTests.cs`,
T082) asserts the DI-registered adapter set is exactly `{Section, Category, ManualProtection}`
and fails on a missing registration or a duplicate/standalone `Order` registration — Order
remains a **facet** of Section (`SortOrder`) and Category (`SiblingOrder`/`SectionOrder`/
`GlobalOrder`), never a fourth registration.

**`030` US1 (T031) acceptance note**: the **Relationship** adapter is versioned, round-trip tested
(both shapes, soft-deleted state, duplicate-collision rejection) and **accepted for `033`**. The §8
registry test's expected set is now exactly `{Section, Category, ManualProtection, Relationship}`;
`030`'s US2 adds `DoorTemplate` and T077 finalizes the gate. Relationship endpoints are **not** a
registered persisted type — they are ordinary `Category` rows already covered by the Category
adapter, so a relationship-endpoint adapter would be a §8 duplicate and must fail CI.
