# Implementation Plan: Words Hub + Unique Words Explorer

**Branch**: `014-words-hub-unique-words` | **Date**: 2026-06-21 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `specs/014-words-hub-unique-words/spec.md`

> **Companion documents (source of truth):**
> `docs/feature-014-words-hub-unique-words/feature-014-words-hub-unique-words-planning-report.md`
> (locked planning report: scope, API direction, UX, query/performance decisions),
> `docs/feature-013-words-roots-explorer/feature-013-unique-words-capability-report.md`
> and `docs/feature-013-words-roots-explorer/feature-013-deterministic-unique-word-ids-plan.md`
> (unique-word data capability and deterministic ID design),
> `Backend/report/feature-013-deterministic-unique-word-ids/002-reset-reseed-acceptance-report.md`
> (deterministic ID acceptance), and
> `Backend/report/database/current-database-tables-and-relationships-report.md`
> (read-only database baseline).
> **Governance:** `AGENTS.md`, `CODING_PRINCIPLES.md`, `Backend/AGENTS.md`,
> `Backend/.architecture/{BACKEND_STRUCTURE,CLEAN_ARCHITECTURE,API_GUIDELINES}.md`,
> `Frontend/quran-dashboard-ui/AGENTS.md`,
> `Frontend/quran-dashboard-ui/.architecture/{FRONTEND_STRUCTURE,API_INTEGRATION_GUIDELINES,UI_STYLE_SYSTEM}.md`,
> `PRODUCT.md`, `DESIGN.md`.

## Summary

Add a dashboard Words hub at `/dashboard/words` and a Unique Words explorer for two unique-word modes: **with tashkeel** (`بالتشكيل`) and **simple/imlaei without tashkeel** (`إملائي (بدون تشكيل)`). The hub exposes one active v1 section (`الكلمات الفريدة`) and four disabled coming-soon sections (`الجذور`, `الصيغة المعجمية`, `الأصل الصرفي`, `أنواع الكلمة`).

Backend: add read-only Words APIs for paged unique-word lists, selected unique-word summary, mentioned-surah drill-down, missing-surah drill-down, and paged ayah-match drill-down. Reads use existing Feature 013 unique-word tables and `quran_words` occurrence links only. Add one small reusable `PagedResult<T>` contract because this is the first paged list API. No schema changes, migrations, imports, writes, or new indexes in v1.

Frontend: add a lazy `features/words/` slice with hub and explorer routes, a facade/state layer for mode/search/sort/page/query-param modal state, a data-access service returning the existing `ApiResponse<T>` envelope, and child components for cards, chips, modal drill-downs, and id-based highlighted ayahs. Search uses normalized contains matching, drill-downs open as modals over the current list, and selected-word URL state uses the stable unique-word ID.

All critical decisions are locked by the Feature 014 spec and clarification session; there are **no unresolved clarifications**.

## Technical Context

**Language/Version**: Backend C# 13 / .NET 10 (`net10.0`); Frontend TypeScript on Angular 20.3 (standalone APIs, no NgModules).  
**Primary Dependencies**:
- Backend: ASP.NET Core controllers + Swagger (existing), EF Core 10 / Npgsql PostgreSQL provider (existing `QuranDashboardDbContext`), existing `ApiResponse<T>` contract and global exception handling.
- Frontend: Angular Router, `provideHttpClient(withFetch())`, RxJS 7.8, Angular Signals/facade patterns, existing `ApiResponse<T>` model, existing `qd-` UI primitives, and existing Arabic search normalization helper semantics.

**Storage**: Existing PostgreSQL database `quran_dashboard`, read-only. Uses existing tables: `quran_words`, `quran_words_unique_tashkeel`, `quran_words_unique_simple`, `quran_surahs`, and canonical ayah metadata reachable from word rows. No new tables, columns, migrations, imports, seed changes, writes, or v1 indexes.  
**Testing**: Backend xUnit + FluentAssertions + Testcontainers PostgreSQL integration tests in `Backend/tests/QuranDashboard.Tests`; frontend Angular unit tests via the project test runner for data-access, facade/state, URL query sync, modal behavior, and highlighted ayah rendering.  
**Target Platform**: Existing local dashboard over HTTPS: backend at `https://localhost:5015`, frontend at `https://localhost:4200`.  
**Project Type**: Full-stack web application: .NET Clean Architecture backend (`Backend/`) plus Angular dashboard frontend (`Frontend/quran-dashboard-ui/`).  
**Performance Goals**:
- Unique-word list reads return one bounded page at a time; default list page size is 50.
- Ayah-match drill-down reads return one bounded page at a time; default ayah page size is 20.
- List cards use precomputed unique-word counts and do not group `quran_words` per card.
- Ayah-match drill-down avoids N+1 reads by fetching matched rows, paged ayah IDs, then all words for those ayahs in a batched read.
- Full drill-down payloads load only when the corresponding modal view is opened or restored from URL state.

