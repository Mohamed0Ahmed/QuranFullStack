# Tasks: Words Hub + Unique Words Explorer

**Input**: Design documents from `specs/014-words-hub-unique-words/`
**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/`, `quickstart.md`

**Tests**: Automated test tasks are included because `plan.md`, `quickstart.md`, and the user request require implementation-safe clarity for backend read behavior, frontend state, URL restoration, and Quranic data safety.

**Organization**: Tasks are grouped by user story so each story can be implemented, tested, and demonstrated independently after the shared foundation is complete.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel because it touches different files and does not depend on incomplete tasks.
- **[Story]**: Maps to user stories in `spec.md`: `[US1]`, `[US2]`, `[US3]`, `[US4]`.
- Every task includes at least one exact repository-relative path.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create compile-safe feature-owned scaffolding files only where implementation files will be added, without changing behavior yet.

- [X] T001 Create backend Words application scaffolding files `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/IUniqueWordsReader.cs` and `Backend/application/QuranDashboard.Application/Quran/Words/Queries/GetUniqueWordsPage/GetUniqueWordsPageQuery.cs` with compile-safe namespaces only; implementation details are filled in T013 and T035.
- [X] T002 [P] Create backend Words infrastructure and API scaffolding files `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/EfUniqueWordsReader.cs` and `Backend/api/QuranDashboard.Api/Controllers/Words/UniqueWordsController.cs` with compile-safe empty class skeletons only.
- [X] T003 [P] Create backend Words test scaffolding file `Backend/tests/QuranDashboard.Tests/Quran/Words/UniqueWordsListReadTests.cs` with a compile-safe empty test class; concrete tests are filled in T029.
- [X] T004 [P] Create frontend Words route and model scaffolding files `Frontend/quran-dashboard-ui/src/app/features/words/words.routes.ts` and `Frontend/quran-dashboard-ui/src/app/features/words/models/unique-words.models.ts` with compile-safe exports only; implementation details are filled in T015 and T022.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Add shared contracts, enums, DTOs, and frontend model types that all user stories depend on.

**CRITICAL**: No user-story implementation should begin until this phase is complete.

- [X] T005 Create minimal `PagedResult<T>` response contract with `Page`, `PageSize`, `TotalCount`, and `Items` in `Backend/application/QuranDashboard.Application.Abstractions/Common/Paging/PagedResult.cs`.
- [X] T006 [P] Create `UniqueWordKind` enum plus `TryParse` helper for `tashkeel` and `simple` in `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/UniqueWordKind.cs`.
- [X] T007 [P] Create `UniqueWordSort` enum plus `TryParse` helper for `mushaf-order`, `occurrences`, and `alpha` in `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/UniqueWordSort.cs`.
- [X] T008 [P] Create `UniqueWordListItemDto` response record with all list fields from `data-model.md` in `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/Responses/UniqueWordListItemDto.cs`.
- [X] T009 [P] Create `UniqueWordSummaryDto` response record with all summary fields from `data-model.md` in `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/Responses/UniqueWordSummaryDto.cs`.
- [X] T010 [P] Create `UniqueWordSurahsResponse` and `UniqueWordSurahItemDto` records in `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/Responses/UniqueWordSurahsResponse.cs`.
- [X] T011 [P] Create `UniqueWordMissingSurahsResponse` and `MissingSurahItemDto` records in `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/Responses/UniqueWordMissingSurahsResponse.cs`.
- [X] T012 [P] Create `UniqueWordAyahMatchDto` and `AyahWordForHighlightDto` records in `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/Responses/UniqueWordAyahMatchDto.cs`.
- [X] T013 Complete `IUniqueWordsReader` with list, summary, mentioned-surahs, missing-surahs, and ayah-match method signatures from `contracts/backend-read-abstractions.md` in `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/IUniqueWordsReader.cs`.
- [X] T014 Add Arabic Words API message constants for list, summary, mentioned surahs, missing surahs, ayahs, invalid kind, invalid paging, and not found in `Backend/api/QuranDashboard.Api/Common/ApiMessages.cs`.
- [X] T015 Complete frontend unique-word DTOs, route key types, sort types, drill-down view types, and state interfaces in `Frontend/quran-dashboard-ui/src/app/features/words/models/unique-words.models.ts`.
- [X] T016 [P] Create frontend Arabic labels/constants for hub cards, tabs, chips, empty states, and drill-down labels in `Frontend/quran-dashboard-ui/src/app/features/words/models/unique-words.labels.ts`.

**Checkpoint**: Shared backend contracts and frontend types exist. User-story implementation can now begin.

---

## Phase 3: User Story 1 - Open The Words Hub (Priority: P1) MVP

**Goal**: Users can open `/dashboard/words`, see the active `الكلمات الفريدة` card, and see four disabled coming-soon sections.

**Independent Test**: Open the Words area from navigation and confirm the hub renders one active card, four disabled `قريبًا` cards, Arabic labels, and no unwanted navigation from disabled cards.

### Tests for User Story 1

- [X] T017 [P] [US1] Add route and navigation tests for `/dashboard/words` and words fallback-route exclusion in `Frontend/quran-dashboard-ui/src/app/features/words/words.routes.spec.ts`.
- [X] T018 [P] [US1] Add hub page rendering tests for active and disabled cards in `Frontend/quran-dashboard-ui/src/app/features/words/pages/words-hub-page/words-hub-page.component.spec.ts`.
- [X] T019 [P] [US1] Add word section card accessibility tests for disabled/non-navigable future cards in `Frontend/quran-dashboard-ui/src/app/features/words/components/word-section-card/word-section-card.component.spec.ts`.

### Implementation for User Story 1

- [X] T020 [US1] Update the `words` nav item route from `/words` to `/dashboard/words` in `Frontend/quran-dashboard-ui/src/app/core/navigation/nav-items.ts`.
- [X] T021 [US1] Add lazy route loading for `/dashboard/words` and exclude `words` from fallback routes in `Frontend/quran-dashboard-ui/src/app/app.routes.ts`.
- [X] T022 [US1] Create `WORDS_ROUTES` with `/dashboard/words` hub route and `/dashboard/words/unique` redirect scaffolding in `Frontend/quran-dashboard-ui/src/app/features/words/words.routes.ts`.
- [X] T023 [P] [US1] Implement `WordSectionCardComponent` inputs for label, description, active route, disabled state, and `قريبًا` badge in `Frontend/quran-dashboard-ui/src/app/features/words/components/word-section-card/word-section-card.component.ts`.
- [X] T024 [P] [US1] Implement `WordSectionCardComponent` template using `qd-card`, `qd-badge`, and accessible disabled behavior in `Frontend/quran-dashboard-ui/src/app/features/words/components/word-section-card/word-section-card.component.html`.
- [X] T025 [P] [US1] Implement small RTL card layout styles for `WordSectionCardComponent` without redefining global `qd-` primitives in `Frontend/quran-dashboard-ui/src/app/features/words/components/word-section-card/word-section-card.component.scss`.
- [X] T026 [US1] Implement `WordsHubPageComponent` TypeScript with one active card and four disabled card view models in `Frontend/quran-dashboard-ui/src/app/features/words/pages/words-hub-page/words-hub-page.component.ts`.
- [X] T027 [US1] Implement `WordsHubPageComponent` Arabic RTL template with `qd-page`, `qd-section-title`, and the card grid in `Frontend/quran-dashboard-ui/src/app/features/words/pages/words-hub-page/words-hub-page.component.html`.
- [X] T028 [P] [US1] Implement local responsive spacing for the hub card grid in `Frontend/quran-dashboard-ui/src/app/features/words/pages/words-hub-page/words-hub-page.component.scss`.

**Checkpoint**: User Story 1 is functional without backend API work. `/dashboard/words` is a real hub and future cards are disabled.

---

## Phase 4: User Story 2 - Browse And Search Unique Quran Words (Priority: P1)

**Goal**: Users can open the Unique Words explorer, switch between `tashkeel` and `simple`, search with normalized contains matching, sort results, paginate results, and see Uthmani display text plus four counts.

**Independent Test**: Open each mode, search using Arabic text with and without diacritics, change sort/page, and confirm each result shows `displayTextUthmani`, `occurrencesCount`, `ayahsCount`, `surahsCount`, and `missingSurahsCount`.

### Tests for User Story 2

- [X] T029 [P] [US2] Add backend list read tests for default page, counts, `missingSurahsCount = 114 - surahsCount`, and simple-mode display label in `Backend/tests/QuranDashboard.Tests/Quran/Words/UniqueWordsListReadTests.cs`.
- [X] T030 [P] [US2] Add backend search, sort, paging bounds, and empty-result tests in `Backend/tests/QuranDashboard.Tests/Quran/Words/UniqueWordsSearchSortPagingTests.cs`.
- [X] T031 [P] [US2] Add backend validation tests for invalid kind, invalid sort, invalid page, and invalid page size in `Backend/tests/QuranDashboard.Tests/Quran/Words/UniqueWordsValidationTests.cs`.
- [X] T032 [P] [US2] Add frontend API service tests for list query params and `ApiResponse<T>` typing in `Frontend/quran-dashboard-ui/src/app/features/words/data-access/unique-words.api.spec.ts`.
- [X] T033 [P] [US2] Add frontend facade list-state tests for loading, empty, backend failure, transport failure, mode, search, sort, and page in `Frontend/quran-dashboard-ui/src/app/features/words/state/unique-words.facade.list.spec.ts`.
- [X] T034 [P] [US2] Add frontend list component tests for tabs, search input, sort control, cards, count chips, and pagination events in `Frontend/quran-dashboard-ui/src/app/features/words/pages/unique-words-page/unique-words-page.component.spec.ts`.

### Backend Implementation for User Story 2

- [X] T035 [P] [US2] Complete `GetUniqueWordsPageQuery` with `Kind`, `Search`, `Sort`, `Page`, and `PageSize` values in `Backend/application/QuranDashboard.Application/Quran/Words/Queries/GetUniqueWordsPage/GetUniqueWordsPageQuery.cs`.
- [X] T036 [P] [US2] Create `GetUniqueWordsPageOutcome` with success, validation failure, and failure message cases in `Backend/application/QuranDashboard.Application/Quran/Words/Queries/GetUniqueWordsPage/GetUniqueWordsPageOutcome.cs`.
- [X] T037 [US2] Implement `GetUniqueWordsPageHandler` validation and reader orchestration in `Backend/application/QuranDashboard.Application/Quran/Words/Queries/GetUniqueWordsPage/GetUniqueWordsPageHandler.cs`.
- [X] T038 [US2] Implement list read logic for `tashkeel` and `simple` using unique tables, precomputed counts, normalized contains search, and no `quran_words` per-card grouping in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/EfUniqueWordsReader.cs`.
- [X] T039 [US2] Register `IUniqueWordsReader` to `EfUniqueWordsReader` in `Backend/infrastructure/QuranDashboard.Infrastructure/DependencyInjection.cs`.
- [X] T040 [US2] Implement list endpoint `GET /api/words/unique/{kind}` in `Backend/api/QuranDashboard.Api/Controllers/Words/UniqueWordsController.cs`.

