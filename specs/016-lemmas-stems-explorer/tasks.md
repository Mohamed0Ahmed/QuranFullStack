---
description: "Dependency-ordered implementation tasks for Quran Lemmas & Stems Explorer (Feature 016)"
---

# Tasks: Quran Lemmas & Stems Explorer

**Input**: Design documents from `specs/016-lemmas-stems-explorer/`
**Prerequisites**: `plan.md`, `spec.md` (US1–US8), `research.md`, `data-model.md`,
`quickstart.md`, `contracts/`, and the fuller
`docs/feature-016-lemmas-stems-explorer/feature-016-lemmas-stems-explorer-combined-implementation-plan.md`.

**Branch**: Workspace branch `016-lemmas-stems-explorer`. Before implementation commits, create or
align the Backend and Frontend child-repository branches with the workspace commit workflow.

**Tests**: Included because the specification and contracts require backend query/count verification,
frontend URL/accessibility behavior, cache/query-bound checks, and Mushaf DTO regression coverage.
Tests are milestone checkpoints at the end of each story, not test-first/TDD.

---

## Implementer Guide — Read Before Every Phase

Use the implemented Feature 015 Roots Explorer as the primary copy/reference:

- Backend controller:
  `Backend/api/QuranDashboard.Api/Controllers/Words/RootsController.cs`
- Backend contracts:
  `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/Roots/`
- Backend handlers:
  `Backend/application/QuranDashboard.Application/Quran/Words/Roots/Queries/`
- Backend EF reader:
  `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/Roots/`
- Backend cache and DI:
  `Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Quran/Words/Roots/` and
  `Backend/infrastructure/QuranDashboard.Infrastructure/DependencyInjection/RootsDependencyInjection.cs`
- Backend tests:
  `Backend/tests/QuranDashboard.Tests/Quran/WordsRoots/`
- Frontend page/state/components:
  `Frontend/quran-dashboard-ui/src/app/features/words/pages/roots-explorer-page/`,
  `state/roots-*.ts`, and the root-specific components.
- Existing shared frontend pieces:
  `components/highlighted-ayah/`, `components/ayah-matches-list/`,
  `components/surah-occurrences-list/`, `components/missing-surahs-list/`,
  `components/word-count-chip/`, `src/app/shared/ui/pagination/`, and
  `src/app/shared/url/deep-link-href.ts`.

Non-negotiable implementation rules:

1. Read-only only. Do not add writes, migrations, indexes, importers, DataPipeline changes, or Quran
   text changes.
2. Keep Lemmas and Stems as explicit bounded contexts. Do not replace them with a generic
   `morphology/{kind}` API, generic controller, or mode-heavy combined page.
3. Numeric IDs are canonical for root/lemma/stem/unique-word links and URL restoration. Never look up
   a selection by Arabic text or Buckwalter.
4. Lemma table root means `quran_lemmas.root_id`. If null, return null and show a non-clickable dash.
5. Stem table lemma/root means dominant co-occurrence: count descending, then earliest Mushaf
   occurrence (`surah_number`, `ayah_number`, `word_number`) ascending. If absent, return null.
6. `النوع` means dominant `head_pos` by the same count/earliest-occurrence rule. Use existing
   controlled POS labels. Full type-distribution counts must total occurrences.
7. Highlight ayahs by exact `quran_words.id` in `matchedQuranWordIds`; never use string replacement or
   mutate stored Quran text.
8. Cross-page root/lemma/stem/word/ayah links are real anchors with `target="_blank"` and
   `rel="noopener noreferrer"`. Same-page list/panel interactions remain current-tab URL updates.
9. Backend cache uses bounded `lemmas:` and `stems:` namespaces over the existing shared
   `IMemoryCache`; no global cache changes and no raw-search cache keys.
10. Logs contain IDs/counts/booleans/paging/sort/measured duration only. Never log Quran, lemma, stem,
    root, Buckwalter, word, raw-search, SQL, or payload text.
11. Reuse shared `<qd-pagination>` and existing `qd-*` state/style primitives. Do not create a new
    design system, palette, or words-only pagination component.
12. Keep frontend files below architecture thresholds. If a facade approaches 400 lines, split
    resource-specific loader/update helpers following `roots-detail-view.loader.ts` and
    `roots-detail-panel.updates.ts`.
13. Frontend tests must retain the repository Vitest worker cap. Guard missing `matchMedia` and
    `ResizeObserver` in jsdom.
14. Use source-safe committed PostgreSQL seed slices. Do not invent or casually copy Quran text into
    tests.

---

## Phase 1: Setup

**Purpose**: Establish a known green baseline and repository state before Feature 016 files are added.

- [x] T001 Confirm the workspace is on `016-lemmas-stems-explorer` and inspect branch/status in `Backend/` and `Frontend/quran-dashboard-ui/` without switching, staging, committing, or discarding existing changes; report any branch mismatch before editing
- [x] T002 Run the baseline commands from `specs/016-lemmas-stems-explorer/quickstart.md`: `dotnet build Backend/QuranDashboard.sln` and `npm run build --prefix Frontend/quran-dashboard-ui`; record any pre-existing failure before changing source files

**Checkpoint**: Baseline state is known; later failures can be attributed to Feature 016.

---

## Phase 2: Foundational — Blocking Prerequisites

**Purpose**: Create explicit contracts, parsers, skeleton readers/controllers, caches, URL models,
route shells, panel shells, and a shared real-database test harness. No user-story behavior is complete
until later phases.

**Critical**: Complete this phase before any user-story phase.

### Backend contracts and wiring

- [x] T003 [P] Create `LemmaSort`, `LemmaSortKeys`, `LemmaSortParser`, `LemmaWordKind`, `LemmaWordKindKeys`, and parser behavior matching Roots in `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/Lemmas/`
- [x] T004 [P] Create `StemSort`, `StemSortKeys`, `StemSortParser`, `StemWordKind`, `StemWordKindKeys`, and parser behavior matching Roots in `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/Stems/`
- [x] T005 [P] Create the shared controlled POS read record `TypeSummaryDto` exactly as contracted in `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/Morphology/Responses/TypeSummaryDto.cs`
- [x] T006 [P] Create all lemma response DTOs from `contracts/morphology-explorer-api.md` in `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/Lemmas/Responses/`, reusing existing `PagedResult<T>` and `AyahWordForHighlightDto`
- [x] T007 [P] Create all stem response DTOs from `contracts/morphology-explorer-api.md` in `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/Stems/Responses/`, reusing existing `PagedResult<T>` and `AyahWordForHighlightDto`
- [x] T008 Create `ILemmasReader` with all seven methods from `contracts/backend-read-abstractions.md` in `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/Lemmas/ILemmasReader.cs` after T003, T005, and T006
- [x] T009 Create `IStemsReader` with all seven methods from `contracts/backend-read-abstractions.md` in `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/Stems/IStemsReader.cs` after T004, T005, and T007
- [x] T010 [P] Add centralized Arabic success, validation, and not-found messages for all fourteen Lemmas/Stems reads in `Backend/api/QuranDashboard.Api/Common/ApiMessages.cs`; follow existing Roots message naming and do not hardcode messages in controllers
- [x] T011 [P] Create bounded `LemmasCacheKeys`, `LemmasCacheEntryOptions`, `StemsCacheKeys`, and `StemsCacheEntryOptions` in `Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Quran/Words/Lemmas/` and `Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Quran/Words/Stems/`
- [x] T012 [P] Create `EfLemmasReader` implementing `ILemmasReader` with constructor injection and explicit not-implemented method bodies in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/Lemmas/EfLemmasReader.cs`
- [x] T013 [P] Create `EfStemsReader` implementing `IStemsReader` with constructor injection and explicit not-implemented method bodies in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/Stems/EfStemsReader.cs`
- [x] T014 [P] Create delegating `CachedLemmasReader` and `CachedStemsReader` decorators with no global cache configuration in `Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Quran/Words/Lemmas/CachedLemmasReader.cs` and `Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Quran/Words/Stems/CachedStemsReader.cs`
- [x] T015 Create `LemmasDependencyInjection.AddLemmas()` and `StemsDependencyInjection.AddStems()`, then call both from `Backend/infrastructure/QuranDashboard.Infrastructure/DependencyInjection.cs` in `Backend/infrastructure/QuranDashboard.Infrastructure/DependencyInjection/LemmasDependencyInjection.cs` and `StemsDependencyInjection.cs`
- [x] T016 [P] Create thin `LemmasController` and `StemsController` shells with route bases `api/words/lemmas` and `api/words/stems` in `Backend/api/QuranDashboard.Api/Controllers/Words/LemmasController.cs` and `Backend/api/QuranDashboard.Api/Controllers/Words/StemsController.cs`; add actions only in story tasks

