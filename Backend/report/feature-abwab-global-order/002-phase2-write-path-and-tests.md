# Phase 2 — write path, scoped reorder: evidence

Plan: `docs/feature-abwab-global-order/plan.md` §7 Phase 2 (T201–T208).

## Build

`dotnet build Backend/QuranDashboard.sln` — succeeded, 0 warnings, 0 errors.

## Tests

```
dotnet test … --filter "FullyQualifiedName~QuranDashboard.Tests.Abwab."   → 46 passed (38 + 8 new §5.1 writer tests)
dotnet test … --filter "FullyQualifiedName~QuranDashboard.Tests.Api"      → 60 passed
dotnet test … --filter "FullyQualifiedName~QuranDashboard.Tests.Smoke."   → 140 total, 127 passed, 13 failed
```

The 13 failures are entirely inside `QuranDashboard.Tests.Smoke.Data` (the canonical-Quran-dump
tier). Excluding that namespace:

```
dotnet test … --filter "FullyQualifiedName~QuranDashboard.Tests.Smoke.&FullyQualifiedName!~QuranDashboard.Tests.Smoke.Data."
→ 127 passed, 0 failed
```

**`Tests.Smoke.Data` ran (dump present) and failed — stale, not skipped. UNRESOLVED, decision
pending.** Phase 1's migration (`20260729105806_AddAbwabGlobalOrderValue`) moved the schema's
migration head past the canonical dump's recorded one
(`20260728144026_AddAbwabDoorsAndSections`), and `SmokeDumpGate` fails loud on a migration-head
mismatch rather than silently restoring mismatched data. `TESTING_STRATEGY.md` §3 names exactly two
valid smoke-evidence forms — "134 passed, 0 skipped" and "121 passed, data tier skipped" — and
today's "127 passed, 13 failed stale" is neither. T505 (phase 5) also requires this tier's count
**re-measured**, which cannot happen against a stale dump. Two ways to resolve, both requiring the
user's go-ahead before either runs: regenerate via `Backend/scripts/create-smoke-dump --yes`
(slow, writes into gitignored `resources/`), or move the dump directory aside so the tier
self-skips (the other explicitly-valid evidence form). Neither has been run.

Every non-data smoke test passes, including the 8 new ones this phase added: `Global` reorder 200
(with the actual `GlobalOrderValue` swap asserted, not just the 200), out-of-range `Global`
position 400, `Global` on a nested door 400, missing/unknown `scope` 400, and stale-version 409 for
both scopes.

## What changed

- `AbwabReorderScope { Section = 1, Global = 2 }` — new. `POST {id}/order` body gains a required
  `scope`; an absent or unmapped value is refused (`Enum.IsDefined` guard in the controller), never
  silently treated as `Section`.
- `EfAbwabDoorsWriter`: `ReorderAsync` branches by scope; every root-membership-changing write
  (`CreateAsync`, `MoveAsync`, `BulkMoveAsync`, `DeleteAsync`, `BulkArchiveAsync`, `RestoreAsync`)
  now also maintains `GlobalOrderValue` per plan §5.1's matrix. A root→root move across sections
  and a `Section`-scoped reorder never touch it — the feature's whole point.
- `AbwabDoorDto` / `AbwabTreeDoorDto` gain `GlobalOrderValue`; `EfAbwabTreeReader`'s own `ORDER BY`
  is unchanged (still scope-ordered; the client sorts).
- `SmokeRouteCatalog`: no new entry (route path unchanged) — the `/order` entry's comment now
  records the body change; `DerivedStatus` re-confirmed at `404` (ParityOnly routes are never
  dispatched by the sweep, so this is documentation, not an assertion the sweep runs).

## Not yet done

Phases 3–5 (contract regeneration, frontend, e2e/docs/evidence) are unstarted.