### Frontend Implementation for User Story 2

- [X] T041 [US2] Add `/dashboard/words/unique`, `/dashboard/words/unique/tashkeel`, and `/dashboard/words/unique/simple` routes in `Frontend/quran-dashboard-ui/src/app/features/words/words.routes.ts`.
- [X] T042 [US2] Implement `UniqueWordsApi` list method returning `Observable<ApiResponse<PagedResultDto<UniqueWordListItemDto>>>` in `Frontend/quran-dashboard-ui/src/app/features/words/data-access/unique-words.api.ts`.
- [X] T043 [US2] Implement list state, route mode state, query param parsing for `search`, `sort`, and `page`, and `ApiResponse<T>` unwrapping in `Frontend/quran-dashboard-ui/src/app/features/words/state/unique-words.facade.ts`.
- [X] T044 [P] [US2] Implement `UniqueWordsTabsComponent` TypeScript for stable `tashkeel` and `simple` route links in `Frontend/quran-dashboard-ui/src/app/features/words/components/unique-words-tabs/unique-words-tabs.component.ts`.
- [X] T045 [P] [US2] Implement `UniqueWordsTabsComponent` template and RTL styles in `Frontend/quran-dashboard-ui/src/app/features/words/components/unique-words-tabs/unique-words-tabs.component.html` and `Frontend/quran-dashboard-ui/src/app/features/words/components/unique-words-tabs/unique-words-tabs.component.scss`.
- [X] T046 [P] [US2] Implement `UniqueWordsSearchBarComponent` TypeScript for search and sort outputs in `Frontend/quran-dashboard-ui/src/app/features/words/components/unique-words-search-bar/unique-words-search-bar.component.ts`.
- [X] T047 [P] [US2] Implement `UniqueWordsSearchBarComponent` template with `qd-input`, `qd-select`, Arabic labels, and submit/change events in `Frontend/quran-dashboard-ui/src/app/features/words/components/unique-words-search-bar/unique-words-search-bar.component.html`.
- [X] T048 [P] [US2] Implement `UniqueWordCardComponent` TypeScript inputs for display word, counts, first location, and disabled drill-down state before US3 in `Frontend/quran-dashboard-ui/src/app/features/words/components/unique-word-card/unique-word-card.component.ts`.
- [X] T049 [P] [US2] Implement `UniqueWordCardComponent` template using Amiri/Uthmani display text and four count chips in `Frontend/quran-dashboard-ui/src/app/features/words/components/unique-word-card/unique-word-card.component.html`.
- [X] T050 [P] [US2] Implement `WordCountChipComponent` TypeScript inputs and output event in `Frontend/quran-dashboard-ui/src/app/features/words/components/word-count-chip/word-count-chip.component.ts`.
- [X] T051 [P] [US2] Implement `WordCountChipComponent` template with real button semantics and `aria-label` text in `Frontend/quran-dashboard-ui/src/app/features/words/components/word-count-chip/word-count-chip.component.html`.
- [X] T052 [US2] Implement `UniqueWordsPageComponent` TypeScript shell that reads facade state and handles search, sort, page, and mode events in `Frontend/quran-dashboard-ui/src/app/features/words/pages/unique-words-page/unique-words-page.component.ts`.
- [X] T053 [US2] Implement `UniqueWordsPageComponent` template for tabs, toolbar, loading, empty, error, card list, and pagination in `Frontend/quran-dashboard-ui/src/app/features/words/pages/unique-words-page/unique-words-page.component.html`.
- [X] T054 [P] [US2] Implement local responsive layout styles for the explorer page without redefining shared primitives in `Frontend/quran-dashboard-ui/src/app/features/words/pages/unique-words-page/unique-words-page.component.scss`.

