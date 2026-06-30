---
description: "Dependency-ordered implementation tasks for Quran Word Types Explorer (Feature 019)"
---

# Tasks: Quran Word Types Explorer

**Input**: Design documents from `specs/019-word-types-explorer/`
**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `quickstart.md`, and `contracts/` (`word-types-api.md`, `backend-read-abstractions.md`, `frontend-routing-state.md`).

**Branch**: Workspace branch `019-word-types-explorer`. Before implementation commits, align Backend and Frontend child repository branches with the workspace commit workflow.

**Tests**: Included because the specification and contracts require query/count correctness, row-context identity, URL restoration, accessibility, and non-regression coverage. Tests are checkpoint tasks at the end of each story, not test-first/TDD.

**Path rule**: All paths below are relative to `/projects/Dashboard/App` unless a task explicitly says otherwise.

---

## Implementer Guide - Read Before Every Phase

Use these sibling features as copy/reference material before editing:

- Backend Roots template: `Backend/api/QuranDashboard.Api/Controllers/Words/RootsController.cs`, `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/Roots/`, `Backend/application/QuranDashboard.Application/Quran/Words/Roots/Queries/`, `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/Roots/`, `Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Quran/Words/Roots/`, `Backend/infrastructure/QuranDashboard.Infrastructure/DependencyInjection/RootsDependencyInjection.cs`, and `Backend/tests/QuranDashboard.Tests/Quran/WordsRoots/`.
- Backend Lemmas/Stems template: `Backend/api/QuranDashboard.Api/Controllers/Words/LemmasController.cs`, `Backend/api/QuranDashboard.Api/Controllers/Words/StemsController.cs`, `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/Lemmas/`, `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/Stems/`, `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/Lemmas/`, `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/Stems/`, and `Backend/tests/QuranDashboard.Tests/Quran/WordsMorphologyExplorers/`.
- Frontend Roots/Lemmas/Stems template: `Frontend/quran-dashboard-ui/src/app/features/words/pages/roots-explorer-page/`, `pages/lemmas-explorer-page/`, `pages/stems-explorer-page/`, `state/roots-*.ts`, `state/lemmas-*.ts`, `state/stems-*.ts`, `data-access/roots.api.ts`, `data-access/lemmas.api.ts`, `data-access/stems.api.ts`, and matching table/detail components.
- Shared frontend pieces to reuse: `Frontend/quran-dashboard-ui/src/app/features/words/components/highlighted-ayah/`, `components/ayah-matches-list/`, `components/surah-occurrences-list/`, `components/missing-surahs-list/`, `components/word-count-chip/`, `Frontend/quran-dashboard-ui/src/app/shared/ui/pagination/`, and `Frontend/quran-dashboard-ui/src/app/shared/url/deep-link-href.ts`.
- Existing per-occurrence analysis endpoint and frontend API: `Backend/application/QuranDashboard.Application.Abstractions/Quran/MushafReader/Responses/WordAnalysisResponse.cs`, `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/MushafReader/EfWordAnalysisReader.cs`, and `Frontend/quran-dashboard-ui/src/app/features/mushaf/data-access/mushaf-word-analysis.api.ts`.

Non-negotiable rules:

1. Read-only only. Do not add writes, migrations, indexes, importers, DataPipeline changes, or Quran text changes.
2. Use `quran_word_morphology` word-level rows joined to `quran_words` and `quran_pos_tags`; do not read segment/prefix/suffix tables for type buckets, filters, or counts.
3. Particle parent `حرف وأداة` must exclude `HeadPos = 'INL'`; `حروف مقطّعة` is its own leaf.
4. A table row is `displayed tashkeel word + resolved context`, never displayed word alone. Do not create dominant or mixed rows.
5. Tree/filter counts are distinct word-context row counts. Table columns are occurrence/ayah/surah counts scoped to that row context.
6. `contextCode` is required for selected-row summary, ayahs, surahs, and URL restoration.
7. Words display Uthmani with tashkeel only. Do not add Simple/without-tashkeel display toggles.
8. POS child labels come from `quran_pos_tags.ArabicLabel`. Only the four main type labels and secondary filter labels may be static UI labels.
9. Backend logs and cache keys must not include Quran text, word text, root/lemma/stem text, raw search text, SQL, or payloads.
10. Frontend child components are presentational; facades own API orchestration, URL state, loading, error, empty, not-found, and selected-row state.
11. Use existing `qd-*` primitives, CSS logical properties, and shared pagination. Do not create a new design system or palette.
12. Tests must use source-safe seed data. Do not invent Quran text, morphology, roots, lemmas, stems, or labels.

---

## Phase 1: Setup

**Purpose**: Establish known repository, data, and build state before feature files are added.