### Backend test foundation

- [x] T017 Create the Feature 016 Testcontainers fixture, xUnit collection, and fixture-start smoke test, reusing `SqlCommandCountInterceptor` and `RecordingLoggerProvider`, in `Backend/tests/QuranDashboard.Tests/Quran/WordsMorphologyExplorers/MorphologyExplorersTestFixture.cs`, `Backend/tests/QuranDashboard.Tests/Quran/WordsMorphologyExplorers/MorphologyExplorersCollection.cs`, and `Backend/tests/QuranDashboard.Tests/Quran/WordsMorphologyExplorers/MorphologyExplorersFixtureSmokeTests.cs`
- [x] T018 Create the embedded source-safe PostgreSQL seed at `Backend/tests/QuranDashboard.Tests/Quran/WordsMorphologyExplorers/morphology-explorers-seed.sql` covering: lemma with null owned root; stem with null lemma/root; multi-type lemma and stem with an exact count tie; stem with multiple lemma/root candidates; multiple matches in one ayah; high-frequency paged rows; simple/tashkeel identities; mentioned/missing surahs; related stems/lemmas; use verified repository data only

### Frontend contracts, routes, and shells

- [x] T019 [P] Create lemma DTO, list state, detail state, query-key, sort/view/sub-view, type-summary, and guard types in `Frontend/quran-dashboard-ui/src/app/features/words/models/lemmas.models.ts`
- [x] T020 [P] Create stem DTO, list state, detail state, query-key, sort/view/sub-view, type-summary, and guard types in `Frontend/quran-dashboard-ui/src/app/features/words/models/stems.models.ts`
- [x] T021 [P] Create all Arabic table, tab, sub-view, empty, error, and accessible-name constants in `Frontend/quran-dashboard-ui/src/app/features/words/models/lemmas.labels.ts` and `stems.labels.ts`; do not duplicate labels inside templates
- [x] T022 [P] Create `parseLemmasQueryParams`, query builders, clear-selection builder, and `buildLemmasDeepLink` in `Frontend/quran-dashboard-ui/src/app/features/words/state/lemmas-url-sync.ts`
- [x] T023 [P] Create `parseStemsQueryParams`, query builders, clear-selection builder, and `buildStemsDeepLink` in `Frontend/quran-dashboard-ui/src/app/features/words/state/stems-url-sync.ts`
- [x] T024 [P] Create typed seven-method `LemmasApi` and `StemsApi` services returning `Observable<ApiResponse<T>>` in `Frontend/quran-dashboard-ui/src/app/features/words/data-access/lemmas.api.ts` and `stems.api.ts`
- [x] T025 [P] Create resource-specific `LemmasCache` and `StemsCache` wrappers over the existing API response cache in `Frontend/quran-dashboard-ui/src/app/features/words/state/lemmas-cache.ts` and `stems-cache.ts`
- [x] T026 [P] Create `LemmasDetailFacade` and any initial focused loader/update helper shells in `Frontend/quran-dashboard-ui/src/app/features/words/state/lemmas-detail.facade.ts`, `lemmas-detail-view.loader.ts`, and `lemmas-detail-panel.updates.ts`; include selected-summary/loading/error/not-found state but no completed detail loaders
- [x] T027 [P] Create `StemsDetailFacade` and any initial focused loader/update helper shells in `Frontend/quran-dashboard-ui/src/app/features/words/state/stems-detail.facade.ts`, `stems-detail-view.loader.ts`, and `stems-detail-panel.updates.ts`; include selected-summary/loading/error/not-found state but no completed detail loaders
- [x] T028 [P] Create the empty persistent lemma details panel shell with exact four tabs and no overview tab in `Frontend/quran-dashboard-ui/src/app/features/words/components/lemma-details-panel/lemma-details-panel.component.ts`, `.html`, and `.scss`
- [x] T029 [P] Create the empty persistent stem details panel shell with exact four tabs and no overview tab in `Frontend/quran-dashboard-ui/src/app/features/words/components/stem-details-panel/stem-details-panel.component.ts`, `.html`, and `.scss`
- [x] T030 Add `WORDS_LEMMAS_SEGMENT`, `WORDS_STEMS_SEGMENT`, `lemmasRoutePath()`, and `stemsRoutePath()` in `Frontend/quran-dashboard-ui/src/app/core/navigation/route-paths.ts`; register lazy routes in `Frontend/quran-dashboard-ui/src/app/features/words/words.routes.ts`; create thin page shells in `Frontend/quran-dashboard-ui/src/app/features/words/pages/lemmas-explorer-page/lemmas-explorer-page.component.ts`, `Frontend/quran-dashboard-ui/src/app/features/words/pages/lemmas-explorer-page/lemmas-explorer-page.component.html`, `Frontend/quran-dashboard-ui/src/app/features/words/pages/lemmas-explorer-page/lemmas-explorer-page.component.scss`, `Frontend/quran-dashboard-ui/src/app/features/words/pages/stems-explorer-page/stems-explorer-page.component.ts`, `Frontend/quran-dashboard-ui/src/app/features/words/pages/stems-explorer-page/stems-explorer-page.component.html`, and `Frontend/quran-dashboard-ui/src/app/features/words/pages/stems-explorer-page/stems-explorer-page.component.scss`
- [x] T031 Run CP-0 after T003–T030 using `dotnet build Backend/QuranDashboard.sln`, `dotnet test Backend/QuranDashboard.sln --filter "FullyQualifiedName~MorphologyExplorersFixtureSmokeTests"`, and `npm run build --prefix Frontend/quran-dashboard-ui`; fix contract/wiring/fixture-start errors only and confirm no migration, package, global cache, or design-token file was added

**Checkpoint CP-0**: Contracts compile, both routes resolve to empty shells, panel shells exist, and the
test fixture can start PostgreSQL.

---

## Phase 3: User Story 1 — Browse and Find Quran Lemmas (Priority: P1) — MVP Part 1

**Goal**: Deliver the complete Lemmas catalogue with the nine locked columns, owned-root semantics,
dominant type, counts, search, sort, pagination, URL list state, and no eager detail reads.

**Independent Test**: Open `/dashboard/words/lemmas`; verify all columns, normalized search, all three
sorts, pagination, null-root display, refresh restoration, and zero detail API calls before selection.

