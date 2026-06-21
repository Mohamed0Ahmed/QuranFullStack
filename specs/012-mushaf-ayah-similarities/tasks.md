# Tasks: Mushaf Reader Ayah Similarities

**Input**: Design documents from `specs/012-mushaf-ayah-similarities/`
**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/`, `quickstart.md`

**Tests**: Included because the feature specification defines independent tests and the implementation plan requires backend/frontend verification. Write the listed tests before implementation work in each user story and confirm they fail for the missing behavior.

**Organization**: Tasks are grouped by user story so each story can be implemented and tested as an independent increment. The descriptions are intentionally explicit so a cheaper implementation model can follow them without guessing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel because it touches different files and does not depend on incomplete tasks.
- **[Story]**: Maps to a user story from `spec.md` (`US1`, `US2`, `US3`, `US4`).
- Every implementation task includes exact file paths.

## Path Conventions

- Backend root: `Backend/`
- Backend source: `Backend/application/`, `Backend/infrastructure/`, `Backend/api/`
- Backend tests: `Backend/tests/QuranDashboard.Tests/`
- Frontend root: `Frontend/quran-dashboard-ui/`
- Frontend source: `Frontend/quran-dashboard-ui/src/app/features/mushaf/`
- Spec artifacts: `specs/012-mushaf-ayah-similarities/`

---

## Phase 1: Setup (Shared Orientation)

**Purpose**: Confirm active feature context and read the files that constrain all implementation work.

- [X] T001 Review the Feature 012 source-of-truth documents in `specs/012-mushaf-ayah-similarities/plan.md`, `specs/012-mushaf-ayah-similarities/spec.md`, `specs/012-mushaf-ayah-similarities/data-model.md`, and `docs/feature-012-mushaf-reader-ayah-similarities/feature-012-mushaf-reader-ayah-similarities-planning-report.md`
- [X] T002 Review backend rules in `Backend/AGENTS.md`, `Backend/.architecture/BACKEND_STRUCTURE.md`, `Backend/.architecture/CLEAN_ARCHITECTURE.md`, and `Backend/.architecture/API_GUIDELINES.md`
- [X] T003 Review frontend rules in `Frontend/quran-dashboard-ui/AGENTS.md`, `Frontend/quran-dashboard-ui/.architecture/FRONTEND_STRUCTURE.md`, `Frontend/quran-dashboard-ui/.architecture/API_INTEGRATION_GUIDELINES.md`, and `Frontend/quran-dashboard-ui/.architecture/UI_STYLE_SYSTEM.md`
- [X] T004 Review existing Feature 011 Mushaf Reader backend files under `Backend/application/QuranDashboard.Application.Abstractions/Quran/MushafReader/`, `Backend/application/QuranDashboard.Application/Quran/MushafReader/Queries/`, `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/MushafReader/`, and `Backend/api/QuranDashboard.Api/Controllers/MushafReader/`
- [X] T005 Review existing Feature 011 Mushaf Reader frontend files under `Frontend/quran-dashboard-ui/src/app/features/mushaf/models/`, `Frontend/quran-dashboard-ui/src/app/features/mushaf/data-access/`, `Frontend/quran-dashboard-ui/src/app/features/mushaf/state/`, and `Frontend/quran-dashboard-ui/src/app/features/mushaf/components/selected-ayah-section/`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Add shared constants and enum/caching surfaces that all later stories depend on.

**Critical**: Do not create migrations, importers, public reader routes, audio, bookmarks, editing workflows, or graph features in any task.

- [X] T006 Add Arabic API message constants for similar ayahs and mutashabihat in `Backend/api/QuranDashboard.Api/Common/ApiMessages.cs`
- [X] T007 Add cache key helpers `SimilarAyahs(verseKey)` and `AyahMutashabihat(verseKey)` in `Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Quran/MushafReader/MushafReaderCacheKeys.cs`
- [X] T008 Widen `AyahStudyTab` to include `similar-ayahs` and `mutashabihat` in `Frontend/quran-dashboard-ui/src/app/features/mushaf/models/mushaf.models.ts`
- [X] T009 Add frontend cache key helpers `similarAyahs(verseKey)` and `ayahMutashabihat(verseKey)` in `Frontend/quran-dashboard-ui/src/app/features/mushaf/state/mushaf-reader-cache.ts`
- [X] T010 Add or update centralized Arabic labels for the two new selected-ayah actions in `Frontend/quran-dashboard-ui/src/app/features/mushaf/models/mushaf.models.ts`

**Checkpoint**: Foundational types/constants are ready. User story work can begin.

---

## Phase 3: User Story 1 - See Similarity Availability For A Selected Ayah (Priority: P1) MVP

**Goal**: Selecting an ayah shows `similaritySummary` counts in the selected ayah study response and the UI exposes the two new actions without loading detail payloads.

**Independent Test**: Select ayahs with and without similarity data. Confirm the Mushaf page response has no similarity counters, the selected ayah study response includes all three counts, and no full similar-ayah or mutashabihat detail request happens until a new action is opened.

### Tests for User Story 1

- [X] T011 [P] [US1] Add backend integration tests for `similaritySummary` count fields in `Backend/tests/QuranDashboard.Tests/Quran/MushafReader/AyahStudySimilaritySummaryTests.cs`
- [X] T012 [P] [US1] Add backend regression test proving `MushafPageResponse` has no similarity counters in `Backend/tests/QuranDashboard.Tests/Quran/MushafReader/MushafPageReadTests.cs`
- [X] T013 [P] [US1] Add frontend facade test proving selected ayah study maps `similaritySummary` without loading detail APIs in `Frontend/quran-dashboard-ui/src/app/features/mushaf/state/mushaf-reader.facade.spec.ts`
- [X] T014 [P] [US1] Add selected ayah section component test for the two new actions and count display in `Frontend/quran-dashboard-ui/src/app/features/mushaf/components/selected-ayah-section/selected-ayah-section.component.spec.ts`

### Implementation for User Story 1

- [X] T015 [US1] Add `SimilaritySummaryDto` and `SimilaritySummary` to `AyahStudyResponse` in `Backend/application/QuranDashboard.Application.Abstractions/Quran/MushafReader/Responses/AyahStudyResponse.cs`
- [X] T016 [US1] Compute `similarAyahCount`, `mutashabihatGroupCount`, and `mutashabihatOccurrenceCount` in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/MushafReader/EfAyahStudyReader.cs`
- [X] T017 [US1] Ensure the selected ayah study handler returns the extended response without changing query inputs in `Backend/application/QuranDashboard.Application/Quran/MushafReader/Queries/GetAyahStudy/GetAyahStudyHandler.cs`
- [X] T018 [US1] Update cache test fixtures for the new `AyahStudyResponse` constructor field in `Backend/tests/QuranDashboard.Tests/Quran/MushafReader/MushafReaderCacheTests.cs`
- [X] T019 [US1] Add `AyahSimilaritySummaryDto` and `similaritySummary` to ayah study frontend DTO/view-model types in `Frontend/quran-dashboard-ui/src/app/features/mushaf/models/mushaf.models.ts`
- [X] T020 [US1] Update selected ayah study API mock data to include `similaritySummary` in `Frontend/quran-dashboard-ui/src/app/features/mushaf/state/mushaf-study-source-catalog.api.mock.ts` and `Frontend/quran-dashboard-ui/src/app/features/mushaf/state/mushaf-reader.facade.spec.ts`
- [X] T021 [US1] Map `similaritySummary` from API data into selected ayah view state in `Frontend/quran-dashboard-ui/src/app/features/mushaf/state/mushaf-reader.facade.ts`
- [X] T022 [US1] Add `similar-ayahs` and `mutashabihat` buttons to the selected ayah tab/action nav in `Frontend/quran-dashboard-ui/src/app/features/mushaf/components/selected-ayah-section/selected-ayah-section.component.html`
- [X] T023 [US1] Add inputs or computed helpers for displaying the two new action counts in `Frontend/quran-dashboard-ui/src/app/features/mushaf/components/selected-ayah-section/selected-ayah-section.component.ts`
- [X] T024 [US1] Update five-action layout styles while preserving RTL and stable panel bounds in `Frontend/quran-dashboard-ui/src/app/features/mushaf/components/selected-ayah-section/selected-ayah-section.component.scss`
- [X] T025 [US1] Run the US1 backend and frontend tests listed in `Backend/tests/QuranDashboard.Tests/Quran/MushafReader/AyahStudySimilaritySummaryTests.cs`, `Backend/tests/QuranDashboard.Tests/Quran/MushafReader/MushafPageReadTests.cs`, `Frontend/quran-dashboard-ui/src/app/features/mushaf/state/mushaf-reader.facade.spec.ts`, and `Frontend/quran-dashboard-ui/src/app/features/mushaf/components/selected-ayah-section/selected-ayah-section.component.spec.ts`

