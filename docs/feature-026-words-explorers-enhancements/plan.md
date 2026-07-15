# Implementation Plan: Words Explorers Enhancements (Word Types Parity, Filters, Statistics)

**Branch**: `026-words-explorers-enhancements` | **Date**: 2026-07-14 | **Spec**: not yet authored — this plan is the seed artifact; `spec.md`/`tasks.md`/`contracts/` will be generated from it with the Spec Kit skills.
**Input**: Read-only inspection of the five Words explorers (2026-07-14, this session) + locked product decisions recorded below.

> **Artifact scope note:** per the feature kickoff instruction, this folder intentionally
> contains ONLY `plan.md` right now. No `spec.md`, `tasks.md`, `contracts/`, or
> `quickstart.md` exist yet; they are generated later, step by step, from this plan.

## Summary

Three workstreams over the existing read-only Words explorers
(`/dashboard/words/unique/:mode`, `/roots`, `/lemmas`, `/stems`, `/types`):

- **A — Word Types parity:** add word-identity search, raise the list page to 1000 rows
  with virtual scrolling, and raise detail pages from 25 to 100 — bringing
  `/dashboard/words/types` in line with the other four explorers.
- **B — New filters:** cheap count-range filters (preset buckets + custom min/max) on
  Unique Words, Roots, Lemmas, Stems; has-root/has-stem/has-lemma flags on Word Types;
  heavier association filters (unique words by primary word type / primary root, lemmas
  by real root FK, stems by derived *primary* root/lemma association).
- **C — Statistics:** a headline result-count stat on the four "normal" explorers
  (surfacing the existing `PagedResult<T>.TotalCount`, zero new aggregation), and a
  scoped four-count summary strip (words / roots / stems / lemmas) on Word Types served
  by one new read that reuses the grouped-count machinery.

Everything is read-only: no schema, no migrations, no importers, no Quran text changes,
no new packages.

## Locked Decisions (do not re-open during spec/tasks/implementation)

| # | Decision |
|---|---|
| A1 | Word Types search matches **normalized imlaei-simple word identity text only** (`quran_words_unique_tashkeel.text_imlaei_simple`), never root/stem/lemma display text. Frontend reuses the page-owned `Subject` + `debounceTime(300)` → URL `search` pattern. Param flows through `GetWordTypeRowsQuery`/`GetWordTypeTableQuery` → `WordTypesHandlerValidation` → `EfWordTypesReader.Sql.cs` (rows + count + grouped SQL) → `WordTypesCacheKeys`. |
| A2 | Word Types list page: backend max 100 → **1000**, default 25 → **1000**; `WORD_TYPES_PAGE_SIZE` aligned; `word-types-table` gets `CdkVirtualScrollViewport` (mirror the other four tables). Perf review of `GroupedRowsSql` at large page sizes + per-page cache-entry growth is mandatory. |
| A3 | Word Types detail pages 25 → **100** (`WORD_TYPES_DETAIL_PAGE_SIZE`; within the existing backend detail cap 100), covering word ayahs, grouped member words, grouped ayahs. Backend detail defaults in the two controllers aligned to 100. |
| B1 | Count-range filters on Unique Words / Roots / Lemmas / Stems from **existing count columns only**; Word Types gets tri-state has-root/has-stem/has-lemma over the existing numeric columns in `BaseRowsSql`. UI = preset bucket chips + a "custom" option revealing min/max. URL **and** cache keys include the chosen range. |
| B2 | Heavy filters: unique words by primary word type AND primary root (predicate moves into the **base query**, not per-page enrichment); lemmas by root via the real FK `QuranLemma.RootId`; stems by root/lemma via the derived **primary** association ONLY, labeled honestly (e.g. "الجذر الأساسي") and documented as primary-not-sole. |
| B3 | **No schema/migrations for any filter.** If one truly needs schema → STOP, flag in the plan/spec, do not migrate. |
| C1 | Normal explorers: ONE headline stat = the current result set's total (the page's existing filtered `PagedResult<T>.TotalCount`), reflecting active search/filters. No new backend aggregation. Arabic label per page. |
| C2 | Word Types: scoped FOUR-count summary (words, roots, stems, lemmas) for the active `type/childCode/case/tense/voice` scope **and** the new search. Uses the scoped word-context count family only; prefers a single new read returning all four counts; never conflated with the global `words_count`-backed family. |
| C3 | Stat areas share the exact URL/cache identity of their page's list. Any new endpoint: `ApiResponse<T>`, read-only, parameterized values + allowlisted identifiers, cache key includes every scope input. Distinct loading/empty/error states; loading is non-interactive. |
| D | Terminology (per spec FR-021): root = **"الجذر"** (plural "الجذور"); stem's canonical user-facing label = **"الأصل الصرفي"** (plural "الأصول الصرفية"; internal reference "الجذع"); lemma's canonical user-facing label = **"الصيغة المعجمية"** (plural "الصيغ المعجمية"; internal reference "اللمّة"). "الجذع"/"اللمّة" are internal reference terms only, never user-facing labels. Every Arabic label must name the dimension it actually counts. |

