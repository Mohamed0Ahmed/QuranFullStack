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
  actor+timestamps, soft-delete). This is the third and last adapter for this feature.

## Snapshot exclusions

Snapshots hold product state only. They exclude `xmin`, logical revision counters
(`TreeRevision`, `CategoryContentRevision`), cache state, and realtime cursors — those are
current technical state, never inverse-restored (§6.3, §6.4, §8).

## Acceptance status

Section and Category adapters are versioned and round-trip tested as of Story 1
(`029-abwab-core`). The ManualProtection adapter is versioned and round-trip tested as of
Story 2, and is **accepted here — before any protected category writer exists** (the §18.3
order gate for `029` US2→US3). All three adapters (Section, Category, ManualProtection) are
now registered; the §8 static registry test (Story 3, T082) still needs to run against all
three together before the final `033`-acceptance note is recorded at feature exit.
