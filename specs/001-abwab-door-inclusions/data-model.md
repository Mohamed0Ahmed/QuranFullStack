# Data Model: Abwab Door Inclusions

**Date**: 2026-08-17
**Spec**: [spec.md](./spec.md)

## Model Overview

```text
AbwabDoor (target) 1 ── * AbwabDoorInclusion * ── 1 AbwabDoor (source)
                              │
                              ├── 1 live internal LinkingSourceContribution
                              │
                              └── * AbwabDoorInclusionUnitSync
                                      ├── 1 source LinkingUnit
                                      └── 0..1 target LinkingUnit
```

The inclusion edge owns synchronization. The source `LinkingUnit.Id` owns occurrence lifetime. The
target clone remains a normal `LinkingUnit`, so existing content reads, projections, metrics, and
Owner link mutations continue to use one public record model.

## AbwabDoorInclusion

Represents one directed edge: `TargetDoorId` includes `SourceDoorId`.

| Field | Store type | Null | Meaning |
| --- | --- | --- | --- |
| `Id` | integer identity | no | Primary key |
| `TargetDoorId` | integer | no | Aggregate door receiving synchronized records |
| `SourceDoorId` | integer | no | Included door supplying records |
| `CreatedAt` | timestamptz | no | Creation audit time |
| `CreatedBy` | integer | yes | Creating actor when available |
| `UpdatedAt` | timestamptz | no | Last edge audit time |
| `UpdatedBy` | integer | yes | Last updating actor when available |
| `DeletedAt` | timestamptz | yes | Detach time; null while active |
| `DeletedBy` | integer | yes | Detaching actor when available |
| `Version` | PostgreSQL `xmin` | no | EF concurrency token |

### Relationships and constraints

- Restrictive foreign key from `TargetDoorId` to `abwab_doors.id`.
- Restrictive foreign key from `SourceDoorId` to `abwab_doors.id`.
- Check: `target_door_id <> source_door_id`.
- Filtered unique index on `(target_door_id, source_door_id)` where `deleted_at IS NULL`.
- Index on `source_door_id` for reverse consumer traversal.
- Index on `deleted_at` for active-edge filtering.
- Exactly one live internal `LinkingSourceContribution` references each live inclusion.

### Lifecycle

```text
Absent ── add/initial sync succeeds ──> Active
Active ── detach succeeds ────────────> Detached (soft-deleted edge)
Detached ── later add succeeds ───────> New Active edge with new Id
```

- Target/source archive does not detach an active edge.
- A detached edge never reactivates; reattachment creates a new edge and fresh state.
- Physical door deletion remains out of scope and restrictive foreign keys fail closed.

## AbwabDoorInclusionUnitSync

Maps one source record occurrence through one direct inclusion to its materialized target clone or
local suppression.

| Field | Store type | Null | Meaning |
| --- | --- | --- | --- |
| `Id` | bigint identity | no | Primary key |
| `DoorInclusionId` | integer | no | Owning direct inclusion |
| `SourceUnitId` | bigint | no | Source occurrence identity (`LinkingUnit.Id`) |
| `TargetUnitId` | bigint | yes | Materialized target clone; null only when suppressed |
| `State` | persisted enum | no | `Active`, `Overridden`, or `Suppressed` |
| `SourceFingerprint` | bytea | no | Hash of last observed canonical source shape |
| `CreatedAt` | timestamptz | no | Mapping creation audit time |
| `CreatedBy` | integer | yes | Actor when available |
| `UpdatedAt` | timestamptz | no | Last synchronization audit time |
| `UpdatedBy` | integer | yes | Actor when available |

### Relationships and constraints

- Restrictive foreign key to `abwab_door_inclusions.id`.
- Restrictive foreign key to source `linking_units.id`.
- Restrictive nullable foreign key to target `linking_units.id`.
- Unique key on `(door_inclusion_id, source_unit_id)`.
- Filtered unique index on `target_unit_id` where it is not null.
- Index on `source_unit_id` for propagation and pre-delete reconciliation.
- Check constraint:
  - `Active` and `Overridden` require `target_unit_id IS NOT NULL`.
  - `Suppressed` requires `target_unit_id IS NULL`.
- The row is removed physically when its source occurrence ends or its inclusion detaches.
- A suppressed row survives every edit to the same source occurrence.

## AbwabDoorInclusionSyncState

| State | Target unit | Source edit | Source delete | Inclusion detach |
| --- | --- | --- | --- | --- |
| `Active` | required | Replace clone shape and fingerprint; propagate target edit | Remove clone and mapping; propagate target delete | Remove clone and mapping |
| `Overridden` | required | Update observed fingerprint/audit only; do not overwrite target | Remove overridden clone and mapping; propagate delete | Remove clone and mapping |
| `Suppressed` | null | Update observed fingerprint/audit only; do not create target | Remove mapping | Remove mapping |

### Target-local transitions

```text
Active ── target selected-word edit ──> Overridden
Active ── target delete ──────────────> Suppressed
Overridden ── later target delete ────> Suppressed
```

No transition flows from target to source. Source edits never move `Overridden` or `Suppressed`
back to `Active`.

## LinkingSourceContribution Extension

Add nullable `DoorInclusionId`, persisted internal source kind `DoorInclusion`, and conditional
`OperationId` nullability to the existing entity. `DoorInclusion` is added to the Domain
`LinkingSourceKind` enum for persistence and internal branching only; it receives no public token or
descriptor subtype.

### Invariants

- `DoorInclusionId` is required only when kind is `DoorInclusion` and forbidden for every public
  Quran source kind.