### Backend lemma catalogue

- [ ] T032 [US1] Implement reusable lemma summary derivation rows and deterministic type ordering in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/Lemmas/LemmasListDerivation.cs`; type order is count descending then earliest Mushaf occurrence
- [ ] T033 [US1] Implement `EfLemmasReader.GetLemmasPageAsync` and `GetLemmaSummaryAsync` in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/Lemmas/EfLemmasReader.cs`: one bounded whole-summary aggregation, owned root from `quran_lemmas.root_id`, counts from matching morphology rows, Arabic-normalized contains search, deterministic sort, and in-memory paging
- [ ] T034 [US1] Implement `lemmas:summary:all` and `lemmas:{id}:summary` caching in `Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Quran/Words/Lemmas/CachedLemmasReader.cs`; never include raw search in a key
- [ ] T035 [US1] Create `GetLemmasPage` and `GetLemmaSummary` query, handler, and outcome files with non-positive paging validation and safe structured logging in `Backend/application/QuranDashboard.Application/Quran/Words/Lemmas/Queries/GetLemmasPage/` and `Backend/application/QuranDashboard.Application/Quran/Words/Lemmas/Queries/GetLemmaSummary/`; valid positive pages beyond the available results must remain successful empty pages
- [ ] T036 [US1] Add `GET /api/words/lemmas` and `GET /api/words/lemmas/{id}` actions with `200/400/404` mappings in `Backend/api/QuranDashboard.Api/Controllers/Words/LemmasController.cs`

### Frontend lemma catalogue

- [ ] T037 [US1] Implement catalogue loading, `ApiResponse` mapping, normalized search debounce, sort/page actions, row selection default, and no eager detail calls in `Frontend/quran-dashboard-ui/src/app/features/words/state/lemmas-explorer.facade.ts`; search/sort must reset only list page to 1 and preserve selected identity plus active detail state
- [ ] T038 [US1] Create the accessible nine-column lemma grid using `word-count-chip`, UI row numbers, non-visible IDs, dominant-type indicator, owned-root link/dash, and event outputs in `Frontend/quran-dashboard-ui/src/app/features/words/components/lemmas-table/lemmas-table.component.ts`, `Frontend/quran-dashboard-ui/src/app/features/words/components/lemmas-table/lemmas-table.component.html`, and `Frontend/quran-dashboard-ui/src/app/features/words/components/lemmas-table/lemmas-table.component.scss`; zero-count controls remain enabled and emit the same mapped detail event as non-zero counts
- [ ] T039 [US1] Compose the search, sort, list states, `lemmas-table`, shared pagination, and lemma panel shell in `Frontend/quran-dashboard-ui/src/app/features/words/pages/lemmas-explorer-page/lemmas-explorer-page.component.ts`, `.html`, and `.scss`
- [ ] T040 [US1] Activate the `الصيغ المعجمية` Words hub card and route it through `lemmasRoutePath()` in `Frontend/quran-dashboard-ui/src/app/features/words/pages/words-hub-page/words-hub-page.component.ts` and `.html`

### US1 checkpoint tests

- [ ] T041 [P] [US1] Add backend lemma catalogue tests for all counts, owned-root/null-root semantics, dominant-type ordering, normalized search, all sorts, non-positive paging validation, positive out-of-range successful empty pages, summary not-found, bounded cache hit, and log redaction in `Backend/tests/QuranDashboard.Tests/Quran/WordsMorphologyExplorers/LemmasListReadTests.cs` and `Backend/tests/QuranDashboard.Tests/Quran/WordsMorphologyExplorers/LemmasLoggingTests.cs`
- [ ] T042 [P] [US1] Add frontend lemma page/table tests for nine columns, no visible IDs, null-root dash, root anchor attributes, search/sort resetting only list page while preserving selection/detail state, row default selection, list restoration, zero-count activation opening a controlled empty mapped detail state, loading/empty/no-results/error, and no detail API calls on catalogue render in `Frontend/quran-dashboard-ui/src/app/features/words/pages/lemmas-explorer-page/lemmas-explorer-page.component.spec.ts` and `Frontend/quran-dashboard-ui/src/app/features/words/components/lemmas-table/lemmas-table.component.spec.ts`

**Checkpoint CP-1A**: Lemmas catalogue is independently usable and testable.

---

## Phase 4: User Story 2 — Browse and Find Quran Stems (Priority: P1) — MVP Part 2

**Goal**: Deliver the complete Stems catalogue with dominant co-occurring lemma/root, null-safe
relationships, dominant type, counts, search, sort, pagination, URL list state, and no eager details.

**Independent Test**: Open `/dashboard/words/stems`; verify all columns, dominant relationship
tie-breaks, null lemma/root display, search/sort/page restoration, and no detail calls.

### Backend stem catalogue

- [ ] T043 [US2] Implement reusable stem summary derivation for dominant lemma, dominant root, and dominant type in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/Stems/StemsListDerivation.cs`; rank each relationship independently by count then earliest Mushaf occurrence
- [ ] T044 [US2] Implement `EfStemsReader.GetStemsPageAsync` and `GetStemSummaryAsync` in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/Stems/EfStemsReader.cs`: one bounded whole-summary aggregation, nullable dominant relations, counts, normalized Arabic search, deterministic sort, and in-memory paging
- [ ] T045 [US2] Implement `stems:summary:all` and `stems:{id}:summary` caching in `Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Quran/Words/Stems/CachedStemsReader.cs`; never include raw search in a key
- [ ] T046 [US2] Create `GetStemsPage` and `GetStemSummary` query, handler, and outcome files with non-positive paging validation and safe structured logging in `Backend/application/QuranDashboard.Application/Quran/Words/Stems/Queries/GetStemsPage/` and `Backend/application/QuranDashboard.Application/Quran/Words/Stems/Queries/GetStemSummary/`; valid positive pages beyond the available results must remain successful empty pages
- [ ] T047 [US2] Add `GET /api/words/stems` and `GET /api/words/stems/{id}` actions with `200/400/404` mappings in `Backend/api/QuranDashboard.Api/Controllers/Words/StemsController.cs`

### Frontend stem catalogue

- [ ] T048 [US2] Implement catalogue loading, `ApiResponse` mapping, search debounce, sort/page actions, row selection default, and no eager details in `Frontend/quran-dashboard-ui/src/app/features/words/state/stems-explorer.facade.ts`; search/sort must reset only list page to 1 and preserve selected identity plus active detail state
- [ ] T049 [US2] Create the accessible nine-column stem grid using `word-count-chip`, UI row numbers, non-visible IDs, dominant-type indicator, dominant lemma/root links or dashes, and event outputs in `Frontend/quran-dashboard-ui/src/app/features/words/components/stems-table/stems-table.component.ts`, `Frontend/quran-dashboard-ui/src/app/features/words/components/stems-table/stems-table.component.html`, and `Frontend/quran-dashboard-ui/src/app/features/words/components/stems-table/stems-table.component.scss`; zero-count controls remain enabled and emit the same mapped detail event as non-zero counts
- [ ] T050 [US2] Compose the search, sort, list states, `stems-table`, shared pagination, and stem panel shell in `Frontend/quran-dashboard-ui/src/app/features/words/pages/stems-explorer-page/stems-explorer-page.component.ts`, `.html`, and `.scss`
- [ ] T051 [US2] Activate the `الأصول الصرفية` Words hub card and route it through `stemsRoutePath()` in `Frontend/quran-dashboard-ui/src/app/features/words/pages/words-hub-page/words-hub-page.component.ts` and `.html`