**Checkpoint**: User Story 1 is independently functional. The selected ayah study area shows counts and two new actions, but details remain lazy.

---

## Phase 4: User Story 2 - Review Similar Meaning Ayahs As A Flat List (Priority: P2)

**Goal**: Opening `آيات قريبة في المعنى` lazy-loads a flat, deduplicated list of related ayahs using both incoming and outgoing directed links.

**Independent Test**: Select an ayah with outgoing, incoming, and bidirectional links. Open `آيات قريبة في المعنى` and confirm all related ayahs appear once in a flat list, with canonical ayah text and a clear empty state when no data exists.

### Tests for User Story 2

- [X] T026 [P] [US2] Add backend validation tests for malformed and unknown verse keys in `Backend/tests/QuranDashboard.Tests/Quran/MushafReader/SimilarAyahsValidationTests.cs`
- [X] T027 [P] [US2] Add backend read tests for outgoing, incoming, bidirectional deduplication, empty list, and canonical ayah text in `Backend/tests/QuranDashboard.Tests/Quran/MushafReader/SimilarAyahsReadTests.cs`
- [X] T028 [P] [US2] Add frontend API/facade lazy-loading tests for similar ayahs in `Frontend/quran-dashboard-ui/src/app/features/mushaf/state/mushaf-reader.facade.similar-ayahs.spec.ts`
- [X] T029 [P] [US2] Add flat similar ayahs component rendering tests in `Frontend/quran-dashboard-ui/src/app/features/mushaf/components/similar-ayahs-card/similar-ayahs-card.component.spec.ts`

