---
description: "Task list for Feature 011 — Mushaf Reader Study Context"
---

# Tasks: Mushaf Reader Study Context

**Input**: Design documents in `specs/011-mushaf-reader-study-context/`
**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/`, `quickstart.md` (all present)

**Tests**: INCLUDED. `plan.md` enumerated specific backend/frontend test files and the workspace mandates the `test-guard` self-check, so each story has test tasks. The Angular project ships **without** a test runner, so configuring one is a Foundational task (**T013**) that blocks all frontend test tasks. For this read-only feature, backend integration tests assert against a seeded Postgres fixture (a representative content slice — see T009); they verify real reads rather than "failing first".

**Organization**: Tasks are grouped by user story (US1–US5 from `spec.md`) so each story is an independently testable increment.

## How to read a task (READ THIS FIRST — for the implementer)

`- [ ] T0XX [P] [USx] <action> in <exact/file/path>`

- **`- [ ]`** = checkbox. **`T0XX`** = do them in number order unless a dependency note says otherwise.
- **`[P]`** = may run in parallel with other `[P]` tasks in the same phase (different files, no shared dependency).
- **`[USx]`** = which user story it belongs to (Setup/Foundational/Polish have no story label).
- Every task names the **exact file(s)** to create or edit.

**Hard rules that apply to EVERY task (do not violate):**
1. **Read-only database.** No migrations, no `DbContext.SaveChanges`, no writes, no importers, no schema edits. Only EF read queries.
2. **Mushaf text is always `quran_words.text_uthmani`.** Never rebuild Mushaf/whole-word text from morphology segments.
3. **Never invent Quranic text.** Missing data → a controlled empty/error state, never fabricated content.
4. **Backend responses use the existing `ApiResponse<T>`** (`Backend/api/QuranDashboard.Api/Contracts/ApiResponse.cs`). User-facing messages are **Arabic**; property/identifier names are **English**.
5. **No `bypassSecurityTrustHtml`.** Render tafsir/i3rab HTML through Angular's built-in sanitizer only.
6. **HTTPS only.** Frontend data calls target `https://localhost:5015` only.
7. **Thin controllers / smart-shell stays thin.** No EF in controllers; no API orchestration or big logic in the page component (use facade/store + data-access). Respect file-size thresholds in `BACKEND_STRUCTURE.md` / `FRONTEND_STRUCTURE.md`.
8. Authoritative field shapes are in `data-model.md` (§B backend DTOs, §C frontend models + **v1 URL enum scope**); endpoint behavior is in `contracts/`.
9. **v1 URL enum scope (locked):** `panel` ∈ {`ayah`,`word`,`none`}; `ayahTab` ∈ {`tafsir`,`translation`,`full-i3rab`}; `wordTab` ∈ {`morphology`,`segments`,`i3rab`,`identity`}. Do NOT implement `panel=sources` or `ayahTab=links` (out of scope; deferred).

## Path conventions (this repo)

- Backend (.NET, Clean Architecture): `Backend/api/...`, `Backend/application/...`, `Backend/application/QuranDashboard.Application.Abstractions/...`, `Backend/infrastructure/...`, `Backend/tests/...`
- Frontend (Angular 20, standalone): `Frontend/quran-dashboard-ui/src/...`

---

## Phase 1: Setup (shared scaffolding)

**Purpose**: Configuration and empty folders/files that later phases fill in. No behavior yet.

- [X] T001 [P] Add the `MushafReader` config section (default source keys) to both `Backend/api/QuranDashboard.Api/appsettings.json` and `Backend/api/QuranDashboard.Api/appsettings.Development.json`: `"MushafReader": { "DefaultTafsirSourceKey": "ar-muyassar", "DefaultTranslationSourceKey": "en-sahih-international", "DefaultFullI3rabSourceKey": "muyassar" }` (see `contracts/local-https-and-frontend-integration.md`).
- [X] T002 [P] Create the frontend feature folder `Frontend/quran-dashboard-ui/src/app/features/mushaf/` and the models file `Frontend/quran-dashboard-ui/src/app/features/mushaf/models/mushaf.models.ts` containing TypeScript interfaces for `MushafPageDto`, `AyahStudyDto`, `WordAnalysisDto` (mirroring `data-model.md` §B) plus `MushafPageViewModel`, `AyahStudyViewModel`, `WordAnalysisViewModel`, `MushafReaderState`, and a `MUSHAF_URL_KEYS` constant (`page,ayah,word,segment,panel,ayahTab,wordTab,tafsirSource,translationSource,fullI3rabSource`) with the **v1 enum value types** from `data-model.md` §C (`panel: 'ayah'|'word'|'none'`, `ayahTab: 'tafsir'|'translation'|'full-i3rab'`, `wordTab: 'morphology'|'segments'|'i3rab'|'identity'`).
- [X] T003 Enable HTTP interceptors in `Frontend/quran-dashboard-ui/src/app/app.config.ts` by changing `provideHttpClient(withFetch())` to `provideHttpClient(withFetch(), withInterceptors([]))` (empty array now; the secure-url interceptor is added in T018).