### US2 checkpoint tests

- [ ] T052 [P] [US2] Add backend stem catalogue tests for all counts, independent dominant lemma/root ranking, exact tie-breaks, missing lemma/root nulls, dominant type, search/sort, non-positive paging validation, positive out-of-range successful empty pages, cache, not-found, and log redaction in `Backend/tests/QuranDashboard.Tests/Quran/WordsMorphologyExplorers/StemsListReadTests.cs` and `Backend/tests/QuranDashboard.Tests/Quran/WordsMorphologyExplorers/StemsLoggingTests.cs`
- [ ] T053 [P] [US2] Add frontend stem page/table tests for nine columns, null dashes, correct root/lemma anchor attributes, no visible IDs, search/sort resetting only list page while preserving selection/detail state, row default selection, zero-count activation opening a controlled empty mapped detail state, list states, and no eager detail calls in `Frontend/quran-dashboard-ui/src/app/features/words/pages/stems-explorer-page/stems-explorer-page.component.spec.ts` and `Frontend/quran-dashboard-ui/src/app/features/words/components/stems-table/stems-table.component.spec.ts`

**Checkpoint CP-1B / Suggested MVP**: Both P1 catalogues are independently usable. This two-page P1
slice is the recommended MVP rather than shipping only one of the two sibling resources.

---

## Phase 5: User Story 3 — Study Exact Ayah Occurrences (Priority: P2)

**Goal**: Both explorers show paginated ayahs with exact word-ID highlights and safe new-tab Mushaf
links.

**Independent Test**: Activate occurrences or ayahs for one lemma and one stem; only matching word IDs
are highlighted, multiple matches in one ayah work, pagination works, and the ayah anchor opens the
correct Mushaf focus URL.

### Backend ayah reads

- [ ] T054 [P] [US3] Implement `EfLemmasReader.GetLemmaAyahMatchesAsync` with paged distinct ayah IDs, batched page-word loading, exact matched IDs, and no per-ayah loop in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/Lemmas/EfLemmasReader.cs`
- [ ] T055 [P] [US3] Implement `EfStemsReader.GetStemAyahMatchesAsync` with the same bounded batched shape in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/Stems/EfStemsReader.cs`
- [ ] T056 [P] [US3] After T054 and T055, add paged ayah cache keys and behavior to `Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Quran/Words/Lemmas/CachedLemmasReader.cs` and `Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Quran/Words/Stems/CachedStemsReader.cs`
- [ ] T057 [P] [US3] Create `GetLemmaAyahs` and `GetStemAyahs` query, handler, and outcome files with ID/paging validation, not-found, positive out-of-range empty-page handling, and safe logs in `Backend/application/QuranDashboard.Application/Quran/Words/Lemmas/Queries/GetLemmaAyahs/` and `Backend/application/QuranDashboard.Application/Quran/Words/Stems/Queries/GetStemAyahs/`
- [ ] T058 [US3] After T057, add both `/ayahs` controller actions and outcome mappings in `Backend/api/QuranDashboard.Api/Controllers/Words/LemmasController.cs` and `Backend/api/QuranDashboard.Api/Controllers/Words/StemsController.cs`

### Frontend ayah views

- [ ] T059 [P] [US3] Add lazy ayah loading, selected-summary restoration, page cache, not-found/error handling, and `detailPage` updates in `Frontend/quran-dashboard-ui/src/app/features/words/state/lemmas-detail.facade.ts` and `Frontend/quran-dashboard-ui/src/app/features/words/state/lemmas-detail-view.loader.ts`
- [ ] T060 [P] [US3] Add the equivalent stem ayah loading path in `Frontend/quran-dashboard-ui/src/app/features/words/state/stems-detail.facade.ts` and `Frontend/quran-dashboard-ui/src/app/features/words/state/stems-detail-view.loader.ts`
- [ ] T061 [P] [US3] Render `ayah-matches-list`, `highlighted-ayah`, shared pagination, and Mushaf anchors in `Frontend/quran-dashboard-ui/src/app/features/words/components/lemma-details-panel/lemma-details-panel.component.ts`, `Frontend/quran-dashboard-ui/src/app/features/words/components/lemma-details-panel/lemma-details-panel.component.html`, and `Frontend/quran-dashboard-ui/src/app/features/words/components/lemma-details-panel/lemma-details-panel.component.scss`
- [ ] T062 [P] [US3] Render the equivalent stem ayah panel path in `Frontend/quran-dashboard-ui/src/app/features/words/components/stem-details-panel/stem-details-panel.component.ts`, `Frontend/quran-dashboard-ui/src/app/features/words/components/stem-details-panel/stem-details-panel.component.html`, and `Frontend/quran-dashboard-ui/src/app/features/words/components/stem-details-panel/stem-details-panel.component.scss`
- [ ] T063 [US3] Wire occurrences/ayahs count events to `view=ayahs&detailPage=1`, integrate both panels into their split pages, and keep panel scroll independent in `Frontend/quran-dashboard-ui/src/app/features/words/pages/lemmas-explorer-page/lemmas-explorer-page.component.ts`, `Frontend/quran-dashboard-ui/src/app/features/words/pages/lemmas-explorer-page/lemmas-explorer-page.component.html`, `Frontend/quran-dashboard-ui/src/app/features/words/pages/lemmas-explorer-page/lemmas-explorer-page.component.scss`, `Frontend/quran-dashboard-ui/src/app/features/words/pages/stems-explorer-page/stems-explorer-page.component.ts`, `Frontend/quran-dashboard-ui/src/app/features/words/pages/stems-explorer-page/stems-explorer-page.component.html`, and `Frontend/quran-dashboard-ui/src/app/features/words/pages/stems-explorer-page/stems-explorer-page.component.scss`

### US3 checkpoint tests

- [ ] T064 [P] [US3] Add backend ayah tests for lemma and stem exact `MatchedQuranWordIds`, multiple same-ayah matches, pagination, positive out-of-range successful empty pages, unknown IDs, cache hits, and bounded SQL command counts in `Backend/tests/QuranDashboard.Tests/Quran/WordsMorphologyExplorers/LemmasAyahsReadTests.cs` and `Backend/tests/QuranDashboard.Tests/Quran/WordsMorphologyExplorers/StemsAyahsReadTests.cs`
- [ ] T065 [P] [US3] Add frontend ayah tests for count mapping, lazy loading, exact highlight payload, shared pagination, controlled out-of-range empty state, panel restoration, independent scroll container, and Mushaf anchor href/target/rel in `Frontend/quran-dashboard-ui/src/app/features/words/components/lemma-details-panel/lemma-details-panel.component.spec.ts` and `Frontend/quran-dashboard-ui/src/app/features/words/components/stem-details-panel/stem-details-panel.component.spec.ts`

**Checkpoint CP-2**: Ayah study works independently for both resources.

---

## Phase 6: User Story 4 — Explore Related Quran Word Forms (Priority: P2)

**Goal**: Both panels provide paginated simple/tashkeel word lists with selection-scoped counts and
safe new-tab links to the existing Unique Words explorer.

**Independent Test**: Select words/simple and words/tashkeel for one lemma and stem; verify scoped
counts, page changes, and destination `kind + wordId + view=ayahs`.

### Backend word reads