### Implementation for User Story 2

- [X] T030 [P] [US2] Create `SimilarAyahsResponse`, `SimilarAyahItemDto`, and relationship direction DTO types in `Backend/application/QuranDashboard.Application.Abstractions/Quran/MushafReader/Responses/AyahSimilaritiesResponse.cs`
- [X] T031 [P] [US2] Create `IAyahSimilaritiesReader` abstraction in `Backend/application/QuranDashboard.Application.Abstractions/Quran/MushafReader/IAyahSimilaritiesReader.cs`
- [X] T032 [US2] Create `GetSimilarAyahsQuery` with `VerseKey` in `Backend/application/QuranDashboard.Application/Quran/MushafReader/Queries/GetSimilarAyahs/GetSimilarAyahsQuery.cs`
- [X] T033 [US2] Create `GetSimilarAyahsOutcome` variants for success, invalid verse key, and not found in `Backend/application/QuranDashboard.Application/Quran/MushafReader/Queries/GetSimilarAyahs/GetSimilarAyahsOutcome.cs`
- [X] T034 [US2] Implement `GetSimilarAyahsHandler` with verse-key validation and reader call in `Backend/application/QuranDashboard.Application/Quran/MushafReader/Queries/GetSimilarAyahs/GetSimilarAyahsHandler.cs`
- [X] T035 [US2] Implement outgoing plus incoming similar link reads, bidirectional deduplication, canonical ayah joins, and sorting in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/MushafReader/EfAyahSimilaritiesReader.cs`
- [X] T036 [US2] Implement successful-read caching decorator for similar ayahs in `Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Quran/MushafReader/CachedAyahSimilaritiesReader.cs`
- [X] T037 [US2] Register `GetSimilarAyahsHandler` in `Backend/application/QuranDashboard.Application/DependencyInjection.cs`
- [X] T038 [US2] Register `IAyahSimilaritiesReader`, `EfAyahSimilaritiesReader`, and cache decorator in `Backend/infrastructure/QuranDashboard.Infrastructure/DependencyInjection.cs`
- [X] T039 [US2] Add read-only controller action for `GET /api/mushaf/ayahs/{verseKey}/similar-ayahs` in `Backend/api/QuranDashboard.Api/Controllers/MushafReader/Ayahs/MushafAyahSimilaritiesController.cs`
- [X] T040 [US2] Add `SimilarAyahsDto`, `SimilarAyahItemDto`, and relationship direction frontend types in `Frontend/quran-dashboard-ui/src/app/features/mushaf/models/mushaf.models.ts`
- [X] T041 [US2] Create `MushafSimilarAyahsApi` that calls `/api/mushaf/ayahs/{verseKey}/similar-ayahs` in `Frontend/quran-dashboard-ui/src/app/features/mushaf/data-access/mushaf-similar-ayahs.api.ts`
- [X] T042 [US2] Add similar ayahs cache lookup, in-flight dedupe, and load state handling in `Frontend/quran-dashboard-ui/src/app/features/mushaf/state/mushaf-reader.facade.ts`
- [X] T043 [US2] Create similar ayahs component class with inputs for data/loading/error/empty state in `Frontend/quran-dashboard-ui/src/app/features/mushaf/components/similar-ayahs-card/similar-ayahs-card.component.ts`
- [X] T044 [US2] Create similar ayahs flat-list template and Arabic empty/loading states in `Frontend/quran-dashboard-ui/src/app/features/mushaf/components/similar-ayahs-card/similar-ayahs-card.component.html`
- [X] T045 [US2] Create similar ayahs component styles that preserve calm RTL card layout in `Frontend/quran-dashboard-ui/src/app/features/mushaf/components/similar-ayahs-card/similar-ayahs-card.component.scss`
- [X] T046 [US2] Render `qd-similar-ayahs-card` when `activeTab() === 'similar-ayahs'` in `Frontend/quran-dashboard-ui/src/app/features/mushaf/components/selected-ayah-section/selected-ayah-section.component.html`
- [X] T047 [US2] Add component imports and inputs/outputs needed for the similar ayahs card in `Frontend/quran-dashboard-ui/src/app/features/mushaf/components/selected-ayah-section/selected-ayah-section.component.ts`
- [X] T048 [US2] Run the US2 backend and frontend tests listed in `Backend/tests/QuranDashboard.Tests/Quran/MushafReader/SimilarAyahsValidationTests.cs`, `Backend/tests/QuranDashboard.Tests/Quran/MushafReader/SimilarAyahsReadTests.cs`, `Frontend/quran-dashboard-ui/src/app/features/mushaf/state/mushaf-reader.facade.similar-ayahs.spec.ts`, and `Frontend/quran-dashboard-ui/src/app/features/mushaf/components/similar-ayahs-card/similar-ayahs-card.component.spec.ts`

**Checkpoint**: User Story 2 is independently functional. Similar meaning ayahs load lazily and render as a flat deduplicated list.

---

## Phase 5: User Story 3 - Review Mutashabihat As Phrase Groups (Priority: P2)

**Goal**: Opening `المتشابهات اللفظية للحفظ` lazy-loads grouped phrase/word-span mutashabihat data, preserving group identity and occurrence lists.

**Independent Test**: Select an ayah in multiple mutashabihat groups. Open `المتشابهات اللفظية للحفظ` and confirm each group is separate, each group contains its own occurrences, selected ayah occurrences are marked without color alone, and phrase text is derived from canonical words when present.

### Tests for User Story 3

- [X] T049 [P] [US3] Add backend validation tests for malformed and unknown verse keys in `Backend/tests/QuranDashboard.Tests/Quran/MushafReader/AyahMutashabihatValidationTests.cs`
- [X] T050 [P] [US3] Add backend grouped read tests for empty groups, multiple groups, sibling occurrences, selected occurrences, and phrase text derivation in `Backend/tests/QuranDashboard.Tests/Quran/MushafReader/AyahMutashabihatReadTests.cs`
- [X] T051 [P] [US3] Add frontend API/facade lazy-loading tests for mutashabihat in `Frontend/quran-dashboard-ui/src/app/features/mushaf/state/mushaf-reader.facade.mutashabihat.spec.ts`
- [X] T052 [P] [US3] Add grouped mutashabihat rendering tests in `Frontend/quran-dashboard-ui/src/app/features/mushaf/components/mutashabihat-groups-card/mutashabihat-groups-card.component.spec.ts`

### Implementation for User Story 3

- [X] T053 [P] [US3] Create `AyahMutashabihatResponse`, `MutashabihatGroupDto`, `MutashabihatSelectedOccurrenceDto`, and `MutashabihatOccurrenceDto` in `Backend/application/QuranDashboard.Application.Abstractions/Quran/MushafReader/Responses/AyahMutashabihatResponse.cs`
- [X] T054 [P] [US3] Create `IAyahMutashabihatReader` abstraction in `Backend/application/QuranDashboard.Application.Abstractions/Quran/MushafReader/IAyahMutashabihatReader.cs`
- [X] T055 [US3] Create `GetAyahMutashabihatQuery` with `VerseKey` in `Backend/application/QuranDashboard.Application/Quran/MushafReader/Queries/GetAyahMutashabihat/GetAyahMutashabihatQuery.cs`
- [X] T056 [US3] Create `GetAyahMutashabihatOutcome` variants for success, invalid verse key, and not found in `Backend/application/QuranDashboard.Application/Quran/MushafReader/Queries/GetAyahMutashabihat/GetAyahMutashabihatOutcome.cs`
- [X] T057 [US3] Implement `GetAyahMutashabihatHandler` with verse-key validation and reader call in `Backend/application/QuranDashboard.Application/Quran/MushafReader/Queries/GetAyahMutashabihat/GetAyahMutashabihatHandler.cs`
- [X] T058 [US3] Implement grouped selected-ayah occurrence lookup, group loading, sibling occurrence loading, canonical ayah joins, canonical word-span phrase derivation, and group/occurrence sorting in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/MushafReader/EfAyahMutashabihatReader.cs`
- [X] T059 [US3] Implement successful-read caching decorator for mutashabihat in `Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Quran/MushafReader/CachedAyahMutashabihatReader.cs`
- [X] T060 [US3] Register `GetAyahMutashabihatHandler` in `Backend/application/QuranDashboard.Application/DependencyInjection.cs`
- [X] T061 [US3] Register `IAyahMutashabihatReader`, `EfAyahMutashabihatReader`, and cache decorator in `Backend/infrastructure/QuranDashboard.Infrastructure/DependencyInjection.cs`
- [X] T062 [US3] Add read-only controller action for `GET /api/mushaf/ayahs/{verseKey}/mutashabihat` in `Backend/api/QuranDashboard.Api/Controllers/MushafReader/Ayahs/MushafAyahMutashabihatController.cs`
- [X] T063 [US3] Add `AyahMutashabihatDto`, `MutashabihatGroupDto`, `MutashabihatSelectedOccurrenceDto`, and `MutashabihatOccurrenceDto` frontend types in `Frontend/quran-dashboard-ui/src/app/features/mushaf/models/mushaf.models.ts`
- [X] T064 [US3] Create `MushafAyahMutashabihatApi` that calls `/api/mushaf/ayahs/{verseKey}/mutashabihat` in `Frontend/quran-dashboard-ui/src/app/features/mushaf/data-access/mushaf-ayah-mutashabihat.api.ts`
- [X] T065 [US3] Add mutashabihat cache lookup, in-flight dedupe, and load state handling in `Frontend/quran-dashboard-ui/src/app/features/mushaf/state/mushaf-reader.facade.ts`
- [X] T066 [US3] Create grouped mutashabihat component class with inputs for data/loading/error/empty state in `Frontend/quran-dashboard-ui/src/app/features/mushaf/components/mutashabihat-groups-card/mutashabihat-groups-card.component.ts`
- [X] T067 [US3] Create grouped mutashabihat template with one section per group, selected-occurrence labels, occurrence lists, and Arabic empty/loading states in `Frontend/quran-dashboard-ui/src/app/features/mushaf/components/mutashabihat-groups-card/mutashabihat-groups-card.component.html`
- [X] T068 [US3] Create grouped mutashabihat styles that preserve calm RTL grouping and stable internal scrolling in `Frontend/quran-dashboard-ui/src/app/features/mushaf/components/mutashabihat-groups-card/mutashabihat-groups-card.component.scss`
- [X] T069 [US3] Render `qd-mutashabihat-groups-card` when `activeTab() === 'mutashabihat'` in `Frontend/quran-dashboard-ui/src/app/features/mushaf/components/selected-ayah-section/selected-ayah-section.component.html`
- [X] T070 [US3] Add component imports and inputs/outputs needed for the mutashabihat groups card in `Frontend/quran-dashboard-ui/src/app/features/mushaf/components/selected-ayah-section/selected-ayah-section.component.ts`
- [X] T071 [US3] Run the US3 backend and frontend tests listed in `Backend/tests/QuranDashboard.Tests/Quran/MushafReader/AyahMutashabihatValidationTests.cs`, `Backend/tests/QuranDashboard.Tests/Quran/MushafReader/AyahMutashabihatReadTests.cs`, `Frontend/quran-dashboard-ui/src/app/features/mushaf/state/mushaf-reader.facade.mutashabihat.spec.ts`, and `Frontend/quran-dashboard-ui/src/app/features/mushaf/components/mutashabihat-groups-card/mutashabihat-groups-card.component.spec.ts`