**Checkpoint**: User Story 2 is independently testable with only the list API and explorer page; drill-down clicks may be disabled or no-op until US3.

---

## Phase 5: User Story 3 - Inspect Word Distribution Drill-Downs (Priority: P2)

**Goal**: Users can open modal drill-downs for mentioned surahs, missing surahs, and ayahs with exact ID-based highlighted matches.

**Independent Test**: Pick a visible unique word, open `السور`, `لم يذكر في`, and `الآيات`; verify counts, missing/mentioned surah partition, paged ayahs, and exact matched word highlighting.

### Tests for User Story 3

- [X] T055 [P] [US3] Add backend mentioned-surahs and missing-surahs read tests including disjoint union with 114 surahs in `Backend/tests/QuranDashboard.Tests/Quran/Words/UniqueWordSurahDrilldownTests.cs`.
- [X] T056 [P] [US3] Add backend ayah-match tests for paged distinct ayahs, all matching IDs, multiple matches in one ayah, marker exclusion, no string matching assumptions, and no N+1/batched access shape in `Backend/tests/QuranDashboard.Tests/Quran/Words/UniqueWordAyahMatchesTests.cs`.
- [X] T057 [P] [US3] Add frontend facade drill-down tests for opening modal views, loading each view, empty states, errors, close behavior, and list context preservation in `Frontend/quran-dashboard-ui/src/app/features/words/state/unique-words.facade.drilldown.spec.ts`.
- [X] T058 [P] [US3] Add frontend modal and list rendering tests for surahs, missing surahs, ayahs, pagination, and close behavior in `Frontend/quran-dashboard-ui/src/app/features/words/components/word-drilldown-modal/word-drilldown-modal.component.spec.ts`.
- [X] T059 [P] [US3] Add frontend highlighted ayah tests proving only `matchedQuranWordIds` are highlighted in `Frontend/quran-dashboard-ui/src/app/features/words/components/highlighted-ayah/highlighted-ayah.component.spec.ts`.

