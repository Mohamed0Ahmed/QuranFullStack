# Feature 026 — Polish-phase validation (T061 / T062 / T063)

**Date**: 2026-07-15 · **Scope**: Phase 11 (Polish & Cross-Cutting) — audit/validation only, NOT feature
work. No feature code changed. **Base**: `951e9a5..89c80cf` (plan-P1 `5d3ae0f`, plan-P2 `d20912c`,
plan-P3 `a187edf`, plan-P4 `89c80cf`); tree clean at start.

**Verdict up front — all three audits PASS:**

| Task | Result |
|---|---|
| T061 count-family audit (SC-007 / FR-017) | **PASS** — no `words_count`-backed number on any Word Types surface; no scoped word-context count on the four normal explorers' stat line |
| T062 ordering-untouched assertion (SC-008) | **PASS** — ordering tests green; every feature filter predicate is a pure `Where`; no `OrderBy` added or moved that changes list ordering |
| T063 full quickstart §P1–§P4 + suites + self-checks | **PASS** — backend 1271/1271, frontend 1113/1113, prod build green, live smoke clean, perf gates recorded |

---

## T061 — Cross-phase count-family audit — PASS

The invariant (spec FR-017 / SC-007): the **scoped word-context count family** (Word Types) and the
**global whole-Quran aggregate family** (`quran_roots.words_count` & friends, surfaced on the
Roots/Lemmas/Stems explorers) must never appear mixed on one surface.

**What was checked and the result:**

- **Backend Word Types surfaces** — `EfWordTypesReader.Sql.cs`, `.ScopeCounts.cs`, `.GroupedDetails.Sql.cs`,
  and `Caching/.../WordTypes/`: the only literal `words_count` in the entire Word Types read/cache tree is a
  **comment** in `EfWordTypesReader.ScopeCounts.cs:13` stating the counts are *never* a global
  `words_count`-backed aggregate. Every Word Types count is a scoped `COUNT(*)` / `COUNT(DISTINCT …)` over
  the shared `BaseRowsSql` occurrence base: words = `COUNT(DISTINCT (tashkeel_word_id, context_code))`
  (`RowsCountSql` formula); roots/stems/lemmas = `COUNT(DISTINCT <dim>_id)` (`GroupedRowsCountSql` formula).
  The scope-counts DTO field named `WordsCount` is this scoped count, not the global column.
- **Frontend Word Types surface** — `word-types-explorer-page.component.html` mounts only
  `<qd-word-type-scope-counts>` (the scoped strip). It does **not** mount `<qd-explorer-result-count>` and
  contains no `words_count`/`wordsCount` global reference. `wordsCount` (camelCase) appears only in the
  scope-counts DTO / component / specs — the scoped family. The only `words_count` (snake_case) in the whole
  frontend words feature is a README sentence documenting the invariant.
- **Four normal explorers' stat line** — all four pages bind
  `<qd-explorer-result-count [count]="listState().totalCount">` (unique-words / roots / lemmas / stems page
  HTML). The stat is each page's own filtered paged total (FR-014, zero new aggregation) — never a scoped
  Word Types word-context count. Labels name the dimension each page counts:
  عدد الكلمات / عدد الجذور / عدد الصيغ المعجمية / عدد الأصول الصرفية.
- **Scope-counts strip labels** reuse the view tabs' SHORT labels verbatim
  (`WORD_TYPE_TABLE_VIEW_OPTIONS`: كلمات | جذور | أصول | صيغ, same RTL order) — no full-term / short-form
  cross-mixing on the strip (spec Clarification).
- **Machine-checked corroboration** — `WordTypesScopeCountsReadTests.ScopeCountsSql_UsesScopedCountFamily_NeverWordsCount`
  intercepts the emitted SQL and asserts it contains `count(distinct` and **no** `words_count`. Green in the
  full run.

No count-family violation found on any new surface.

---

## T062 — Ordering-untouched assertion — PASS

**Existing ordering tests — all green** (from the full backend run; representative):

- Derivation sorts: `RootsListReadTests` / `LemmasListReadTests` / `StemsListReadTests` —
  `*_applies_each_supported_sort_without_error(alpha|mushaf-order|occurrences)`,
  `*_occurrences_sort_orders_by_count_desc`, `*_mushaf_order_sort_orders_by_first_word_order_in_mushaf`,
  `StemsListReadTests.GetStemsPage_alpha_sort_orders_by_normalized_text_then_identity`.
- Related-item ordering (`MorphologyRelatedItemsOrdering` coverage):
  `MorphologyRelationshipsReadTests.GetLemmaStems_returns_related_stems_in_deterministic_order`,
  `GetStemLemmas_returns_related_lemmas_in_deterministic_order`.
