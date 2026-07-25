# Contract: Versioned restore adapters and the application-event interpreter (accepted for `033`)

**Feature**: `030-abwab-relationships-templates` | **Source**: Master Plan §8 (registry — governs),
§18.4 (exit/acceptance), §6.3, §6.4, §7.3, §7.4. This feature **produces and accepts** these adapters;
`033` **consumes** them over the direct `030 → 033` edges. `030` builds no restore preview, planner, or
execution surface.

## Registered adapters — exactly TWO new (§8 governs)

The §8 registry is **keyed by persisted aggregate/type**, and "**duplicate as well as missing
registrations fail CI**".

| Registered adapter | Persisted type(s) | Round-trips |
|---|---|---|
| **Relationship** | `CategoryRelationship` | relationship type, mutual pair (canonical order) **or** directional pair, soft-delete state; delete → restore → delete round-trip preserves identity and history |
| **DoorTemplate aggregate** | `DoorTemplate` + `TemplateNode` + `TemplateNodeSearchAlias` | template identity/name/normalized name/description; the full node tree with parent links and explicit `SiblingOrder`; node excerpt/description; **alias history** (active and soft-deleted); aggregate soft-delete state |

`TemplateNode` and `TemplateNodeSearchAlias` are **facets of the one DoorTemplate aggregate adapter**,
never separate registrations (§8: "One Template aggregate adapter `030`").

## The application-event interpreter is NOT an adapter

Template application creates **ordinary Category aggregate rows**, so its inversion is performed by the
**single `029` `CategoryRestoreAdapter`** (§8: "it is not a second 'template-created category'
adapter"). `030` therefore ships a **versioned application-event interpreter** that:

- maps a template-application event to real-category inversion executed by the **existing** Category
  adapter,
- is registered as an **event-kind interpreter**, **not** as an `IAbwabRestoreAdapterDescriptor`,
- adds **zero** entries to the adapter registry, and
- has its reuse of the Category adapter **verified**, not merely asserted in prose.

## Registry gate (extends the `029` test)

The static §8 registry test is extended so the DI-registered adapter set is exactly:

```text
{ Section, Category, ManualProtection, Relationship, DoorTemplate }
```

It **fails CI** on:

- a missing registration (either new adapter absent),
- a duplicate registration for an already-registered persisted type — in particular a second,
  "template-created category" adapter, or a standalone `TemplateNode` / `TemplateNodeSearchAlias` /
  relationship-endpoint adapter,
- the application-event interpreter being registered as an adapter descriptor.

## Rules

- Each adapter is **versioned** (`SnapshotSchemaVersion`) and **round-trip tested** (write → snapshot →
  reconstruct → equality on product state).
- Snapshots store product state only. They **exclude** `xmin`, logical revision counters
  (`TemplateRevision`, `TreeRevision`), cache state, and realtime cursors (§6.3, §6.4, §8) — those are
  current technical state, never inverse-restored.
- Both new persisted types are **Reversible product state** in §8; neither has a "no adapter" class.
- Reconstruct paths must respect the same invariants as the writers: a reconstruction that would
  produce a duplicate active relationship, or a cyclic template, **fails** rather than persisting an
  invalid row.
- At feature exit, **both** adapters **and** the application-event interpreter (with its verified
  Category-adapter reuse) are **marked accepted for `033`**.

## Tests

- Versioned round-trip per adapter, including the soft-deleted states and the template alias history.
- Cyclic-restore rejection through the DoorTemplate adapter; duplicate-collision rejection through the
  Relationship adapter.
- Interpreter test: a template-application event inverts through the **`029` `CategoryRestoreAdapter`**
  — asserted by observing the Category adapter perform the inversion, with the registry unchanged.
- Extended registry test: exact set assertion plus explicit duplicate/missing failure cases.
- Acceptance marker present for both adapters and the interpreter at exit.
