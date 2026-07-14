# Feature 026 — plan-P4 perf gate & acceptance evidence (T060)

**Date**: 2026-07-14 · **Scope**: Word Types scoped four-count summary (US8) ·
**Budget**: p95 ≤ 2 s per **uncached** `/scope-counts` call on the widest scopes; the read must be
**exactly one SQL command**.

## Environment

- Database: full local `quran_dashboard` PostgreSQL — 77,432 `quran_word_morphology` rows,
  21,294 `quran_words_unique_tashkeel`, 11,843 `quran_stems`, 1,642 `quran_roots`,
  4,817 `quran_lemmas` (production-scale).
- Method: the scope-counts read is served by `EfWordTypesReader.GetScopeCountsAsync` (one SQL command —
  a single CTE over the shared scoped `BaseRowsSql` base, then four `COUNT(DISTINCT …)` aggregates). The
  exact emitted command was reconstructed from `BaseRowsSql` + `ScopeCountsSql` and timed directly with
  `psql \timing` — **10 samples per scope**, warm server (Postgres caches plans/buffers, never result
  sets, so every sample re-executes the aggregate uncached). The `/scope-counts` cache key is keyed by the
  full scope and nothing view/page, so end-to-end HTTP only yields one uncached sample per scope per
  process; SQL-layer timing is the decisive, repeatable uncached-cost measurement of the single command.

## T060 — timing results (uncached single command)

| Scope (unscoped, empty search) | Result counts (words / roots / stems / lemmas) | Samples | Median | p95 | Max | Verdict |
|---|---|---|---|---|---|---|
| `type=noun` (widest noun scope) | 12,364 / 1,407 / 6,968 / 3,301 | 10 | 232.4 ms | **241.5 ms** | 241.5 ms | PASS |
| `type=verb` | 8,544 / 943 / 4,780 / 1,474 | 10 | 95.2 ms | **106.5 ms** | 106.5 ms | PASS |

Both widest scopes sit an order of magnitude under the 2 s budget (worst p95 = 241.5 ms).
**Gate: PASS.**

## Single-command budget (1 SQL command)

Pinned by `WordTypesScopeCountsReadTests.ScopeCounts_UsesSingleSqlCommand` (asserts the
`SqlCommandCountInterceptor` records exactly **1** command for an uncached
`GetScopeCountsAsync` call, including a scope with search + a presence flag active). Green.

## Count-family & equality (correctness alongside perf)

- **Count family**: `ScopeCountsSql_UsesScopedCountFamily_NeverWordsCount` inspects the emitted SQL and
  confirms it uses `COUNT(DISTINCT …)` over the scoped base and contains no `words_count` — the scoped
  word-context family only, never the global aggregate family. Green.
- **Equality (real data)**: the `type=verb` uncached `WordsCount` = **8,544**, identical to the
  `type=verb&tableView=words` unscoped `TotalCount` recorded in `001-plan-p1-perf-gate.md` — a full-DB
  cross-check of the FR-016 equality contract. Equality across the full scope matrix (each main type, a
  child, case/tense/voice variants, ±search, ±presence flags — 18 cases) is pinned by
  `ScopeCounts_EqualEveryTableViewTotal_ForIdenticalScope` against the Testcontainers seed. Green.
- A valid zero-row scope returns an all-zero DTO; an invalid scope is a controlled 400 `InvalidFilter`
  (`ScopeCounts_ZeroRowValidScope_ReturnsAllZeros`, `ScopeCountsHandler_InvalidFilter_ReturnsControlledOutcome`).

## End-to-end deploy-smoke (live API, full local DB) — plan-P4 checkpoint

Run after review via the repo launch profile (`dotnet run --project Backend/api/QuranDashboard.Api
--launch-profile http --no-build`, content root = project dir so appsettings + user-secrets load; the
earlier DLL-from-bin startup failure was a content-root artifact, no config was modified). End-to-end
`curl %{time_total}` against `http://localhost:5014`:

| Check | Result |
|---|---|
| `/api/health` | 200, `database: healthy` |
| `/scope-counts?type=noun` (uncached, fresh process incl. EF warmup) | 200 in **0.821 s** — `12364/1407/6968/3301` |
| `/scope-counts?type=noun` (cached re-read) | 200 in **0.005 s** |
| `/scope-counts?type=verb` (uncached) | 200 in **0.116 s** — `8544/943/4780/1474` |
| zero-row valid scope (`search` matching nothing) | 200 all-zeros |
| invalid scope (`type=bogus`) | 400 `WordTypesInvalidFilter` (Arabic message) |
| 65-char search | 400 |
| composition (`search=كلم&hasRoot=true`) | 200 — `33/3/15/5` |

**Live FR-016 equality**: the four `/table?tableView=…&pageSize=1` `totalCount`s equal the strip counts
for BOTH the widest noun scope (12364/1407/6968/3301) and the composed search+flag scope (33/3/15/5).
The live uncached numbers corroborate the SQL-layer gate (worst observed end-to-end uncached call:
0.821 s, cold process — still ~2.4× under the 2 s budget). **Deploy-smoke: PASS.**