- `OperationId` is null only when kind is `DoorInclusion`; every public Quran source kind continues
  to require its existing non-null `LinkingOperation` foreign key.
- An internal inclusion contribution never creates, references, or appears as a synthetic
  `LinkingOperation`.
- Root, lemma, stem, unique-word, and word-type references are null for `DoorInclusion`.
- One live contribution per live inclusion is enforced by an active unique index.
- Existing contribution-to-unit mappings own the inclusion's target clones.
- Before any inclusion can be created, public source tokens, descriptors, workspace bodies,
  prepared-preflight bodies, overlapping-source labels, and content DTOs must reject or omit
  `DoorInclusion`. Internal confirmed-state calculation may retain its contribution units only for
  door ayah/word impact.
- Internal labels/scopes satisfy storage contracts only; they are not Quran-content attribution.

## Existing LinkingUnit Role

### Source occurrence

- `LinkingUnit.Id` is the occurrence identity.
- Selected-word replacement and other same-record edits preserve the ID.
- Deletion ends the occurrence.
- Only a later explicit link creation after committed deletion creates a new occurrence eligible to
  return after suppression.
- Renaming, refreshing, archiving/restoring, reconfirming unchanged content, or an internal row
  replacement does not create a new occurrence.

### Target clone

- Uses the normal `LinkingUnit` structure, ayahs, words, descriptions, and grouped flag.
- Uses a stable internal identity salted by inclusion ID and source unit ID.
- Cannot merge with direct target units or another edge's clone.
- Remains visible through current record reads without origin fields.

## Source Fingerprint

Hash the canonical persisted source shape:

1. grouped flag;
2. ayah IDs in Quran order;
3. selected canonical Quran word IDs per ayah; and
4. ordered descriptions per ayah.

The fingerprint detects same-occurrence edits. It is not a concurrency version, HTTP validator,
user-visible value, or replacement for canonical Quran identities.

## Occurrence Reconciliation Contract

Each source writer supplies one transaction-local mutation set:

```text
addedUnitIds[]
editedUnitIds[]
deletedUnitIds[]
replacements[]: oldUnitId -> newUnitId (one pair per preserved logical occurrence)
```

Rules:

- An ID appears in one category only.
- Each replacement pair denotes one preserved logical occurrence; the complete replacement set must
  be a deterministic bijection across all affected logical occurrences.
- Transfer every ledger row, target unit reference, state, and fingerprint before deleting the old
  unit.
- Content similarity is never sufficient proof of lineage.
- A physical one-to-many or many-to-one reshape may proceed only when the owning writer preserves
  every logical occurrence ID or expresses an equivalent deterministic bijection. A true split or
  merge that cannot represent every prior logical occurrence and transfer its state fails before
  commit rather than weakening suppression/override semantics.
- An unmatched added ID with no predecessor is a fresh occurrence. Content from a previously
  suppressed occurrence becomes eligible again only after that predecessor deletion has committed
  and a later explicit link creation produces the added ID.

## Door-Level Ayah and Word Projection

The maintained projection remains a distinct union over surviving direct and synchronized records:

- One door-ayah row per `(DoorId, AyahId)`.
- One selected-word row per `(DoorAyahId, QuranWordId)`.
- Removing a record removes an ayah/word only when no other live record supplies it.
- Rebuild only affected ayah IDs inside the mutation transaction.
- Existing `LinkCount` and record-based `SelectedWordCount` include materialized clones without a
  second metric mode.

## Graph and Batch Validation

For one target-first add command:

1. Normalize source IDs and reject empty or repeated input.
2. Lock the target and sources in deterministic order after the global synchronization lock.
3. Require a live target and live sources at creation time.
4. Reject the target as its own source.
5. Reject every active duplicate direct edge.
6. Evaluate the active graph plus the entire proposed batch and reject any cycle.
7. Validate the expected target version after acquiring synchronization ownership.
8. Create every edge and its initial synchronization or none.

There is no hard product cap on source count or graph depth. Algorithms must not encode one.

## Archive, Detach, and Reattach

- Source archive preserves edges, mappings, target clones, counts, and target content; archived
  sources cannot be newly selected.
- Target archive preserves edges and state; internal source propagation continues while user
  mutations against the target are rejected.
- Restore creates no record and performs no catch-up duplication.
- Detach removes active/overridden clones, suppressed mappings, internal contribution ownership, and
  the active edge; it rebuilds target projections and propagates target removals.
- Reattach creates a new edge and fresh initial synchronization; retired suppressions/overrides do
  not carry forward.

## Deletion Order

Before deleting a source occurrence:

1. Load all sync mappings for it.
2. Recursively remove downstream clones for `Active`/`Overridden` mappings.
3. Remove contribution-unit ownership and target clones.
4. Remove all mappings, including `Suppressed`.
5. Rebuild affected target projections and advance changed door versions.
6. Continue the original source-unit deletion.

Restrictive foreign keys enforce this ordering and prevent orphaned synchronization state.

## Migration and Backfill

- Required migration name: `AddAbwabDoorInclusionSynchronization`.
- Generate only through `Backend/scripts/add-mig` after explicit authorization.
- Never hand-edit the migration, designer, or model snapshot.
- Applying the migration requires separate database-update authorization.
- Existing contribution rows retain non-null `OperationId` and receive null `DoorInclusionId`.
- The `operation_id` column becomes nullable, with a coherence check requiring null only for
  `DoorInclusion` and non-null for every public kind; no synthetic operation row is inserted.
- No inclusion, synchronization, Quran, or other data backfill is required.
- Audit the complete `wipe-abwab` cascade closure before changing its allowlist or fixture resets.