- [X] T001 Inspect branch/status for `/projects/Dashboard/App`, `Backend/`, and `Frontend/quran-dashboard-ui/`; confirm they are on or intentionally aligned with `019-word-types-explorer`, and do not stage, commit, switch, or discard changes in `/projects/Dashboard/App`
- [X] T002 Run the pre-implementation `PRO` data gate exactly as documented in `specs/019-word-types-explorer/quickstart.md`; stop implementation if live `quran_pos_tags` does not return `PRO`, `حرف نهي`, and `particle`
- [X] T003 Run the baseline backend and frontend build commands from `specs/019-word-types-explorer/quickstart.md`; record any pre-existing failure before editing `Backend/` or `Frontend/quran-dashboard-ui/`

**Checkpoint**: Branch, live-data gate, and baseline build status are known.

---

## Phase 2: Foundational - Blocking Prerequisites

**Purpose**: Create contracts, read-boundary skeletons, cache/DI/controller shells, test fixture, frontend route/state shells, and reusable UI shells. No story behavior is complete until later phases.

**Critical**: Complete this phase before any user-story phase.

### Backend contracts and wiring

- [X] T004 [P] Create `WordTypeFilter` with `Type`, `ChildCode`, `Case`, `Tense`, and `Voice` validation-ready fields in `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/WordTypes/WordTypeFilter.cs`
- [X] T005 [P] Create `WordTypeRowIdentity` with positive `TashkeelWordId`, required `ContextCode`, and active `Case`/`Tense`/`Voice` fields in `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/WordTypes/WordTypeRowIdentity.cs`
- [X] T006 [P] Create `WordTypeSort`, `WordTypeSortKeys`, and `WordTypeSortParser` for `occurrences`, `ayahs`, `surahs`, `mushaf-order`, and `alpha` in `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/WordTypes/WordTypeSort.cs`
- [X] T007 [P] Create response DTOs from `contracts/word-types-api.md`: `WordTypeTreeDto`, `WordTypeRowDto`, `WordTypeSummaryDto`, `WordTypeAyahMatchDto`, and `WordTypeSurahsResponse` in `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/WordTypes/Responses/`
- [X] T008 Create `IWordTypesReader` with `GetTreeAsync`, `GetRowsAsync`, `GetSummaryAsync`, `GetAyahMatchesAsync`, and `GetSurahsAsync` exactly per `contracts/backend-read-abstractions.md` in `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/WordTypes/IWordTypesReader.cs`
- [X] T009 [P] Add centralized Arabic success, validation, invalid-filter, invalid-identity, not-found, and paging messages for Word Types in `Backend/api/QuranDashboard.Api/Common/ApiMessages.cs`; do not hardcode user-facing messages in controllers or handlers
- [X] T010 [P] Create `GetWordTypeTreeQuery.cs`, `GetWordTypeTreeHandler.cs`, and `GetWordTypeTreeOutcome.cs` with safe structured logging fields in `Backend/application/QuranDashboard.Application/Quran/Words/WordTypes/Queries/GetWordTypeTree/`
- [X] T011 [P] Create `GetWordTypeRowsQuery.cs`, `GetWordTypeRowsHandler.cs`, and `GetWordTypeRowsOutcome.cs` with filter, sort, and paging validation placeholders in `Backend/application/QuranDashboard.Application/Quran/Words/WordTypes/Queries/GetWordTypeRows/`
- [X] T012 [P] Create query, handler, and outcome skeleton files for `GetWordTypeSummary`, `GetWordTypeAyahs`, and `GetWordTypeSurahs` in `Backend/application/QuranDashboard.Application/Quran/Words/WordTypes/Queries/GetWordTypeSummary/`, `GetWordTypeAyahs/`, and `GetWordTypeSurahs/`
- [X] T013 Create `EfWordTypesReader.cs` and `WordTypeGrouping.cs` skeletons implementing `IWordTypesReader`, injecting `QuranDashboardDbContext`, using `AsNoTracking`, and throwing explicit `NotImplementedException` from each method in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/WordTypes/`
- [X] T014 [P] Create `WordTypesCacheKeys.cs`, `WordTypesCacheEntryOptions.cs`, and delegating `CachedWordTypesReader.cs` under a bounded `wordtypes:` namespace in `Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Quran/Words/WordTypes/`; do not change global cache registration
- [X] T015 Create `WordTypesDependencyInjection.AddWordTypes()` registering `EfWordTypesReader` wrapped by `CachedWordTypesReader`, then call it from `Backend/infrastructure/QuranDashboard.Infrastructure/DependencyInjection.cs` using `Backend/infrastructure/QuranDashboard.Infrastructure/DependencyInjection/WordTypesDependencyInjection.cs`
- [X] T016 Create a thin `WordTypesController` shell with route base `api/words/word-types`, constructor-injected handlers, and no EF/infrastructure logic in `Backend/api/QuranDashboard.Api/Controllers/Words/WordTypesController.cs`

### Backend test foundation

- [X] T017 Create the Word Types Testcontainers fixture, xUnit collection, and fixture-start smoke test by mirroring `WordsMorphologyExplorers` in `Backend/tests/QuranDashboard.Tests/Quran/WordsWordTypes/WordTypesTestFixture.cs`, `WordTypesCollection.cs`, and `WordTypesFixtureSmokeTests.cs`
- [X] T018 Create source-safe embedded seed data in `Backend/tests/QuranDashboard.Tests/Quran/WordsWordTypes/word-types-explorer-seed.sql` covering: PRO particle, INL disconnected letters, noun/verb/particle rows, one out-of-bucket POS row, a same-tashkeel multi-context word, case values including null, verb tense and voice, ayah markers, null root/lemma/stem, and surah distribution; use verified repository data only

### Frontend contracts, routes, and shells

- [X] T019 [P] Create `word-types.models.ts` with API DTOs, `ApiResponse` payload types, `PagedResult` shape, list/detail state, query keys, page sizes, type guards, and row identity types in `Frontend/quran-dashboard-ui/src/app/features/words/models/word-types.models.ts`
- [X] T020 [P] Create `word-types.labels.ts` with static UI labels for main types, secondary filter options, table headers, tabs, empty/error states, and accessible names in `Frontend/quran-dashboard-ui/src/app/features/words/models/word-types.labels.ts`; do not duplicate these strings in templates
- [X] T021 [P] Create `parseWordTypesQueryParams`, `buildWordTypesQueryParams`, `clearWordTypesSelection`, and `buildWordTypesDeepLink` in `Frontend/quran-dashboard-ui/src/app/features/words/state/word-types-url-sync.ts`
- [X] T022 [P] Create typed `WordTypesApi` methods for E1-E5 returning `Observable<ApiResponse<T>>` in `Frontend/quran-dashboard-ui/src/app/features/words/data-access/word-types.api.ts`
- [X] T023 [P] Create `WordTypesCache` over the existing API response cache with keys for tree, rows, summary, ayahs, surahs, and analysis view reuse in `Frontend/quran-dashboard-ui/src/app/features/words/state/word-types-cache.ts`
- [X] T024 [P] Create a pure context-scoped ayah mapper that passes backend `matchedWordPositions`/word ids to `highlighted-ayah` without string replacement in `Frontend/quran-dashboard-ui/src/app/features/words/utils/word-type-ayah-match.mapper.ts`
- [X] T025 [P] Create `WordTypesExplorerFacade` and `WordTypesDetailFacade` shells with signals/state for normalized query, loading, empty, error, not-found, rows, tree, selected row, active detail view, and no eager detail calls in `Frontend/quran-dashboard-ui/src/app/features/words/state/word-types-explorer.facade.ts` and `word-types-detail.facade.ts`
- [X] T026 [P] Create empty presentational component shell files `word-type-filter.component.ts`, `word-type-filter.component.html`, and `word-type-filter.component.scss` in `Frontend/quran-dashboard-ui/src/app/features/words/components/word-type-filter/`
- [X] T027 [P] Create empty presentational component shell files for `word-types-table.component.*` and `word-type-details-panel.component.*` in `Frontend/quran-dashboard-ui/src/app/features/words/components/word-types-table/` and `Frontend/quran-dashboard-ui/src/app/features/words/components/word-type-details-panel/`
- [X] T028 Add `WORDS_TYPES_SEGMENT`, `wordTypesRoutePath()`, lazy `WORDS_TYPES_ROUTE`, and routeable page shell files `word-types-explorer-page.component.ts`, `.html`, and `.scss` in `Frontend/quran-dashboard-ui/src/app/core/navigation/route-paths.ts`, `Frontend/quran-dashboard-ui/src/app/features/words/words.routes.ts`, and `Frontend/quran-dashboard-ui/src/app/features/words/pages/word-types-explorer-page/`
- [X] T029 Run CP-0 verification from `specs/019-word-types-explorer/quickstart.md`: backend build, `WordTypesFixtureSmokeTests`, frontend build, and any route-shell tests; fix only skeleton wiring errors in `Backend/` and `Frontend/quran-dashboard-ui/`

**Checkpoint CP-0**: Contracts compile, `/dashboard/words/types` resolves to an empty shell, and the Word Types test fixture starts PostgreSQL.

---

## Phase 3: User Story 1 - Browse Words by Main Type (Priority: P1) - MVP

**Goal**: The page shows the four main type filters with row counts, defaults to `noun`, and displays a paged word-context table for each selected main type.

**Independent Test**: Open `/dashboard/words/types`, select `اسم`, `فعل`, `حرف وأداة`, and `حروف مقطّعة`, and confirm the table contains only matching word-context rows, uses Uthmani-with-tashkeel display, and `tree count == table totalCount` for each main type.

### Backend main-type reads

- [X] T030 [US1] Implement main-type predicates and E1 parent counts in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/WordTypes/EfWordTypesReader.cs`: noun uses POS category `noun`, verb uses `IsVerb`, particle uses POS category `particle` with `HeadPos <> "INL"`, INL uses `HeadPos = "INL"`, and out-of-bucket POS rows are excluded from all four buckets
- [X] T031 [US1] Implement E2 main-type row grouping, row counts, paging, default `occurrences` sort, deterministic tie-breaks, marker exclusion, and `contextCode` emission in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/WordTypes/EfWordTypesReader.cs` and `WordTypeGrouping.cs`
- [X] T032 [US1] Implement row enrichment for `rootText`, `lemmaText`, and `stemText`: return root where source data provides it, allow lemma/stem winners to return null if deferred in v1, and never drop a row because any enrichment value is null in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/WordTypes/EfWordTypesReader.cs`
- [X] T033 [US1] Implement `wordtypes:tree` and `wordtypes:rows:{filter-hash}:sort:{sort}:p{page}:s{pageSize}` caching without raw text in `Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Quran/Words/WordTypes/CachedWordTypesReader.cs` and `WordTypesCacheKeys.cs`
- [X] T034 [US1] Complete `GetWordTypeTree` and `GetWordTypeRows` handlers with missing-type default `noun`, invalid type/sort/paging outcomes, and safe structured logs in `Backend/application/QuranDashboard.Application/Quran/Words/WordTypes/Queries/GetWordTypeTree/` and `GetWordTypeRows/`
- [X] T035 [US1] Add `GET api/words/word-types/tree` and `GET api/words/word-types/words` actions with `200/400` mappings and `ApiResponse<T>` wrapping in `Backend/api/QuranDashboard.Api/Controllers/Words/WordTypesController.cs`