### Decision reconciliation (A1 × C2) — recorded, not re-opened

A1 scopes search to "tableView=words only"; C2 requires the four-count summary — and the
acceptance criteria require each grouped tableView's total — to reflect the active search.
These reconcile as follows, and this is the contract this plan encodes:

- The search **predicate** always matches word identity text only (A1). It never matches
  root/stem/lemma display text.
- The predicate is applied to the shared scoped occurrence base (`BaseRowsSql`), which all
  four tableViews and the four-count summary already derive from (see reads README:
  grouped reads reuse the scoped `BaseRowsSql` occurrence base verbatim). Grouped views
  therefore show the roots/stems/lemmas **of the matching words**, and the four counts
  equal the four tableView totals for the identical scope — which is exactly the C2
  acceptance criterion.
- UI: one search input in the Word Types toolbar, visible on all tableViews, with an
  Arabic placeholder that says it searches **words** (e.g. "ابحث في الكلمات"). Word-search
  semantics stay honest on grouped views because the placeholder and label name the word
  grain.

If, during implementation, `BaseRowsSql` cannot take the word-text predicate without
breaking the byte-for-byte grouped-summary equivalence documented in the reads README,
that is a **stop condition** (see below), not a license to search dimension text.

## Technical Context

**Language/Version**: Backend C# / .NET 10 (`net10.0`, EF Core 10 + Npgsql); Frontend TypeScript / Angular 20 standalone components + Signals.
**Primary Dependencies**: existing `ApiResponse<T>` envelope + `PagedResult<T>` (`Backend/application/QuranDashboard.Application.Abstractions/Common/Paging/PagedResult.cs`); `IMemoryCache` decorators (`Infrastructure/Caching/Quran/Words/**`); Angular Router, RxJS (`debounceTime`), Angular CDK scrolling; existing Words explorer utilities/components; frontend `ApiResponseCache` (`core/caching/api-response-cache.ts`).
**Storage**: existing PostgreSQL, read-only: `quran_word_morphology`, `quran_words`, `quran_words_unique_tashkeel` (21,294), `quran_words_unique_simple` (14,783), `quran_roots` (1,642), `quran_lemmas` (4,790), `quran_stems` (12,108), `quran_ayahs`, `quran_surahs`. Row counts per `Backend/report/database-inventory/current-database-inventory.md`.
**Testing**: Backend xUnit + Testcontainers with source-safe seed slices (`Backend/tests/QuranDashboard.Tests/Quran/{Words,WordsRoots,WordsMorphologyExplorers,WordsWordTypes}`); Frontend Vitest under the repo worker cap (see repo test-command rule in the root README).
**Target Platform**: Arabic-first RTL dashboard (PRODUCT.md / DESIGN.md — scholarly, calm; quiet chrome; restrained color).
**Performance Goals**: Word Types 1000-row page renders without jank (virtual scroll); grouped SQL + four-count summary bounded and measured; no N+1 anywhere; detail hydration keeps its documented command budgets.
**Constraints**: read-only; count-family invariant is HARD (see below); URL params are user-facing contracts; identity = clean imlaei-simple, Uthmani display-only; no Quran/search text in logs; fail-closed URL parsing.
**Scale/Scope**: five pages, ~6 backend read areas touched, no new tables, one new backend read (four-count summary), zero migrations.

## Constitution Check

`.specify/memory/constitution.md` is still the unfilled template → formal constitution
compliance **not evaluable** (same status as features 015–019). Practical governance:

| Gate | Source | Status |
|---|---|---|
| Clean Architecture layering | `Backend/CLAUDE.md`, `Backend/.architecture/BACKEND_STRUCTURE.md`, `CLEAN_ARCHITECTURE.md` | PASS — new params/DTO in Abstractions, validation in Application handlers, SQL in Infrastructure readers, thin controllers. |
| API boundary | `Backend/.architecture/API_GUIDELINES.md` | PASS — GET-only, `ApiResponse<T>`, centralized Arabic messages in `Api/Common/ApiMessages.cs`, controlled 400 outcomes. |
| Quran data safety | root `CLAUDE.md`, `CODING_PRINCIPLES.md` | PASS — no writes, no invented Quran content, ayah words stay hydrated from canonical `quran_words.text_uthmani`. |
| Count-family invariant | `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/README.md` | PASS by design — C2 counts come only from the scoped grouped machinery; C1 surfaces existing totals; nothing mixes the families. |
| Frontend structure / API integration | `Frontend/quran-dashboard-ui/.architecture/FRONTEND_STRUCTURE.md`, `API_INTEGRATION_GUIDELINES.md` | PASS — pages → facades → api services; URL state owns filters; presentational children. |
| URL-state contract discipline | `features/words/README.md` | PASS — every new param added to the url-sync modules with fail-closed parsing + spec updates in the same change. |
| Product/design fit | `PRODUCT.md`, `DESIGN.md` | PASS — quiet stat lines and chips, no new visual system, Arabic-first labels per lock D. |
| Scope / YAGNI | `CODING_PRINCIPLES.md` | PASS — non-goals below are explicit; no aggregation beyond counts. |

