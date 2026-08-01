# Slice F — Evidence

Plan: `docs/feature-ux-slice-f/plan.md`. Branch: `ux-slice-f-sections`, off `dev` @ `7b0e8fba`.

## T101 — Baseline (dev @ `7b0e8fba`, clean)

| Check | Command | Result |
|---|---|---|
| Backend build | `dotnet build Backend/QuranDashboard.sln` | Build succeeded, 0 warnings, 0 errors, 35.8s |
| No-pipeline regression | `dotnet test … --filter "...!~QuranDashboard.Tests.Smoke."` (§5 catalog) | 1086 passed, 0 failed, 0 skipped, 22s |
| Route-smoke tier | `dotnet test … --filter "FullyQualifiedName~QuranDashboard.Tests.Smoke."` | 140 passed, 0 failed, 0 skipped, 49s |
| **`Tests.Smoke.Data` — RAN** | `dotnet test … --filter "FullyQualifiedName~QuranDashboard.Tests.Smoke.Data"` | 13 passed (subset of the 140 above). `resources/db-dumps/quran-canonical/quran-canonical.dump` is present, so the fixture did not self-skip. |
| `check-api-contract` | `Backend/scripts/check-api-contract` | "API contract up to date." — clean at baseline; `git status --short` empty after the run. Run so a later staleness report can be attributed to this slice's own changes, not pre-existing drift. |
| Frontend tests | `npm test` | 193 files, 2316 tests passed, 0 failed. 202.32s (TESTING_STRATEGY.md's catalog figure of 191 files / 2161 tests is the prior measurement point — both counts have since grown; no contradiction). |
| Frontend build | `npm run build` | Succeeded, 17.9s. Pre-existing budget warnings (initial bundle +69.54 kB over 500 kB budget; two mushaf SCSS files over their 4 kB budget) — none introduced by this slice, carried forward as-is. |

**Baseline verdict:** both stacks green. Nothing outstanding to disentangle from this slice's own changes.

## T102 — Branch and feature record

- Branch `ux-slice-f-sections` created off `dev` @ `7b0e8fba`.
- Plan committed to the branch (not to `dev`).
- Root `CLAUDE.md` "Active Spec Kit Feature" section updated: `ux-slice-e` entry (merged) replaced with `ux-slice-f`. `docs/feature-ux-slice-e/` left untouched — no planning-artifact sweep in this slice (§3).

## Phase 2 — T201-T205 (backend route/writer/wiring/catalog)

- `dotnet build Backend/QuranDashboard.sln` — succeeded, 0 warnings, 0 errors (after retrying past one transient MSB3883 file-lock error from a lingering build-server process, unrelated to this change).
- `Tests.Abwab` — 46 passed, 0 failed (matches plan's precondition count — unchanged, no new backend tests per the rush-period posture).
- `Tests.Api` — 60 passed, 0 failed (matches §5's catalog figure).

## Phase 3 — T301 (route gate)

- `dotnet test … --filter "FullyQualifiedName~QuranDashboard.Tests.Smoke."` — **140 passed, 0 failed, 0 skipped**, 47s. Same total as the T101 baseline: `SmokeCoverageParityTests` asserts both parity directions (every registered route has a catalog entry, every catalog entry maps to a registered route) as a fixed handful of tests over the whole catalog, not one test per route, so an unchanged total together with a passing run is the expected signal that the new `POST api/abwab/sections/{id:int}/order` entry landed correctly in both directions.
- **`Tests.Smoke.Data` — RAN** (subset of the above; same `resources/db-dumps/quran-canonical/` dump as baseline, unchanged).

## Phase 5 — T501-T502 (frontend write path)

- `AbwabSectionsModalComponent` now declares `reorderSection` as a required input (T601's button/editor UI is the consumer; declaring the signature now is what T501's "bound into the modal" step requires — Angular's `strictTemplates` rejects a binding to an unknown property, so the input had to land in this phase, not deferred whole to Phase 6). The existing 15-case modal spec's shared `render()` helper got a matching stub so no existing case broke.
- Focused glob `npm test -- --include="src/app/features/abwab/**/*.spec.ts"` — **371 passed** (366 baseline-for-this-glob + 5 new: 1 in `abwab.api.spec.ts`, 4 in `abwab-write.controller.spec.ts`), 0 failed, 24 files. `abwab.api.spec.ts` asserts the POST verb, URL, and `{ position, version }` body; `abwab-write.controller.spec.ts` asserts success refreshes the snapshot, 409 maps to `conflict` with the backend message, 400 maps to `invalid`, and the door selection is untouched (a section write invalidates no door selection, matching `renameSection`).