**Constraints**:
- Read-only feature: no schema changes, migrations, imports, writes, editing, curation, approval flows, public reader scope, audio, global search, or roots/lemma/stem/POS exploration in v1.
- Stable unique-word IDs from Feature 013 are the selected-word identity and URL key.
- Raw technical keys such as `word_key_imlaei_simple` must not be the primary user-facing label.
- Quran word and ayah text must come from canonical Quran data; frontend/backend must not invent, correct, or fabricate Quranic text.
- Ayah markers and non-readable markers must be excluded from counts, occurrence lists, and highlighted matches.
- Highlighting uses occurrence IDs (`quran_words.id` / frontend `quranWordId`) rather than string replacement.
- Search uses normalized contains matching for both modes.
- Drill-downs open in modal state represented by query params over the current list route.
- Arabic-default user-facing messages; English identifiers/property names.
- Frontend is Arabic-first/RTL-first and must compose existing `qd-` primitives.
- File-size review thresholds from backend/frontend architecture docs apply; split components/services before hard thresholds.

**Scale/Scope**: Current data scale: 21,294 unique tashkeel words, 14,783 unique simple words, 83,668 total word rows, 77,432 readable word occurrences, 6,236 ayah markers, 114 surahs. Delivery scope: one backend Words read slice with five read resources, one frontend Words feature slice with hub, explorer, modal drill-downs, URL state, and tests.

*No unresolved clarification items. See [research.md](./research.md) for locked technical decisions.*

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

The project constitution (`.specify/memory/constitution.md`) is still an unratified template. As in prior features, the operative governance authority is the workspace/backend/frontend rule set: `AGENTS.md`, `CODING_PRINCIPLES.md`, `Backend/.architecture/{BACKEND_STRUCTURE,CLEAN_ARCHITECTURE,API_GUIDELINES}.md`, and `Frontend/quran-dashboard-ui/.architecture/{FRONTEND_STRUCTURE,API_INTEGRATION_GUIDELINES,UI_STYLE_SYSTEM}.md`. Do not infer additional MUST rules from the unratified constitution.

| Gate | Status | Notes |
|---|---|---|
| Clean Architecture dependency direction | PASS | Application owns use cases and read abstractions; Infrastructure implements EF reads; Api remains thin. No EF/Infrastructure usage inside controllers. |
| Backend feature/domain foldering | PASS | New backend code stays under `Quran/Words/` feature folders and avoids global dumping folders. `PagedResult<T>` is allowed as a small shared abstraction because it is the first reusable paging contract. |
| API boundary | PASS | Existing `ApiResponse<T>` envelope, resource-oriented read routes, Arabic messages, controlled `200`/`400`/`404`, no EF entities exposed. |
| Read-only & Quran data safety | PASS | Existing data only; no writes, migrations, imports, source-data mutation, invented Quran text, or frontend fallback Quran text. |
| Deterministic identity safety | PASS | Stable unique-word IDs from Feature 013 are used for selected-word URLs; display text is not used as identity. |
| Pagination discipline | PASS | List and ayah drill-down are bounded pages; no all-results load for large lists. |
| Frontend structure | PASS | Add a focused `features/words/` slice with pages, components, data-access, state, models, and routes; routeable pages stay thin. |
| Frontend API integration | PASS | Components dispatch to facade/store; API service returns `ApiResponse<T>`; facade maps loading/empty/error state and URL query state. |
| URL state | PASS | Major modes use stable route segments; list and modal state use query params; selected word uses stable ID. |
| Localization, RTL, accessibility | PASS | Arabic labels/states are specified; UI is RTL-first; focus, disabled states, contrast, and no color-only meaning are required. |

**Post-design re-check:** PASS. Phase 1 data model and contracts preserve these gates and introduce no justified violations.

## Project Structure

### Documentation (this feature)

```text
specs/014-words-hub-unique-words/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── unique-words-api.md
│   ├── backend-read-abstractions.md
│   └── frontend-routing-state.md
├── checklists/
│   └── requirements.md
└── tasks.md             # Created later by /speckit.tasks, not by /speckit.plan
```

