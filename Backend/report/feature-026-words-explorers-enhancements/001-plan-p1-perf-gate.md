# Feature 026 — plan-P1 perf gate & acceptance evidence (T020 / T023)

**Date**: 2026-07-14 · **Scope**: Word Types parity (US1 search, US2 1000-row list, US3 100-item
details) · **Budget**: p95 ≤ 2 s per uncached list read at the default 1000-row page size
(decision-record stop condition 4; feature 019's populate target).

## Environment

- API: `QuranDashboard.Api` (Development, HTTP `localhost:5014`), local run.
- Database: full local `quran_dashboard` PostgreSQL — 77,432 `quran_word_morphology` rows,
  21,294 `quran_words_unique_tashkeel` (all with `search_text_normalized` populated),
  11,843 `quran_stems`, 1,642 `quran_roots`.
- Method: `GET /api/words/word-types/table`, 10 uncached samples per scope (distinct cache
  keys via `pageSize` 991–1000, then a fresh 981–990 set once the process was warm; query
  cost is effectively identical across that range). Timings are end-to-end HTTP
  (`curl %{time_total}`).

## T020 — timing results

| Scope | Samples | Median | p95 | Max | Verdict |
|---|---|---|---|---|---|
| `type=verb&tableView=words` (8,544 rows), first set incl. process cold start | 10 | 0.340 s | 1.193 s | 1.193 s | PASS |
| `type=verb&tableView=words`, steady-state set | 10 | 0.293 s | 0.349 s | 0.349 s | PASS |
| `type=verb&tableView=stems` (grouped before pagination, ~11.8k stems), first set | 10 | 0.166 s | 0.231 s | 0.231 s | PASS |
| `type=verb&tableView=stems`, steady-state set | 10 | 0.155 s | 0.238 s | 0.238 s | PASS |
| `type=noun&tableView=words` (widest scope, reference, 3 samples) | 3 | 0.834 s | ~0.835 s | 0.835 s | PASS |
| Warm cache-hit re-read (`pageSize=1000`, words / stems) | 2 | — | — | 0.007 s / 0.002 s | — |

The single 1.19 s outlier is the first EF query of a fresh process (JIT/plan/pool warmup);
every other uncached sample, including the widest `noun` scope, sits far under the 2 s budget.
**Gate: PASS — stop condition 4 not triggered.**

## Cache-entry growth observation

Each distinct `scope × tableView × sort × page × pageSize` produces one `IMemoryCache` entry
now holding up to 1000 rows instead of 25 (~40× larger per entry), but a scope now spans ~40×
fewer pages, so the entry count per browsed scope drops proportionally. Entry options are
unchanged (`WordTypesCacheEntryOptions.PagedRows()`, 15-minute absolute expiration) and remain
tunable without contract change.

## UI scroll note (T018 / SC-003)

`word-types-table` adopted `CdkVirtualScrollViewport` mirroring the proven `roots-table`
pattern (`useVirtualScroll = HAS_RESIZE_OBSERVER` guard, desktop/mobile row-height sync), so
only the visible window of a 1000-row page is in the DOM — the same mechanism that keeps the
four already-1000-row explorers smooth. Production (AOT) build green; component spec pins the
rows rendering inside a stable `role="rowgroup"` body in both branches. A live pointer-scroll
check was not driven in this headless environment.

## T023 — 100-item detail sanity

Detail defaults verified end-to-end against the full DB: grouped member-words default
`pageSize=100` (response `pageSize` = 100), cap still 100 (`pageSize=101` → 400). Word-ayah
pages at 100 ayahs (~1,500 word spans) reuse the existing ayah-matches list unchanged — no
render issue observed via the API payloads; virtualized list rendering was not needed for the
detail panel (unchanged component).

## End-to-end smoke (real DB, plan-P1 endpoints)

| Check | Result |
|---|---|
| `type=verb&tableView=words` unscoped total | 8,544 |
| same + `search=كان` | 22 (narrowed; identity-text match) |
| `/table?type=noun&pageSize=1000` | 200 |
| `/table?type=noun&pageSize=1001` | 400 (`WordTypesInvalidPaging`) |
| `/words?type=noun` default | `pageSize=1000` |
| grouped words `pageSize=100` / `101` | 200 / 400 |
| grouped words default | `pageSize=100` |
| 65-char search | 400 (`WordTypesInvalidFilter`) |