- [ ] T066 [P] [US4] Implement `EfLemmasReader.GetLemmaWordsAsync` for simple/tashkeel unique identities, stored display text, scoped count, first occurrence, and server paging in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/Lemmas/EfLemmasReader.cs`
- [ ] T067 [P] [US4] Implement `EfStemsReader.GetStemWordsAsync` with equivalent semantics in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/Stems/EfStemsReader.cs`
- [ ] T068 [P] [US4] Add bounded `{resource}:{id}:words:{kind}:p{page}:s{size}` caching to both cache decorators in `Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Quran/Words/Lemmas/CachedLemmasReader.cs` and `Caching/Quran/Words/Stems/CachedStemsReader.cs`
- [ ] T069 [P] [US4] Create `GetLemmaWords` and `GetStemWords` query, handler, and outcome files validating ID, kind, and paging in `Backend/application/QuranDashboard.Application/Quran/Words/Lemmas/Queries/GetLemmaWords/` and `Quran/Words/Stems/Queries/GetStemWords/`
- [ ] T070 [US4] Add both `/words/{wordKind}` controller actions and outcome mappings in `Backend/api/QuranDashboard.Api/Controllers/Words/LemmasController.cs` and `StemsController.cs`

### Frontend word views

- [ ] T071 [P] [US4] Create paginated lemma word rows with stored display text, scoped count, and `buildUniqueWordsDeepLink(kind, { wordId, view: 'ayahs' })` anchors in `Frontend/quran-dashboard-ui/src/app/features/words/components/lemma-words-list/lemma-words-list.component.ts`, `.html`, and `.scss`
- [ ] T072 [P] [US4] Create the equivalent stem word list in `Frontend/quran-dashboard-ui/src/app/features/words/components/stem-words-list/stem-words-list.component.ts`, `.html`, and `.scss`
- [ ] T073 [P] [US4] Add lazy simple/tashkeel word loading, page cache, and URL updates in `Frontend/quran-dashboard-ui/src/app/features/words/state/lemmas-detail.facade.ts` and `lemmas-detail-view.loader.ts`
- [ ] T074 [P] [US4] Add the equivalent stem word loading in `Frontend/quran-dashboard-ui/src/app/features/words/state/stems-detail.facade.ts` and `stems-detail-view.loader.ts`
- [ ] T075 [US4] Render nested word sub-tabs and shared pagination, then wire simple/tashkeel count mappings in `Frontend/quran-dashboard-ui/src/app/features/words/components/lemma-details-panel/`, `stem-details-panel/`, and both explorer page folders

### US4 checkpoint tests

- [ ] T076 [P] [US4] Add backend word tests for both resources and both kinds: correct unique IDs, stored display text, selection-scoped counts, paging, invalid kind, not-found, and cache hit in `Backend/tests/QuranDashboard.Tests/Quran/WordsMorphologyExplorers/LemmasWordsReadTests.cs` and `StemsWordsReadTests.cs`
- [ ] T077 [P] [US4] Add frontend word tests for sub-tab URL mapping, pagination, scoped count display, exact Unique Words href, `target="_blank"`, `rel="noopener noreferrer"`, and no text-derived ID in `Frontend/quran-dashboard-ui/src/app/features/words/components/lemma-words-list/lemma-words-list.component.spec.ts` and `stem-words-list.component.spec.ts`

**Checkpoint CP-3**: Word study works independently for both resources.

---

## Phase 7: User Story 5 — Review Surah Distribution (Priority: P3)

**Goal**: Both panels provide whole mentioned/missing surah lists whose disjoint union is 114.

**Independent Test**: Open mentioned and missing surahs for a lemma and stem; verify Arabic names,
per-surah counts, empty missing state, no pagination, and total 114.

### Backend surah reads

- [ ] T078 [P] [US5] Implement lemma mentioned and missing surah reads with whole-list ordering and 114-surah complement in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/Lemmas/EfLemmasReader.cs`
- [ ] T079 [P] [US5] Implement equivalent stem mentioned and missing surah reads in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/Stems/EfStemsReader.cs`
- [ ] T080 [P] [US5] Add bounded surah/missing cache entries to both decorators in `Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Quran/Words/Lemmas/CachedLemmasReader.cs` and `Caching/Quran/Words/Stems/CachedStemsReader.cs`
- [ ] T081 [P] [US5] Create four query/handler/outcome groups under `Backend/application/QuranDashboard.Application/Quran/Words/Lemmas/Queries/GetLemmaMentionedSurahs/`, `GetLemmaMissingSurahs/`, `Quran/Words/Stems/Queries/GetStemMentionedSurahs/`, and `GetStemMissingSurahs/`
- [ ] T082 [US5] Add `/surahs` and `/missing-surahs` actions to both controllers in `Backend/api/QuranDashboard.Api/Controllers/Words/LemmasController.cs` and `StemsController.cs`

### Frontend surah views

- [ ] T083 [P] [US5] Add lazy mentioned/missing loading and same-selection reuse in `Frontend/quran-dashboard-ui/src/app/features/words/state/lemmas-detail.facade.ts` and `lemmas-detail-view.loader.ts`
- [ ] T084 [P] [US5] Add equivalent stem surah loading in `Frontend/quran-dashboard-ui/src/app/features/words/state/stems-detail.facade.ts` and `stems-detail-view.loader.ts`
- [ ] T085 [US5] Reuse `surah-occurrences-list` and `missing-surahs-list`, render nested surah tabs without pagination, and wire `surahView` in `Frontend/quran-dashboard-ui/src/app/features/words/components/lemma-details-panel/lemma-details-panel.component.ts`, `Frontend/quran-dashboard-ui/src/app/features/words/components/lemma-details-panel/lemma-details-panel.component.html`, `Frontend/quran-dashboard-ui/src/app/features/words/components/stem-details-panel/stem-details-panel.component.ts`, and `Frontend/quran-dashboard-ui/src/app/features/words/components/stem-details-panel/stem-details-panel.component.html`

### US5 checkpoint tests

- [ ] T086 [P] [US5] Add backend tests that mentioned and missing sets are disjoint, union to 114, preserve Arabic names, have correct scoped counts, support empty missing results, and cache correctly in `Backend/tests/QuranDashboard.Tests/Quran/WordsMorphologyExplorers/LemmasSurahsReadTests.cs` and `StemsSurahsReadTests.cs`
- [ ] T087 [P] [US5] Add frontend tests for surah count mapping, mentioned/missing URL state, whole-list loading, no detail pagination, and clear empty missing state in `Frontend/quran-dashboard-ui/src/app/features/words/components/lemma-details-panel/lemma-details-panel.component.spec.ts` and `Frontend/quran-dashboard-ui/src/app/features/words/components/stem-details-panel/stem-details-panel.component.spec.ts`

**Checkpoint CP-4**: Surah distribution works independently for both resources.

---

## Phase 8: User Story 6 — Understand Type and Morphology Relationships (Priority: P3)

**Goal**: Show complete type distributions; link lemma→stems, stem→lemmas, and available roots using
stable IDs and safe new-tab anchors.

**Independent Test**: Verify a type tie, distribution total, lemma stems count equality, stem related
lemmas, null relationship fallback, and all root/lemma/stem destination links.

### Backend relationships