### Frontend main-type browse

- [X] T036 [US1] Implement tree and row loading, `ApiResponse` mapping, default `type=noun`, sort/page actions, no eager detail calls, and selected-row clearing on filter/sort change in `Frontend/quran-dashboard-ui/src/app/features/words/state/word-types-explorer.facade.ts`
- [X] T037 [US1] Implement main label selection, count display, current-state styling, keyboard operation, and expand-arrow placeholders in `Frontend/quran-dashboard-ui/src/app/features/words/components/word-type-filter/word-type-filter.component.ts`, `.html`, and `.scss`
- [X] T038 [US1] Implement the table columns `الكلمة`, `النوع`, `الجذر`, `الصيغة`, `الأصل`, `المواضع`, `الآيات`, and `السور`, using `word-count-chip`, neutral placeholders for null enrichment, and no visible backend IDs in `Frontend/quran-dashboard-ui/src/app/features/words/components/word-types-table/word-types-table.component.ts`, `.html`, and `.scss`
- [X] T039 [US1] Compose the table-first split page, shared pagination, loading/empty/error states, and Words hub card link in `Frontend/quran-dashboard-ui/src/app/features/words/pages/word-types-explorer-page/word-types-explorer-page.component.ts`, `.html`, `.scss`, `Frontend/quran-dashboard-ui/src/app/features/words/pages/words-hub-page/words-hub-page.component.ts`, and `.html`

