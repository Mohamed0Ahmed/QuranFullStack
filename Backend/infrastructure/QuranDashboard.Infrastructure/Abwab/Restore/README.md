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

`ManualProtection` is the third and last adapter for this feature; it is added in Story 2
(`029` US2), before any protected category writer exists.

## Snapshot exclusions

Snapshots hold product state only. They exclude `xmin`, logical revision counters
(`TreeRevision`, `CategoryContentRevision`), cache state, and realtime cursors — those are
current technical state, never inverse-restored (§6.3, §6.4, §8).

## Acceptance status

Section and Category adapters are versioned and round-trip tested as of Story 1
(`029-abwab-core`). Full acceptance for `033` (all three adapters, plus the §8 static
registry test) is recorded once Story 3 completes.