**Checkpoint**: Config + skeletons exist; nothing behaves yet.

---

## Phase 2: Foundational (blocking prerequisites for ALL stories)

**⚠️ CRITICAL**: No user story phase can be completed until this phase is done.

### Backend foundation

- [X] T004 [P] Create `MushafReaderOptions` (properties `DefaultTafsirSourceKey`, `DefaultTranslationSourceKey`, `DefaultFullI3rabSourceKey`) in `Backend/application/QuranDashboard.Application.Abstractions/Quran/MushafReader/MushafReaderOptions.cs` (see `contracts/backend-read-abstractions.md`).
- [X] T005 [P] Create the read interfaces `IMushafPageReader`, `IAyahStudyReader`, `IWordAnalysisReader` and the `WordAnalysisOutcome` result type (Found/NotFound/NotAnalyzable) in `Backend/application/QuranDashboard.Application.Abstractions/Quran/MushafReader/` (signatures per `contracts/backend-read-abstractions.md`).
- [X] T006 [P] Create the response DTO records (no logic, just shapes from `data-model.md` §B) (include nested records: `SurahOnPage`, `AyahRange`, `PageNavigationSummary`, `MushafLineDto`, `MushafWordDto`, `PageMarkerDto`, `AyahCoreDto`, `SajdaDto`, `SelectedSourcesDto`, `TafsirEntryDto`, `TranslationEntryDto`, `FullI3rabEntryDto`, `WordOccurrenceDto`, `WordIdentityDto`, `WordMorphologyDto`, `RenderedSegmentDto`). **Placement (review-adjusted):** DTOs live with the read interfaces in `Backend/application/QuranDashboard.Application.Abstractions/Quran/MushafReader/Responses/` (`MushafPageResponse.cs`, `AyahStudyResponse.cs`, `WordAnalysisResponse.cs`), NOT under `Application/.../Queries/` as originally written — `Infrastructure` references `Application.Abstractions` (not `Application`), and the reader interfaces return these types, so the types must sit in `Application.Abstractions` to avoid a circular dependency. `LocalizedLabel` and `SegmentFeaturesDto` live in `WordAnalysisResponse.cs`; later phases should read the responses from `.../MushafReader/Responses/`.
- [X] T007 [P] Feature API messages: Arabic values live centralized in `Backend/api/QuranDashboard.Api/Common/ApiMessages.cs` (`MushafPageLoaded`, `MushafInvalidPageNumber`, `MushafAyahStudyLoaded`, `MushafInvalidVerseKey`, `MushafWordAnalysisLoaded`, `MushafInvalidWordLocation`, `MushafWordNotAnalyzable`, `NotFound`). **Review-adjusted:** the separate `MushafReaderMessages` dotted-key catalog was NOT created — following the existing codebase convention, controllers return `ApiMessages` Arabic values directly (handlers return typed outcomes; controllers map them to Arabic values). No raw keys like `MushafPages.Loaded` are ever returned to users.
- [X] T008 Bind `MushafReaderOptions` from configuration section `"MushafReader"` and add a `MushafReader` registration block in `Backend/infrastructure/QuranDashboard.Infrastructure/DependencyInjection.cs` (reader/handler registrations are added by their stories; this task only wires options binding).
- [X] T009 [P] Create the backend integration test fixture in `Backend/tests/QuranDashboard.Tests/Quran/MushafReader/MushafReaderTestFixture.cs` using `Testcontainers.PostgreSql`. **Pin the seeding strategy (G2):** seed a **representative content slice** (NOT the full DB and NOT the developer's local DB) sufficient for every assertion — the lines/words for pages 1, 5, and 604; ayah `2:25` with tafsir `ar-muyassar`, translation `en-sahih-international`, and full i3rab `muyassar` (plus at least one grouped/ranged entry for the grouped-metadata test); word `2:25:3` with its morphology, ordered/unique identity rows, and segments, including one word that has an empty segment form (for the fallback test) and one ayah-end marker row (for the rejection test). Load the slice from a committed seed script/snapshot so runs are deterministic and offline. (Optional: support an env flag to instead run against a fully seeded local DB, gated like Feature 009's real-run.)