### Backend Implementation for User Story 3

- [X] T060 [P] [US3] Create `GetUniqueWordSurahsQuery`, `GetUniqueWordSurahsOutcome`, and `GetUniqueWordSurahsHandler` in `Backend/application/QuranDashboard.Application/Quran/Words/Queries/GetUniqueWordSurahs/GetUniqueWordSurahsQuery.cs`, `Backend/application/QuranDashboard.Application/Quran/Words/Queries/GetUniqueWordSurahs/GetUniqueWordSurahsOutcome.cs`, and `Backend/application/QuranDashboard.Application/Quran/Words/Queries/GetUniqueWordSurahs/GetUniqueWordSurahsHandler.cs`.
- [X] T061 [P] [US3] Create `GetUniqueWordMissingSurahsQuery`, `GetUniqueWordMissingSurahsOutcome`, and `GetUniqueWordMissingSurahsHandler` in `Backend/application/QuranDashboard.Application/Quran/Words/Queries/GetUniqueWordMissingSurahs/GetUniqueWordMissingSurahsQuery.cs`, `Backend/application/QuranDashboard.Application/Quran/Words/Queries/GetUniqueWordMissingSurahs/GetUniqueWordMissingSurahsOutcome.cs`, and `Backend/application/QuranDashboard.Application/Quran/Words/Queries/GetUniqueWordMissingSurahs/GetUniqueWordMissingSurahsHandler.cs`.
- [X] T062 [P] [US3] Create `GetUniqueWordAyahsQuery`, `GetUniqueWordAyahsOutcome`, and `GetUniqueWordAyahsHandler` in `Backend/application/QuranDashboard.Application/Quran/Words/Queries/GetUniqueWordAyahs/GetUniqueWordAyahsQuery.cs`, `Backend/application/QuranDashboard.Application/Quran/Words/Queries/GetUniqueWordAyahs/GetUniqueWordAyahsOutcome.cs`, and `Backend/application/QuranDashboard.Application/Quran/Words/Queries/GetUniqueWordAyahs/GetUniqueWordAyahsHandler.cs`.
- [X] T063 [US3] Implement mentioned-surahs, missing-surahs, and ayah-match read methods in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/EfUniqueWordsReader.cs`; ayah matches MUST use the matched rows → paged ayah IDs → batched ayah words flow from `contracts/unique-words-api.md` to avoid N+1 reads.
- [X] T064 [US3] Add endpoints for `GET /api/words/unique/{kind}/{id}/surahs`, `/missing-surahs`, and `/ayahs` in `Backend/api/QuranDashboard.Api/Controllers/Words/UniqueWordsController.cs`.

### Frontend Implementation for User Story 3

- [X] T065 [US3] Add API methods for mentioned surahs, missing surahs, and ayah matches in `Frontend/quran-dashboard-ui/src/app/features/words/data-access/unique-words.api.ts`.
- [X] T066 [US3] Add modal drill-down state, per-view loading/empty/error handling, close behavior, and ayah page state to `Frontend/quran-dashboard-ui/src/app/features/words/state/unique-words.facade.ts`.
- [X] T067 [P] [US3] Implement `WordDrilldownModalComponent` TypeScript inputs and outputs for selected word, active view, close, and ayah page events in `Frontend/quran-dashboard-ui/src/app/features/words/components/word-drilldown-modal/word-drilldown-modal.component.ts`.
- [X] T068 [P] [US3] Implement `WordDrilldownModalComponent` template using `qd-modal`, segmented view buttons, loading, empty, and error states in `Frontend/quran-dashboard-ui/src/app/features/words/components/word-drilldown-modal/word-drilldown-modal.component.html`.
- [X] T069 [P] [US3] Implement modal local layout styles and focus-safe RTL spacing in `Frontend/quran-dashboard-ui/src/app/features/words/components/word-drilldown-modal/word-drilldown-modal.component.scss`.
- [X] T070 [P] [US3] Implement `SurahOccurrencesListComponent` for `السور` rows with surah name and occurrence count in `Frontend/quran-dashboard-ui/src/app/features/words/components/surah-occurrences-list/surah-occurrences-list.component.ts` and `Frontend/quran-dashboard-ui/src/app/features/words/components/surah-occurrences-list/surah-occurrences-list.component.html`.
- [X] T071 [P] [US3] Implement `MissingSurahsListComponent` for `لم يذكر في` rows in `Frontend/quran-dashboard-ui/src/app/features/words/components/missing-surahs-list/missing-surahs-list.component.ts` and `Frontend/quran-dashboard-ui/src/app/features/words/components/missing-surahs-list/missing-surahs-list.component.html`.
- [X] T072 [P] [US3] Implement `HighlightedAyahComponent` to render word tokens and mark only IDs in `matchedQuranWordIds` in `Frontend/quran-dashboard-ui/src/app/features/words/components/highlighted-ayah/highlighted-ayah.component.ts` and `Frontend/quran-dashboard-ui/src/app/features/words/components/highlighted-ayah/highlighted-ayah.component.html`.
- [X] T073 [P] [US3] Implement `AyahMatchesListComponent` with paged ayah cards and `HighlightedAyahComponent` in `Frontend/quran-dashboard-ui/src/app/features/words/components/ayah-matches-list/ayah-matches-list.component.ts` and `Frontend/quran-dashboard-ui/src/app/features/words/components/ayah-matches-list/ayah-matches-list.component.html`.
- [X] T074 [US3] Wire `WordCountChipComponent` events for `السور`, `لم يذكر في`, and `الآيات` to open the modal in `Frontend/quran-dashboard-ui/src/app/features/words/pages/unique-words-page/unique-words-page.component.ts`.
- [X] T075 [US3] Render `WordDrilldownModalComponent` from the explorer page and bind facade modal state in `Frontend/quran-dashboard-ui/src/app/features/words/pages/unique-words-page/unique-words-page.component.html`.

**Checkpoint**: User Story 3 is independently testable from a visible unique-word card; all three modal views work and highlighting is ID-based.

---

## Phase 6: User Story 4 - Restore A Shared Explorer State (Priority: P3)

**Goal**: Users can refresh, bookmark, or share a URL that restores mode, search, sort, list page, selected word by stable ID, modal view, and ayah page.

**Independent Test**: Open a modal drill-down with non-default mode/search/sort/page, copy the URL, reopen it, and confirm the same state is restored or a controlled Arabic not-found state appears for invalid word IDs.

### Tests for User Story 4

- [X] T076 [P] [US4] Add backend summary endpoint tests for valid ID, invalid kind, and unknown ID in `Backend/tests/QuranDashboard.Tests/Quran/Words/UniqueWordSummaryTests.cs`.
- [X] T077 [P] [US4] Add frontend URL sync tests for mode, search, sort, page, word, view, ayah page, modal close cleanup, and back/forward behavior in `Frontend/quran-dashboard-ui/src/app/features/words/state/unique-words-url-sync.spec.ts`.
- [X] T078 [P] [US4] Add frontend restored invalid state tests for unknown word ID and invalid drill-down view in `Frontend/quran-dashboard-ui/src/app/features/words/state/unique-words.facade.restore.spec.ts`.

### Backend Implementation for User Story 4

- [X] T079 [P] [US4] Create `GetUniqueWordSummaryQuery`, `GetUniqueWordSummaryOutcome`, and `GetUniqueWordSummaryHandler` in `Backend/application/QuranDashboard.Application/Quran/Words/Queries/GetUniqueWordSummary/GetUniqueWordSummaryQuery.cs`, `Backend/application/QuranDashboard.Application/Quran/Words/Queries/GetUniqueWordSummary/GetUniqueWordSummaryOutcome.cs`, and `Backend/application/QuranDashboard.Application/Quran/Words/Queries/GetUniqueWordSummary/GetUniqueWordSummaryHandler.cs`.
- [X] T080 [US4] Implement selected unique-word summary read method in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/EfUniqueWordsReader.cs`.
- [X] T081 [US4] Add endpoint `GET /api/words/unique/{kind}/{id}` in `Backend/api/QuranDashboard.Api/Controllers/Words/UniqueWordsController.cs`.