## Required Reads (before implementing any phase — cite in the spec too)

- `Frontend/quran-dashboard-ui/src/app/features/words/README.md` (URL contracts, table-view invariants, TDZ label getters)
- `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/README.md` (identity, count families, ordering-as-contract, command budgets)
- `docs/contracts/words-explorers.md` (pointer index; precedence rules)
- `Backend/CLAUDE.md`, `Frontend/quran-dashboard-ui/CLAUDE.md`, `PRODUCT.md`, `DESIGN.md`
- Code: `state/*-url-sync.ts` (+ specs), `state/*-cache.ts`, `Infrastructure/Caching/Quran/Words/**` (`CachedWordTypesReader.cs`, `WordTypesCacheKeys.cs`, `UniqueWordsCacheKeys.cs`, …), `EfWordTypesReader.cs`/`.Sql.cs`/`.GroupedDetails.*`, `EfUniqueWordsReader.cs`, `RootsListDerivation.cs` (+ lemma/stem derivations), `MorphologyRelatedItemsOrdering.cs`, `WordTypesHandlerValidation.cs`

## Non-Goals (explicit)

- No row-cap change on the already-1000 explorers (Unique/Roots/Lemmas/Stems lists stay 1000/1000).
- Unique-word ayahs cap stays **100** (`GetUniqueWordAyahsHandler.MaxPageSize`).
- No SUM/average/occurrence aggregations in any stat area — counts only (C1 result count; C2 four dimension counts).
- `TypeDistributionListComponent` is NOT deleted here (separate cleanup).
- No importer, no Quran text change, no schema/migration, no new packages, no unrelated refactors.

---

## Workstream A — Word Types parity

### A1. Search (words identity text)

**Backend contract**

- New optional query param `search` on `GET api/words/word-types/words` and
  `GET api/words/word-types/table` (`WordTypesController.GetRows` / `GetTable`,
  `Backend/api/QuranDashboard.Api/Controllers/Words/WordTypesController.cs`).
- Query records: add `string? Search` to `GetWordTypeRowsQuery` and
  `GetWordTypeTableQuery` (`Application/Quran/Words/WordTypes/Queries/**`).
