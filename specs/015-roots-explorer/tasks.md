---
description: "Task list for Quran Roots Explorer (Feature 015)"
---

# Tasks: Quran Roots Explorer

**Input**: Design documents from `specs/015-roots-explorer/`
**Prerequisites**: `plan.md`, `spec.md` (US1–US5), `research.md`, `data-model.md`, `contracts/` (`roots-api.md`, `backend-read-abstractions.md`, `frontend-routing-state.md`), and the fuller `docs/feature-015-roots-explorer/feature-015-roots-explorer-combined-implementation-plan.md`.

**Branch**: `015-roots-explorer` (all 3 repos: `App`, `Backend`, `Frontend/quran-dashboard-ui`).

**Tests**: Included. Per the project's milestone-based testing directive, focused tests are placed as a **checkpoint at the END of each user story** (not test-first/TDD, and not a full suite after every task). A single full suite runs in Polish before merge.

> **Revision note (post-`/speckit-analyze`)**: the persistent `root-details-panel` *shell* was moved into **Foundational (T020)** so User Stories 2–5 are independent siblings (each depends only on the Foundational phase). Test tasks were strengthened for clear-selection (T034) and the no-overview tab strip (T043). The Words-hub entry task (T031) maps to **FR-047**.

---

## Implementer guide (READ FIRST — this is a full-stack feature with a proven sibling to copy)

**Golden reuse templates — copy their structure, change only the root-specific bits:**

- Backend list/detail read feature → Feature 014 "Unique Words":
  - Controller: `Backend/api/QuranDashboard.Api/Controllers/Words/UniqueWordsController.cs`
  - Reader interface: `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/IUniqueWordsReader.cs`
  - Handlers: `Backend/application/QuranDashboard.Application/Quran/Words/Queries/GetUniqueWords*/…`
  - EF reader: `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/EfUniqueWordsReader.cs`
  - Cache decorator + keys: `Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Quran/Words/{CachedUniqueWordsReader,UniqueWordsCacheKeys}.cs`
  - DI: `Backend/infrastructure/QuranDashboard.Infrastructure/DependencyInjection/UniqueWordsDependencyInjection.cs`
  - Messages: `Backend/api/QuranDashboard.Api/Contracts/ApiMessages.cs`
  - Tests: `Backend/tests/QuranDashboard.Tests/Quran/Words/{UniqueWordsTestFixture,UniqueWordsCollection,SqlCommandCountInterceptor}.cs` and `…/TestSupport/Logging/RecordingLoggerProvider.cs`
- Frontend explorer + drilldown → Feature 014 "words" feature:
  - Models/url-sync/api/cache/facades: `Frontend/quran-dashboard-ui/src/app/features/words/{models/unique-words.models.ts, state/unique-words-url-sync.ts, data-access/unique-words.api.ts, state/unique-words-cache.ts, state/unique-words.facade.ts, state/unique-words-drilldown.facade.ts}`
  - Table + components: `…/features/words/components/{unique-words-table, highlighted-ayah, word-count-chip, ayah-matches-list, surah-occurrences-list, missing-surahs-list}/…`
  - Shared pagination (USE THIS, not the old words pagination): `…/src/app/shared/ui/pagination/pagination.component.ts` (`<qd-pagination>`)
  - Shared cache base: `…/src/app/core/caching/api-response-cache.ts`
  - Deep-link helper (USE for word clicks): `buildUniqueWordsDeepLink(kind, { wordId })` in `…/state/unique-words-url-sync.ts`
  - Existing entities (read-only, do NOT modify): `Backend/domain/QuranDashboard.Domain/Quran/Words/Morphology/{QuranRoot,QuranLemma,QuranStem,WordMorphology}.cs`, `…/Words/QuranWord.cs`

**Non-negotiable rules (apply to EVERY task):**