### US1 checkpoint tests

- [X] T040 [P] [US1] Add backend tests for four main types, particle excluding INL, INL isolation, out-of-bucket POS exclusion, marker exclusion, tree count equals table `TotalCount` with no secondary filter, scoped occurrence/ayah/surah columns, null/deferred enrichment rows retained, invalid type/sort/paging, cache hit, and log redaction in `Backend/tests/QuranDashboard.Tests/Quran/WordsWordTypes/WordTypesMainReadTests.cs`, `WordTypesCacheReadTests.cs`, and `WordTypesLoggingTests.cs`
- [X] T041 [P] [US1] Add frontend tests for route default `type=noun`, main type selection, table columns, Uthmani-with-tashkeel display only, no Simple toggle, no visible IDs, no eager detail calls, list loading/empty/error states, and Words hub access in `Frontend/quran-dashboard-ui/src/app/features/words/pages/word-types-explorer-page/word-types-explorer-page.component.spec.ts`, `components/word-type-filter/word-type-filter.component.spec.ts`, and `components/word-types-table/word-types-table.component.spec.ts`

**Checkpoint CP-1 / Suggested MVP**: US1 is usable and independently testable.

---

## Phase 4: User Story 2 - Inspect a Selected Word's Details (Priority: P2)

**Goal**: Selecting a row opens the details card for that exact word-context row, with summary, ayahs, surahs, and reused per-occurrence analysis.

**Independent Test**: Select a row, open `الآيات`, `السور`, and `التحليل`, and confirm each view is scoped to `tashkeelWordId + contextCode + active feature`, not all usages of the displayed spelling.

### Backend selected-row reads