### Frontend foundation

- [X] T010 [P] Create the page-state facade skeleton (state holders for page/ayah/word selections, sources, and per-resource `isLoading`/`isEmpty`/`errorMessage`; no load methods yet) in `Frontend/quran-dashboard-ui/src/app/features/mushaf/state/mushaf-reader.facade.ts`.
- [X] T011 [P] Create the three data-access API service shells that build URLs from `environment.apiBaseUrl` and return `Observable<ApiResponse<T>>` (reuse the existing `ApiResponse` model at `src/app/core/data-access/api-response.model.ts`): `Frontend/quran-dashboard-ui/src/app/features/mushaf/data-access/mushaf-pages.api.ts`, `.../mushaf-ayah-study.api.ts`, `.../mushaf-word-analysis.api.ts` (method bodies are filled by their stories).
- [X] T012 Add the lazy route `dashboard/mushaf` in `Frontend/quran-dashboard-ui/src/app/app.routes.ts` pointing at `features/mushaf/mushaf.routes.ts`, and create `Frontend/quran-dashboard-ui/src/app/features/mushaf/mushaf.routes.ts` plus a minimal shell component `features/mushaf/pages/mushaf-reader-page/mushaf-reader-page.component.{ts,html,scss}` (renders an empty RTL two-column grid placeholder; logic added by stories).
- [X] T013 Configure the Angular unit-test runner — **the project currently ships without one (G1)** (no `test` script, no test target, no karma/jasmine/vitest). Add a `test` target to `Frontend/quran-dashboard-ui/angular.json` using `@angular/build:unit-test` (Vitest runner) with devDependencies `vitest` + `jsdom` (and a `src/test-setup.ts` if the runner needs it), **or** the team's Karma+Jasmine setup; add a `"test": "ng test"` script to `Frontend/quran-dashboard-ui/package.json`. Verify `npm test` runs a passing/empty suite. **This task BLOCKS all frontend test tasks: T019, T028, T038, T047, T050.**

**Checkpoint**: App routes to `/dashboard/mushaf` and shows an empty shell; backend read seams + DTOs + options + fixture exist; frontend test runner is configured.

---

## Phase 3: User Story 2 — Secure local environment (HTTPS) (Priority: P1)

**Goal**: Both apps run over HTTPS locally; every normal frontend data call targets `https://localhost:5015` only; no HTTP/mixed-content; no silent HTTP fallback.

**Independent Test**: Start both apps; backend reachable only at `https://localhost:5015` (HTTP redirects), frontend at `https://localhost:4200`; the secure-url interceptor blocks any non-HTTPS request (unit test); DevTools Network shows only HTTPS `/api/...` calls.

> Implemented before US1's browser rendering because the page can only be fetched in the browser once the secure environment + base URL are in place. (US1's *backend* tasks do not depend on this and may proceed in parallel.)

