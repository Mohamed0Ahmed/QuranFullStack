# Implementation Plan: Mushaf Reader Ayah Similarities

**Branch**: `012-mushaf-reader-ayah-similarities` | **Date**: 2026-06-21 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `specs/012-mushaf-ayah-similarities/spec.md`

> **Companion documents (source of truth):**
> `docs/feature-012-mushaf-reader-ayah-similarities/feature-012-mushaf-reader-ayah-similarities-planning-report.md`
> (locked planning report: scope + UX + API direction + lazy-loading decisions),
> `specs/011-mushaf-reader-study-context/plan.md` and sibling artifacts
> (existing Mushaf Reader contracts and frontend/backend slice),
> `docs/feature-011-mushaf-reader-study-context/feature-011-ayah-word-data-capability-report.md`
> (data-capability report that first identified similar-ayah/mutashabihat availability), and
> `Backend/report/database/current-database-tables-and-relationships-report.md`
> (read-only database baseline).
> **Governance:** `AGENTS.md`, `CODING_PRINCIPLES.md`, `Backend/AGENTS.md`,
> `Backend/.architecture/{BACKEND_STRUCTURE,CLEAN_ARCHITECTURE,API_GUIDELINES}.md`,
> `Frontend/quran-dashboard-ui/AGENTS.md`,
> `Frontend/quran-dashboard-ui/.architecture/{FRONTEND_STRUCTURE,API_INTEGRATION_GUIDELINES,UI_STYLE_SYSTEM}.md`,
> `PRODUCT.md`, `DESIGN.md`.

## Summary

Extend the existing dashboard Mushaf Reader selected-ayah study area with two new ayah study actions: **similar meaning ayahs** (`آيات قريبة في المعنى`) and **mutashabihat / memorization similarities** (`المتشابهات اللفظية للحفظ`). The initial Mushaf page response remains unchanged and lean: no similarity counters and no similarity detail payloads are added to page, line, word, or page-ayah DTOs.

Backend: add lightweight `similaritySummary` counts to the existing selected ayah study response, plus two separate lazy read endpoints under the existing Mushaf ayah route family: one flat similar-ayah list and one grouped mutashabihat detail response. Reads use the already-imported Feature 006 tables and canonical Quran tables only. No schema changes, migrations, imports, writes, or Quran text copying.

Frontend: widen the selected-ayah action/tab state to include `similar-ayahs` and `mutashabihat`, add two lazy data-access/state slices, and render similar ayahs flat while rendering mutashabihat grouped by phrase/group. Phrase/word-span text, if displayed, is derived from canonical Quran word text returned by the backend, not from mutashabihat tables.

All decisions are locked by the Feature 012 planning report and spec; there are **no unresolved clarifications**.

## Technical Context

**Language/Version**: Backend C# 13 / .NET 10 (`net10.0`); Frontend TypeScript on Angular 20.3 (standalone APIs, no NgModules).  
**Primary Dependencies**:
- Backend: ASP.NET Core controllers + Swagger (existing), EF Core 10 / Npgsql PostgreSQL provider (existing `QuranDashboardDbContext`), `IMemoryCache` for post-read cache decorators, existing `ApiResponse<T>` contract and global exception handling.
- Frontend: Angular Router, `provideHttpClient(withFetch())`, RxJS 7.8, existing feature facade/cache patterns under `features/mushaf/`, and the existing `qd-` style/state primitives.

