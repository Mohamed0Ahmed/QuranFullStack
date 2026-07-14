# Tasks: Words Explorers Enhancements (Word Types Parity, Filters, Statistics)

**Input**: Design documents from `specs/026-words-explorers-enhancements/`
(plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md).
Locked decision record: `docs/feature-026-words-explorers-enhancements/plan.md`.

**Tests**: INCLUDED — the plan mandates tests per phase (backend xUnit under the four
Words test areas; frontend url-sync/api/facade/component specs) and README updates in
the same commit as the contract they document.

**Organization**: grouped by user story (US1–US8 from spec.md). Commit boundaries
follow the plan's four phases: **plan-P1 = US1+US2+US3 (one commit)**,
**plan-P2 = US4+US5+US6 (one commit)**, **plan-P3 = US7 (one commit)**,
**plan-P4 = US8 (one commit)**.

**Path shorthand** (expand to full repo-relative paths):
- `BE-API` = `Backend/api/QuranDashboard.Api`
- `BE-APP` = `Backend/application/QuranDashboard.Application`
- `BE-ABS` = `Backend/application/QuranDashboard.Application.Abstractions`
- `BE-INF` = `Backend/infrastructure/QuranDashboard.Infrastructure`
- `BE-TST` = `Backend/tests/QuranDashboard.Tests`
- `FE` = `Frontend/quran-dashboard-ui/src/app`
- `WORDS` = `FE/features/words`
- reads README = `BE-INF/Persistence/Reads/Quran/Words/README.md`
- words README = `WORDS/README.md`

## Format: `[ID] [P?] [Story] Description`

---

## Phase 1: Setup

**Purpose**: verified clean baseline before touching contracts.

- [x] T001 Verify baseline is green on branch `026-words-explorers-enhancements`: `dotnet build Backend/QuranDashboard.sln`, `dotnet test Backend/QuranDashboard.sln`, and frontend `npm test` under the repo vitest worker cap (root README rule). Record any pre-existing failure BEFORE starting — do not absorb it into feature commits.

---

## Phase 2: Foundational (blocking prerequisites)

**Purpose**: shared Arabic search normalizer — US1's search must normalize
identically to Unique Words search (research R2). Blocks US1 only, but do it first
to keep the Unique Words behavior pin honest.

- [x] T002 Extract the private Arabic normalization from `BE-INF/Persistence/Reads/Quran/Words/EfUniqueWordsReader.cs` (`NormalizeArabicQuery`) into a new shared internal static helper `BE-INF/Persistence/Reads/Quran/Words/ArabicSearchQueryNormalizer.cs`; `EfUniqueWordsReader` consumes it; logic byte-identical (pure move, no behavior change).
- [x] T003 Re-run the Unique Words search/sort/paging tests (`BE-TST/Quran/Words/UniqueWordsSearchSortPagingTests.cs` and siblings) to pin that T002 changed nothing. All green before any US1 work.

**Checkpoint**: normalizer shared + pinned. User stories may begin.

---

## Phase 3: User Story 1 — Word Types search by word text (Priority: P1) 🎯 MVP

**Goal**: search input on `/dashboard/words/types` matching normalized imlaei-simple
word identity text; narrows ALL tableViews (predicate on the shared `BaseRowsSql`
occurrence base); URL-shareable, debounced, fail-closed.

**Independent Test**: quickstart.md §P1 step 2 — type a fragment, words view narrows;
switch tabs, grouped views show roots/stems/lemmas of matching words; URL carries
`search=`; refresh restores; no dimension-text matches.

### Implementation — backend