- [X] T042 [US2] Implement E3 summary lookup for exact `WordTypeRowIdentity`, returning null for valid not-found identities and rejecting missing/invalid identity values in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/WordTypes/EfWordTypesReader.cs`
- [X] T043 [US2] Implement E4 ayah matches with exact row-context occurrence filtering, paged distinct ayahs, batched page-word loading, and no per-ayah query loop in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/WordTypes/EfWordTypesReader.cs`
- [X] T044 [US2] Implement E5 surah distribution and missing-surahs complement for the exact row context in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/WordTypes/EfWordTypesReader.cs`
- [X] T045 [US2] Add `wordtypes:summary:{identity-hash}`, `wordtypes:ayahs:{identity-hash}:p{page}:s{pageSize}`, and `wordtypes:surahs:{identity-hash}` caching in `Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Quran/Words/WordTypes/CachedWordTypesReader.cs` and `WordTypesCacheKeys.cs`
- [X] T046 [US2] Complete `GetWordTypeSummaryHandler.cs`, `GetWordTypeAyahsHandler.cs`, `GetWordTypeSurahsHandler.cs`, and controller actions with `200/400/404` mappings in `Backend/application/QuranDashboard.Application/Quran/Words/WordTypes/Queries/GetWordTypeSummary/`, `GetWordTypeAyahs/`, `GetWordTypeSurahs/`, and `Backend/api/QuranDashboard.Api/Controllers/Words/WordTypesController.cs`

### Frontend selected-row details

- [X] T047 [US2] Implement lazy summary, ayah, surah, and per-occurrence analysis loading for the active selected row in `Frontend/quran-dashboard-ui/src/app/features/words/state/word-types-detail.facade.ts`, reusing `Frontend/quran-dashboard-ui/src/app/features/mushaf/data-access/mushaf-word-analysis.api.ts` for the `analysis` view
- [X] T048 [US2] Render the details summary and tabs `الآيات الخاصة بالكلمة`, `السور`, and `التحليل` in `Frontend/quran-dashboard-ui/src/app/features/words/components/word-type-details-panel/word-type-details-panel.component.ts`, `.html`, and `.scss`
- [X] T049 [US2] Wire row select, count chips, ayah pagination, surah view, analysis occurrence selection, and context-scoped highlight mapping in `Frontend/quran-dashboard-ui/src/app/features/words/pages/word-types-explorer-page/word-types-explorer-page.component.ts`, `components/word-types-table/word-types-table.component.ts`, `components/word-type-details-panel/word-type-details-panel.component.ts`, and `utils/word-type-ayah-match.mapper.ts`

### US2 checkpoint tests

- [X] T050 [P] [US2] Add backend tests for summary, ayah matches, surah distribution, missing surahs, row-context scoping, no widening to all usages, unknown identity not-found, invalid identity bad-request, and bounded SQL command counts in `Backend/tests/QuranDashboard.Tests/Quran/WordsWordTypes/WordTypesDetailsReadTests.cs`
- [X] T051 [P] [US2] Add frontend tests for lazy detail calls, exact selected-row identity payloads, tab switching, ayah highlight input, surah list rendering, analysis API reuse, controlled not-found, and panel empty/error states in `Frontend/quran-dashboard-ui/src/app/features/words/components/word-type-details-panel/word-type-details-panel.component.spec.ts` and `pages/word-types-explorer-page/word-types-explorer-page.component.spec.ts`

**Checkpoint CP-2**: US1 and US2 work independently.

---

## Phase 5: User Story 3 - Refine a Main Type by Subtype (Priority: P3)

**Goal**: Expanding parent types reveals supported children; selecting a child narrows rows and counts to that child.

**Independent Test**: Expand `اسم` and `فعل`, select each child such as `اسم علم` or `أمر`, and confirm the table total equals the child count and is a strict subset of the parent.

### Backend subtype reads

- [ ] T052 [US3] Implement E1 child nodes: all noun-category POS children from `quran_pos_tags` ordered by `SortOrder`, verb tense children `past`, `present`, `imperative`, no particle children in v1, and INL as leaf in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/WordTypes/EfWordTypesReader.cs`
- [ ] T053 [US3] Implement E2 `childCode` filtering and grouping for noun POS children and verb tense children while preserving no-mixed-row behavior in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/WordTypes/EfWordTypesReader.cs` and `WordTypeGrouping.cs`
- [ ] T054 [US3] Add child-code validation, invalid-child outcomes, and safe logs to `Backend/application/QuranDashboard.Application/Quran/Words/WordTypes/Queries/GetWordTypeRows/GetWordTypeRowsHandler.cs` and `GetWordTypeTree/GetWordTypeTreeHandler.cs`

### Frontend subtype browse

- [ ] T055 [US3] Implement expand arrow behavior, child-node rendering, parent label selection, child selection, `aria-expanded`, and keyboard navigation in `Frontend/quran-dashboard-ui/src/app/features/words/components/word-type-filter/word-type-filter.component.ts`, `.html`, and `.scss`
- [ ] T056 [US3] Persist `childCode` in URL state, clear invalid child codes during normalization, reset page to `1`, and clear selected row when subtype changes in `Frontend/quran-dashboard-ui/src/app/features/words/state/word-types-url-sync.ts` and `word-types-explorer.facade.ts`

### US3 checkpoint tests

- [ ] T057 [P] [US3] Add backend tests for noun catalogue children, verb tense children, no particle children, INL leaf behavior, child count equals table `TotalCount`, child subset of parent, invalid child rejection, and exact `typeLabel` values from POS catalogue in `Backend/tests/QuranDashboard.Tests/Quran/WordsWordTypes/WordTypesSubtypeReadTests.cs`
- [ ] T058 [P] [US3] Add frontend tests for expand/collapse, parent-vs-child click behavior, child count display, child selection URL state, invalid child cleanup, and table reload on child changes in `Frontend/quran-dashboard-ui/src/app/features/words/components/word-type-filter/word-type-filter.component.spec.ts` and `state/word-types-url-sync.spec.ts`

**Checkpoint CP-3**: Subtype browse works independently on top of US1.

---

## Phase 6: User Story 4 - Apply Secondary Grammatical Filters (Priority: P4)

**Goal**: Nominal selections show case filters, verb selections show tense and voice filters, particle and INL selections show none, and filters narrow rows without crossing type boundaries.

**Independent Test**: Select noun plus `مجرور`, verb plus `مضارع` and `مجهول`, particle, and INL; verify only valid filter controls appear and rows match the selected grammatical feature.

### Backend secondary filters

- [ ] T059 [US4] Implement case, tense, and voice predicates in row and selected-row queries, including null case as `غير محدد`, in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/WordTypes/EfWordTypesReader.cs` and `WordTypeGrouping.cs`
- [ ] T060 [US4] Enforce filter validity rules: case only for noun, tense/voice only for verb, no secondary filters for particle or INL, and INL rejects child nodes in `Backend/application/QuranDashboard.Application/Quran/Words/WordTypes/Queries/GetWordTypeRows/GetWordTypeRowsHandler.cs` and selected-row handlers under `Backend/application/QuranDashboard.Application/Quran/Words/WordTypes/Queries/`
- [ ] T061 [US4] Ensure cache keys include secondary filters through the filter/identity hash and never reuse unfiltered row/detail data for filtered requests in `Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Quran/Words/WordTypes/WordTypesCacheKeys.cs`

