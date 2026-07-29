# Testing debt

Deliberately skipped test coverage, and what pays it back.

This file exists because a testing posture can be a legitimate decision but must never be an
invisible one. Every line below names a **concrete future trigger** — a change that will already
be touching that code, at which point writing the missing tests costs almost nothing extra.
"Later" is not a trigger.

**What does not belong here:**

- **`SmokeRouteCatalog` parity entries are not debt-able.** `SmokeCoverageParityTests` fails by
  name when a registered route has no catalog entry, so an entry is a build-level gate, not
  coverage. A route added without one fails the suite; it cannot be deferred into this file.
- Tiers `TESTING_STRATEGY.md` requires. This file records what was *not written*, never a reason
  to skip a run that document mandates.

Rows stay until they are paid. Delete a row when its tests land — do not mark it done.

## abwab-relations (branch `abwab-relations`, 2026-07-29)

Posture: **no new tests in the feature**, by explicit decision. Verification was the existing
suites staying green plus a manual pass over the feature's own interaction checklist. Nothing in
this feature's evidence claims behavioral coverage.

| # | Uncovered area | Where | Pays it |
|---|---|---|---|
| 1 | Backend write behavior — canonical pair ordering (`door_a_id < door_b_id` for all three types), `broader_door_id` direction storage, all-or-nothing multi-target add, self/unknown/archived refusals, soft delete with no revive | `Persistence/Writes/Abwab/EfAbwabRelationsWriter.cs` | The next change to the relations writer, **or** adding a fourth relation type — both have to re-derive these rules anyway |
| 2 | Backend read behavior — the dormancy join (relation visible iff its own `deleted_at` is null **and** both endpoints are live) and `RelationCount`'s live-endpoint-only counting | `Persistence/Reads/Abwab/EfAbwabRelationsReader.cs`, `EfAbwabTreeReader.GetLiveRelationCountsAsync` | The next change to the archive / restore / bulk-archive paths — dormancy rides entirely on those, so a regression there surfaces here first |
| 3 | Relations smoke — the `201` / `204` / `400` / `404` / `409` status and envelope contract of the three routes (all three are catalogued `ParityOnly`, i.e. listed but not dispatched) | `Tests/Smoke/`, `SmokeRouteCatalog.cs` | When write protection lands and `/api/abwab` stops being `Open`: the auth cases force a dispatched test per route regardless |
| 4 | Frontend modal behavior — the four-group derivation, type-switch clearing picks, already-linked disabling per (pair, type), anchor-pick mode's inverted picker and its add-count | `features/abwab/components/abwab-relations-modal/` | The next time the modal changes shape — a component with no spec cannot be refactored safely twice |
| 5 | One e2e flow — add a relation, see the chip on both doors, archive one endpoint, watch the chip and the tree flag vanish, restore, watch them return | `Frontend/quran-dashboard-ui/e2e/` | Same trigger as row 4; this is the only check that would catch dormancy end to end, and it is the §6.3 cell most likely to break silently |

Rows 1 and 2 are the ones with no cover **anywhere** — not a spec, not a smoke case, not an e2e
flow. Row 5 is the single cheapest thing that would cover the most: it crosses the read, the
write, the count, and the flag in one pass.