### Frontend Implementation for User Story 4

- [X] T082 [US4] Add summary API method in `Frontend/quran-dashboard-ui/src/app/features/words/data-access/unique-words.api.ts`.
- [X] T083 [P] [US4] Implement pure query-param parse/build helpers for `search`, `sort`, `page`, `word`, `view`, and `ap` in `Frontend/quran-dashboard-ui/src/app/features/words/state/unique-words-url-sync.ts`.
- [X] T084 [US4] Integrate URL hydration, summary loading for restored modal state, invalid view normalization, and unknown word feedback in `Frontend/quran-dashboard-ui/src/app/features/words/state/unique-words.facade.ts`.
- [X] T085 [US4] Update modal close behavior to clear only `word`, `view`, and `ap` query params while preserving list context in `Frontend/quran-dashboard-ui/src/app/features/words/pages/unique-words-page/unique-words-page.component.ts`.
- [X] T086 [US4] Add controlled Arabic not-found and invalid-state messages to page/modal rendering in `Frontend/quran-dashboard-ui/src/app/features/words/pages/unique-words-page/unique-words-page.component.html`.

**Checkpoint**: User Story 4 is independently testable by URL refresh/share and invalid restored-state scenarios.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Verify quality, accessibility, data safety, and documentation across all completed stories.