**Checkpoint**: User Story 3 is independently functional. Mutashabihat load lazily and render grouped by phrase/group.

---

## Phase 6: User Story 4 - Reopen A Similarity Study View From The URL (Priority: P3)

**Goal**: Shared/reopened URLs restore the selected page, selected ayah, and active selected-ayah action for `similar-ayahs` and `mutashabihat`.

**Independent Test**: Open each new action, copy the URL, reopen it in a new session, and confirm the reader restores the same selected ayah action and lazy-loads the correct details only for that action.

### Tests for User Story 4

- [X] T072 [P] [US4] Add URL parsing and normalization tests for `ayahTab=similar-ayahs` and `ayahTab=mutashabihat` in `Frontend/quran-dashboard-ui/src/app/features/mushaf/state/mushaf-url-sync.spec.ts`
- [X] T073 [P] [US4] Add session/query-param serialization tests for the widened `ayahTab` values in `Frontend/quran-dashboard-ui/src/app/features/mushaf/state/mushaf-reader-session.spec.ts`
- [X] T074 [P] [US4] Add facade deep-link restoration tests proving only the active similarity detail loads in `Frontend/quran-dashboard-ui/src/app/features/mushaf/state/mushaf-reader.facade.spec.ts`

### Implementation for User Story 4

- [X] T075 [US4] Widen `VALID_AYAH_TABS` and `normalizeAyahTab` behavior for `similar-ayahs` and `mutashabihat` in `Frontend/quran-dashboard-ui/src/app/features/mushaf/state/mushaf-url-sync.ts`
- [X] T076 [US4] Preserve widened `ayahTab` values during query-param serialization in `Frontend/quran-dashboard-ui/src/app/features/mushaf/state/mushaf-reader-session.ts`
- [X] T077 [US4] Trigger similar ayahs lazy load when restored state has `ayahTab=similar-ayahs` and a selected ayah in `Frontend/quran-dashboard-ui/src/app/features/mushaf/state/mushaf-reader.facade.ts`
- [X] T078 [US4] Trigger mutashabihat lazy load when restored state has `ayahTab=mutashabihat` and a selected ayah in `Frontend/quran-dashboard-ui/src/app/features/mushaf/state/mushaf-reader.facade.ts`
- [X] T079 [US4] Run the US4 frontend tests listed in `Frontend/quran-dashboard-ui/src/app/features/mushaf/state/mushaf-url-sync.spec.ts`, `Frontend/quran-dashboard-ui/src/app/features/mushaf/state/mushaf-reader-session.spec.ts`, and `Frontend/quran-dashboard-ui/src/app/features/mushaf/state/mushaf-reader.facade.spec.ts`