1. **Read-only.** No writes, no importer, no data pipeline, no Quran-text changes. **No migrations. No new indexes.**
2. **Lemmas = co-occurrence**: `DISTINCT quran_word_morphology.lemma_id WHERE root_id = X` (equals `quran_roots.distinct_lemmas_count`). NEVER `COUNT(quran_lemmas WHERE root_id)`. The table column count and the lemmas-tab item count MUST be equal.
3. **occurrences = `quran_roots.words_count`**. Stems = `DISTINCT stem_id` via morphology.
4. **Never display backend IDs** in the UI (root/word/lemma/stem ids). Use UI row numbers. IDs go only in the URL (`root=`) and in DTOs for deep-links.
5. **Highlight by `quran_words.id`** (`matchedQuranWordIds`) — never string replacement; never mutate Quran text; highlight not by color alone.
6. **Reuse the shared `<qd-pagination>`** and the `ApiResponseCache`. Do not reintroduce `unique-words-list-pagination`.
7. **Detail experience is a persistent side panel, not a modal** (drawer only on narrow screens). No "نظرة عامة" tab.
8. Backend cache uses a new `roots:` namespace over the already-registered shared `IMemoryCache`; **no global cache reconfiguration**.
9. Logs carry IDs/counts/`hasSearch`/elapsed only — **never** Quran/root/word/raw-search text.
10. Frontend tests MUST keep the worker cap: `VITEST_MAX_FORKS=2 npm test …` (the runner OOMs the machine otherwise). Guard `matchMedia`/`ResizeObserver` absence (jsdom) and default desktop.

**Exact Arabic labels (use verbatim):** columns `الجذر · المواضع · الآيات · السور · كلمات بدون تشكيل · كلمات بالتشكيل · الصيغ المعجمية · الأصول الصرفية`; tabs `الكلمات (بدون تشكيل / بالتشكيل) · الآيات · السور (ورد فيها / لم يذكر فيها) · الصيغ المعجمية · الأصول الصرفية`; empty-selection `اختر جذرًا لعرض تفاصيله`. Column 6 is `كلمات بالتشكيل` — never `الصيغ بالتشكيل`.

---

## Phase 1: Setup

**Purpose**: Confirm a green starting point across all three repos.

- [x] T001 Confirm all three repos are on branch `015-roots-explorer`, then confirm a green baseline: `dotnet build Backend/QuranDashboard.sln` succeeds and `Frontend/quran-dashboard-ui` installs/compiles. Record the baseline so later failures are attributable.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Shared contracts, types, wiring, the persistent panel shell, and the test harness that EVERY user story needs. These compile but contain no per-story behavior yet (readers throw "not implemented"; controller actions and panel views are added per story).

**⚠️ No user-story work may begin until this phase is complete.**

### Backend foundations