### Frontend secondary filters

- [ ] T062 [US4] Render nominal case controls, verb tense/voice controls, and no controls for particle/INL in `Frontend/quran-dashboard-ui/src/app/features/words/components/word-type-filter/word-type-filter.component.ts`, `.html`, and `.scss`
- [ ] T063 [US4] Normalize secondary query params, ignore irrelevant filters, reset page to `1`, clear selected row, reload rows, update table `totalCount` and active UI count chips only, and do not request scoped tree counts on secondary filter changes in `Frontend/quran-dashboard-ui/src/app/features/words/state/word-types-url-sync.ts` and `word-types-explorer.facade.ts`

### US4 checkpoint tests

- [ ] T064 [P] [US4] Add backend tests for nominal case filter, `null` case filter, verb tense filter, verb voice filter, tense+voice combination, cross-type rejection, particle/INL rejection, context-scoped detail identity under secondary filters, and no E1 tree-count equality expectation while secondary filters are active in `Backend/tests/QuranDashboard.Tests/Quran/WordsWordTypes/WordTypesSecondaryFilterReadTests.cs`
- [ ] T065 [P] [US4] Add frontend tests for secondary filter visibility, URL normalization, filter changes clearing selection/page, ignored irrelevant filters, filtered row reloads, active UI count-chip updates, and no scoped tree-count request on secondary filter changes in `Frontend/quran-dashboard-ui/src/app/features/words/components/word-type-filter/word-type-filter.component.spec.ts`, `state/word-types-url-sync.spec.ts`, and `pages/word-types-explorer-page/word-types-explorer-page.component.spec.ts`

**Checkpoint CP-4**: Secondary filters work independently on top of US1.

---

## Phase 7: User Story 5 - Share and Restore an Exact View (Priority: P5)

**Goal**: A copied URL restores filters, sort/page, active detail tab, selected exact row, and selected analysis occurrence without collapsing same-spelling different-context rows.

**Independent Test**: Select a row for a word with multiple contexts, copy the URL, reload it, and confirm the same `word + contextCode + active feature` row and detail view restore.

### Exact URL restoration