**Storage**: Existing PostgreSQL database `quran_dashboard`, read-only. Uses existing tables: `quran_ayahs`, `quran_surahs`, `quran_words`, `quran_similar_ayah_links`, `quran_mutashabihat_groups`, and `quran_mutashabihat_occurrences`. No new tables, columns, migrations, imports, seed changes, or writes.  
**Testing**: Backend xUnit + FluentAssertions + Testcontainers PostgreSQL integration tests in `Backend/tests/QuranDashboard.Tests`; frontend Angular unit tests for data-access, facade/cache, URL-state parsing, and selected-ayah child components.  
**Target Platform**: Existing local dashboard over HTTPS: backend at `https://localhost:5015`, frontend at `https://localhost:4200`.  
**Project Type**: Full-stack web application: .NET Clean Architecture backend (`Backend/`) plus Angular dashboard frontend (`Frontend/quran-dashboard-ui/`).  
**Performance Goals**: Initial Mushaf page load stays unchanged and does not include similarity counts/details; selected ayah study adds counts only; full similar-ayah and mutashabihat details are lazy-loaded only on the active action. Repeat immutable reads can be served from cache after read behavior is tested.  
**Constraints**:
- Read-only feature: no schema changes, migrations, imports, writes, editing, approval workflows, public reader features, audio, bookmarks, or graph exploration.
- Mushaf page response must not gain `similarAyahCount`, `mutashabihatGroupCount`, `mutashabihatOccurrenceCount`, or detail payloads.
- Ayah text must come from `quran_ayahs.text_uthmani`.
- Phrase/word-span text, if returned, must be derived at read time from canonical `quran_words` using the occurrence ayah and word range.
- Similar meaning ayahs render flat and combine incoming + outgoing directed links, deduplicating bidirectional rows.
- Mutashabihat render grouped by phrase/group and are never flattened.
- Arabic-default user-facing messages; English identifiers/property names.
- URL state uses natural Quran keys and widened selected-ayah action values: `tafsir`, `translation`, `full-i3rab`, `similar-ayahs`, `mutashabihat`.
- File-size review thresholds from backend/frontend architecture docs apply; split components/services before hard thresholds.

**Scale/Scope**: Data scale from current baseline: 6,236 ayahs, 3,552 similar ayah directed links, 814 mutashabihat groups, 3,557 mutashabihat occurrences. Delivery scope: extend one existing selected-ayah study endpoint with counts, add two lazy backend read endpoints, add frontend data/state/UI for two selected-ayah actions, and update URL-state handling.

*No unresolved clarification items. See [research.md](./research.md) for locked technical decisions.*

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

The project constitution (`.specify/memory/constitution.md`) is still an unratified placeholder template. As in prior features, the operative governance authority is the workspace/backend/frontend rule set: `AGENTS.md`, `CODING_PRINCIPLES.md`, `Backend/.architecture/{BACKEND_STRUCTURE,CLEAN_ARCHITECTURE,API_GUIDELINES}.md`, and `Frontend/quran-dashboard-ui/.architecture/{FRONTEND_STRUCTURE,API_INTEGRATION_GUIDELINES,UI_STYLE_SYSTEM}.md`. Do not infer additional MUST rules from the placeholder constitution.

| Gate | Status | Notes |
|---|---|---|
| Clean Architecture dependency direction | PASS | Application owns use cases and read abstractions; Infrastructure implements EF reads and cache decorators; Api remains thin. No EF/Infrastructure usage inside controllers. |
| Backend feature/domain foldering | PASS | New backend code stays under existing `Quran/MushafReader/` feature folders, with a nested ayah-similarities slice where useful. No global dumping folders. |
| API boundary | PASS | Existing `ApiResponse<T>` envelope, resource-oriented read routes, Arabic messages, controlled `200`/`400`/`404`, no EF entities exposed. |
| Read-only & Quran data safety | PASS | Existing data only; no writes, migrations, imports, source-data mutation, or copied Quran text from mutashabihat tables. |
| Mushaf page payload discipline | PASS | Page response remains unchanged for similarity purposes; counts live only in selected ayah study. |
| Frontend structure | PASS | Extend existing `features/mushaf/` slice via data-access/state/components/models; smart shell stays thin. |
| Frontend API integration | PASS | Page/components dispatch to facade/store; API services return `ApiResponse<T>`; facade maps loading/empty/error state. |
| URL state | PASS | Widen existing `ayahTab` values rather than adding a separate key unless implementation finds a concrete conflict. |
| Localization and RTL UI | PASS | Arabic labels/states are specified; English property names remain. |

**Post-design re-check:** PASS. Phase 1 data model and contracts preserve these gates and introduce no justified violations.

## Project Structure

### Documentation (this feature)

```text
specs/012-mushaf-ayah-similarities/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── ayah-study-similarity-summary.api.md
│   ├── similar-ayahs.api.md
│   ├── ayah-mutashabihat.api.md
│   ├── backend-read-abstractions.md
│   └── frontend-url-state-and-lazy-loading.md
├── checklists/
│   └── requirements.md
└── tasks.md             # Created later by /speckit.tasks, not by /speckit.plan
```

### Source Code (repository root)