- [x] T002 [P] Create `RootSort` enum + `RootSortKeys` constants (`mushaf-order`, `occurrences`, `alpha`) + `RootSortParser` in `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/Roots/`. Model on `Quran/Words/UniqueWordSort.cs`.
- [x] T003 [P] Create `RootWordKind` enum + `RootWordKindKeys` (`simple`, `tashkeel`) + parser in the same folder. Model on `Quran/Words/UniqueWordKind.cs`.
- [x] T004 [P] Create all read DTOs in `…/Quran/Words/Roots/Responses/` exactly per `contracts/roots-api.md`: `RootListItemDto`, `RootSummaryDto`, `RootWordItemDto`, `RootAyahMatchDto` (reuse F014 `AyahWordForHighlightDto`), `RootSurahsResponse`+`RootSurahItemDto`, `RootMissingSurahsResponse`+`MissingSurahItemDto`, `RootLemmasResponse`+`RootLemmaItemDto`, `RootStemsResponse`+`RootStemItemDto`. Reuse the existing `PagedResult<T>`.
- [x] T005 Create `IRootsReader` interface in `…/Quran/Words/Roots/IRootsReader.cs` with all 8 methods per `contracts/backend-read-abstractions.md` (depends on T002–T004).
- [x] T006 [P] Add Arabic `ApiMessages` constants for roots (list/summary/words/ayahs/surahs/missing/lemmas/stems loaded; invalid kind/sort/paging/id; root not found) in `Backend/api/QuranDashboard.Api/Contracts/ApiMessages.cs`. Model on the existing UniqueWords messages.
- [x] T007 [P] Create `RootsCacheKeys` in `Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Quran/Words/Roots/RootsCacheKeys.cs` with the `roots:` keys from `contracts/roots-api.md`. Model on `UniqueWordsCacheKeys.cs`.
- [x] T008 Create `EfRootsReader` skeleton implementing `IRootsReader` (every method `throw new NotImplementedException()` for now), `AsNoTracking`, injecting `QuranDashboardDbContext`, in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/Roots/EfRootsReader.cs`. Model on `EfUniqueWordsReader.cs` (depends on T005).
- [x] T009 Create `CachedRootsReader` decorator (wraps inner `IRootsReader` + `IMemoryCache`, delegates all methods; per-method caching is filled in by story tasks) in `…/Caching/Quran/Words/Roots/CachedRootsReader.cs`. Model on `CachedUniqueWordsReader.cs` (depends on T005, T007).
- [x] T010 Create `RootsDependencyInjection.AddRoots()` registering `EfRootsReader` then wrapping it as `IRootsReader` via `CachedRootsReader`, and call it from the infrastructure composition root, in `Backend/infrastructure/QuranDashboard.Infrastructure/DependencyInjection/RootsDependencyInjection.cs`. Model on `UniqueWordsDependencyInjection.cs` (depends on T008, T009).
- [x] T011 Create `RootsController` skeleton at route `api/words/roots` (constructor injects the 8 handlers; actions are added per story) in `Backend/api/QuranDashboard.Api/Controllers/Words/RootsController.cs`. Model on `UniqueWordsController.cs` (depends on T006).

### Backend test harness (folder: `Backend/tests/QuranDashboard.Tests/Quran/WordsRoots/`)

- [x] T012 Create `RootsExplorerTestFixture` (Testcontainers `postgres:16-alpine`, `EnsureCreated`, loads embedded `roots-explorer-seed.sql`, real-DB env escape hatch) + `RootsExplorerCollection`; reuse `SqlCommandCountInterceptor` and `…/TestSupport/Logging/RecordingLoggerProvider.cs`. Model on `UniqueWordsTestFixture.cs` + `UniqueWordsCollection.cs`.
- [x] T013 Author the embedded `roots-explorer-seed.sql` representative slice in `Backend/tests/QuranDashboard.Tests/Quran/WordsRoots/` (committed resource, NOT the full/local DB). It MUST include ~5 roots covering: (a) a high-frequency root (many ayahs + many surahs), (b) a root where a lemma co-occurs under more than one root (so co-occurrence ≠ `quran_lemmas.root_id` ownership), (c) a root present in nearly all surahs (missing-surahs edge), (d) roots with several distinct stems — plus their `quran_word_morphology` rows (root_id/lemma_id/stem_id), `quran_words` (with `unique_simple_word_id`/`unique_tashkeel_word_id`), `quran_ayahs`, `quran_surahs`, `quran_lemmas`, `quran_stems`. Use canonical Uthmani text verbatim; source-safe (depends on T012).

### Frontend foundations (folder: `Frontend/quran-dashboard-ui/src/app/features/words/`)

- [x] T014 [P] Create `models/roots.models.ts` — DTO interfaces (mirror `contracts/roots-api.md`), view models, `LoadStatus`, list + panel state interfaces, query-key constants (`search`, `sort`, `page`, `root`, `view`, `wordView`, `surahView`, `detailPage`), defaults (`sort=mushaf-order`, default `view=ayahs`, `wordView=simple`, `surahView=mentioned`, fixed page sizes), and type guards. Model on `models/unique-words.models.ts`.
- [x] T015 [P] Create `state/roots-url-sync.ts` — `parseRootsQueryParams` / `buildRootsQueryParams` per `contracts/frontend-routing-state.md` (sub-views valid only under their parent view; `detailPage` only for `ayahs`/`words`; clearing the selection clears `root`/`view`/`wordView`/`surahView`/`detailPage` while preserving `search`/`sort`/`page`). Model on `state/unique-words-url-sync.ts` (depends on T014).
- [x] T016 [P] Create `data-access/roots.api.ts` — all 8 `GET` methods returning `Observable<ApiResponse<T>>`, using `HttpParams` and `encodeURIComponent` on segments. Model on `data-access/unique-words.api.ts` (depends on T014).
- [x] T017 [P] Create `state/roots-cache.ts` — `RootsCache extends ApiResponseCache` + a `RootsCacheKeys` object mirroring the backend `roots:` keys. Model on `state/unique-words-cache.ts` (depends on T014).
- [x] T018 Register the route: add `WORDS_ROOTS_SEGMENT = 'roots'` + `rootsRoutePath()` to `src/app/core/navigation/route-paths.ts`; add a lazy `WORDS_ROOTS_ROUTE` to `src/app/features/words/words.routes.ts`; create an empty `pages/roots-explorer-page/` shell that renders the split-screen layout placeholders (depends on T014).
- [x] T019 Create `state/roots-explorer.facade.ts` skeleton (signals for list state + selection; method stubs) and `state/roots-detail.facade.ts` skeleton (signals for panel state per view; method stubs). Model on `state/unique-words.facade.ts` + `state/unique-words-drilldown.facade.ts` — but the detail facade is a **persistent panel** (no `isOpen`/modal-close; selection drives visibility) (depends on T014, T016, T017).
- [x] T020 Create the persistent `components/root-details-panel/` **shell only** (no view content, no data calls): its own scroll container (`overflow-y:auto`, constrained height); a `role="tablist"` strip containing **exactly** the 5 tabs `الكلمات · الآيات · السور · الصيغ المعجمية · الأصول الصرفية` (and NO "نظرة عامة" tab); drawer scaffolding for narrow screens; and the empty-selection state (`اختر جذرًا لعرض تفاصيله`). Story phases plug their view content into this shell. Model the tab-strip a11y on `FRONTEND_STRUCTURE.md` tab rules (depends on T019).
- [x] T021 [P] Create `state/roots-url-sync.spec.ts` — parse/build round-trip for all params including sub-views, the clear-selection rule, and invalid-value handling (depends on T015).

**Checkpoint CP-0**: `dotnet build` is green; frontend compiles; `/dashboard/words/roots` resolves to the empty shell with the 5-tab panel showing the empty-selection state; T021 passes.

---

## Phase 3: User Story 1 — Browse the roots table with summary numbers (Priority: P1) 🎯 MVP

**Goal**: A searchable, sortable, paginated roots table showing the eight summary counts per root, with no per-root detail loading.

**Independent Test**: Open `/dashboard/words/roots`; the table lists roots with all 8 counts; search/sort/page work and survive refresh/back-forward; clicking a count sets the URL per the mapping; no detail API calls fire on table render.

### Backend — list + summary

- [x] T022 [US1] Implement `EfRootsReader.GetRootsPageAsync` in `…/Reads/Quran/Words/Roots/EfRootsReader.cs`: ONE grouped aggregation over `quran_word_morphology` (`root_id IS NOT NULL`) joined to `quran_words` producing all 8 counts for every root (occurrences from `quran_roots.words_count`; ayahs/surahs/simple/tashkeel/stems via `DISTINCT`; **lemmas via `DISTINCT lemma_id`**), then apply search (Arabic-normalized contains on root text) → sort → page in memory. Reuse F014's Arabic fold for search (depends on T008).
- [x] T023 [US1] Implement `EfRootsReader.GetRootSummaryAsync` (single-root counts; return `null` if the id does not exist) in the same file (depends on T008).
- [x] T024 [US1] Implement caching in `CachedRootsReader`: cache the whole summary under `roots:summary:all` and derive search/sort/page from it (no per-search key); cache summary under `roots:{id}:summary` (depends on T022, T023, T009).
- [x] T025 [US1] Create `GetRootsPage` query + handler + outcome in `Backend/application/QuranDashboard.Application/Quran/Words/Roots/Queries/GetRootsPage/`: validate sort + paging (→ invalid outcomes), call reader, emit structured log (`feature="Roots"`, `operation`, `sort`, `pageNumber`, `pageSize`, `hasSearch`, `totalCount`, `itemCount`, `cacheResult`, `elapsedMs`). Model on `GetUniqueWordsPageHandler.cs` (depends on T022).
- [x] T026 [US1] Create `GetRootSummary` query + handler + outcome in `…/Queries/GetRootSummary/`: validate id positive, map `null` → not-found outcome, log (depends on T023).
- [x] T027 [US1] Wire `RootsController`: `GET /api/words/roots` (→ T025) and `GET /api/words/roots/{id}` (→ T026); map outcomes to `200/400/404` with the Arabic `ApiMessages` (depends on T011, T025, T026).

### Frontend — table + list state

- [x] T028 [US1] Implement list logic in `state/roots-explorer.facade.ts`: load list via `roots.api.ts` + `RootsCache`, map `ApiResponse` → page-ready state (loading/empty/no-results/error), expose search/sort/page actions, a `selectRoot` that sets `root` + default `view=ayahs`, and a `clearSelection` that clears `root`/`view`/sub-views/`detailPage` while preserving `search`/`sort`/`page`; coordinate URL via `roots-url-sync.ts` (depends on T019, T016).
- [x] T029 [US1] Create `components/roots-table/` — a `role="table"` div-grid (rows/cells with ARIA), the 8 columns with the exact Arabic headers, UI row numbers (page-relative), each count cell a `<qd-word-count-chip>` button that emits its target view, row-select output, CDK virtual scroll with the `ResizeObserver`-absent fallback. Model on `components/unique-words-table/` (depends on T014).
- [x] T030 [US1] Compose `roots-explorer-page`: add a roots search bar + sort control + the shared `<qd-pagination>`, render `roots-table`, and wire everything to `roots-explorer.facade`; render loading / empty / no-results states using shared `qd-*` state primitives (depends on T028, T029).
- [x] T031 [US1] Activate the `الجذور` card in the Words hub to link to `/dashboard/words/roots` in `pages/words-hub-page/words-hub-page.component.*` (satisfies **FR-047**) (depends on T018).
- [x] T032 [US1] Wire count-cell clicks and row-select in `roots-explorer-page` to update the URL (`root`, `view`, and sub-views) per the count-click mapping in `contracts/frontend-routing-state.md` (depends on T028, T029).

### Tests for User Story 1 (checkpoint CP-1)

- [x] T033 [P] [US1] Backend tests in `Backend/tests/QuranDashboard.Tests/Quran/WordsRoots/`: list returns all 8 counts for seeded roots; `occurrences == words_count`; **lemmas co-occurrence** (assert on the divergent seeded root that the count uses co-occurrence, not ownership); search + each sort + paging; cache-once (via `SqlCommandCountInterceptor`: a second identical read issues no new DB commands); invalid sort/paging → 400; logging emits required fields and contains no Quran/root/search text (`RecordingLoggerProvider`) (depends on T025, T027, T013).
- [x] T034 [P] [US1] Frontend tests for `components/roots-table` + page: renders 8 counts and UI row numbers (no ids); each count-click produces the correct `view`/sub-view URL; row-select defaults to `view=ayahs`; **clearing the selection preserves `search`/`sort`/`page` and clears only `root`/`view`/sub-views/`detailPage`**; list URL state restores on reload; **assert the API service is NOT called for any detail endpoint on table render** (depends on T029, T032).

**Checkpoint**: US1 is a fully functional, independently testable MVP.

---

## Phase 4: User Story 2 — Inspect verses with the root's words highlighted (Priority: P2)

**Goal**: Selecting a root (or its المواضع/الآيات count) shows, in the persistent panel's الآيات tab, a paginated list of verses with the root's words highlighted by word id.

**Independent Test**: Click a root's المواضع count → panel shows الآيات with paginated, highlighted verses; only the root's words are highlighted; panel scrolls independently; deep link restores root+view+detailPage.

### Backend — ayah matches

- [ ] T035 [US2] Implement `EfRootsReader.GetRootAyahMatchesAsync`: page the distinct matched ayah ids, batch-load the page's ayah words (NO per-ayah N+1), build `MatchedQuranWordIds` from the root's `quran_words.id` set, reuse `AyahWordForHighlightDto`. Model on `EfUniqueWordsReader.GetAyahMatchesAsync` (depends on T008).
- [ ] T036 [US2] Add caching `roots:{id}:ayahs:p{page}:s{size}` in `CachedRootsReader` (depends on T035).
- [ ] T037 [US2] Create `GetRootAyahs` query + handler + outcome in `…/Queries/GetRootAyahs/`: validate id + paging, map `null` → not-found, log (`view="ayahs"`, page/pageSize/totalCount/elapsed) (depends on T035).
- [ ] T038 [US2] Wire `RootsController` `GET /api/words/roots/{id}/ayahs` → T037; map 200/400/404 (depends on T037).

### Frontend — ayahs view (plugs into the foundational panel shell)

- [ ] T039 [US2] Implement ayahs loading in `state/roots-detail.facade.ts`: lazy-load on activation, cache via `RootsCache`, restore-from-URL, and controlled not-found/error handling. Model on the F014 drilldown facade's ayah path (depends on T019, T016, T017).
- [ ] T040 [US2] Render the الآيات view inside the `root-details-panel` shell (T020): **reuse** `highlighted-ayah` + `ayah-matches-list`, wire the shared `<qd-pagination>` to `detailPage`, and render loading/empty/error states (depends on T020, T039).
- [ ] T041 [US2] Integrate `root-details-panel` into the `roots-explorer-page` split layout; wire selection + URL (`root`, `view=ayahs`, `detailPage`) restoration end-to-end (depends on T020, T028).

### Tests for User Story 2 (checkpoint CP-2)

- [ ] T042 [P] [US2] Backend tests: ayah matches return exact `MatchedQuranWordIds` for a seeded root; **bounded query count (no N+1)** via `SqlCommandCountInterceptor`; pagination correct; unknown root → 404 (depends on T037, T013).
- [ ] T043 [P] [US2] Frontend tests: the panel tab strip renders **exactly the 5 named tabs and no "نظرة عامة" tab** (covers FR-005); `highlighted-ayah` marks only matched ids; panel opens and restores from URL; ayahs load only when the tab is activated; panel scroll container is independent (depends on T040, T041).

**Checkpoint**: US1 + US2 both work independently.

---

## Phase 5: User Story 3 — Explore a root's words and open the existing word details (Priority: P3)

**Goal**: الكلمات tab with بدون تشكيل / بالتشكيل sub-views; each word shows display text + per-root occurrence count and deep-links into the existing Unique Words detail flow.

**Independent Test**: Click كلمات بدون تشكيل → الكلمات/بدون تشكيل lists the root's simple words (paginated); click a word → existing Unique Words simple detail opens; same for بالتشكيل → tashkeel.

### Backend — root words

- [ ] T044 [US3] Implement `EfRootsReader.GetRootWordsAsync(kind)`: distinct `unique_simple_word_id` / `unique_tashkeel_word_id` for the root with in-context occurrence count + display text, ordered by first occurrence, server-paged (depends on T008).
- [ ] T045 [US3] Add caching `roots:{id}:words:{kind}:p{page}:s{size}` in `CachedRootsReader` (depends on T044).
- [ ] T046 [US3] Create `GetRootWords` query + handler + outcome in `…/Queries/GetRootWords/`: validate kind + id + paging, not-found, log (`subView=kind`) (depends on T044).
- [ ] T047 [US3] Wire `RootsController` `GET /api/words/roots/{id}/words/{wordKind}` → T046 (depends on T046).

### Frontend — words list + deep links

- [ ] T048 [US3] Create `components/root-words-list/` — paginated word rows showing display text + per-root count, each row a button that deep-links via `buildUniqueWordsDeepLink(kind, { wordId })`; supports the simple and tashkeel sub-views; renders inside the panel shell (depends on T020).
- [ ] T049 [US3] Add words loading to `state/roots-detail.facade.ts` (both sub-views, lazy, cached, `detailPage`); render the nested sub-view tablist and wire `<qd-pagination>` (depends on T039, T048).
- [ ] T050 [US3] Wire URL (`view=words`, `wordView`, `detailPage`) and the count-cell mapping for كلمات بدون تشكيل / كلمات بالتشكيل (depends on T049, T032).

### Tests for User Story 3 (checkpoint CP-3)

- [ ] T051 [P] [US3] Backend tests: word items carry the correct unique word ids and in-context occurrence counts for both kinds; pagination correct (depends on T046, T013).
- [ ] T052 [P] [US3] Frontend tests: a word click builds the correct deep link (`kind` + `wordId`); sub-view URL routing; pagination (depends on T048, T050).

**Checkpoint**: US1–US3 work independently.

---

## Phase 6: User Story 4 — See which surahs contain (or omit) a root (Priority: P3)

**Goal**: السور tab with ورد فيها (with per-surah counts) and لم يذكر فيها sub-views; both loaded whole.

**Independent Test**: Click السور → ورد فيها lists surahs with counts; toggle to لم يذكر فيها; the two counts sum to 114; a root in every surah shows an empty missing list.

### Backend — surahs

- [ ] T053 [US4] Implement `EfRootsReader.GetRootMentionedSurahsAsync` (distinct surahs + per-surah occurrence counts + Arabic names, whole) and `GetRootMissingSurahsAsync` (114 − mentioned, whole) in `EfRootsReader.cs`. Model on F014 surahs/missing reads (depends on T008).
- [ ] T054 [US4] Add caching `roots:{id}:surahs` and `roots:{id}:missing` in `CachedRootsReader` (depends on T053).
- [ ] T055 [US4] Create `GetRootMentionedSurahs` and `GetRootMissingSurahs` queries + handlers + outcomes in `…/Queries/GetRootMentionedSurahs/` and `…/Queries/GetRootMissingSurahs/`: validate id, not-found, log (depends on T053).
- [ ] T056 [US4] Wire `RootsController` `GET /api/words/roots/{id}/surahs` and `GET /api/words/roots/{id}/missing-surahs` (depends on T055).

### Frontend — surahs view

- [ ] T057 [US4] In the السور view, **reuse** `surah-occurrences-list` (ورد فيها) and `missing-surahs-list` (لم يذكر فيها) with a nested sub-view tablist; whole load (no pagination); renders inside the panel shell (depends on T020).
- [ ] T058 [US4] Add surahs loading to `state/roots-detail.facade.ts` (mentioned/missing, lazy, cached, reuse already-loaded); wire URL (`view=surahs`, `surahView`) and the السور → mentioned count-cell mapping (depends on T039, T057, T032).

### Tests for User Story 4 (checkpoint CP-4)

- [ ] T059 [P] [US4] Backend tests: mentioned + missing counts sum to 114; per-surah counts correct; the seeded near-all-surahs root yields an empty/near-empty missing list (depends on T055, T013).
- [ ] T060 [P] [US4] Frontend tests: surah sub-view URL routing; whole load; empty missing-surahs state renders cleanly (depends on T057, T058).

**Checkpoint**: US1–US4 work independently.

---

## Phase 7: User Story 5 — View a root's lemmas and stems (display only) (Priority: P4)

**Goal**: الصيغ المعجمية and الأصول الصرفية tabs list the root's lemmas (co-occurrence) and stems with per-root counts, as **static, non-interactive** items (ids retained for future linking).

**Independent Test**: Click الصيغ المعجمية → lemmas list with counts; the list item count equals the table الصيغ المعجمية column for that root; items are not clickable; same for الأصول الصرفية.

### Backend — lemmas + stems

- [ ] T061 [US5] Implement `EfRootsReader.GetRootLemmasAsync` (**`DISTINCT lemma_id` co-occurrence** + lemma text + in-root count, whole) and `GetRootStemsAsync` (`DISTINCT stem_id` via morphology + stem text + in-root count, whole) in `EfRootsReader.cs` (depends on T008).
- [ ] T062 [US5] Add caching `roots:{id}:lemmas` and `roots:{id}:stems` in `CachedRootsReader` (depends on T061).
- [ ] T063 [US5] Create `GetRootLemmas` and `GetRootStems` queries + handlers + outcomes in `…/Queries/GetRootLemmas/` and `…/Queries/GetRootStems/`: validate id, not-found, log (depends on T061).
- [ ] T064 [US5] Wire `RootsController` `GET /api/words/roots/{id}/lemmas` and `GET /api/words/roots/{id}/stems` (depends on T063).

### Frontend — lemmas/stems views

- [ ] T065 [US5] Create `components/root-lemmas-list/` and `components/root-stems-list/` — **static, non-interactive** list items (text + per-root count); render inside the panel shell. Keep the id in the model for future linking but DO NOT render buttons/links (no fake interactive elements) (depends on T020).
- [ ] T066 [US5] Add lemmas/stems loading to `state/roots-detail.facade.ts` (lazy, whole, cached); wire URL (`view=lemmas` / `view=stems`) and the count-cell mappings (depends on T039, T065, T032).

### Tests for User Story 5 (checkpoint CP-5)

- [ ] T067 [P] [US5] Backend tests: the lemmas count from `GetRootLemmasAsync` equals `RootListItemDto.LemmasCount` for the same root (assert on the divergent seeded root — locks co-occurrence == column, covering SC-003); stems aggregation correct; both lists bounded/whole (depends on T063, T013).
- [ ] T068 [P] [US5] Frontend tests: lemmas/stems render as non-interactive items (no buttons/links); counts shown (depends on T065, T066).

**Checkpoint**: All five user stories are independently functional.

---

## Phase 8: Polish & Cross-Cutting Concerns (checkpoint CP-6)

**Purpose**: Hardening that spans all stories. Run after the stories you intend to ship are complete.

- [ ] T069 [P] Frontend: drawer behavior polish in `components/root-details-panel/` (narrow screens → dismissible drawer built on the foundational shell scaffold: focus-trap, `Esc` to close, focus returns to the originating control) and verify RTL via CSS logical properties.
- [ ] T070 [P] Frontend: accessibility pass across `roots-table` + `root-details-panel` — `role="tablist"/tab/tabpanel`, `aria-selected`, roving `tabindex`, RTL-aware arrow keys; selected row `aria-current`; panel load status via `role="status" aria-live="polite"`; confirm count cells + word links are keyboard-operable and lemmas/stems are non-interactive.
- [ ] T071 [P] Backend: logging completeness + redaction audit across all 8 handlers using `RecordingLoggerProvider` (required fields present; NO Quran/root/word/raw-search text).
- [ ] T072 [P] Backend: cache-hit verification across all detail endpoints via `SqlCommandCountInterceptor` (repeat reads issue no new DB commands); confirm no global cache reconfiguration was introduced.
- [ ] T073 [P] Frontend: empty / error / not-found / no-results state matrix across all tabs; invalid `root` in the URL → controlled not-found while the table stays usable.
- [ ] T074 Run `quickstart.md` validation: backend `dotnet test --filter FullyQualifiedName~Quran.WordsRoots`; frontend `VITEST_MAX_FORKS=2 npm test -- --run src/app/features/words`; confirm the Definition-of-Done checklist in `quickstart.md`.
- [ ] T075 [P] Clean-code + `test-guard` self-check on all changed files; confirm file-size thresholds (`FRONTEND_STRUCTURE.md`); confirm no backend IDs are rendered anywhere in the UI.

---

## Dependencies & Execution Order

### Phase dependencies

- **Setup (Phase 1)** → no dependencies.
- **Foundational (Phase 2)** → depends on Setup; **blocks all user stories**. Now includes the persistent panel shell (T020). CP-0 gate.
- **User stories (Phases 3–7)** → each depends ONLY on the Foundational phase (which already provides the panel shell, facades, models, api, cache, route). US1 is the MVP. **US2, US3, US4, US5 are independent siblings** — none depends on another; they can be built in any order or in parallel.
- **Polish (Phase 8)** → depends on the user stories you intend to ship.

### Shared-file sequencing (NOT parallel with each other — same file)

- `EfRootsReader.cs`: T008 → T022, T023, T035, T044, T053, T061.
- `CachedRootsReader.cs`: T009 → T024, T036, T045, T054, T062.
- `RootsController.cs`: T011 → T027, T038, T047, T056, T064.
- `state/roots-detail.facade.ts`: T019 → T039, T040, T049, T058, T066.

Edits to each of these files must be sequenced; everything else across stories is independent.

### Within each user story

- Backend reader method → cache wiring → handler → controller action.
- Frontend view component → facade wiring → URL wiring.
- Story-level focused tests run as the checkpoint at the end of the story.

---

## Parallel opportunities

- **Foundational**: T002, T003, T004 run in parallel; T006, T007 are independent; T014 then T015/T016/T017 (all `[P]`, depend on T014) run in parallel; T021 (`[P]`) after T015.
- **Within a story**: the two `[P]` test tasks run together; the backend track (reader→cache→handler→controller) and the frontend track (component→facade→URL) can run in parallel once Foundational is complete.
- **Across stories**: after Foundational (including the panel shell T020), US2 / US3 / US4 / US5 can be built fully in parallel by different implementers — coordinate only the shared-file edits listed above.

### Parallel example — User Story 1 tests

```text
Task: "T033 [US1] Backend list/counts/lemma-co-occurrence/cache/logging tests"
Task: "T034 [US1] Frontend table render + count-click mapping + clear-selection + no-detail-calls tests"
```

---

## Implementation Strategy

### MVP first

1. Phase 1 Setup → 2. Phase 2 Foundational (CP-0) → 3. Phase 3 US1 (CP-1) → **STOP & validate the table MVP** → demo.

### Incremental delivery

US1 (table) → US2 (verses+highlight) → US3 (words+deep-links) → US4 (surahs) → US5 (lemmas/stems). Each adds value without breaking the previous; run that story's checkpoint tests before moving on. Run T074 (full suite) before opening the PR.

---

## Notes

- `[P]` = different files, no incomplete dependencies. `[USx]` maps a task to its spec user story.
- Commit per logical group; commit children repos (Backend, Frontend) before the workspace pointer (use the project commit workflow). Do not commit unless asked.
- Stop at any checkpoint to validate a story independently.
- Re-read the **Implementer guide** rules before each task; the most common mistakes here are: using ownership lemmas instead of co-occurrence, rendering ids, string-based highlighting, reintroducing the old pagination, building a modal instead of a panel, and dropping the `VITEST_MAX_FORKS` cap.