- [X] T014 [US2] Restrict CORS to the HTTPS origin only: set `"Cors": { "AllowedOrigins": ["https://localhost:4200"] }` (remove the `http://localhost:4200` entry) in both `Backend/api/QuranDashboard.Api/appsettings.json` and `appsettings.Development.json`.
- [X] T015 [US2] Make the `https` profile the default in `Backend/api/QuranDashboard.Api/Properties/launchSettings.json` (so `dotnet run` serves `https://localhost:5015` with HTTP→HTTPS redirect; keep the existing `UseHttpsRedirection()`).
- [X] T016 [US2] Set `apiBaseUrl: 'https://localhost:5015'` in `Frontend/quran-dashboard-ui/src/environments/environment.development.ts` (leave `environment.ts` production `apiBaseUrl: ''` unchanged).
- [X] T017 [US2] Enable the HTTPS dev server: add `"ssl": true`, `"sslCert"`, `"sslKey"` to the `serve` options in `Frontend/quran-dashboard-ui/angular.json`, and add a `"start:https": "ng serve --ssl --ssl-cert <cert> --ssl-key <key>"` script in `Frontend/quran-dashboard-ui/package.json` (cert paths per `quickstart.md` step 1).
- [X] T018 [P] [US2] Implement the dev-time secure-URL guard interceptor in `Frontend/quran-dashboard-ui/src/app/core/data-access/secure-url.interceptor.ts`: allow a request only if its absolute URL starts with `environment.apiBaseUrl` (which is `https://`); otherwise throw a controlled error (never rewrite to HTTP). Register it in the `withInterceptors([...])` array in `app.config.ts`.
- [X] T019 [P] [US2] Add the interceptor unit test `Frontend/quran-dashboard-ui/src/app/core/data-access/secure-url.interceptor.spec.ts`: asserts an `http://` URL is blocked with a controlled error and an `https://localhost:5015/...` URL passes. **(Depends on T013.)**

**Checkpoint**: Servers run over HTTPS; data calls are HTTPS-only and enforced by the interceptor.

---

## Phase 4: User Story 1 — Read and navigate a Mushaf page (Priority: P1) 🎯 MVP

**Goal**: Render one real Mushaf page (lines/words RTL from `text_uthmani`), with header context (surah(s)/juz/hizb/rub/page), prev/next/jump-by-surah navigation, and juz/hizb/rub/sajda markers placed by the first-line rule.

**Independent Test**: Open `/dashboard/mushaf?page=5`; lines/words render in order RTL; header shows correct context; navigation works and stays in 1–604; markers sit beside the right ayah on the first line; page payload carries no tafsir/translation/i3rab/morphology.

### Backend (US1) — independent of US2; can run in parallel with Phase 3

