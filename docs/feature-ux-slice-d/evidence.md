# Slice D — evidence

Companion to `plan.md`. Every number here is measured, not estimated.

## T101 — Baseline (before any change)

- Branch: `ux-slice-d-tree`, cut from `dev` at **`a6601a1f`** (clean tree; only
  `docs/feature-ux-slice-d/` untracked).
- Date: 2026-08-01.
- No CI exists (`TESTING_STRATEGY.md` §8) — this local run is the only comparison point
  for T901.

### Full Vitest suite (`npm test`, fork cap `VITEST_MIN_FORKS=1 VITEST_MAX_FORKS=2`)

```
Test Files  193 passed (193)
     Tests  2219 passed (2219)
  Duration  198.28s (transform 6.25s, setup 79.51s, collect 15.59s,
                    tests 61.32s, environment 181.66s, prepare 18.17s)
```

Exit code 0.

### Production build (`npm run build`)

```
Application bundle generation complete. [16.824 seconds]
```

Exit code 0, with three **pre-existing** budget warnings that are baseline noise, not
regressions introduced by this slice:

| Warning | Amount |
|---|---|
| `bundle initial exceeded maximum budget` (500.00 kB) | 569.06 kB (+69.06 kB) |
| `selected-ayah-section.component.scss` (4.00 kB) | 5.85 kB (+1.85 kB) |
| `selected-word-section.component.scss` (4.00 kB) | 4.65 kB (+649 B) |

Relevant lazy chunk baselines (raw / transfer):

| Chunk | Raw | Transfer |
|---|---|---|
| `abwab-page-component` | 103.34 kB | 18.19 kB |
| `abwab-templates-page-component` | 46.45 kB | 9.08 kB |

### Environment available for the browser/backend-dependent tasks

Checked at Phase 1 so the plan's browser tasks (T201/T202, T303, T502, T602, T804, T902)
are not silently downgraded:

| Prerequisite | State |
|---|---|
| PostgreSQL | up on `localhost:5432`, DB `quran_dashboard` present |
| Backend user secret (`ConnectionStrings:QuranDashboardDb`) | configured (`Password=123456`) |
| `dotnet` / `psql` / `docker` | all on PATH |
| Abwab data | 13 live doors, 343 total (330 archived), 46 relations, 166 sections |
| Browser automation | Chrome MCP + Playwright available |

So Phase 2's DevTools method and Phase 3's live reproduction are both runnable as
specified.
