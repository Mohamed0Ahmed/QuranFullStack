# Phase 1 — migration and backfill evidence

> **Retained evidence — 2026-08-04.** This file survived the planning-artifact lifecycle sweep
> because a live document cites it as the only existing check for the work it records
> (it is the only record of the hand-appended `migrationBuilder.Sql(...)` backfill, a documented deviation from the no-hand-written-migrations rule). Its sibling `plan.md` and the other slice folders it references were swept and
> resolve in **git history** only. The citations are left as written: this is a dated record of
> what was verified and when, and repointing it at today's READMEs would falsify it.

Plan: `docs/feature-abwab-global-order/plan.md` §7 Phase 1 (T101–T105).

## Migration

- Name: `20260729105806_AddAbwabGlobalOrderValue`
- Generated files:
  - `Backend/infrastructure/QuranDashboard.Infrastructure/Migrations/20260729105806_AddAbwabGlobalOrderValue.cs`
  - `Backend/infrastructure/QuranDashboard.Infrastructure/Migrations/20260729105806_AddAbwabGlobalOrderValue.Designer.cs`
  - `Backend/infrastructure/QuranDashboard.Infrastructure/Migrations/QuranDashboardDbContextModelSnapshot.cs` (updated)
- Generated via `Backend/scripts/add-mig AddAbwabGlobalOrderValue` (EF tooling only). One
  `migrationBuilder.Sql(...)` call appended to the generated `Up()` for the backfill — the
  documented deviation in plan §7 T103; no `.Designer.cs`/snapshot hand-edits.
- Build: `dotnet build Backend/QuranDashboard.sln` — succeeded, 0 warnings, 0 errors (after
  generation and again after appending the backfill SQL).
- `dotnet ef database update` — **applied, local dev DB only** (`localhost:5432/quran_dashboard`),
  via `Backend/scripts/update-db`, on explicit user go-ahead.

## Backfill evidence (T105, R3)

Real-run capture against the local dev DB — a live root order comparison, not a test (the
Testcontainers-backed `AbwabSchemaFixture` starts from an empty schema, where the backfill is a
no-op and proves nothing).

**Before** (`ORDER BY order_value, id` — today's render order, `abwab-tree.builder.ts:5-7`):

| id | section_id | order_value |
|---|---|---|
| 336 | (none) | 1 |
| 337 | 217 | 1 |

**After** (`ORDER BY global_order_value, id` — the new superset order):

| id | section_id | global_order_value |
|---|---|---|
| 336 | (none) | 1 |
| 337 | 217 | 2 |

Root id sequence: `336, 337` before and after — unchanged. The superset renders identically on
first load, per plan §2/§7 T105.

Captured immediately before applying the migration (plan R3: a dev DB write between capture and
apply would invalidate this evidence; none occurred here).