- [ ] T088 [P] [US6] Implement `EfLemmasReader.GetLemmaStemsAsync` using distinct non-null stem IDs, scoped counts, deterministic ordering, and item-count equality with `LemmaSummaryDto.StemsCount` in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/Lemmas/EfLemmasReader.cs`
- [ ] T089 [P] [US6] Implement `EfStemsReader.GetStemLemmasAsync` using distinct non-null lemma IDs, scoped counts, optional Buckwalter display, and deterministic ordering in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/Stems/EfStemsReader.cs`
- [ ] T090 [P] [US6] Add `lemmas:{id}:stems` and `stems:{id}:lemmas` caching in `Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Quran/Words/Lemmas/CachedLemmasReader.cs` and `Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Quran/Words/Stems/CachedStemsReader.cs`
- [ ] T091 [P] [US6] Create `GetLemmaStems` and `GetStemLemmas` query, handler, and outcome groups in `Backend/application/QuranDashboard.Application/Quran/Words/Lemmas/Queries/GetLemmaStems/` and `Backend/application/QuranDashboard.Application/Quran/Words/Stems/Queries/GetStemLemmas/`
- [ ] T092 [US6] Add `/stems` to `Backend/api/QuranDashboard.Api/Controllers/Words/LemmasController.cs` and `/lemmas` to `Backend/api/QuranDashboard.Api/Controllers/Words/StemsController.cs` with controlled outcomes

### Frontend relationships and types

- [ ] T093 [P] [US6] Create the shared type-distribution list with controlled labels, counts, and non-color-only dominant state in `Frontend/quran-dashboard-ui/src/app/features/words/components/type-distribution-list/type-distribution-list.component.ts`, `Frontend/quran-dashboard-ui/src/app/features/words/components/type-distribution-list/type-distribution-list.component.html`, and `Frontend/quran-dashboard-ui/src/app/features/words/components/type-distribution-list/type-distribution-list.component.scss`
- [ ] T094 [P] [US6] Create lemma related-stems rows with scoped counts and `buildStemsDeepLink({ stemId, view: 'words', wordView: 'simple' })` anchors in `Frontend/quran-dashboard-ui/src/app/features/words/components/lemma-stems-list/lemma-stems-list.component.ts`, `.html`, and `.scss`
- [ ] T095 [P] [US6] Create stem related-lemmas rows with scoped counts and `buildLemmasDeepLink({ lemmaId, view: 'words', wordView: 'simple' })` anchors in `Frontend/quran-dashboard-ui/src/app/features/words/components/stem-lemmas-list/stem-lemmas-list.component.ts`, `.html`, and `.scss`
- [ ] T096 [P] [US6] Add related-stems loading to `Frontend/quran-dashboard-ui/src/app/features/words/state/lemmas-detail.facade.ts` and `Frontend/quran-dashboard-ui/src/app/features/words/state/lemmas-detail-view.loader.ts`; add related-lemmas loading to `Frontend/quran-dashboard-ui/src/app/features/words/state/stems-detail.facade.ts` and `Frontend/quran-dashboard-ui/src/app/features/words/state/stems-detail-view.loader.ts`
- [ ] T097 [US6] Render type distributions and related-item tabs in `Frontend/quran-dashboard-ui/src/app/features/words/components/lemma-details-panel/lemma-details-panel.component.ts`, `Frontend/quran-dashboard-ui/src/app/features/words/components/lemma-details-panel/lemma-details-panel.component.html`, `Frontend/quran-dashboard-ui/src/app/features/words/components/stem-details-panel/stem-details-panel.component.ts`, and `Frontend/quran-dashboard-ui/src/app/features/words/components/stem-details-panel/stem-details-panel.component.html`; ensure lemma root and stem dominant lemma/root table links use `buildRootsDeepLink`/`buildLemmasDeepLink` and render dashes when IDs are null in `Frontend/quran-dashboard-ui/src/app/features/words/components/lemmas-table/lemmas-table.component.ts`, `Frontend/quran-dashboard-ui/src/app/features/words/components/lemmas-table/lemmas-table.component.html`, `Frontend/quran-dashboard-ui/src/app/features/words/components/stems-table/stems-table.component.ts`, and `Frontend/quran-dashboard-ui/src/app/features/words/components/stems-table/stems-table.component.html`

### US6 checkpoint tests

- [ ] T098 [P] [US6] Add backend tests for exact POS tie-break, distribution total equals occurrences, lemma stems count equals list item count, stem related lemmas, deterministic ordering, null relationships, and cache hits in `Backend/tests/QuranDashboard.Tests/Quran/WordsMorphologyExplorers/MorphologyRelationshipsReadTests.cs`
- [ ] T099 [P] [US6] Add frontend tests for type distribution, additional-type indicator, related count display, exact root/lemma/stem hrefs, safe anchor attributes, and null non-link fallback in `Frontend/quran-dashboard-ui/src/app/features/words/components/type-distribution-list/type-distribution-list.component.spec.ts`, `lemma-stems-list.component.spec.ts`, and `stem-lemmas-list.component.spec.ts`

**Checkpoint CP-5**: Morphology relationships and type semantics are complete.

---

## Phase 9: User Story 7 — Move Between Mushaf and Morphology Explorers (Priority: P3)

**Goal**: Add stable lemma/stem identities to selected-word analysis and render root/lemma/stem explorer
anchors only when each identity exists.

**Independent Test**: Open selected words with and without identities; correct explorer URLs open in new
tabs, missing identities remain plain text, and existing identity/segment behavior is unchanged.