**Checkpoint**: User Story 4 is independently functional. URLs restore both new selected-ayah actions.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Validate the full feature, reduce risk, and document the final implementation state.

- [ ] T080 [P] Verify no EF migration files or model snapshot changes were created under `Backend/infrastructure/QuranDashboard.Infrastructure/Migrations/`
- [ ] T081 [P] Verify no importer, source package, or resources changes were added under `Backend/tools/QuranDashboard.DataImporter/` or `resources/`
- [ ] T082 [P] Review API contracts against implementation in `specs/012-mushaf-ayah-similarities/contracts/ayah-study-similarity-summary.api.md`, `specs/012-mushaf-ayah-similarities/contracts/similar-ayahs.api.md`, and `specs/012-mushaf-ayah-similarities/contracts/ayah-mutashabihat.api.md`
- [ ] T083 [P] Review frontend lazy-loading and URL behavior against `specs/012-mushaf-ayah-similarities/contracts/frontend-url-state-and-lazy-loading.md`
- [ ] T084 Run the full backend test suite for Feature 012 using `Backend/tests/QuranDashboard.Tests/`
- [ ] T085 Run the full frontend test suite for Feature 012 using `Frontend/quran-dashboard-ui/src/app/features/mushaf/`
- [ ] T086 Execute the smoke-test flow in `specs/012-mushaf-ayah-similarities/quickstart.md`
- [ ] T087 Validate SC-008 label comprehension with at least a small reviewer sample or documented product review: participants must distinguish `آيات قريبة في المعنى` from `المتشابهات اللفظية للحفظ`; record the result in the implementation completion report
- [ ] T088 Perform clean-code self-check against `.claude/skills/engineering-review/references/clean-code-guard/`
- [ ] T089 Perform test-code self-check against `.claude/skills/test-guard/`
- [ ] T090 Create implementation completion report in `Backend/report/feature-012-mushaf-reader-ayah-similarities/001-implementation-completion-report.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 Setup**: No dependencies; start immediately.
- **Phase 2 Foundational**: Depends on Phase 1 orientation; blocks all story work.
- **Phase 3 US1**: Depends on Phase 2; this is the MVP and should complete first.
- **Phase 4 US2**: Depends on Phase 2 for backend work and on US1 UI action shell for frontend integration.
- **Phase 5 US3**: Depends on Phase 2 for backend work and on US1 UI action shell for frontend integration.
- **Phase 6 US4**: Depends on US1, and benefits from US2/US3 detail loaders for full restoration tests.
- **Phase 7 Polish**: Depends on all desired user stories being complete.

### User Story Dependencies

- **US1 (P1)**: MVP. No dependency on US2, US3, or US4.
- **US2 (P2)**: Backend endpoint can start after Phase 2; frontend rendering depends on US1 tabs/action shell.
- **US3 (P2)**: Backend endpoint can start after Phase 2; frontend rendering depends on US1 tabs/action shell.
- **US4 (P3)**: Depends on the widened tab model from Phase 2 and detail loaders from US2/US3 for complete deep-link restoration.

### Within Each User Story

- Write the story's test tasks first and confirm they fail for missing behavior.
- Add/extend DTOs before readers, handlers, and controllers.
- Implement readers before handlers/controllers when endpoint behavior depends on read data.
- Implement API services before facade integration on the frontend.
- Implement facade state before rendering components consume it.
- Run the story-specific test task before moving to the next story.

---

## Parallel Opportunities

- T002 and T003 can run in parallel because they inspect different architecture docs.
- T004 and T005 can run in parallel because they inspect backend and frontend source separately.
- T006, T007, T008, and T009 can run in parallel if coordinated carefully because they touch different backend/frontend files, but T008 and T010 both touch `mushaf.models.ts` and should not run simultaneously.
- US1 tests T011, T012, T013, and T014 can run in parallel because they create or update different test files.
- US2 backend test tasks T026 and T027 can run in parallel with frontend test tasks T028 and T029.
- US2 backend implementation tasks T030 and T031 can run in parallel because they create different files.
- US3 backend test tasks T049 and T050 can run in parallel with frontend test tasks T051 and T052.
- US3 backend implementation tasks T053 and T054 can run in parallel because they create different files.
- US4 tests T072, T073, and T074 can run in parallel because they touch different test files.
- Polish checks T080, T081, T082, and T083 can run in parallel because they inspect different paths.

---

## Parallel Example: User Story 1

```bash
# Run these test-writing tasks in parallel:
Task T011: Add backend similarity summary tests in Backend/tests/QuranDashboard.Tests/Quran/MushafReader/AyahStudySimilaritySummaryTests.cs
Task T012: Add backend page response regression test in Backend/tests/QuranDashboard.Tests/Quran/MushafReader/MushafPageReadTests.cs
Task T013: Add frontend facade summary test in Frontend/quran-dashboard-ui/src/app/features/mushaf/state/mushaf-reader.facade.spec.ts
Task T014: Add selected ayah section action/count test in Frontend/quran-dashboard-ui/src/app/features/mushaf/components/selected-ayah-section/selected-ayah-section.component.spec.ts
```

## Parallel Example: User Story 2

```bash
# Run backend and frontend test-writing tasks in parallel:
Task T026: Add validation tests in Backend/tests/QuranDashboard.Tests/Quran/MushafReader/SimilarAyahsValidationTests.cs
Task T027: Add read/dedupe tests in Backend/tests/QuranDashboard.Tests/Quran/MushafReader/SimilarAyahsReadTests.cs
Task T028: Add facade tests in Frontend/quran-dashboard-ui/src/app/features/mushaf/state/mushaf-reader.facade.similar-ayahs.spec.ts
Task T029: Add component tests in Frontend/quran-dashboard-ui/src/app/features/mushaf/components/similar-ayahs-card/similar-ayahs-card.component.spec.ts
```

## Parallel Example: User Story 3

```bash
# Run backend and frontend test-writing tasks in parallel:
Task T049: Add validation tests in Backend/tests/QuranDashboard.Tests/Quran/MushafReader/AyahMutashabihatValidationTests.cs
Task T050: Add grouped read tests in Backend/tests/QuranDashboard.Tests/Quran/MushafReader/AyahMutashabihatReadTests.cs
Task T051: Add facade tests in Frontend/quran-dashboard-ui/src/app/features/mushaf/state/mushaf-reader.facade.mutashabihat.spec.ts
Task T052: Add component tests in Frontend/quran-dashboard-ui/src/app/features/mushaf/components/mutashabihat-groups-card/mutashabihat-groups-card.component.spec.ts
```

## Parallel Example: User Story 4

```bash
# Run URL-state test-writing tasks in parallel:
Task T072: Add URL parsing tests in Frontend/quran-dashboard-ui/src/app/features/mushaf/state/mushaf-url-sync.spec.ts
Task T073: Add session serialization tests in Frontend/quran-dashboard-ui/src/app/features/mushaf/state/mushaf-reader-session.spec.ts
Task T074: Add facade restoration tests in Frontend/quran-dashboard-ui/src/app/features/mushaf/state/mushaf-reader.facade.spec.ts
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 setup orientation.
2. Complete Phase 2 foundational constants/types.
3. Complete Phase 3 US1 tasks T011-T025.
4. Stop and validate US1 independently: page response unchanged, selected ayah study has counts, no detail payloads are loaded.
5. Only then continue to US2 and US3 detail payloads.