- [X] T020 [US1] Implement `EfMushafPageReader` in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/MushafReader/EfMushafPageReader.cs`: read lines (`quran_mushaf_lines` ordered by `line_number`) + words per line (`quran_words` by `page_number`+`line_number` ordered by `line_word_order`, `verse_key` via `quran_ayahs`), build `surahs`/`ayahRange`/`navigation` from the page's distinct ayahs, and build juz/hizb/rub/sajda markers using the first-line rule `MIN(line_number)` for the ayah on the page (joins per `data-model.md` §A and capability report §2). Return `null` if the page has no rows.
- [X] T021 [US1] Implement `GetMushafPageQuery` + `GetMushafPageHandler` in `Backend/application/QuranDashboard.Application/Quran/MushafReader/Queries/GetMushafPage/`: validate `pageNumber` ∈ [1,604] (else invalid), call `IMushafPageReader`, populate `GetMushafPageResponse`; set `previous/nextPageNumber` (null at 1/604).
- [X] T022 [US1] Implement the thin controller `MushafPagesController` (`GET /api/mushaf/pages/{pageNumber}`) in `Backend/api/QuranDashboard.Api/Controllers/Mushaf/MushafPagesController.cs` mapping to `ApiResponse<MushafPageResponse>` (200 `MushafPages.Loaded`, 400 `MushafPages.InvalidPageNumber`, 404 `Common.NotFound`); register `EfMushafPageReader` in `Infrastructure/DependencyInjection.cs`. Behavior per `contracts/mushaf-page.api.md`.
- [X] T023 [P] [US1] Backend tests in `Backend/tests/QuranDashboard.Tests/Quran/MushafReader/`: `MushafPageReadTests.cs` (pages 1, 5, 604: line/word ordering, lean payload, line types), `MushafPageValidationTests.cs` (page 0/605/non-numeric → controlled result), `MarkerPlacementTests.cs` (multi-line ayah → marker on first line on the page).

### Frontend (US1) — depends on Foundational + US2

- [X] T024 [P] [US1] Implement `getPage(pageNumber)` in `features/mushaf/data-access/mushaf-pages.api.ts` (GET `${apiBaseUrl}/api/mushaf/pages/{pageNumber}`).
- [X] T025 [US1] Add `loadPage(pageNumber)` to `features/mushaf/state/mushaf-reader.facade.ts`: call the API, check `isSuccess`, map `data` → `MushafPageViewModel`, set loading/empty/error (use `qd-loading-state`/`qd-empty-state`/`qd-error-state`).
- [X] T026 [P] [US1] Create the Mushaf render components under `features/mushaf/components/`: `mushaf-header-navigation` (surah(s)/juz/hizb/rub/page + prev/next + jump-by-surah outputs), `mushaf-page-area`, `mushaf-page-view`, `mushaf-line`, `mushaf-word`, `mushaf-marker` (each as `.ts/.html/.scss`; RTL; text from `textUthmani`; markers from `isAyahMarker` + page markers; **no** segment rendering here). Components receive inputs and emit events only.
- [X] T027 [US1] Wire the shell `mushaf-reader-page.component`: read the `page` query param, call `facade.loadPage`, compose `mushaf-header-navigation` + `mushaf-page-area` on the **right** column and an empty **left** study column; navigation events update the `page` URL param within 1–604.
- [X] T028 [P] [US1] Frontend tests (`features/mushaf/...spec.ts`): page renders lines/words from view model; navigation stays within 1–604; marker placement on first line; assert Mushaf area text equals `textUthmani` and no segment forms are rendered in the Mushaf area. **(Depends on T013.)**

**Checkpoint**: 🎯 MVP — with US2 + US1 done you can open `/dashboard/mushaf` over HTTPS and read/navigate real pages.

**Phase 4 addendum (engineering review, non-blocking):** To satisfy **FR-017 jump-by-surah**, a fourth read endpoint was added during Phase 4 (not a separate numbered task): `GET /api/mushaf/surahs` — surah catalog with `startPageNumber` for header navigation. Contract: `contracts/mushaf-surahs.api.md`. Backend: `EfMushafSurahCatalogReader`, `GetMushafSurahCatalogHandler`, `MushafSurahCatalogController`, `MushafSurahCatalogTests`. Frontend: `mushaf-surah-catalog.api.ts`, facade `loadSurahCatalog()` / `resolveSurahStartPage()`. No Phase 5+ behavior (ayah study, word analysis, full URL sync, caching) was introduced.

---

## Phase 5: User Story 3 — Study a selected ayah (Priority: P2)

**Goal**: Selecting an ayah shows core identity + tafsir + translation + full i3rab **together** (configured defaults), with source switching, grouped-coverage notes, and sanitized HTML.

**Independent Test**: Select an ayah with no source params → defaults `ar-muyassar`/`en-sahih-international`/`muyassar` load together, each labeled with the source used; switch each source → content + label update; HTML renders sanitized; a grouped entry shows which verses it covers.

### Backend (US3)

- [ ] T029 [US3] Implement `EfAyahStudyReader` in `Backend/infrastructure/.../Persistence/Reads/Quran/MushafReader/EfAyahStudyReader.cs`: read `AyahCoreDto` (+ sajda) and, for the **resolved** source key per kind, one tafsir / one translation / one full-i3rab entry with grouped metadata (`isGroupLeader`, `sourceValueKind`, `sourceLeaderVerseKey`, `coveredAyahCount`, `coveredAyahKeys`); a kind whose source is missing/unknown returns `null` for that block only (joins per `data-model.md` §A, capability report §3). HTML returned unmodified.
- [ ] T030 [US3] Implement `GetAyahStudyQuery` + `GetAyahStudyHandler` in `.../Queries/GetAyahStudy/`: resolve each source kind explicit→`MushafReaderOptions` default→null; load the three together; populate `selectedSources` with the resolved keys; validate `verseKey` format/existence (400/404).
- [ ] T031 [US3] Implement thin controller `MushafAyahStudyController` (`GET /api/mushaf/ayahs/{verseKey}/study?tafsirSource&translationSource&fullI3rabSource`) in `Backend/api/QuranDashboard.Api/Controllers/Mushaf/MushafAyahStudyController.cs` → `ApiResponse<AyahStudyResponse>`; register reader DI. Behavior per `contracts/ayah-study.api.md`.
- [ ] T032 [P] [US3] Backend tests: `AyahStudyReadTests.cs` (defaults applied + explicit sources; all three blocks present together), `AyahStudyGroupedEntryTests.cs` (grouped/ranged metadata exposed), `AyahStudyMissingSourceTests.cs` (unknown source → that block null + others still load; no substitution).

### Frontend (US3)

- [ ] T033 [P] [US3] Implement `getAyahStudy(verseKey, {tafsirSource, translationSource, fullI3rabSource})` in `features/mushaf/data-access/mushaf-ayah-study.api.ts`.
- [ ] T034 [P] [US3] Create the sanitized HTML pipe `safe-html.pipe.ts` in `Frontend/quran-dashboard-ui/src/app/shared/ui/safe-html/` using Angular's built-in sanitizer via `[innerHTML]` (NO `bypassSecurityTrustHtml`).
- [ ] T035 [US3] Add `loadAyahStudy(verseKey)` + `setTafsirSource/setTranslationSource/setFullI3rabSource` to the facade (map to `AyahStudyViewModel`; per-kind empty state when a block is null; reflect source keys into URL params).
- [ ] T036 [P] [US3] Create components under `features/mushaf/components/`: `selected-ayah-section`, `tafsir-card`, `translation-card`, `full-i3rab-card` (render HTML with `safe-html`; show grouped-coverage note; `ayahTab` ∈ {`tafsir`,`translation`,`full-i3rab`} only), and `source-selector` (reusable for the three kinds). Place `selected-ayah-section` in the **bottom** of the left study column.
- [ ] T037 [US3] Wire the shell: read the `ayah` (+ `tafsirSource`/`translationSource`/`fullI3rabSource`, `ayahTab`) params; selecting an ayah in the page sets the `ayah` URL param and triggers `facade.loadAyahStudy`.
- [ ] T038 [P] [US3] Frontend tests: ayah study shows the three sources together; switching a source updates URL + content + "source used" label; HTML is sanitized (a `<script>` is not executed); grouped coverage is shown. **(Depends on T013.)**

**Checkpoint**: US1 + US3 work; an ayah's full study context is visible in the bottom-left.

---

## Phase 6: User Story 4 — Analyze a selected word and its segments (Priority: P2)

**Goal**: Selecting a readable word shows identity + morphology + glued color-linked segments with simple i3rab; ayah-end markers are not selectable; empty segment forms fall back safely.

**Independent Test**: Select a multi-segment word → morphology + identity + glued colored segments render, with matching colors across the glued word, the data rows, and the i3rab labels; selecting an ayah-end marker yields no analysis; a word with an empty segment form shows a placeholder (no invented text) and the full word is preserved.

### Backend (US4)

- [ ] T039 [US4] Implement `EfWordAnalysisReader` in `Backend/infrastructure/.../Persistence/Reads/Quran/MushafReader/EfWordAnalysisReader.cs`: reject `is_ayah_marker = true` (→ `NotAnalyzable`); read occurrence + display forms + ordered/unique identity counts + head morphology + ordered segments; assign a stable `segmentColorSlot` by `segment_number`; set `displayTextStatus="missing"` (no invented text) when `form_arabic_normalized` is empty/null (joins per `data-model.md` §A, capability report §4–§5).
- [ ] T040 [US4] Implement `GetWordAnalysisQuery` + `GetWordAnalysisHandler` in `.../Queries/GetWordAnalysis/`: validate `wordLocation` format; map `WordAnalysisOutcome` (Found/NotFound/NotAnalyzable) to the response.
- [ ] T041 [US4] Implement thin controller `MushafWordAnalysisController` (`GET /api/mushaf/words/{wordLocation}/analysis`) in `Backend/api/QuranDashboard.Api/Controllers/Mushaf/MushafWordAnalysisController.cs` → `ApiResponse<WordAnalysisResponse>` (200 / 404 / 400 `MushafWords.NotAnalyzable`); register reader DI. Behavior per `contracts/word-analysis.api.md`.
- [ ] T042 [P] [US4] Backend tests: `WordAnalysisReadTests.cs` (normal word: morphology + identity + ordered segments + slots), `WordAnalysisMarkerRejectionTests.cs` (ayah marker → not analyzable), `WordAnalysisSegmentFallbackTests.cs` (empty segment form → `displayTextStatus:"missing"`, no fabricated text).

### Frontend (US4)

- [ ] T043 [P] [US4] Implement `getWordAnalysis(wordLocation)` in `features/mushaf/data-access/mushaf-word-analysis.api.ts`.
- [ ] T044 [US4] Add `loadWordAnalysis(wordLocation)` to the facade: reject selecting a marker; map to `WordAnalysisViewModel`, converting `segmentColorSlot` → a color via a small frontend palette (visual-linking only); flag missing segments.
- [ ] T045 [P] [US4] Create components under `features/mushaf/components/`: `selected-word-section`, `segment-rendered-word` (inline spans glued with NO inserted spaces, colored by slot; placeholder for missing form; never used in the Mushaf area), `word-morphology-summary`, `segment-data-rows` (color-linked to the glued segments and to the i3rab labels; `wordTab` ∈ {`morphology`,`segments`,`i3rab`,`identity`}). Place `selected-word-section` at the **top** of the left study column.
- [ ] T046 [US4] Wire the shell: read the `word` and `segment` params and the `wordTab` param; selecting a **readable** word sets the `word` URL param and triggers `facade.loadWordAnalysis`; ayah-end markers are not selectable.
- [ ] T047 [P] [US4] Frontend tests: word analysis renders; segment colors match across glued word + data rows + i3rab labels; marker is not selectable; empty-segment fallback shows placeholder; assert segment rendering never appears in the Mushaf page area. **(Depends on T013.)**

**Checkpoint**: US1 + US3 + US4 work; the full right-Mushaf / left-study (word top, ayah bottom) layout is functional.

---

## Phase 7: User Story 5 — Reproduce any view from its URL (Priority: P3)

**Goal**: All view state lives in the URL via natural Quran keys; reopening a URL restores the exact view; on wide desktop `panel` is focus state (not exclusive hiding).

**Independent Test**: Build a specific view (page + ayah + word + segment + tabs + sources), copy the URL, reopen in a fresh tab → identical view restored; on wide desktop both word and ayah sections stay visible regardless of `panel`.

- [ ] T048 [US5] Implement full URL↔state synchronization in `features/mushaf/state/mushaf-reader.facade.ts`: read/write all params (`page,ayah,word,segment,panel,ayahTab,wordTab,tafsirSource,translationSource,fullI3rabSource`) using natural keys (`ayah=2:25`, `word=2:25:3`, `segment=2:25:3:1`); ignore/normalize any out-of-scope enum value (`panel=sources`, `ayahTab=links`) to the nearest valid v1 value; use `Router` `replaceUrl` for fine-grained changes; on init, hydrate state from the URL and trigger the right loads (page → ayah → word) so deep links restore the view.
- [ ] T049 [US5] Wire `panel` semantics in the shell: on wide desktop both study sections remain visible (panel = focus only, value set `ayah|word|none`); below the breakpoint, `panel` selects which section/drawer is active. Ensure changing `panel`/tabs updates the URL without losing other state.
- [ ] T050 [P] [US5] Frontend tests: deep-link restore for page/ayah/word/segment/tabs/sources; `panel` does not hide a section on wide desktop; URL state is preserved when toggling responsive layout; out-of-scope enum values are normalized. **(Depends on T013.)**

**Checkpoint**: Any reader view is shareable and reproducible.

---

## Phase 8: Polish & cross-cutting concerns

**Purpose**: Caching (locked to come AFTER the APIs/tests are stable), responsiveness, navigation, and final guards.

### Backend cache (only after Phases 4–6 readers + tests are green)

- [ ] T051 [P] Implement `IMemoryCache` decorators `CachedMushafPageReader`, `CachedAyahStudyReader`, `CachedWordAnalysisReader` + `MushafReaderCacheKeys` in `Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Quran/MushafReader/`; register them as decorators in `Infrastructure/DependencyInjection.cs`. Keys per `data-model.md` §E; cache only successful immutable reads; never cache not-found/not-analyzable or user-specific data; no Redis.
- [ ] T052 [P] Backend test `Backend/tests/QuranDashboard.Tests/Quran/MushafReader/MushafReaderCacheTests.cs`: second identical read is served from cache; failures/not-found are not cached.

### Frontend polish

- [ ] T053 [P] Implement the bounded request cache + dedupe in `features/mushaf/state/mushaf-reader-cache.ts` (cache successful page/ayah/word responses by their keys; share in-flight observables for identical concurrent requests; optional prev/next page prefetch after the current page loads; bounded size) and use it from the facade.
- [ ] T054 [P] Implement responsive behavior in the shell + study components: wide desktop two-column (Mushaf right ~55–60%, study left ~40–45%; word top ~35–40% / ayah bottom ~60–65%); tablet stack/collapse; mobile drawer/bottom-sheet with word/ayah tabs; all cards keep stable outer dimensions and scroll internally; URL state preserved across modes (compose `qd-` classes per `UI_STYLE_SYSTEM.md`).
- [ ] T055 [P] Add a navigation entry for the Mushaf Reader (route `/dashboard/mushaf`, Arabic label) in `Frontend/quran-dashboard-ui/src/app/core/navigation/nav-items.ts`.

### Final guards & validation

- [ ] T056 [P] Update Swagger summaries/descriptions for the three endpoints (clear, realistic, no placeholders) per `API_GUIDELINES.md` §9.
- [ ] T057 Run the clean-code guard self-check and `test-guard` self-check; verify file-size thresholds for the shell, facade, and EF readers (split if a soft threshold is approached); confirm all user-facing messages are Arabic and centralized.
- [ ] T058 Run `specs/011-mushaf-reader-study-context/quickstart.md` end-to-end: both apps over HTTPS, the smoke-test table passes, and DevTools shows only HTTPS `/api/mushaf/*` calls (zero HTTP/mixed-content).

---

## Dependencies & Execution Order

### Phase order
- **Phase 1 Setup** → **Phase 2 Foundational** → **Phase 3 (US2)** → **Phase 4 (US1)** → **Phase 5 (US3)** → **Phase 6 (US4)** → **Phase 7 (US5)** → **Phase 8 Polish**.
- Phase 2 BLOCKS every user story. **T013 (frontend test runner) BLOCKS every frontend test task (T019, T028, T038, T047, T050).** Phase 8 caching depends on Phases 4–6 being green.

### Cross-story notes
- **US2 (HTTPS)** is implemented first among the stories because US1's *browser* rendering needs the secure base URL. **US1 backend tasks (T020–T023)** do **not** depend on US2 and may be built in parallel with Phase 3.
- **US3** and **US4** are independent of each other (different readers/endpoints/components) and both attach to the US1 shell; they can be built in parallel by different developers after US1.
- **US5** consolidates URL handling; basic per-story URL params are already set in US1/US3/US4, so US5 mostly hardens deep-link restore + `panel` semantics + out-of-scope enum normalization.

### Within a backend story
Reader (EF) → Handler/Query → Controller(+DI) → tests. (Reader before handler before controller; tests `[P]` once the targets exist.)

### Within a frontend story
API service `[P]` → facade method → components `[P]` → shell wiring → tests `[P]` (tests also require T013).

---

## Parallel execution examples

**Phase 2 Foundational — run together:**
```
Backend [P]: T004 options · T005 read interfaces+outcome · T006 response DTOs · T007 message keys · T009 test fixture
Frontend [P]: T010 facade skeleton · T011 api shells · T013 test runner
```

**Phase 4 US1 — backend tests in parallel after T020–T022:**
```
T023 MushafPageReadTests / MushafPageValidationTests / MarkerPlacementTests
```

**Phase 4 US1 — frontend render components in parallel (T026):**
```
mushaf-header-navigation, mushaf-page-area, mushaf-page-view, mushaf-line, mushaf-word, mushaf-marker
```

**US3 and US4 by two developers in parallel (after US1 shell exists):**
```
Dev A: T029–T038 (ayah study)
Dev B: T039–T047 (word analysis)
```

---

## Implementation Strategy

### MVP first
1. Phase 1 Setup → Phase 2 Foundational (includes T013 frontend test runner).
2. Phase 3 (US2 HTTPS) + Phase 4 (US1 page) → **STOP & VALIDATE**: open `/dashboard/mushaf` over HTTPS, read and navigate pages. This is the demoable MVP.

### Incremental delivery
3. Add US3 (ayah study) → validate → demo.
4. Add US4 (word analysis) → validate → demo.
5. Add US5 (URL reproducibility) → validate → demo.
6. Phase 8 polish: backend cache (after APIs/tests stable) → frontend cache → responsive → final guards → quickstart validation.

### Notes
- `[P]` = different files, no incomplete dependency.
- Commit after each task or logical group; keep the DB read-only; never fabricate Quranic data.
- Stop at any checkpoint to validate the story independently.
- Caching is intentionally last (locked decision: add cache only after the read APIs and tests are stable).
- Frontend test tasks cannot run until T013 configures the Angular test runner.