- Validation (`WordTypesHandlerValidation.cs`): trim; empty/whitespace → treated as null;
  no length rejection beyond a defensive max (e.g. 64 chars → `InvalidFilter`). Search
  text must never be logged (log only `hasSearch` boolean, mirroring
  `GetRootsPageHandler`'s `{hasSearch}` pattern).
- Normalization: reuse the exact Arabic normalization the Unique Words reader applies
  (`EfUniqueWordsReader.NormalizeArabicQuery`) — extract to a shared internal helper in
  `Reads/Quran/Words/` rather than copy (both readers live in the same namespace); the
  helper move must not change Unique Words behavior (pin with existing tests).
- SQL (`EfWordTypesReader.Sql.cs`): one parameterized predicate added to `BaseRowsSql`'s
  occurrence base —
  `EXISTS`/join against `quran_words_unique_tashkeel u ON u.id = <base word id> AND u.text_imlaei_simple LIKE @search` —
  with `@search` = `%<normalized>%` as a **parameter value** (never interpolated;
  identifiers stay the existing allowlisted columns). Because `RowsSql`, `RowsCountSql`,
  `GroupedRowsSql`, and `GroupedRowsCountSql` all derive from the same base, the
  predicate automatically scopes words view, all three grouped views, and (in P4) the
  four-count summary. The grouped summary/member/ayah/surah detail reads
  (`.GroupedDetails.*`) do **not** take search — detail identity is numeric and already
  scoped; document this asymmetry in the reads README.
- Cache keys: `WordTypesCacheKeys.cs` — rows/table keys gain the normalized search
  component (empty ⇒ same key as today to keep warm entries valid).

**Frontend contract**

- New URL key `search` in `WORD_TYPES_QUERY_KEYS` (`models/word-types.models.ts`);
  parse fail-closed in `word-types-url-sync.ts` (trim, empty → absent). `search` is
  list-scope: changing it resets `page` and clears the detail selection snapshot only if
  the selected row falls out of the result (same behavior the other explorers have:
  simplest contract — searching resets list page, keeps detail selection; detail is
  identity-loaded and independent of the list page).
- Page: `word-types-explorer-page` adds the search input to the toolbar (all tableViews;
  placeholder names the word grain, per the A1×C2 reconciliation), wired with the
  standard `Subject` + `debounceTime(300)` → `updateQueryParams({ search, page: null })`
  pattern (mirror `roots-explorer-page.component.ts:100`).
- Facade/cache: `word-types-explorer.facade.ts` passes search into the API call;
  `word-types-cache.ts` `table(...)`/rows keys gain the search component.
- API: `word-types.api.ts` sends `search` only when non-empty.

### A2. List rows → 1000 + virtual scroll

- `WordTypesHandlerValidation.cs`: **split the single `MaxPageSize = 100` into
  `MaxListPageSize = 1000` and `MaxDetailPageSize = 100`.** The current constant gates
  both list and detail reads; raising it wholesale would silently raise the documented
  grouped member/ayah `pageSize 1..100` contract (reads README). List reads (rows/table)
  validate against 1000; all detail reads keep 100.
- `WordTypesController.cs`: `DefaultListPageSize` 25 → 1000.
- `models/word-types.models.ts`: `WORD_TYPES_PAGE_SIZE` 25 → 1000.
- `word-types-table.component.*`: adopt `CdkVirtualScrollViewport` exactly as
  `roots-table.component.ts` does (`useVirtualScroll = HAS_RESIZE_OBSERVER` guard,
  viewport wiring via `explorer-table-scroll.ts`). All four row kinds (word/root/stem/
  lemma views) render inside the viewport; skeleton rows and the mounted-shell invariant
  from `features/words/README.md` (strip/shell/details host stay mounted through every
  transition) must survive the change.
- Spec pins to update: `word-types.api.spec.ts` and facade specs currently assert
  `pageSize=25`.

### A3. Detail pages → 100

- `models/word-types.models.ts`: `WORD_TYPES_DETAIL_PAGE_SIZE` 25 → 100 (used by
  `word-types-detail-view.loader.ts:64–80` for word ayahs, grouped member words, grouped
  ayahs — one constant covers all three, as required).
- `WordTypesController.cs` `DefaultDetailPageSize` and
  `WordTypeGroupedDetailsController.cs` `DefaultDetailPageSize` 25 → 100 (align defaults;
  cap stays 100 via `MaxDetailPageSize`).
- `detailPage` canonicalization (`canonicalWordTypesDetailPage`) is page-size-agnostic —
  no change, but its specs re-run.

---

## Workstream B — New filters

### B1. Cheap count-range filters + Word Types has-flags

**Range grammar (shared, all pages):**

- URL value grammar per metric key: `min..max`, either side omissible (`..10`, `11..`,
  `5..5`). Bucket chips are presentation only — the URL always stores the canonical
  range, so links are shareable and parseable fail-closed (malformed ⇒ filter absent).
- Backend params: per-metric nullable `int? <metric>Min` / `int? <metric>Max`.
  Validation: `Min >= 0`, `Max >= Min` when both present, else 400 (each page's existing
  `InvalidFilter`-style outcome; Roots/Lemmas/Stems/Unique add an `InvalidFilter` outcome
  alongside their current `InvalidSort`/`InvalidPaging`).
- Cache keys: **frontend** list cache keys gain the range components on all four pages.
  **Backend**: Unique Words list keys (`UniqueWordsCacheKeys.cs`) gain them; Roots/
  Lemmas/Stems need **no backend cache-key change** — their `Cached*Reader`s cache the
  whole summary and apply derivation per request (`CachedRootsReader.GetRootsPageAsync`
  → `GetOrLoadWholeSummaryAsync`). Word Types rows/table keys gain the has-flags.

**Per page — metrics (existing columns only, cite):**

| Page | Metrics | Backing |
|---|---|---|
| Unique Words | occurrences, ayahs, surahs | `occurrences_count`, `ayahs_count`, `surahs_count` on `quran_words_unique_tashkeel/simple` (`Display/UniqueTashkeelWord.cs`) — SQL predicates in `BuildTashkeelQuery`/`BuildSimpleQuery` |
| Roots | occurrences, ayahs, surahs, simple words, tashkeel words, lemmas, stems | `RootSummaryRow` fields — in-memory predicates in `RootsListDerivation.FilterAndSort` |
| Lemmas | occurrences, ayahs, surahs, simple words, tashkeel words, stems | lemma summary rows — lemma list derivation |
| Stems | occurrences, ayahs, surahs, simple words, tashkeel words | `StemListItemDto` count fields — stem list derivation |
| Word Types | has-root / has-stem / has-lemma (tri-state: any / has / missing) | `m.root_id` / `m.stem_id` / `m.lemma_id` in `BaseRowsSql` — allowlisted `IS [NOT] NULL` predicates, no user text |

- Note the grain honestly in labels/specs: Unique Words ranges filter unique-word
  identities; Roots/Lemmas/Stems ranges filter dimension entries; Word Types has-flags
  filter word-context occurrence rows (and therefore also reshape the grouped views and
  P4 counts — has-flags are part of the list scope like case/tense/voice).
- Preset buckets (initial proposal; final values are a spec-phase decision, and the URL
  stores ranges, so changing buckets later is not a contract change):
  occurrences `1 · 2–10 · 11–100 · 101–1000 · 1000+`; ayahs/surahs
  `1 · 2–10 · 11–50 · 50+` (surahs capped at 114); words/lemmas/stems counts
  `1 · 2–5 · 6–20 · 20+`.
- UI: one collapsible filter row per page (chips per metric + "مخصّص" revealing min/max
  numeric inputs); RTL layout; chips are buttons with `aria-pressed`; active filter state
  visible; clearing resets `page`.

### B2. Heavy filters

1. **Unique Words by primary word type** — new param `primaryType` (POS code, validated
   against the catalogue). The predicate must move into the base SQL of
   `BuildTashkeelQuery`/`BuildSimpleQuery`, reproducing exactly the "primary" selection
   rule that `LoadPrimaryWordTypesAsync` (`EfUniqueWordsReader.cs:281–305`) uses for
   display — the displayed chip and the filter must never disagree. Implementation shape:
   the same join/window the enrichment uses, expressed as a filterable subquery.
2. **Unique Words by primary root** — new param `rootId` (positive int). Same rule:
   predicate mirrors `LoadPrimaryRootsAsync` exactly. UI: root picker fed from the
   existing Roots list read (reuse `roots.api.ts` + cache; no new endpoint).
3. **Lemmas by root** — new param `rootId`; real FK `QuranLemma.RootId`
   (`Domain/Quran/Words/Morphology/QuranLemma.cs:8`); in-memory predicate in the lemma
   list derivation (RootId is already on the summary row / `LemmaListItemDto`).
4. **Stems by root / by lemma** — params `rootId` / `lemmaId`; predicate over the derived
   **primary** association already surfaced in `StemListItemDto.RootId/LemmaId`. Arabic
   labels must say primary: **"الجذر الأساسي"** / **"اللمّة الأساسية"** (alt spelling per
   app convention "الصيغة المعجمية الأساسية" acceptable under lock D). Reads README gets
   an explicit sentence: this filter uses the primary association, not all co-occurring
   associations.
- All四 params: URL keys added fail-closed to the respective `*-url-sync.ts`; frontend +
  (Unique only) backend cache keys extended; validation 400 on nonpositive IDs/unknown
  POS codes.

### B3. Schema guard

No filter above needs schema (verified against entities + inventory during inspection).
If implementation discovers otherwise → **stop condition**.

---

## Workstream C — Statistics

### C1. Headline result count (Unique Words, Roots, Lemmas, Stems)

- **Zero backend work.** Surface `listState().totalCount` (already delivered by the
  paged list read and already fed to `qd-pagination`).
- New small shared presentational component (suggested:
  `features/words/components/explorer-result-count/`) rendering:
  count (Arabic-formatted number) + per-page noun per lock D — words: "كلمة";
  roots: "جذر/جذرًا"; lemmas: "لمّة" (app-consistent alt: "صيغة معجمية");
  stems: "جذع" (app-consistent alt: "أصل صرفي"). Final inflected phrasing (تمييز/plural
  forms) is a spec-phase Arabic-copy decision; the noun→dimension mapping is locked.
- States (C3): list loading → non-interactive skeleton chip; list error → stat hidden
  (the page's existing error state owns the message); success with 0 → shows "0" +
  existing empty state below. The stat reflects the active search + filters by
  construction (it IS the filtered query's total).
- Placement: toolbar/recess row next to search/sort, mirrored on all four pages.

### C2 + C3. Word Types four-count scoped summary

**Backend — one new read:**

- Endpoint: `GET api/words/word-types/scope-counts`
  (`WordTypesController`, new action) with params `type`, `childCode`, `case`, `tense`,
  `voice`, `search`, plus the B1 has-flags — i.e., **exactly the list scope, nothing
  else**.
- DTO (`Application.Abstractions/Quran/Words/WordTypes/Responses/`):
  `WordTypeScopeCountsDto(int WordsCount, int RootsCount, int StemsCount, int LemmasCount)`.
- Handler `GetWordTypeScopeCountsHandler` (+ query record, outcomes
  `Success | InvalidFilter`), validation via `WordTypesHandlerValidation`.
- Reader: new partial `EfWordTypesReader.ScopeCounts.cs` (respect the existing
  partial-split convention). **One SQL command**: CTE over the same scoped `BaseRowsSql`
  base (including the search predicate and has-flags), then four aggregates —
  words = the words-view grouping formula count (`COUNT(DISTINCT (unique_tashkeel_word_id, context_code))`,
  byte-identical to `RowsCountSql`); roots/stems/lemmas =
  `COUNT(DISTINCT root_id|stem_id|lemma_id)` excluding NULLs, byte-identical to each
  `GroupedRowsCountSql`. Equality with the four tableView totals is the correctness
  test, not a coincidence — reuse the SQL fragments, do not re-derive them.
- Caching: `CachedWordTypesReader` + `WordTypesCacheKeys.scopeCounts(...)` — key includes
  **every** scope input (type, childCode, case, tense, voice, normalized search,
  has-flags). Entry options mirror the table read's.
- `ApiMessages.cs`: new Arabic success/failure messages, centralized.

**Frontend:**

- `word-types.api.ts`: `getScopeCounts(scope)` → `ApiResponse<WordTypeScopeCountsDto>`;
  wire DTO re-exported from `core/api/generated/` per the feature's model conventions
  (regenerate the API client per the repo's api-contract staleness guard).
- `word-types-explorer.facade.ts`: loads scope counts alongside the table whenever the
  list scope changes (same trigger set: type/childCode/case/tense/voice/search/has-flags;
  NOT tableView, NOT page — the counts are scope-level). Cache via `word-types-cache.ts`
  key with the same components as the backend key.
- New presentational component (suggested:
  `features/words/components/word-type-scope-counts/`): four labeled counts, RTL order
  matching the existing tabs strip **كلمات | جذور | أصول | صيغ** (labels follow lock D
  with the app's current terms; tab labels and count labels must stay consistent).
  Non-interactive (counts are informational; the tabs remain the navigation).
- States (C3): own loading (skeleton, non-interactive) / error (compact error +
  **إعادة المحاولة** retry that refetches only the counts) / empty (all-zero renders as
  zeros). Partial failure: table success + counts failure must NOT block the table —
  the strip shows its error state independently; table failure keeps the strip's last
  valid counts hidden (scope unconfirmed).
- Placement: between the filter strip and the table-view tabs (visual spec-phase
  decision; must not break the mounted-shell invariant).

---

## Phases (ordered, with dependencies)

### P1 — Word Types parity (A1 + A2 + A3)

Self-contained; touches the Word Types SQL first so later phases rebase on the final
base-SQL shape.

- **Backend files**: `WordTypesController.cs`, `WordTypeGroupedDetailsController.cs`,
  `Application/Quran/Words/WordTypes/Queries/GetWordTypeRows/**`, `GetWordTypeTable/**`,
  `WordTypesHandlerValidation.cs`, `Application.Abstractions/.../WordTypes/` (query
  param plumbing), `EfWordTypesReader.cs`/`.Sql.cs`, `WordTypesCacheKeys.cs`,
  `CachedWordTypesReader.cs`, `ApiMessages.cs`, shared Arabic-normalize helper move in
  `Reads/Quran/Words/` (+ `EfUniqueWordsReader.cs` consuming it unchanged-behavior).
- **Frontend files**: `models/word-types.models.ts` (+labels), `word-types-url-sync.ts`
  (+spec), `word-types.api.ts` (+spec), `word-types-explorer.facade.ts` (+spec),
  `word-types-cache.ts` (+spec), `word-types-explorer-page.component.*`,
  `word-types-table.component.*` (virtual scroll), `word-types-detail-view.loader.ts`
  specs (page-size pins).
- **Tests**: backend `WordsWordTypes` — search filtering/normalization (word-text only;
  grouped views reflect searched base; no dimension-text matches), list cap 1000 accepted
  / 1001 rejected, detail cap still 100, defaults; Unique Words tests re-run green after
  the normalize-helper extraction. Frontend — url-sync search key (fail-closed), debounce
  wiring, cache keys include search, virtual-scroll table spec, updated 25→1000/100 pins.
- **README/spec updates**: `features/words/README.md` (search param + page sizes + list
  scope), reads README (search predicate location + caps split + detail-read asymmetry).
- **Perf checks (mandatory)**: timing of `RowsSql`/`GroupedRowsSql` at `pageSize=1000`
  for worst scopes (`type=verb` unscoped; stems grouped view ≈ up to 12,108 groups);
  cache-entry size sanity (25→1000 rows per entry, but page count per scope drops
  ~40×); DOM check that virtual scroll keeps initial render bounded; 100-ayah detail
  page render sanity (~1,500 word spans).
- **Acceptance**: search narrows words view by identity text and reshapes grouped views
  + totals identically; 1000 rows served and scrolled without jank; details page at 100;
  all existing URL-state invariants (mounted shell, detail snapshot fail-closed,
  `detailPage` canonicalization) still hold; count families untouched.
- **Commit boundary**: one commit (or PR) `feat(words): word-types parity — search, 1000-row list, 100-row details`.

### P2 — Cheap filters + headline result count (B1 + C1) — depends on P1 (shared Word Types SQL)

- **Backend files**: `UniqueWordsController.cs` + `GetUniqueWordsPageQuery/Handler` +
  `EfUniqueWordsReader.cs` (+ `UniqueWordsCacheKeys.cs`); `RootsController.cs`/
  `LemmasController.cs`/`StemsController.cs` + their `Get*sPage` handlers +
  `*ListDerivation.cs`; Word Types has-flags through the P1-shaped query/validation/SQL/
  cache-key path; `ApiMessages.cs` (new `InvalidFilter` messages).
- **Frontend files**: per page — models (+labels, bucket presets), `*-url-sync.ts`
  (+specs), `*.api.ts` (+specs), `*-explorer.facade.ts` / `unique-words.facade.ts`
  (+specs), `*-cache.ts`, page templates; new shared components
  `explorer-count-range-filter` (chips + custom min/max) and `explorer-result-count`.
- **Tests**: backend — range validation (min>max → 400, negatives → 400), predicate
  correctness per metric, unique-words SQL ranges, word-types has-flags reshape rows AND
  grouped views; frontend — url-sync range grammar fail-closed cases, chips↔custom↔URL
  round-trips, result-count states, cache-key inclusion.
- **README updates**: `features/words/README.md` (new URL keys + grammar + stat line),
  reads README (filter predicates + which family they filter).
- **Acceptance**: headline stat equals the paged result total for the active query and
  changes when search/filters change (all four pages); every filter is shareable via
  URL, restores on refresh/Back, and fails closed on malformed values; word-types
  has-flags participate in scope (grouped views + P4 counts later).
- **Commit boundary**: one commit `feat(words): count-range filters + result-count stat`.

### P3 — Heavy filters (B2) — depends on P2 (filter UI + URL grammar exist)

- **Backend files**: `EfUniqueWordsReader.cs` (base-query predicates mirroring the
  primary-selection rules), `GetUniqueWordsPageQuery/Handler`, `UniqueWordsController.cs`,
  `UniqueWordsCacheKeys.cs`; lemma/stem list derivations + handlers + controllers.
- **Frontend files**: unique-words/lemmas/stems models + url-sync (+specs) + api +
  facades + caches + filter UI additions (type select from catalogue; root/lemma pickers
  reusing existing list reads).
- **Tests**: backend — filter⇔displayed-chip agreement for primary type and primary root
  (the row shown must match its filter bucket), FK filter for lemmas, primary-association
  filter for stems incl. the documented primary-not-sole semantics; frontend — url-sync
  + picker specs.
- **README updates**: reads README primary-association sentence; features README URL keys.
- **Acceptance**: filtering by primary type/root never contradicts the chips displayed in
  the rows; labels say "الأساسي/الأساسية"; no schema touched.
- **Commit boundary**: one commit `feat(words): association filters (primary type/root/lemma)`.

### P4 — Word Types four-count scoped summary (C2/C3) — depends on P1 (search in scope) and P2 (has-flags in scope)

- **Backend files**: `WordTypesController.cs` (new action),
  `Application/Quran/Words/WordTypes/Queries/GetWordTypeScopeCounts/**` (new),
  `Application.Abstractions/.../Responses/WordTypeScopeCountsDto.cs` (new),
  `IWordTypesReader.cs`, `EfWordTypesReader.ScopeCounts.cs` (new partial),
  `WordTypesCacheKeys.cs`, `CachedWordTypesReader.cs`, `ApiMessages.cs`.
- **Frontend files**: generated API model + `word-types.api.ts` (+spec),
  `word-types-explorer.facade.ts` (+spec), `word-types-cache.ts` (+spec),
  new `word-type-scope-counts` component (+spec), page template, labels per lock D.
- **Tests**: backend — **equality test**: for a matrix of scopes (each main type, a
  child, case/tense/voice variants, with/without search, with/without has-flags) the
  four counts equal the `TotalCount` of the corresponding words/roots/stems/lemmas
  tableView reads for the identical scope; single-SQL-command budget pinned (reuse the
  `SqlCommandCountInterceptor` pattern from `Backend/tests/.../Quran/Words/`); cache-key
  isolation per scope input. Frontend — strip states (loading/error-retry/zero), counts
  refetch on scope change but not on tableView/page change, partial-failure isolation.
- **README updates**: features README (strip + URL/cache identity), reads README (the new
  read, its family, its command budget), `docs/contracts/` untouched (pointer index
  already defers to these READMEs).
- **Perf check**: scope-counts SQL timed on worst scope (`type=noun|verb` unscoped +
  empty search) — must stay a single bounded command; cached thereafter.
- **Acceptance**: four counts equal grouped totals of the four tableViews for the
  identical active scope; update with type selection, sub-filters, has-flags, and
  search; **scoped vs global count families never mixed** (no `words_count`-backed number
  appears in the strip); loading is non-interactive; counts failure never blocks the
  table.
- **Commit boundary**: one commit `feat(words): word-types scoped four-count summary`.

---

## Data validation & performance checks (cross-phase)

- **Count-family invariant test** (P4, plus assertion review in P1/P2): nothing in the
  Word Types strip/list derives from `quran_roots.words_count` & friends; nothing in the
  Roots/Lemmas/Stems stat line derives from scoped word-context counts.
- **Search normalization equivalence**: the shared normalizer produces identical results
  to today's Unique Words normalization (regression-pinned before reuse).
- **Ordering untouched**: filter predicates are pure `Where`s; ordering paths
  (`MorphologyRelatedItemsOrdering`, `*ListDerivation` sort, `WordTypeSort`) are not
  reordered — assert existing ordering tests stay green.
- **Command budgets**: grouped ayahs stay 3 commands/page, grouped surahs 2, scope
  counts 1 (new pin).
- **Deploy smoke**: after P1 and P4, run the repo's deploy-smoke flow (build + local
  endpoint smoke on the changed endpoints) before review.

## Risks & mitigations

| Risk | Mitigation |
|---|---|
| `GroupedRowsSql` at pageSize 1000 too slow on stems (12k groups before pagination) | P1 perf measurement gate; if p95 exceeds budget, fall back to a smaller Word Types default (e.g. 100) while keeping cap 1000 — flag to user before deciding, since A2 locks default 1000 (stop condition below). |
| 1000 static rows regress the mounted-shell/focus invariants | Virtual scroll is mandatory in the same commit as the page-size change; the focus controller (`explorer-table-focus-controller.ts`) tests re-run. |
| Primary-type/root filter drifts from the displayed chips | One shared SQL shape for enrichment + predicate; agreement test in P3. |
| Search predicate breaks grouped-summary byte-equivalence (reads README invariant) | Predicate goes into the shared base only; grouped detail summary reads are scope-consistent by construction; equality tests in P4 catch drift. |
| Cache-entry growth (1000-row pages) | Fewer distinct pages per scope (~40× fewer); measure entry sizes in P1; entry options tunable in `WordTypesCacheEntryOptions.cs` without contract change. |
| URL contract churn | Every new key lands with fail-closed parsing + spec + README in the same commit — never a follow-up. |

## Rollback

Each phase is one commit with no schema changes → `git revert` restores the previous
contract cleanly. Constants (page sizes) and additive params degrade gracefully:
old URLs without the new keys remain valid throughout (all new params optional,
defaults preserve current behavior except the locked page-size changes).

## Stop conditions (report, don't redesign)

1. Any filter turns out to need schema/migration (B3).
2. The word-text search predicate cannot be added to `BaseRowsSql` without breaking the
   grouped-summary byte-for-byte equivalence or the row-for-row member-words subset
   invariants (reads README).
3. The four-count summary cannot reuse the existing grouped-count SQL fragments (i.e.
   equality with tableView totals would require a second, diverging derivation).
4. P1 perf gate fails hard at default 1000 (see risk table — default change needs user
   sign-off; cap raise can still land).
5. Any change would force a modification to importer code, EF migrations/snapshots, or
   Quran source data.

## Final acceptance (feature level)

- Unique/Roots/Lemmas/Stems: headline stat = paged result total of the active query;
  updates with search + every new filter; zero new backend aggregation for it.
- Word Types: search (word identity text), 1000-row virtually-scrolled list, 100-row
  details, has-flags, and a four-count strip whose numbers equal the four tableView
  totals for the identical scope and react to type/sub-filter/has-flag/search changes.
- All new state is URL-shareable, restores on refresh/Back/Forward, fails closed.
- Count families never conflated; identity stays clean imlaei-simple; ordering
  contracts untouched; no schema, no migrations, no new packages.
- READMEs (`features/words/README.md`, reads README) updated in the same commits that
  change the contracts they document.