- [X] T087 [P] Add Swagger-visible summaries or XML comments for Words endpoints if the project uses them in `Backend/api/QuranDashboard.Api/Controllers/Words/UniqueWordsController.cs`.
- [X] T088 [P] Review Arabic backend messages for consistency and absence of invented Quranic content in `Backend/api/QuranDashboard.Api/Common/ApiMessages.cs`.
- [X] T089 [P] Review frontend Arabic labels for consistency and absence of invented Quranic content in `Frontend/quran-dashboard-ui/src/app/features/words/models/unique-words.labels.ts`.
- [X] T090 [P] Ensure component SCSS uses shared `qd-` classes/tokens and does not redefine cards/buttons/modals in `Frontend/quran-dashboard-ui/src/app/features/words/components/word-section-card/word-section-card.component.scss`, `Frontend/quran-dashboard-ui/src/app/features/words/components/unique-words-tabs/unique-words-tabs.component.scss`, `Frontend/quran-dashboard-ui/src/app/features/words/components/word-drilldown-modal/word-drilldown-modal.component.scss`, `Frontend/quran-dashboard-ui/src/app/features/words/pages/words-hub-page/words-hub-page.component.scss`, and `Frontend/quran-dashboard-ui/src/app/features/words/pages/unique-words-page/unique-words-page.component.scss`.
- [X] T091 Run backend tests for the Words feature using `dotnet test Backend/QuranDashboard.sln --filter Words` from `/projects/Dashboard/App`.
- [X] T092 Run frontend tests for the Words feature using `npm test` from `Frontend/quran-dashboard-ui/`, preserving the script's existing `VITEST_MIN_FORKS=1` and `VITEST_MAX_FORKS=2` limits in `Frontend/quran-dashboard-ui/package.json`.
- [X] T093 Run backend build using `dotnet build Backend/QuranDashboard.sln` from `/projects/Dashboard/App`.
- [X] T094 Run frontend build using `npm run build` from `Frontend/quran-dashboard-ui/`.
- [X] T095 Execute the browser/API smoke checks from `specs/014-words-hub-unique-words/quickstart.md` and note any deviations in `specs/014-words-hub-unique-words/quickstart.md`.
- [X] T096 Perform clean-code and test-code self-checks against `CODING_PRINCIPLES.md`, `.claude/skills/engineering-review/references/clean-code-guard/`, and `.claude/skills/test-guard/`.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 Setup**: No dependencies.
- **Phase 2 Foundational**: Depends on Phase 1 and blocks all user-story work.
- **Phase 3 US1**: Depends on Phase 2 only; delivers the MVP hub without backend API.
- **Phase 4 US2**: Depends on Phase 2; can begin after foundation, but frontend navigation is easiest after US1 route setup.
- **Phase 5 US3**: Depends on Phase 2 and the shared DTOs; it can be developed alongside US2 with coordination on `EfUniqueWordsReader.cs`, `UniqueWordsController.cs`, `unique-words.api.ts`, and `unique-words.facade.ts`.
- **Phase 6 US4**: Depends on modal/list concepts from US2 and US3, so implement after US2 and US3 for lowest risk.
- **Phase 7 Polish**: Depends on all desired user stories being complete.