- `WordTypeSort`: `WordTypesMainReadTests.Rows_MushafOrder_UsesFirstOccurrenceOrder_NotTashkeelIdentity`,
  `Rows_ReturnScopedCounts_AndDeterministicFirstOccurrenceOrder`,
  `WordTypesTableReadTests.GroupedViews_Sort_ByAlpha_UsesArabicFoldAndCollation_Deterministically`,
  `GroupedViews_Sort_ByMushafOrder_Deterministically`.

**Inspection of the feature diff (`951e9a5..HEAD`, ordering-relevant files):**

- `MorphologyRelatedItemsOrdering.cs` — **not in the diff** (untouched).
- `RootsListDerivation.cs` / `LemmasListDerivation.cs` / `StemsListDerivation.cs` — the only additions are
  pure `.Where(...)` predicates (`MatchesFilter` count-range; `DominantRootId/DominantLemmaId` association)
  applied **before** the pre-existing sort inside `FilterAndSort`; **no** sort/`OrderBy`/`ThenBy` line was
  added, removed, or moved.
- `EfWordTypesReader.Sql.cs` — `OrderBy(sort)` / `GroupedOrderBy(sort)` unchanged; the diff only adds
  `SearchPredicate` and `PresenceFilterPredicate` `WHERE` fragments on the shared base.
- Unique Words `ApplySort` (`OrderByDescending(OccurrencesCount).ThenBy(FirstWordOrderInMushaf)` /
  `OrderBy(SearchText).ThenBy(...)` / `OrderBy(FirstWordOrderInMushaf)`) was **relocated byte-identically**
  from `EfUniqueWordsReader.cs` into the new `EfUniqueWordsReader.List.cs` partial (a size-driven
  partial-split) — same keys, directions, and tie-breakers; still applied once at the same point in
  `GetUniqueWordsPageAsync`.