- [ ] T100 [P] [US7] Add `Id` to `WordMorphologyLemma` and `WordMorphologyStem` without changing surrounding nullability or display fields in `Backend/application/QuranDashboard.Application.Abstractions/Quran/MushafReader/Responses/WordAnalysisResponse.cs`
- [ ] T101 [US7] Map existing loaded `lemma.Id` and `stem.Id` values in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/MushafReader/EfWordAnalysisReader.cs`; do not add any text lookup or database schema change
- [ ] T102 [P] [US7] Update matching TypeScript morphology types in `Frontend/quran-dashboard-ui/src/app/features/mushaf/models/mushaf.models.ts`
- [ ] T103 [US7] Build lemma and stem hrefs from stable IDs in `Frontend/quran-dashboard-ui/src/app/features/mushaf/components/selected-word-section/selected-word-section.component.ts` using `buildLemmasDeepLink` and `buildStemsDeepLink`
- [ ] T104 [US7] Pass the hrefs into and render root/lemma/stem as safe anchors only when available in `Frontend/quran-dashboard-ui/src/app/features/mushaf/components/selected-word-section/selected-word-section.component.html`, `Frontend/quran-dashboard-ui/src/app/features/mushaf/components/word-morphology-summary/word-morphology-summary.component.ts`, and `Frontend/quran-dashboard-ui/src/app/features/mushaf/components/word-morphology-summary/word-morphology-summary.component.html`
- [ ] T105 [P] [US7] Add backend regression tests for present/null lemma and stem IDs without changing other WordAnalysis fields in `Backend/tests/QuranDashboard.Tests/Quran/MushafReader/WordAnalysisMorphologyIdentityTests.cs`
- [ ] T106 [P] [US7] Add frontend tests for correct root/lemma/stem hrefs, `target`/`rel`, missing-ID plain-text fallback, and preserved unique-word links in `Frontend/quran-dashboard-ui/src/app/features/mushaf/components/selected-word-section/selected-word-section.component.spec.ts` and `Frontend/quran-dashboard-ui/src/app/features/mushaf/components/word-morphology-summary/word-morphology-summary.component.spec.ts`

**Checkpoint CP-6**: Mushaf and morphology explorers are bidirectionally connected.

---

## Phase 10: User Story 8 — Restore and Navigate Exact Explorer State (Priority: P4)

**Goal**: Harden complete list and detail URL restoration, browser navigation, invalid-state
normalization, selection clearing, and same-selection cache reuse for both explorers.

**Independent Test**: Build deep lemma and stem URLs for every view/sub-view, refresh, copy/reopen, use
back/forward, clear selection, and try malformed/unknown state without breaking the catalogue.

- [ ] T107 [P] [US8] Finalize lemma query normalization in `Frontend/quran-dashboard-ui/src/app/features/words/state/lemmas-url-sync.ts`: malformed/non-positive `page` and `detailPage` become 1; valid positive out-of-range values remain unchanged; irrelevant `wordView`, `surahView`, and `detailPage` keys are ignored/cleared by parent view; selection clearing preserves list state; deep-link defaults are canonical
- [ ] T108 [P] [US8] Apply the identical normalization, selection-clearing, and canonical deep-link rules for stems in `Frontend/quran-dashboard-ui/src/app/features/words/state/stems-url-sync.ts`
- [ ] T109 [P] [US8] Implement route hydration, browser back/forward handling, unknown-selection one-time not-found behavior, and selected-identity cache reset for lemmas in `Frontend/quran-dashboard-ui/src/app/features/words/state/lemmas-explorer.facade.ts` and `Frontend/quran-dashboard-ui/src/app/features/words/state/lemmas-detail.facade.ts`
- [ ] T110 [P] [US8] Implement equivalent route hydration and cache/session behavior for stems in `Frontend/quran-dashboard-ui/src/app/features/words/state/stems-explorer.facade.ts` and `Frontend/quran-dashboard-ui/src/app/features/words/state/stems-detail.facade.ts`
- [ ] T111 [US8] Ensure both explorer pages write same-page state changes through router query updates, preserve list state on panel close, and restore active focus/selection after navigation in `Frontend/quran-dashboard-ui/src/app/features/words/pages/lemmas-explorer-page/lemmas-explorer-page.component.ts`, `Frontend/quran-dashboard-ui/src/app/features/words/pages/lemmas-explorer-page/lemmas-explorer-page.component.html`, `Frontend/quran-dashboard-ui/src/app/features/words/pages/stems-explorer-page/stems-explorer-page.component.ts`, and `Frontend/quran-dashboard-ui/src/app/features/words/pages/stems-explorer-page/stems-explorer-page.component.html`
- [ ] T112 [P] [US8] Add exhaustive data-driven lemma URL tests for valid/invalid sort, malformed/non-positive page normalization, preserved positive out-of-range pages, identity, view, wordView, surahView, detailPage, search/sort selection preservation, clear selection, and deep-link output in `Frontend/quran-dashboard-ui/src/app/features/words/state/lemmas-url-sync.spec.ts`
- [ ] T113 [P] [US8] Add the equivalent exhaustive stem URL tests in `Frontend/quran-dashboard-ui/src/app/features/words/state/stems-url-sync.spec.ts`
- [ ] T114 [P] [US8] Add facade/page restoration tests for refresh, copied deep links, browser back/forward, unknown identity, irrelevant query keys, positive out-of-range controlled empty results, panel close, search/sort selection preservation, and same-selection cached view reuse in `Frontend/quran-dashboard-ui/src/app/features/words/pages/lemmas-explorer-page/lemmas-explorer-page.component.spec.ts` and `Frontend/quran-dashboard-ui/src/app/features/words/pages/stems-explorer-page/stems-explorer-page.component.spec.ts`

**Checkpoint CP-7**: Every meaningful explorer state is restorable and safely normalized.

---

## Phase 11: Polish and Cross-Cutting Hardening

**Purpose**: Verify behavior spanning all stories and enforce workspace delivery gates.

- [ ] T115 [P] Audit all fourteen handlers for required structured fields and forbidden text using `RecordingLoggerProvider`; consolidate missing cases in `Backend/tests/QuranDashboard.Tests/Quran/WordsMorphologyExplorers/MorphologyExplorersLoggingTests.cs`
- [ ] T116 [P] Audit all bounded cache entries and repeat-read SQL command counts; assert no global cache configuration and no raw-search keys in `Backend/tests/QuranDashboard.Tests/Quran/WordsMorphologyExplorers/MorphologyExplorersCacheReadTests.cs`
- [ ] T117 [P] Complete responsive drawer, Escape, focus trap/return, RTL logical properties, and `matchMedia`/`ResizeObserver` fallbacks in `Frontend/quran-dashboard-ui/src/app/features/words/components/lemma-details-panel/lemma-details-panel.component.ts`, `Frontend/quran-dashboard-ui/src/app/features/words/components/lemma-details-panel/lemma-details-panel.component.html`, `Frontend/quran-dashboard-ui/src/app/features/words/components/lemma-details-panel/lemma-details-panel.component.scss`, `Frontend/quran-dashboard-ui/src/app/features/words/components/stem-details-panel/stem-details-panel.component.ts`, `Frontend/quran-dashboard-ui/src/app/features/words/components/stem-details-panel/stem-details-panel.component.html`, `Frontend/quran-dashboard-ui/src/app/features/words/components/stem-details-panel/stem-details-panel.component.scss`, `Frontend/quran-dashboard-ui/src/app/features/words/pages/lemmas-explorer-page/lemmas-explorer-page.component.ts`, `Frontend/quran-dashboard-ui/src/app/features/words/pages/lemmas-explorer-page/lemmas-explorer-page.component.html`, `Frontend/quran-dashboard-ui/src/app/features/words/pages/lemmas-explorer-page/lemmas-explorer-page.component.scss`, `Frontend/quran-dashboard-ui/src/app/features/words/pages/stems-explorer-page/stems-explorer-page.component.ts`, `Frontend/quran-dashboard-ui/src/app/features/words/pages/stems-explorer-page/stems-explorer-page.component.html`, and `Frontend/quran-dashboard-ui/src/app/features/words/pages/stems-explorer-page/stems-explorer-page.component.scss`
- [ ] T118 [P] Complete the accessibility/state and entry-point test matrix: exact tab sets with no overview tab, keyboard count controls, tablist semantics, selected-row state, live loading status, loading/empty/no-results/error/not-found, visible focus, registered lemma/stem routes, and Words hub access to each explorer in no more than two interactions in `Frontend/quran-dashboard-ui/src/app/features/words/components/lemma-details-panel/lemma-details-panel.component.spec.ts`, `Frontend/quran-dashboard-ui/src/app/features/words/components/stem-details-panel/stem-details-panel.component.spec.ts`, `Frontend/quran-dashboard-ui/src/app/features/words/components/lemmas-table/lemmas-table.component.spec.ts`, `Frontend/quran-dashboard-ui/src/app/features/words/components/stems-table/stems-table.component.spec.ts`, `Frontend/quran-dashboard-ui/src/app/features/words/pages/lemmas-explorer-page/lemmas-explorer-page.component.spec.ts`, `Frontend/quran-dashboard-ui/src/app/features/words/pages/stems-explorer-page/stems-explorer-page.component.spec.ts`, `Frontend/quran-dashboard-ui/src/app/features/words/pages/words-hub-page/words-hub-page.component.spec.ts`, `Frontend/quran-dashboard-ui/src/app/features/words/words.routes.spec.ts`, and `Frontend/quran-dashboard-ui/src/app/core/navigation/route-paths.spec.ts`
- [ ] T119 Run targeted Feature 016 backend tests with `dotnet test Backend/QuranDashboard.sln --filter "FullyQualifiedName~WordsMorphologyExplorers|FullyQualifiedName~WordAnalysisMorphologyIdentity"` and fix only Feature 016 failures
- [ ] T120 Run targeted frontend tests with `npm test --prefix Frontend/quran-dashboard-ui -- --run src/app/features/words src/app/features/mushaf/components/selected-word-section src/app/features/mushaf/components/word-morphology-summary`, preserving the configured Vitest worker cap
- [ ] T121 Run full builds `dotnet build Backend/QuranDashboard.sln` and `npm run build --prefix Frontend/quran-dashboard-ui`; verify no migration/index/package/design-token files and no visible backend IDs were introduced
- [ ] T122 Measure SC-002 after T121 using production frontend/backend builds, the local API, warm application/cache state, and no browser throttling: record 20 navigation-start-to-first-successful-table-render timings for `/dashboard/words/lemmas` and 20 for `/dashboard/words/stems`; require at least 19/20 timings per route at or below 1,000 ms and write the environment, all 40 timings, pass counts, and result into `specs/016-lemmas-stems-explorer/quickstart.md`
- [ ] T123 Run the workspace clean-code guard and test-code self-check against all changed production/test files, split any file beyond `FRONTEND_STRUCTURE.md` thresholds, then update the remaining completion evidence in `specs/016-lemmas-stems-explorer/quickstart.md` without marking unrun checks as passed

**Final Checkpoint**: All desired stories, targeted tests, full builds, architecture rules, Quranic data
safety rules, and completion evidence pass.

---

## Dependencies and Execution Order

### Phase dependencies

- **Phase 1 Setup**: No dependency.
- **Phase 2 Foundational**: Depends on Setup and blocks all stories.
- **US1 Lemmas catalogue** and **US2 Stems catalogue**: Depend only on Foundational; they can proceed
  in parallel on separate resource files.
- **US3 Ayahs**, **US4 Words**, **US5 Surahs**, and **US6 Relationships/Types**: Depend on Foundational
  and the applicable resource summary/selection path from US1 or US2. Backend lemma/stem tracks can
  proceed in parallel, but edits to each shared reader/cache/controller/facade must be serialized.
- **US7 Mushaf links**: Depends on Foundational route/deep-link builders; can run after those exist,
  though end-to-end verification benefits from US1/US2 pages.
- **US8 URL restoration**: Depends on all URL-addressable views intended for release.
- **Polish**: Depends on every story included in the release.

### User story dependency graph

```text
Setup
  └─ Foundational
      ├─ US1 Lemmas catalogue ─┬─ US3 Ayahs ───────┐
      │                       ├─ US4 Words ────────┤
      │                       ├─ US5 Surahs ───────┤
      │                       └─ US6 Types/Links ──┤
      ├─ US2 Stems catalogue ──┬─ US3 Ayahs ───────┤
      │                       ├─ US4 Words ────────┤
      │                       ├─ US5 Surahs ───────┤
      │                       └─ US6 Types/Links ──┤
      └─ US7 Mushaf identities/links ──────────────┤
                                                   └─ US8 Full URL restoration
                                                        └─ Polish