### User Story Dependencies

- **US1 (P1)**: Independent frontend MVP after foundation.
- **US2 (P1)**: Independent list/search/pagination slice after foundation; can be demonstrated without drill-downs.
- **US3 (P2)**: Requires visible unique-word cards from US2 for normal UI entry, but backend drill-down APIs can be built independently after foundation.
- **US4 (P3)**: Depends on US2 list state and US3 modal state to restore the full shared URL behavior.

### Within Each User Story

- Add tests first when listed for that story.
- Add backend query/outcome/handler before controller endpoint tasks.
- Add reader implementation before endpoint task validation.
- Add frontend API service before facade integration.
- Add child components before binding them in the routeable page template.

---

## Parallel Opportunities

- **Setup**: T002, T003, and T004 can run in parallel after T001 starts.
- **Foundational**: T006 through T012 and T016 can run in parallel; T013 depends on response DTO names being agreed.
- **US1**: T017 through T019 can be written in parallel; T023 through T025 can run in parallel with T026 through T028.
- **US2**: Backend tests T029 through T031 can run in parallel with frontend tests T032 through T034; frontend components T044 through T051 can run in parallel after models exist.
- **US3**: Backend query folders T060 through T062 can run in parallel; frontend modal/list/highlight components T067 through T073 can run in parallel.
- **US4**: T076 through T078 can run in parallel; T079 and T083 can run in parallel.
- **Polish**: T087 through T090 can run in parallel after implementation tasks finish.