### Incremental Delivery

1. **US1**: Counts and UI actions only, no details. This proves payload discipline and selected ayah summary behavior.
2. **US2**: Similar meaning ayahs flat lazy list. This adds semantic similarity without touching mutashabihat grouping.
3. **US3**: Mutashabihat grouped lazy details. This adds the more complex grouped phrase behavior.
4. **US4**: URL restoration for both new actions.
5. **Polish**: Full tests, quickstart, data-safety checks, and completion report.

### Single-Agent Strategy For A Cheaper Model

1. Execute tasks strictly in numeric order.
2. Do not combine US2 and US3, because both touch facade/model/selected-ayah UI files and can create merge conflicts.
3. After each checkpoint, run only the story-specific tests named in the checkpoint task before continuing.
4. If a task would require a migration/importer/public-reader feature, stop; that is out of scope.
5. If a file approaches architecture size thresholds, split using the component/service names already listed in this tasks file.

---

## Notes

- No task should create database migrations, importers, resource source packages, public reader features, audio, bookmarks, editing, approval workflows, or graph exploration.
- All Quran ayah text must come from canonical ayah data, and all phrase text must come from canonical word ranges.
- Similar ayahs are flat; mutashabihat are grouped.
- `[P]` marks tasks that can run in parallel only when different agents are available and file paths do not overlap.
- Story labels map directly to the user stories in `specs/012-mushaf-ayah-similarities/spec.md`.