- [ ] T066 [US5] Finalize selected-row identity handling so `tashkeelWordId`, `contextCode`, and active `case`/`tense`/`voice` reproduce the same E2 row in E3-E5 in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/WordTypes/EfWordTypesReader.cs`
- [ ] T067 [US5] Finalize `buildWordTypesDeepLink`, clear-selection behavior, canonical query param ordering, `view`, `detailPage`, `location`, and `column` handling in `Frontend/quran-dashboard-ui/src/app/features/words/state/word-types-url-sync.ts`
- [ ] T068 [US5] Implement route hydration, reload restoration, browser back/forward handling, selected-row cache reuse, and controlled not-found panel state in `Frontend/quran-dashboard-ui/src/app/features/words/state/word-types-explorer.facade.ts` and `word-types-detail.facade.ts`

### US5 checkpoint tests

- [ ] T069 [P] [US5] Add backend identity tests proving same-tashkeel different-context rows restore separately and never widen selected-row reads to all usages in `Backend/tests/QuranDashboard.Tests/Quran/WordsWordTypes/WordTypesRowIdentityTests.cs`
- [ ] T070 [P] [US5] Add frontend data-driven URL tests for defaults, invalid values, filter changes, clear selection, deep-link output, `word + contextCode`, active tab, detail page, analysis location, and exact restore in `Frontend/quran-dashboard-ui/src/app/features/words/state/word-types-url-sync.spec.ts` and `pages/word-types-explorer-page/word-types-explorer-page.component.spec.ts`

**Checkpoint CP-5**: All meaningful Word Types Explorer state is shareable and restorable.

---

## Phase 8: Polish and Cross-Cutting Concerns

**Purpose**: Harden logging, caching, accessibility, responsiveness, non-regression, and delivery verification across all stories intended to ship.

- [ ] T071 [P] Audit all Word Types handlers for required structured fields and forbidden text using `RecordingLoggerProvider` in `Backend/tests/QuranDashboard.Tests/Quran/WordsWordTypes/WordTypesLoggingTests.cs`
- [ ] T072 [P] Audit all Word Types cache entries and repeated-read SQL command counts, confirming no global cache configuration and no raw text keys, in `Backend/tests/QuranDashboard.Tests/Quran/WordsWordTypes/WordTypesCacheReadTests.cs`
- [ ] T073 [P] Complete responsive stacked/drawer behavior, Escape/focus return where applicable, tablist semantics, selected-row state beyond color, visible focus, logical CSS properties, polite loading regions, and reduced-motion-safe interactions in `Frontend/quran-dashboard-ui/src/app/features/words/pages/word-types-explorer-page/word-types-explorer-page.component.ts`, `Frontend/quran-dashboard-ui/src/app/features/words/pages/word-types-explorer-page/word-types-explorer-page.component.html`, `Frontend/quran-dashboard-ui/src/app/features/words/pages/word-types-explorer-page/word-types-explorer-page.component.scss`, `Frontend/quran-dashboard-ui/src/app/features/words/components/word-type-filter/word-type-filter.component.ts`, `Frontend/quran-dashboard-ui/src/app/features/words/components/word-type-filter/word-type-filter.component.html`, `Frontend/quran-dashboard-ui/src/app/features/words/components/word-type-filter/word-type-filter.component.scss`, `Frontend/quran-dashboard-ui/src/app/features/words/components/word-types-table/word-types-table.component.ts`, `Frontend/quran-dashboard-ui/src/app/features/words/components/word-types-table/word-types-table.component.html`, `Frontend/quran-dashboard-ui/src/app/features/words/components/word-types-table/word-types-table.component.scss`, `Frontend/quran-dashboard-ui/src/app/features/words/components/word-type-details-panel/word-type-details-panel.component.ts`, `Frontend/quran-dashboard-ui/src/app/features/words/components/word-type-details-panel/word-type-details-panel.component.html`, and `Frontend/quran-dashboard-ui/src/app/features/words/components/word-type-details-panel/word-type-details-panel.component.scss`
- [ ] T074 [P] Run non-regression checks for existing Roots, Lemmas, Stems, and Unique Words behavior using focused backend tests under `Backend/tests/QuranDashboard.Tests/Quran/WordsRoots/`, `Backend/tests/QuranDashboard.Tests/Quran/WordsMorphologyExplorers/`, `Backend/tests/QuranDashboard.Tests/Quran/Words/`, and frontend tests under `Frontend/quran-dashboard-ui/src/app/features/words/`
- [ ] T075 [P] Run a lightweight manual timing checkpoint for SC-001: after initial app bootstrap, selecting each main type in `/dashboard/words/types` shows the first row page within 2 seconds; record PASS/FAIL in the implementation summary using `specs/019-word-types-explorer/quickstart.md`
- [ ] T076 [P] Run a frontend/manual checkpoint for SC-011: from page open, reach a selected row's `الآيات` view and `التحليل` view in at most 4 interactions; record PASS/FAIL in the implementation summary using `specs/019-word-types-explorer/quickstart.md`
- [ ] T077 Run the full Feature 019 quickstart validation commands from `specs/019-word-types-explorer/quickstart.md`, then perform the clean-code and test-code self-checks referenced by `CODING_PRINCIPLES.md` and `.claude/skills/test-guard/`

---

## Dependencies and Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies. Must complete before editing.
- **Foundational (Phase 2)**: Depends on Setup. Blocks all user stories.
- **US1 (Phase 3)**: Depends on Foundational. This is the MVP.
- **US2 (Phase 4)**: Depends on Foundational plus row identity from US1; can be built after US1 table rows exist.
- **US3 (Phase 5)**: Depends on Foundational and can be built after US1 tree/rows exist.
- **US4 (Phase 6)**: Depends on Foundational and can be built after US1 tree/rows exist.
- **US5 (Phase 7)**: Depends on selected-row reads from US2 and URL state from Foundational.
- **Polish (Phase 8)**: Depends on all stories intended to ship.

### Shared-File Sequencing

- `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/WordTypes/EfWordTypesReader.cs`: T013 -> T030 -> T031 -> T032 -> T042 -> T043 -> T044 -> T052 -> T053 -> T059 -> T066.
- `Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Quran/Words/WordTypes/CachedWordTypesReader.cs`: T014 -> T033 -> T045 -> T061 -> T072.
- `Backend/api/QuranDashboard.Api/Controllers/Words/WordTypesController.cs`: T016 -> T035 -> T046.
- `Frontend/quran-dashboard-ui/src/app/features/words/state/word-types-url-sync.ts`: T021 -> T056 -> T063 -> T067 -> T070.
- `Frontend/quran-dashboard-ui/src/app/features/words/state/word-types-explorer.facade.ts`: T025 -> T036 -> T056 -> T063 -> T068.
- `Frontend/quran-dashboard-ui/src/app/features/words/components/word-type-filter/`: T026 -> T037 -> T055 -> T062 -> T073.

### Within Each Backend Endpoint Slice

1. Reader query logic.
2. Cache wrapper/key.
3. Application handler/outcome validation.
4. Controller action and `ApiResponse<T>` mapping.
5. Backend checkpoint tests.

### Within Each Frontend Slice

1. Models/URL state/API service are already foundational.
2. Facade orchestration.
3. Presentational component rendering.
4. Page composition.
5. Frontend checkpoint tests.

---

## Parallel Opportunities

- Foundational backend DTO/parser work T004-T007 can run in parallel.
- Foundational frontend models/API/cache/component shells T019-T027 can run in parallel after their referenced files are read.
- In US1, backend tasks T030-T035 and frontend tasks T036-T039 can progress in parallel once foundational contracts are stable.
- In US2, backend detail reads T042-T046 and frontend panel work T047-T049 can progress in parallel once E2 row identity is stable.
- US3 subtype work and US4 secondary-filter work can proceed in parallel after US1, but coordinate edits to `EfWordTypesReader.cs`, `WordTypeGrouping.cs`, `word-type-filter`, and `word-types-url-sync.ts`.
- Checkpoint test tasks marked `[P]` can run in parallel with each other when they touch different files.

### Parallel Example - US1

```text
Task: "T030 [US1] Implement main-type predicates and E1 parent counts in Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/WordTypes/EfWordTypesReader.cs"
Task: "T036 [US1] Implement tree and row loading in Frontend/quran-dashboard-ui/src/app/features/words/state/word-types-explorer.facade.ts"
Task: "T037 [US1] Implement main label selection in Frontend/quran-dashboard-ui/src/app/features/words/components/word-type-filter/"
```

### Parallel Example - US2 Tests

```text
Task: "T050 [US2] Backend selected-row details tests in Backend/tests/QuranDashboard.Tests/Quran/WordsWordTypes/WordTypesDetailsReadTests.cs"
Task: "T051 [US2] Frontend details panel tests in Frontend/quran-dashboard-ui/src/app/features/words/components/word-type-details-panel/word-type-details-panel.component.spec.ts"
```

---

## Implementation Strategy

### MVP First

1. Complete Phase 1 Setup.
2. Complete Phase 2 Foundational and CP-0.
3. Complete Phase 3 US1 and CP-1.
4. Stop and validate the browsable main-type table MVP before details/subtypes/secondary filters.

### Incremental Delivery

1. US1 main-type browse.
2. US2 selected-row details.
3. US3 subtype browse.
4. US4 secondary filters.
5. US5 exact deep-link restore.
6. Phase 8 polish and quickstart validation.

Each story should be independently demonstrable without breaking completed stories.

### Cheap-Model Guardrails

- Re-read the Implementer Guide before each phase.
- Do not make generic morphology endpoints or a mode-heavy generic page.
- Do not change existing Roots, Lemmas, Stems, or Unique Words contracts.
- Do not use `quran_words_unique_tashkeel` aggregate columns for scoped counts.
- Do not use displayed word text as row identity or highlight mechanism.
- Do not add search, particle children, Simple display, importer work, migrations, or schema/index changes.
- If a task would push a file past a hard architecture threshold, stop and split by responsibility before continuing.

---

## Notes

- `[P]` means the task touches different files and has no dependency on incomplete tasks.
- `[USx]` maps the task to the user story from `spec.md`.
- Commit only when explicitly asked, and use child repositories before the workspace pointer.
- Stop at each checkpoint if tests expose data, contract, or count-semantics issues.