### Source Code (repository root)

```text
Backend/
  domain/QuranDashboard.Domain/
    # NO CHANGES expected. Feature reads existing Quran word/display entities only.

  application/QuranDashboard.Application.Abstractions/
    Common/Paging/
      PagedResult.cs                         # minimal reusable paging response contract
    Quran/Words/
      IUniqueWordsReader.cs                  # focused read abstraction for Feature 014
      Responses/
        UniqueWordListItemDto.cs
        UniqueWordSummaryDto.cs
        UniqueWordSurahsResponse.cs
        UniqueWordMissingSurahsResponse.cs
        UniqueWordAyahMatchDto.cs

  application/QuranDashboard.Application/
    Quran/Words/Queries/
      GetUniqueWordsPage/
        GetUniqueWordsPageQuery.cs
        GetUniqueWordsPageHandler.cs
        GetUniqueWordsPageOutcome.cs
      GetUniqueWordSummary/
        GetUniqueWordSummaryQuery.cs
        GetUniqueWordSummaryHandler.cs
        GetUniqueWordSummaryOutcome.cs
      GetUniqueWordSurahs/
        GetUniqueWordSurahsQuery.cs
        GetUniqueWordSurahsHandler.cs
        GetUniqueWordSurahsOutcome.cs
      GetUniqueWordMissingSurahs/
        GetUniqueWordMissingSurahsQuery.cs
        GetUniqueWordMissingSurahsHandler.cs
        GetUniqueWordMissingSurahsOutcome.cs
      GetUniqueWordAyahs/
        GetUniqueWordAyahsQuery.cs
        GetUniqueWordAyahsHandler.cs
        GetUniqueWordAyahsOutcome.cs

  infrastructure/QuranDashboard.Infrastructure/
    Persistence/Reads/Quran/Words/
      EfUniqueWordsReader.cs                 # list + summary + drill-down reads; split if threshold approaches
    DependencyInjection.cs

  api/QuranDashboard.Api/
    Controllers/Words/
      UniqueWordsController.cs               # thin selected resource endpoints
    Common/ApiMessages.cs                    # add/centralize Arabic Words messages if needed

  tests/QuranDashboard.Tests/Quran/Words/
    UniqueWordsListReadTests.cs
    UniqueWordsSearchSortPagingTests.cs
    UniqueWordsValidationTests.cs
    UniqueWordSurahDrilldownTests.cs
    UniqueWordAyahMatchesTests.cs
    UniqueWordsPagingTests.cs

Frontend/quran-dashboard-ui/
  src/app/core/navigation/nav-items.ts        # words route points to /dashboard/words
  src/app/app.routes.ts                       # lazy-load words feature and exclude fallback route

  src/app/features/words/
    pages/
      words-hub-page/
      unique-words-page/
    components/
      word-section-card/
      unique-words-tabs/
      unique-words-search-bar/
      unique-word-card/
      word-count-chip/
      word-drilldown-modal/
      surah-occurrences-list/
      missing-surahs-list/
      ayah-matches-list/
      highlighted-ayah/
    data-access/
      unique-words.api.ts
    state/
      unique-words.facade.ts                 # list, modal, query-param, loading/error state
    models/
      unique-words.models.ts
    words.routes.ts

  src/app/features/words/**/*.spec.ts         # frontend coverage for facade, URL state, components
```

**Structure Decision**: Add a new `Quran/Words` backend slice because this feature reads Quran word-display data and is not part of Mushaf Reader. Add a new frontend `features/words/` slice because `/dashboard/words` becomes a real routeable area, not a fallback page. No Domain or database schema changes are planned. `PagedResult<T>` is the only shared contract addition and must remain minimal because pagination will be reused by future feature slices.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| _(none)_ | - | - |

### Watch items (review thresholds, not violations)

- **`EfUniqueWordsReader.cs`** owns five related read shapes. Mitigation: keep each query method focused and split into list/drill-down read services only if it approaches the backend read-service soft threshold.
- **`unique-words.facade.ts`** owns list state, modal state, URL sync, and loading/error state. Mitigation: keep typed state slices small and split query-param parsing or drill-down state helpers if it approaches the frontend facade soft threshold.
- **`unique-words-page.component.html`** may grow with list, filters, cards, pagination, and modal. Mitigation: use child components listed above before the template approaches review thresholds.
- **Search normalization** must match Arabic user expectations without broad new indexing. Mitigation: v1 uses existing simplified columns/normalization semantics; add generated columns or indexes only after measured evidence.