```text
Backend/
  domain/QuranDashboard.Domain/
    # NO CHANGES expected. Feature reads existing Quran/Mutashabihat entities only.

  application/QuranDashboard.Application.Abstractions/Quran/MushafReader/
    IAyahStudyReader.cs                  # extend response shape with similaritySummary
    IAyahSimilaritiesReader.cs           # read flat similar ayahs for selected ayah
    IAyahMutashabihatReader.cs           # read grouped mutashabihat for selected ayah

  application/QuranDashboard.Application.Abstractions/Quran/MushafReader/Responses/
    AyahStudyResponse.cs                 # add SimilaritySummaryDto
    AyahSimilaritiesResponse.cs          # flat list response
    AyahMutashabihatResponse.cs          # grouped mutashabihat response

  application/QuranDashboard.Application/Quran/MushafReader/Queries/
    GetAyahStudy/                        # add summary-count orchestration
    GetSimilarAyahs/
      GetSimilarAyahsQuery.cs
      GetSimilarAyahsHandler.cs
      GetSimilarAyahsOutcome.cs
    GetAyahMutashabihat/
      GetAyahMutashabihatQuery.cs
      GetAyahMutashabihatHandler.cs
      GetAyahMutashabihatOutcome.cs

  infrastructure/QuranDashboard.Infrastructure/
    Persistence/Reads/Quran/MushafReader/
      EfAyahStudyReader.cs               # count summary joins; no page response changes
      EfAyahSimilaritiesReader.cs
      EfAyahMutashabihatReader.cs
    Caching/Quran/MushafReader/
      CachedAyahStudyReader.cs           # cache key includes existing source params
      CachedAyahSimilaritiesReader.cs
      CachedAyahMutashabihatReader.cs
      MushafReaderCacheKeys.cs           # add two detail keys if caching phase is included
    DependencyInjection.cs

  api/QuranDashboard.Api/
    Controllers/MushafReader/Ayahs/
      MushafAyahStudyController.cs       # existing endpoint returns summary counts
      MushafAyahSimilaritiesController.cs
      MushafAyahMutashabihatController.cs
    Common/ApiMessages.cs                # add/centralize Arabic feature messages if needed

  tests/QuranDashboard.Tests/Quran/MushafReader/
    AyahStudySimilaritySummaryTests.cs
    SimilarAyahsReadTests.cs
    SimilarAyahsValidationTests.cs
    AyahMutashabihatReadTests.cs
    AyahMutashabihatValidationTests.cs
    MushafReaderCacheTests.cs            # extend cache coverage for new reads

Frontend/quran-dashboard-ui/
  src/app/features/mushaf/
    components/
      selected-ayah-section/             # add two actions/tabs; keep component under thresholds
      similar-ayahs-card/                # flat list/empty/error rendering
      mutashabihat-groups-card/          # grouped rendering
      mutashabihat-group/                # optional split if template grows
    data-access/
      mushaf-ayah-study.api.ts           # AyahStudyDto gains similaritySummary
      mushaf-similar-ayahs.api.ts
      mushaf-ayah-mutashabihat.api.ts
    state/
      mushaf-reader.facade.ts            # lazy load two new action states; watch size threshold
      mushaf-reader-cache.ts             # add two cache keys
      mushaf-url-sync.ts                 # widen ayahTab values
    models/
      mushaf.models.ts                   # DTO/view/state models + widened AyahStudyTab

  src/app/features/mushaf/**/*.spec.ts   # frontend unit coverage for URL state, facade, components
```

**Structure Decision**: Reuse Feature 011's existing Mushaf Reader slice. Backend code remains in `Quran/MushafReader/` because this is selected-ayah reader context over Quran data, not a new import/data-foundation feature. Frontend code remains under `features/mushaf/`, extending the existing selected-ayah section rather than adding a new route. No Domain or database schema changes are planned.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| _(none)_ | - | - |

### Watch items (review thresholds, not violations)

- **`mushaf-reader.facade.ts`** may grow because it already owns page/ayah/word state. Mitigation: keep new behavior as two focused lazy-loading methods and split a similarity state helper only if it approaches the frontend soft threshold.
- **`selected-ayah-section.component.html`** may grow with five actions. Mitigation: extract `similar-ayahs-card`, `mutashabihat-groups-card`, and possibly a `selected-ayah-tabs` child component if the template approaches review thresholds.
- **EF read repositories** need careful joins for incoming/outgoing similar links and grouped occurrences. Mitigation: one read abstraction per resource and integration tests over real seeded fixtures.