---

## Parallel Example: User Story 2

```text
Task: T029 Add backend list read tests in Backend/tests/QuranDashboard.Tests/Quran/Words/UniqueWordsListReadTests.cs
Task: T030 Add backend search/sort/paging tests in Backend/tests/QuranDashboard.Tests/Quran/Words/UniqueWordsSearchSortPagingTests.cs
Task: T032 Add frontend API service tests in Frontend/quran-dashboard-ui/src/app/features/words/data-access/unique-words.api.spec.ts
Task: T044 Implement UniqueWordsTabsComponent in Frontend/quran-dashboard-ui/src/app/features/words/components/unique-words-tabs/unique-words-tabs.component.ts
Task: T046 Implement UniqueWordsSearchBarComponent in Frontend/quran-dashboard-ui/src/app/features/words/components/unique-words-search-bar/unique-words-search-bar.component.ts
Task: T048 Implement UniqueWordCardComponent in Frontend/quran-dashboard-ui/src/app/features/words/components/unique-word-card/unique-word-card.component.ts
```

## Parallel Example: User Story 3

```text
Task: T055 Add surah drill-down tests in Backend/tests/QuranDashboard.Tests/Quran/Words/UniqueWordSurahDrilldownTests.cs
Task: T056 Add ayah-match tests in Backend/tests/QuranDashboard.Tests/Quran/Words/UniqueWordAyahMatchesTests.cs
Task: T060 Create GetUniqueWordSurahsQuery.cs, GetUniqueWordSurahsOutcome.cs, and GetUniqueWordSurahsHandler.cs
Task: T061 Create GetUniqueWordMissingSurahsQuery.cs, GetUniqueWordMissingSurahsOutcome.cs, and GetUniqueWordMissingSurahsHandler.cs
Task: T062 Create GetUniqueWordAyahsQuery.cs, GetUniqueWordAyahsOutcome.cs, and GetUniqueWordAyahsHandler.cs
Task: T072 Implement HighlightedAyahComponent in Frontend/quran-dashboard-ui/src/app/features/words/components/highlighted-ayah/
```

---

## Implementation Strategy

### MVP First (US1 Only)

1. Complete Phase 1 setup.
2. Complete Phase 2 foundation.
3. Complete Phase 3 US1.
4. Validate `/dashboard/words` hub manually and with frontend tests.
5. Stop for demo if only the Words entry point is needed.

### Practical Feature Increment

1. Complete setup and foundation.
2. Complete US1 for the hub and navigation.
3. Complete US2 for the core Unique Words explorer list, search, sort, and pagination.
4. Complete US3 for modal drill-downs and exact highlighting.
5. Complete US4 for refresh/share restoration.
6. Complete polish tasks and quickstart verification.

### Notes For A Lower-Cost Implementation Model

- Do not implement roots, lemmas, stems, POS categories, audio, editing, imports, migrations, or global search.
- Do not add database indexes unless a measured performance problem is documented outside this task list.
- Do not use displayed Quran text as identity; use stable unique-word IDs.
- Do not highlight by string replacement; highlight only by matched word IDs.
- Do not let child components call API services directly; use `unique-words.facade.ts`.
- Keep routeable page components thin and split UI into the listed child components.
- If a listed file already exists during implementation, edit it in place instead of creating a duplicate.