- The two new `DISTINCT ON (unique_id) … ORDER BY …` fragments (`PrimaryWordTypeWinnerPredicate` /
  `PrimaryRootWinnerPredicate`) are **winner-selection subqueries feeding an `id IN (…)` predicate** (the US7
  association filter reproducing the displayed-chip's primary-selection rule) — they are part of a narrowing
  `WHERE`, not the list's result ordering.

Every filter predicate this feature adds is a pure `Where`; no result ordering was changed anywhere.

---

## T063 — Full quickstart validation — PASS

### Build & test suites

| Check | Result |
|---|---|
| `dotnet build Backend/QuranDashboard.sln` | **Succeeded** (0 errors; 6 pre-existing CS1573 XML-doc warnings — see self-check) |
| `dotnet test Backend/QuranDashboard.sln` (Testcontainers) | **1271 / 1271 passed**, 0 failed (5m 22s) |
| Frontend `npm test` (worker cap `VITEST_MIN_FORKS=1 VITEST_MAX_FORKS=2`) | **1113 / 1113 passed**, 117 files, 0 failed (130s) |
| Frontend `npm run build` (production AOT) | **Succeeded** (1 pre-existing SCSS-budget warning on `word-type-filter.component.scss`, not in the 026 diff) |

### Live API smoke (quickstart §P1–§P4)

Environment: `dotnet run --project Backend/api/QuranDashboard.Api --launch-profile http --no-build`
(content root = project dir; `http://localhost:5014`, Development). Full local DB confirmed matching the
perf-gate evidence: 21,294 unique-tashkeel / 77,432 morphology / 11,843 stems / 1,642 roots / 4,817 lemmas.
`/api/health` → 200 `database: healthy`. API stopped after the run. No secrets/config modified.

**Smoke matrix (41 effective checks, 0 real defects):**

| Phase | Check | Result |
|---|---|---|
| P1 | `/word-types/table?type=noun&pageSize=1000` | 200, response `pageSize=1000` |
| P1 | `pageSize=1001` | 400 `معطيات التصفح غير صالحة` |
| P1 | `/word-types/words?type=verb` default | `pageSize=1000` |
| P1 | verb words unscoped `totalCount` | 8,544 (== 001 evidence) |
| P1 | + `search=كان` | 22 (narrowed; identity-text match) |
| P1 | grouped `roots/{id}/words` `pageSize=101 / 100 / default` | 400 / 200 (pageSize 100) / default 100 |
| P1 | search on grouped `tableView=roots` under `search=كان` | 2 (roots of matching words only — list scope) |
| P2 | `/unique/tashkeel?occMin=11&occMax=100` | 200, `totalCount` 811 (narrowed from 21,294) |
| P2 | `occMin=5&occMax=2` (direct invalid) | 400 `نطاق التصفية غير صالح` |
| P2 | Word Types `hasRoot` split (particle words) | all 800 = missing 793 + has 7 (tri-state reshapes) |
| P3 | Unique Words `primaryType=N` | 200, 10,348 rows — **every** row's `primaryWordTypeCode == N` (chip⇔filter) |
| P3 | Unique Words `rootId=1` | 200, 52 rows — every row `rootId == 1` |
| P3 | Lemmas `rootId=1` | 200, 6 rows — every lemma `rootId == 1` (belonging) |
| P3 | Stems `rootId=1` | 200 — every stem's primary `rootId == 1` (primary-not-sole) |
| P3 | invalid `primaryType=ZZZZ` / `rootId=-1` | 400 / 400 |
| P3 | valid-but-unmatched `rootId=999999` (unique + lemmas) | 200 empty page, `totalCount=0` (not 404) |
| P4 | **FR-016 equality**, `type=noun` (widest) | scope-counts `12364/1407/6968/3301` == four tab totals |
| P4 | **FR-016 equality**, `type=verb&search=كان&hasRoot=true` | scope-counts `22/2/6/2` == four tab totals |
| P4 | zero-row valid scope (`search=zzzzzzzzzz`) | 200, all-zeros |
| P4 | invalid `type=bogus` / 65-char search | 400 / 400 |

### Perf gates (recorded)

Both formal gates are recorded and PASS in the two evidence files, re-confirmed present:

- `001-plan-p1-perf-gate.md` — P1 `/table` at `pageSize=1000` (verb words / stems), p95 well under the
  2 s budget (worst 1.19 s cold outlier); stop condition 4 not triggered.
- `002-plan-p4-perf-gate.md` — P4 `/scope-counts` single-command, uncached noun p95 241.5 ms / verb 106.5 ms
  (budget ≤ 2 s); 1-SQL-command pinned by `ScopeCounts_UsesSingleSqlCommand`.

Live smoke corroboration (end-to-end `curl` this run): `/scope-counts?type=noun` 0.26 s uncached,
composed verb scope 0.01 s, `/table?type=noun&pageSize=1000` 1.26 s (cold first EF query) — all consistent
with the recorded gates.

### Clean-code self-check (root CLAUDE.md guard) — PASS with one minor nit

Sampled across layers — `CountRange` value object, `GetWordTypeScopeCountsHandler`,
`EfWordTypesReader.ScopeCounts.cs`/`.Sql.cs`, `words-range-filters.ts`, the two new components:

- Naming/functions/formatting: small, single-responsibility units; established feature patterns followed
  (primary-constructor DI, `ArgumentNullException.ThrowIfNull`, controlled outcome types, TDZ label
  getters per the words README).
- DRY/KISS/YAGNI: the scope-counts read **reuses** `RowsCountSql`/`GroupedRowsCountSql`/`BaseRowsSql`
  fragments rather than re-deriving; `CountRange` and `words-range-filters.ts` are shared across the four
  explorers; no speculative surface beyond the locked scope.
- SOLID / layering: params/DTO in Abstractions, validation in Application handlers, SQL in Infrastructure
  readers, thin controllers.
- FR-006 (no search text in logs): the scope-counts and rows/table handlers log only `hasSearch`.
- **Minor nit (non-blocking, NOT fixed — no feature-code changes in Polish):** 6 CS1573 XML-doc warnings —
  `WordTypesController.GetRows`/`GetTable` lack `<param>` tags for the US6 `hasRoot`/`hasStem`/`hasLemma`
  params while documenting the others. Compiles clean; recommend adding the three `<param>` lines in a
  follow-up.

### Test-code self-check (root CLAUDE.md guard) — PASS

Reviewed `WordTypesScopeCountsReadTests` (and cross-referenced the other new test files):

- Tests behavior, not implementation: the FR-016 equality matrix asserts each count against the real
  `GetTableRowsAsync` `TotalCount` oracle for the identical scope — not against a re-derived SQL string.
- Real boundaries only: real Testcontainers DB + real `EfWordTypesReader`/`CachedWordTypesReader` + real
  `DbCommandInterceptor`s (`SqlCommandCountInterceptor`, `CommandTextCapture`); no reader mocking for
  correctness tests; real `WordTypeFilter`/`WordTypeScopeCountsDto` constructed.
- Data-driven variants: `EqualityScopes` `MemberData` covers 18 scopes (each type, a child, case/tense/voice,
  ±search, ±flags, search+flag composition) — no copy-paste duplication.
- No framework-guarantee tests; a vacuous-pass guard exists
  (`ScopeCounts_UnscopedNoun_HasNonZeroCountsAcrossEveryDimension`).
- Quran-data safety: search inputs (`كلم`, `مثل`) are query fragments, not stored Quran text; seeds stay
  source-safe.

---

## Conclusion

All Polish-phase audits PASS. Count families are cleanly separated on every new surface; result ordering is
untouched (pure `Where` filters only); the full quickstart validates end-to-end against the live API with
both perf gates recorded; both suites and the production build are green. One non-blocking clean-code nit
(missing XML `<param>` tags on three US6 controller params) is flagged for a follow-up, not fixed here.
No feature code was modified during Polish.
