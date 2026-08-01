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