```

### Shared-file serialization

Do not run tasks that edit the same file concurrently:

- `EfLemmasReader.cs`: T012 → T033 → T054 → T066 → T078 → T088.
- `EfStemsReader.cs`: T013 → T044 → T055 → T067 → T079 → T089.
- `CachedLemmasReader.cs`: T014 → T034 → T056/T068/T080/T090 in story order.
- `CachedStemsReader.cs`: T014 → T045 → T056/T068/T080/T090 in story order.
- `LemmasController.cs`: T016 → T036 → T058 → T070 → T082 → T092.
- `StemsController.cs`: T016 → T047 → T058 → T070 → T082 → T092.
- `lemmas-detail.facade.ts`: T026 → T059 → T073 → T083 → T096 → T109.
- `stems-detail.facade.ts`: T027 → T060 → T074 → T084 → T096 → T110.
- Lemma panel files: T028 → T061 → T075 → T085 → T097 → T117/T118.
- Stem panel files: T029 → T062 → T075 → T085 → T097 → T117/T118.
- Words hub files: T040 then T051.

### Within each story

1. Implement reader/derivation behavior.
2. Add cache behavior.
3. Add Application query/handler/outcome.
4. Add controller action.
5. Implement frontend state/data loading.
6. Implement frontend presentation and routing.
7. Run the story's backend and frontend checkpoint tests.

---

## Parallel Opportunities

- Foundational backend lemma and stem contracts/readers are parallel: T003/T006/T008/T012 versus
  T004/T007/T009/T013.
- Foundational frontend lemma and stem tracks are parallel: T019/T022/T026/T028 versus
  T020/T023/T027/T029.
- US1 and US2 can run fully in parallel because they use resource-specific files, except the two Words
  hub edits T040/T051 must be sequential.
- In US3–US6, the lemma and stem backend tasks marked `[P]` can run together; the corresponding
  frontend resource tasks can also run together.
- Story checkpoint backend and frontend tests marked `[P]` can run simultaneously.
- US7 backend DTO/mapping and frontend model preparation can begin in parallel after the route/deep-link
  builders exist.

### Parallel example — P1 catalogues

```text
Agent A: T032–T042 (US1 Lemmas catalogue)
Agent B: T043–T053 (US2 Stems catalogue)
Coordinate only T040/T051 because both edit the Words hub.
```

### Parallel example — Ayahs

```text
Backend lemma reader track: T054
Backend stem reader track: T055
After both readers: run T056 and T057 in parallel, then T058
Frontend lemma track: T059, T061
Frontend stem track: T060, T062
Do not assign shared tasks T056–T058 to both agents; one owner performs each shared task.
```

### Parallel example — Story checkpoint

```text
Run T064 backend ayah tests and T065 frontend ayah tests at the same time.
Do not start the next shared-file edit until both checkpoint results are understood.
```

---

## Implementation Strategy

### Recommended MVP

The coherent MVP is both P1 catalogue stories:

1. Complete Setup and Foundational.
2. Complete US1 Lemmas catalogue.
3. Complete US2 Stems catalogue.
4. Stop and run CP-1A and CP-1B.
5. Demo two searchable/restorable summary catalogues before adding detail views.

Shipping only US1 is technically testable but leaves the combined Feature 016 product incomplete; use
it only as an internal implementation checkpoint.

### Incremental delivery

1. P1 catalogues.
2. P2 ayah context.
3. P2 word forms and Unique Words links.
4. P3 surah distribution.
5. P3 type distribution and morphology links.
6. P3 Mushaf bidirectional links.
7. P4 complete restoration.
8. Cross-cutting hardening and full builds.

### Cheaper-model execution rule

For every task:

1. Read the exact referenced contract section and the Feature 015 analog before editing.
2. Edit only the named path(s).
3. Do not broaden scope or create a generic abstraction unless the task explicitly requests it.
4. Run the smallest relevant compile/test after the task.
5. At the story checkpoint, run the listed focused tests and compare behavior to the independent test.
6. If a required source field or existing helper differs from the plan, stop and document the mismatch
   instead of guessing Quran data or changing the contract silently.

---

## Notes

- `[P]` means different files or independently owned resource tracks with no incomplete dependency.
- `[USx]` maps directly to the corresponding user story in `spec.md`.
- Every checklist item includes an exact target path; do not replace paths with new dumping folders.
- Commit only when explicitly requested. In this workspace, child repositories are committed before
  the workspace submodule pointers.
- The optional Spec Kit commit hooks remain optional and are not part of implementation.