- [x] T004 [US1] Add `string? Search` to `GetWordTypeRowsQuery` and `GetWordTypeTableQuery` records in `BE-APP/Quran/Words/WordTypes/Queries/GetWordTypeRows/` and `.../GetWordTypeTable/`; thread through both handlers; log ONLY a `hasSearch` boolean (mirror `GetRootsPageHandler`'s `{hasSearch}`), never the text.
- [x] T005 [US1] Add search validation to `BE-APP/Quran/Words/WordTypes/Queries/WordTypesHandlerValidation.cs`: trim; empty/whitespace → null; length > 64 → the existing `InvalidFilter`-style outcome (400).
- [x] T006 [US1] Add the search predicate to `BaseRowsSql` in `BE-INF/Persistence/Reads/Quran/Words/WordTypes/EfWordTypesReader.Sql.cs`: parameterized `LIKE @search` (`%normalized%` as a VALUE) against `quran_words_unique_tashkeel.text_imlaei_simple` joined/EXISTS on the base's word id; normalize via `ArabicSearchQueryNormalizer` (T002). Plumb the param through `EfWordTypesReader.cs` `GetRowsAsync`/`GetTableRowsAsync` and the reader interface `BE-ABS/Quran/Words/WordTypes/IWordTypesReader.cs`. Grouped detail reads (`.GroupedDetails.*`) take NO search param.
- [x] T007 [US1] Include normalized search in rows/table cache keys in `BE-INF/Caching/Quran/Words/WordTypes/WordTypesCacheKeys.cs` + pass-through in `CachedWordTypesReader.cs`. Empty search ⇒ key byte-identical to today (warm entries stay valid).
- [x] T008 [US1] Add `[FromQuery] string? search` to `GetRows` and `GetTable` in `BE-API/Controllers/Words/WordTypesController.cs`; add any new Arabic message to `BE-API/Common/ApiMessages.cs`.
- [x] T009 [US1] Backend tests in `BE-TST/Quran/WordsWordTypes/`: (a) search narrows words rows by identity text only (tashkeel-insensitive match hits); (b) `tableView=roots|stems|lemmas` rows AND `TotalCount` reflect the searched base; (c) a term matching only `root_text`/`lemma_text`/`stem_text` (not word text) matches NOTHING; (d) normalization equivalence with Unique Words search; (e) 65-char search → 400; (f) cache-key isolation (search vs no-search never cross-serve); (g) grouped detail reads unaffected.

### Implementation — frontend

- [x] T010 [P] [US1] Add `search` to `WORD_TYPES_QUERY_KEYS` in `WORDS/models/word-types.models.ts`; parse fail-closed (trim, empty → absent) in `WORDS/state/word-types-url-sync.ts`; search change serializes with `page: null`; update `WORDS/state/word-types-url-sync.spec.ts` (parse/serialize/fail-closed/restore cases + one backward-compat case: a pre-feature URL without `search` parses byte-identically to today).
- [x] T011 [US1] `WORDS/data-access/word-types.api.ts`: send `search` param only when non-empty on rows + table calls; update `word-types.api.spec.ts`.
- [x] T012 [US1] `WORDS/state/word-types-explorer.facade.ts`: search is part of the list query; `WORDS/state/word-types-cache.ts`: rows/table keys gain the search component (empty ⇒ unchanged key); update both specs.
- [x] T013 [US1] `WORDS/pages/word-types-explorer-page/word-types-explorer-page.component.ts` + `.html`: toolbar search input visible on ALL tableViews, wired `Subject` + `debounceTime(300)` → `updateQueryParams({ search: value || null, page: null })` (mirror `roots-explorer-page.component.ts:100`); labels/placeholder in `WORDS/models/word-types.labels.ts` — placeholder names the word grain: "ابحث في الكلمات" (TDZ getter pattern — do not use readonly fields).
- [x] T014 [US1] Update words README (search param, list-scope semantics, all-views behavior) and reads README (predicate location, identity-text-only rule, grouped-detail asymmetry) in the same commit.

**Checkpoint**: search fully functional and URL-restorable on all four views.

---

## Phase 4: User Story 2 — 1000-row Word Types list (Priority: P1)

**Goal**: list default+cap 1000 (detail cap untouched at 100); virtual scrolling.

**Independent Test**: quickstart.md §P1 steps 1+4 — `pageSize=1000` → 200 with up to
1000 rows; `pageSize=1001` → 400; page scrolls smoothly; shell invariants hold.

### Implementation

- [x] T015 [US2] In `BE-APP/Quran/Words/WordTypes/Queries/WordTypesHandlerValidation.cs`, split `MaxPageSize = 100` into `MaxListPageSize = 1000` (rows/table reads) and `MaxDetailPageSize = 100` (word ayahs, grouped member words/ayahs); update every handler that validates paging to use the correct constant.
- [x] T016 [US2] `BE-API/Controllers/Words/WordTypesController.cs`: `DefaultListPageSize` 25 → 1000.
- [x] T017 [US2] `WORDS/models/word-types.models.ts`: `WORD_TYPES_PAGE_SIZE` 25 → 1000; update every spec pinning 25 for LIST reads (`word-types.api.spec.ts`, `word-types-explorer.facade.spec.ts`).
- [x] T018 [US2] `WORDS/components/word-types-table/word-types-table.component.ts` + `.html` + `.scss`: adopt `CdkVirtualScrollViewport` mirroring `WORDS/components/roots-table/roots-table.component.ts` (`useVirtualScroll = HAS_RESIZE_OBSERVER`, wiring via `WORDS/utils/explorer-table-scroll.ts`); all four row kinds render inside the viewport; skeletons, mounted-shell invariant, focus controller, statistic buttons unchanged; update `word-types-table.component.spec.ts`.
- [x] T019 [US2] Backend tests in `BE-TST/Quran/WordsWordTypes/`: list `pageSize=1000` → 200; `1001` → 400 `InvalidPaging`; default page size is 1000; DETAIL `pageSize=101` still → 400 (cap split verified).
- [x] T020 [US2] Perf gate (MANDATORY, record numbers). Budget: **p95 ≤ 2s per uncached list read** (feature 019's populate target). Time `/table` at `pageSize=1000` for `type=verb` unscoped (`tableView=words`) and `tableView=stems` (~12,108 groups pre-pagination); measure cache-entry growth; verify UI scroll on the loaded page. p95 > 2s at default 1000 → STOP per decision-record stop condition 4 (cap may land, default needs user sign-off).

**Checkpoint**: 1000-row pages served + smooth; caps proven split.

---

## Phase 5: User Story 3 — 100-item Word Types detail pages (Priority: P1)

**Goal**: word ayahs, grouped member words, grouped ayahs at 100/page (backend
defaults aligned; cap stays 100). Surahs views stay single-shot.

**Independent Test**: quickstart.md §P1 step 3 — open any detail list → 100 items/page.

### Implementation

- [x] T021 [US3] `WORDS/models/word-types.models.ts`: `WORD_TYPES_DETAIL_PAGE_SIZE` 25 → 100 (single constant covers word ayahs + grouped member words + grouped ayahs via `WORDS/state/word-types-detail-view.loader.ts`); update `word-types-detail-view.loader.spec.ts` + `word-types-detail.facade.spec.ts` pins.
- [x] T022 [US3] Backend: `DefaultDetailPageSize` 25 → 100 in `BE-API/Controllers/Words/WordTypesController.cs` AND `BE-API/Controllers/Words/WordTypeGroupedDetailsController.cs`; backend tests assert new defaults + grouped-ayah 3-command budget unchanged in `BE-TST/Quran/WordsWordTypes/`.
- [x] T023 [US3] Update words README + reads README page-size documentation (list 1000/1000, detail 100/100); sanity-render a 100-ayah detail page (~1,500 word spans) and note result.
- [x] T024 [US3] **Plan-P1 checkpoint**: full `dotnet test` + `npm test` green; run repo deploy-smoke flow on the changed Word Types endpoints; commit plan-P1: `feat(words): word-types parity — search, 1000-row list, 100-row details`.

**Checkpoint**: plan-P1 committed — Word Types at parity. MVP delivered.

---

## Phase 6: User Story 4 — headline result count on four explorers (Priority: P2)

**Goal**: "عدد الـ…: N" stat = existing `listState().totalCount`; zero backend work.

**Independent Test**: quickstart.md §P2 step 1 — stat equals pagination total on all
four pages; updates on search; skeleton while loading; hidden on error; 0 on empty.

### Implementation

- [x] T025 [P] [US4] New shared presentational component `WORDS/components/explorer-result-count/` (`.ts/.html/.scss/.spec.ts`): inputs `count`, `labelPrefix`, `loading`, `hasError`; renders label-prefix phrasing; non-interactive skeleton while loading; renders nothing when `hasError`; "0" on zero. RTL-safe, Arabic-formatted number.
- [x] T026 [P] [US4] Add the four label constants (TDZ getters) — "عدد الكلمات" / "عدد الجذور" / "عدد الصيغ المعجمية" / "عدد الأصول الصرفية" — to `WORDS/models/unique-words.labels.ts`, `roots.labels.ts`, `lemmas.labels.ts`, `stems.labels.ts`.
- [x] T027 [US4] Wire the component into the four page templates next to search/sort toolbar: `WORDS/pages/unique-words-page/`, `roots-explorer-page/`, `lemmas-explorer-page/`, `stems-explorer-page/` (`.component.html` + `.ts` bindings from `listState()`); update the four page specs (stat equals totalCount; states).
- [x] T028 [US4] Update words README (stat surface + phrasing contract).

**Checkpoint**: stat live on four pages, driven purely by existing state.

---

## Phase 7: User Story 5 — count-range filters on four explorers (Priority: P2)

**Goal**: preset bucket chips + مخصّص min/max per metric; URL grammar `min..max`;
backend `<metric>Min/Max` params; stat reflects filtered totals.

**Independent Test**: quickstart.md §P2 steps 2+4 — bucket narrows rows+stat; URL
`occ=11..100` restores; `occ=9..2` in URL ignored (fail-closed); direct API
`occMin=5&occMax=2` → 400.

### Implementation — backend

- [x] T029 [US5] Unique Words: add `occMin/occMax/ayahsMin/ayahsMax/surahsMin/surahsMax` (`int?`) to `GetUniqueWordsPageQuery` + validation (`Min >= 0`, `Max >= Min`, else new `InvalidFilter` outcome) in `BE-APP/Quran/Words/Queries/GetUniqueWordsPage/`; controller params in `BE-API/Controllers/Words/UniqueWordsController.cs`; Arabic message in `BE-API/Common/ApiMessages.cs`.
- [x] T030 [US5] Unique Words reader: SQL range predicates on `occurrences_count`/`ayahs_count`/`surahs_count` inside `BuildTashkeelQuery`/`BuildSimpleQuery` in `BE-INF/Persistence/Reads/Quran/Words/EfUniqueWordsReader.cs` (parameter values only); list cache keys gain the range components in `BE-INF/Caching/Quran/Words/UniqueWordsCacheKeys.cs` + `CachedUniqueWordsReader.cs` (absent ⇒ pre-feature key).
- [x] T031 [P] [US5] Roots: range params (`occ/ayahs/surahs/simpleWords/tashkeelWords/lemmas/stems`) through `BE-APP/Quran/Words/Roots/Queries/GetRootsPage/` (+ `InvalidFilter` outcome) and `BE-API/Controllers/Words/RootsController.cs`; in-memory predicates in `BE-INF/Persistence/Reads/Quran/Words/Roots/RootsListDerivation.cs` (`FilterAndSort`). No backend cache-key change (whole-summary cached).
- [x] T032 [P] [US5] Lemmas: same pattern (`… + stems`, minus `lemmas`) through `BE-APP/Quran/Words/Lemmas/Queries/GetLemmasPage/`, `BE-API/Controllers/Words/LemmasController.cs`, and the lemmas list derivation in `BE-INF/Persistence/Reads/Quran/Words/Lemmas/`.
- [x] T033 [P] [US5] Stems: same pattern (five metrics) through `BE-APP/Quran/Words/Stems/Queries/GetStemsPage/`, `BE-API/Controllers/Words/StemsController.cs`, and the stems list derivation in `BE-INF/Persistence/Reads/Quran/Words/Stems/`.
- [x] T034 [US5] Backend tests: validation matrix (min>max → 400; negative → 400; open-ended OK) + predicate correctness per metric per page, in `BE-TST/Quran/Words/` (unique), `BE-TST/Quran/WordsRoots/`, `BE-TST/Quran/WordsMorphologyExplorers/` (lemmas+stems). Filtered `TotalCount` equals filtered row count (stat contract). Plus one AND-composition case per page: range + `search` active together narrow to the intersection (data-driven, one case each).

### Implementation — frontend

- [x] T035 [US5] New shared component `WORDS/components/explorer-count-range-filter/` (`.ts/.html/.scss/.spec.ts`): per-metric row of preset bucket chips (buttons with `aria-pressed`) + "مخصّص" revealing min/max numeric inputs; RTL; disabled while list loading; emits canonical range. Bucket preset constants (disjoint, from spec Clarifications): occurrences `1 · 2–10 · 11–100 · 101–1000 · 1001+`; ayahs/surahs `1 · 2–10 · 11–50 · 51+`; sub-counts `1 · 2–5 · 6–20 · 21+` — defined once in a NEW `WORDS/models/words-filter-presets.ts` (presets are config, not labels; do not put them in `words-shared.labels.ts`).
- [x] T036 [US5] URL range grammar `min..max` (either side omissible; malformed ⇒ absent; change resets page) in the four url-sync modules: `WORDS/state/unique-words-url-sync.ts` (`occ/ayahs/surahs`), `roots-url-sync.ts` (7 keys), `lemmas-url-sync.ts` (6), `stems-url-sync.ts` (5) + their four spec files (parse/serialize/fail-closed/restore + one backward-compat case per module: pre-feature URLs without range keys parse identically to today).
- [x] T037 [US5] Thread ranges through the four data-access + state layers: `WORDS/data-access/{unique-words,roots,lemmas,stems}.api.ts` (send `<k>Min/<k>Max` only when set), facades (`unique-words.facade.ts`, `roots-explorer.facade.ts`, `lemmas-explorer.facade.ts`, `stems-explorer.facade.ts`), caches (`unique-words-cache.ts`, `roots-cache.ts`, `lemmas-cache.ts`, `stems-cache.ts` — list keys gain range components) + all specs.
- [x] T038 [US5] Wire the filter row into the four page templates (collapsible row under the toolbar) + page specs (chips↔custom↔URL round-trip; stat updates).
- [x] T039 [US5] Update words README (new URL keys + grammar) and reads README (range predicates; which count family they filter) in the same commit.

**Checkpoint**: ranges live on four pages; stat + pagination + URL agree.

---

## Phase 8: User Story 6 — Word Types has-root/has-stem/has-lemma (Priority: P2)

**Goal**: tri-state presence flags as part of the Word Types list scope (words +
grouped views reshape together; later US8 counts inherit).

**Independent Test**: quickstart.md §P2 step 3 — hasRoot=missing → only rootless word
rows; grouped views reflect the same narrowed base; URL restores.

### Implementation

- [x] T040 [US6] Backend: `bool? hasRoot/hasStem/hasLemma` through `GetWordTypeRowsQuery`/`GetWordTypeTableQuery` + `WordTypesHandlerValidation.cs` + allowlisted `m.root_id|stem_id|lemma_id IS [NOT] NULL` predicates in `BaseRowsSql` (`EfWordTypesReader.Sql.cs`) + `WordTypesController.cs` params + `WordTypesCacheKeys.cs`/`CachedWordTypesReader.cs` key components.
- [x] T041 [US6] Backend tests in `BE-TST/Quran/WordsWordTypes/`: each flag true/false/absent reshapes words rows AND grouped rows/totals identically; flags compose with search + case/tense/voice; cache isolation.
- [x] T042 [US6] Frontend: URL keys `hasRoot/hasStem/hasLemma` (`true|false`, absent=any, fail-closed) in `word-types.models.ts` + `word-types-url-sync.ts` (+spec incl. one backward-compat case: pre-feature URLs without flag keys parse identically); api/facade/cache threading (+specs); tri-state filter UI on the word-types page (reuse the chip pattern from T035; labels per lock D in `word-types.labels.ts`).
- [x] T043 [US6] Update words README + reads README (flags are list-scope like case/tense/voice). **Plan-P2 checkpoint**: full test suites green; commit plan-P2: `feat(words): count-range filters + result-count stat`.

**Checkpoint**: plan-P2 committed — filters + stat shipped.

---

## Phase 9: User Story 7 — association filters (Priority: P3)

**Goal**: Unique Words by primary word type / primary root (base-query predicates that
can never disagree with displayed chips); Lemmas by root FK; Stems by primary
root/lemma with honest labels.

**Independent Test**: quickstart.md §P3 — filter by type → every row's chip equals the
filter; lemmas by root → only that root's lemmas; unmatched valid id → empty page + 0.

### Implementation — backend

- [x] T044 [US7] Unique Words: `primaryType` (catalogue-validated POS code) + `rootId` (positive int) through `GetUniqueWordsPageQuery/Handler` + `UniqueWordsController.cs`; base-SQL predicates in `EfUniqueWordsReader.cs` reproducing EXACTLY the primary-selection rules of `LoadPrimaryWordTypesAsync`/`LoadPrimaryRootsAsync` (one shared SQL shape for enrichment + predicate); `UniqueWordsCacheKeys.cs` gains both. (POS validation via new `IPosTagCatalogueReader`/`EfPosTagCatalogueReader`.)
- [x] T045 [US7] Agreement tests in `BE-TST/Quran/Words/UniqueWordsAssociationFilterTests.cs`: for every filtered row, the displayed primary type/root equals the filter value (the chip⇔filter invariant); valid-but-unmatched id → 200 empty page `TotalCount=0`; invalid POS code / nonpositive id → 400.
- [x] T046 [P] [US7] Lemmas: `rootId` through `GetLemmasPage` query/handler/controller + in-memory FK predicate (`RootId` on the summary row) in the lemmas derivation. Stems: `rootId`/`lemmaId` through `GetStemsPage` + primary-association predicates in the stems derivation. (New `LemmasAssociationFilter`/`StemsAssociationFilter` value objects.)
- [x] T047 [US7] Backend tests in `BE-TST/Quran/WordsMorphologyExplorers/MorphologyAssociationFilterTests.cs`: lemma FK filtering; stems primary-association filtering (a stem whose primary root differs is excluded even if co-occurring — pins the primary-not-sole semantics via seed S602).
- [x] T048 [US7] Frontend: URL keys (`primaryType`, `rootId` on unique; `rootId` on lemmas; `rootId`/`lemmaId` on stems) fail-closed in the three url-sync modules (+specs, each with a backward-compat pre-feature-URL case); api/facade/cache threading; new shared `explorer-association-filter` search-select (+spec) fed by `WordsAssociationOptionsService` (root/lemma pickers reuse roots/lemmas apis+caches; type select flattens the word-types tree's noun/particle POS leaves — no new endpoint); backend `primaryType` validated against `quran_pos_tags`. Labels per lock D in the labels files.
- [x] T049 [US7] Update words README (keys + picker behavior) and reads README (explicit primary-not-sole sentence for the stems filter). **Plan-P3 checkpoint**: suites green (backend 1245, frontend 1094); committed plan-P3: `feat(words): association filters (primary type/root/lemma)`.

**Checkpoint**: plan-P3 committed.

---

## Phase 10: User Story 8 — Word Types scoped four-count summary (Priority: P4)

**Goal**: one new read returning words/roots/stems/lemmas counts for the full active
scope (type, childCode, case, tense, voice, search, has-flags); strip between filter
strip and tabs; counts always equal the four tableView totals.

**Independent Test**: quickstart.md §P4 — four counts equal the four tabs' pagination
totals for the identical scope (repeat with search + flag active); counts reload on
scope change only; strip retry refetches counts without touching the table.

### Implementation — backend

- [ ] T050 [P] [US8] New DTO `BE-ABS/Quran/Words/WordTypes/Responses/WordTypeScopeCountsDto.cs` (`WordsCount, RootsCount, StemsCount, LemmasCount` ints) + reader method signature on `BE-ABS/Quran/Words/WordTypes/IWordTypesReader.cs`.
- [ ] T051 [US8] New query/handler `BE-APP/Quran/Words/WordTypes/Queries/GetWordTypeScopeCounts/` (query record = full scope incl. search + flags; outcomes `Success | InvalidFilter`; validation via `WordTypesHandlerValidation`; `hasSearch`-only logging).
- [ ] T052 [US8] New reader partial `BE-INF/Persistence/Reads/Quran/Words/WordTypes/EfWordTypesReader.ScopeCounts.cs`: ONE SQL command — CTE over the scoped `BaseRowsSql` base (search + flags included), four aggregates reusing the EXISTING fragments: words = `RowsCountSql` formula (`COUNT(DISTINCT (unique_tashkeel_word_id, context_code))`), dimensions = `GroupedRowsCountSql` formula (`COUNT(DISTINCT <dim>_id)` excl. NULL). Do not re-derive; reference the shared SQL constants.
- [ ] T053 [US8] Caching: `WordTypesCacheKeys.ScopeCounts(...)` with EVERY scope input + `CachedWordTypesReader.cs` wrap; entry options mirror the table read (`WordTypesCacheEntryOptions.cs`).
- [ ] T054 [US8] Controller action `GET api/words/word-types/scope-counts` in `BE-API/Controllers/Words/WordTypesController.cs` + Arabic messages in `BE-API/Common/ApiMessages.cs`; zero-row valid scope → 200 all-zeros; invalid scope → 400.
- [ ] T055 [US8] Backend tests in `BE-TST/Quran/WordsWordTypes/`: EQUALITY MATRIX — for scopes covering each main type, a child, case/tense/voice variants, ±search, ±has-flags: each count equals the corresponding tableView's `TotalCount`; single-SQL-command budget pinned via the `SqlCommandCountInterceptor` pattern (`BE-TST/Quran/Words/SqlCommandCountInterceptor.cs`); cache-key isolation per scope input; count-family audit (no `words_count`-backed values anywhere in the read).

### Implementation — frontend

- [ ] T056 [US8] Regenerate the frontend API client (`FE/core/api/generated/`) per the repo api-contract staleness guard so `WordTypeScopeCountsDto` is generated; re-export per the words models convention.
- [ ] T057 [US8] `WORDS/data-access/word-types.api.ts`: `getScopeCounts(scope)`; `WORDS/state/word-types-explorer.facade.ts`: load counts on scope change ONLY (type/childCode/case/tense/voice/search/flags — NOT tableView, NOT page); `WORDS/state/word-types-cache.ts`: scope-counts key mirroring backend components; update the three spec files (incl. "tabs/page changes trigger no counts fetch").
- [ ] T058 [US8] New component `WORDS/components/word-type-scope-counts/` (`.ts/.html/.scss/.spec.ts`): four labeled counts reusing the existing view tabs' SHORT labels verbatim — RTL order كلمات | جذور | أصول | صيغ (tabs NOT renamed; spec Clarifications; labels in `word-types.labels.ts`, TDZ getters); non-interactive; states — loading skeleton / compact error + "إعادة المحاولة" retry (refetches counts only) / zeros. Place in `word-types-explorer-page.component.html` BETWEEN the filter strip and the table-view tabs; mounted-shell invariant preserved; counts failure never blocks the table; table failure hides the strip numbers.
- [ ] T059 [US8] Update words README (strip, URL/cache identity, states) + reads README (new read, scoped family, 1-command budget).
- [ ] T060 [US8] Perf gate: time `/scope-counts` on widest scopes (`type=noun` and `type=verb`, unscoped, empty search). Budget: **p95 ≤ 2s uncached**; confirm 1 SQL command; run deploy-smoke. **Plan-P4 checkpoint**: suites green; commit plan-P4: `feat(words): word-types scoped four-count summary`.

**Checkpoint**: plan-P4 committed — all stories live.

---

## Phase 11: Polish & Cross-Cutting

- [ ] T061 [P] Cross-phase count-family audit: grep/read every new surface — no `words_count`-backed number on any Word Types surface; no scoped word-context count on the four normal explorers' stat line (spec SC-007).
- [ ] T062 [P] Ordering-untouched assertion: existing ordering tests green (`MorphologyRelatedItemsOrdering`, `*ListDerivation` sorts, `WordTypeSort`); filter predicates are pure `Where`s — confirm no `OrderBy` was added/moved.
- [ ] T063 Run the full quickstart.md validation end-to-end (all four phase smokes + perf gates recorded) and the clean-code + test-code self-checks from root `CLAUDE.md` before requesting `engineering-review`.

---

## Dependencies & Execution Order

### Phase dependencies

```
Setup (T001)
  └─► Foundational (T002–T003)
        └─► US1 search (T004–T014) ─┐
        └─► US2 1000 rows (T015–T020) ─┼─► T024 plan-P1 commit
        └─► US3 100 details (T021–T023) ─┘
              │
              ├─► US4 stat (T025–T028)      [independent of US5/US6]
              ├─► US5 ranges (T029–T039)    [independent of US4]
              └─► US6 has-flags (T040–T043) [after US1: same BaseRowsSql/keys]
                    └─► T043 plan-P2 commit (US4+US5+US6 done)
                          ├─► US7 associations (T044–T049) [needs US5 filter UI]
                          │     └─► T049 plan-P3 commit
                          └─► US8 scope counts (T050–T060) [needs US1 search + US6 flags in scope]
                                └─► T060 plan-P4 commit
                                      └─► Polish (T061–T063)
```

- US1/US2/US3 share files (`WordTypesHandlerValidation.cs`, `WordTypesController.cs`, `word-types.models.ts`) — do them sequentially in one worktree, commit together (plan-P1).
- US4 is frontend-only and independent — may run in parallel with US5.
- US6 must follow US1 (both edit `BaseRowsSql` + cache keys).
- US8 must follow US1 + US6 (scope = search + flags); may run in parallel with US7.

### Parallel opportunities

- T010 (frontend URL key) parallel with T004–T008 (backend) inside US1.
- T031/T032/T033 (roots/lemmas/stems backend ranges) — three parallel tasks, disjoint files.
- T025/T026 parallel; US4 as a whole parallel with US5.
- T046 parallel with T044; T050 parallel with anything before T051.
- T061/T062 parallel.

### Parallel example — US5 backend

```bash
Task: "T031 Roots range params + RootsListDerivation predicates"
Task: "T032 Lemmas range params + lemmas derivation predicates"
Task: "T033 Stems range params + stems derivation predicates"
```

---

## Implementation Strategy

- **MVP = plan-P1 (US1+US2+US3)**: Word Types parity alone is a complete, shippable
  increment (T001–T024). STOP, validate via quickstart §P1, deploy-smoke, commit.
- **Incremental**: plan-P2 (US4+US5+US6) → plan-P3 (US7) → plan-P4 (US8); each ends
  in one commit, full suites green, READMEs updated in the same commit.
- **Stop conditions** (decision record — report, don't redesign): schema-needing
  filter; search predicate breaking grouped-summary byte-equivalence; scope counts
  unable to reuse grouped fragments; hard perf failure at default 1000; anything
  touching importer/migrations/Quran source.
- Quran-data safety throughout: identity = clean imlaei-simple; no invented Arabic
  content; search text never logged; test seeds stay source-safe.
