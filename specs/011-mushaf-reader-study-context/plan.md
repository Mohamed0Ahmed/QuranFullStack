# Implementation Plan: Mushaf Reader Study Context

**Branch**: `011-mushaf-reader-study-context` | **Date**: 2026-06-17 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `specs/011-mushaf-reader-study-context/spec.md`

> **Companion documents (source of truth):**
> `docs/feature-011-mushaf-reader-study-context/feature-011-mushaf-reader-study-context-planning-report.md`
> (locked planning report: scope + UX + API + caching + layout decisions), and
> `docs/feature-011-mushaf-reader-study-context/feature-011-ayah-word-data-capability-report.md`
> (data-capability report: every join path, column, and DTO field traces here).
> **Database baseline:** `Backend/report/database/current-database-tables-and-relationships-report.md`.
> **Governance:** `AGENTS.md`, `CODING_PRINCIPLES.md`, `Backend/CLAUDE.md`,
> `Backend/.architecture/{BACKEND_STRUCTURE,CLEAN_ARCHITECTURE,API_GUIDELINES}.md`,
> `Frontend/quran-dashboard-ui/CLAUDE.md`,
> `Frontend/quran-dashboard-ui/.architecture/{FRONTEND_STRUCTURE,API_INTEGRATION_GUIDELINES,UI_STYLE_SYSTEM}.md`,
> `PRODUCT.md`, `DESIGN.md`.

## Summary

Build a **read-only, full-stack dashboard Mushaf Reader** at the route `/dashboard/mushaf`. It renders one real Mushaf page from the already-seeded `quran_dashboard` database, supports page/surah navigation, and adds two lazy-loaded study contexts: a **selected-ayah study** (core ayah + tafsir + translation + full i3rab, the three sources loaded together) and a **selected-word analysis** (morphology + ordered/unique identity + glued color-linked segments with simple i3rab). The layout is **Mushaf on the right, a single wide study area on the left** (selected-word analysis on top, selected-ayah study on bottom), both visible on wide desktop. All important view state lives in the URL using natural Quran keys (`page`, `ayah=2:25`, `word=2:25:3`, `segment=2:25:3:1`, `panel`, `ayahTab`, `wordTab`, source keys).

Backend: three thin `GET` endpoints under `/api/mushaf/*` returning the existing `ApiResponse<T>` envelope, backed by Application queries/handlers and Infrastructure read repositories that query the seeded tables (no schema changes, no migrations, no writes). A minimal `IMemoryCache` layer is added **after** the read services and their tests are stable.

Frontend: an Angular 20 standalone smart **page shell** under `features/mushaf/` that composes child components, talks to a feature **facade/store** which calls feature **data-access** API services, maps `ApiResponse<T>` into page-ready state, handles loading/empty/error, deduplicates concurrent requests, keeps a bounded page/ayah/word cache, and renders tafsir/i3rab HTML **sanitized by default** (Angular's built-in `[innerHTML]` sanitizer; no `bypassSecurityTrustHtml`).

This plan also locks the **secure local environment** requirement that drove this feature's `/speckit-specify` input: both the backend API and the Angular dev server run over **HTTPS** locally, and every normal frontend data request targets the **HTTPS backend URL only** (`https://localhost:5015`). The backend HTTPS profile, HTTPS redirection, and CORS already exist; the concrete changes are (1) point the frontend `apiBaseUrl` at the HTTPS backend, (2) serve Angular over HTTPS, (3) restrict CORS to the HTTPS origin, and (4) add a small dev-time secure-URL guard so no request can target a non-HTTPS address.

All decisions are locked by the planning report and spec; there are **no open clarifications**.

## Technical Context

**Language/Version**: Backend C# 13 / .NET 10 (`net10.0`); Frontend TypeScript on Angular **20.3** (standalone APIs, no NgModules).
**Primary Dependencies**:
- Backend: ASP.NET Core controllers + Swagger (existing), EF Core 10 / Npgsql PostgreSQL provider (existing `QuranDashboardDbContext`), `Microsoft.Extensions.Caching.Memory` (`IMemoryCache`) for the cache phase, the existing `ApiResponse<T>` contract and `GlobalExceptionHandler`.
- Frontend: Angular Router, `provideHttpClient(withFetch())` (existing), RxJS 7.8, Tailwind/`qd-` style system (existing), Angular `DomSanitizer`/built-in `[innerHTML]` sanitization for safe HTML.

**Storage**: Existing PostgreSQL database `quran_dashboard`, **read-only** for this feature. No new tables, no new columns, **no migrations**, no data writes. All reads use existing tables and indexes documented in the database baseline (`quran_mushaf_pages`, `quran_mushaf_lines`, `quran_words`, `quran_ayahs`, `quran_surahs`, `quran_juzs`/`quran_hizbs`/`quran_rubs`/`quran_sajdas`, `quran_word_morphology`, `quran_word_morphology_segments`, `quran_roots`/`quran_lemmas`/`quran_stems`/`quran_pos_tags`/`quran_i3rab_rules`, `quran_words_ordered_*`/`quran_words_unique_*`, `quran_tafsir_*`, `quran_translation_*`, `quran_full_i3rab_*`).

**Testing**:
- Backend: xUnit + FluentAssertions + `Testcontainers.PostgreSql` for read integration tests (real Postgres with a seeded fixture), plus pure unit tests for marker-placement, default-source resolution, segment-fallback, and cache-key logic — in `Backend/tests/QuranDashboard.Tests`.
- Frontend: an Angular unit-test runner that **must be set up as a foundational step** — the project currently ships **no** test runner (no `test` script/target, no karma/jasmine/vitest) — e.g., `@angular/build:unit-test` (Vitest) or Karma+Jasmine, for facade/store, data-access, URL-state sync, and key presentational components (segment color linking, fixed-card scroll, sanitized rendering). See tasks.md T013.

**Target Platform**: Local developer environment over **HTTPS**. Backend Kestrel at `https://localhost:5015` (HTTP `5014` redirects to HTTPS); Angular dev server at `https://localhost:4200`. Backend on .NET 10 (Linux/macOS/Windows); frontend on Node + Angular CLI/`@angular/build`.

**Project Type**: Web application — an existing .NET Clean Architecture backend (`Backend/`: api/application/application.abstractions/domain/infrastructure/shared) plus an existing Angular dashboard (`Frontend/quran-dashboard-ui/`). Both are already scaffolded; this feature adds a feature slice to each.

**Performance Goals**: Page response is small and must stay lean (no tafsir/translation/i3rab/morphology in the page payload). Ayah-study and word-analysis are lazy and filtered by indexed source/ayah/word keys. Repeat reads are served from cache. User-facing targets are expressed in the spec's success criteria (SC-007 lazy initial load, SC-008 faster repeat access, SC-009 stable scrollable cards).

**Constraints**:
- Read-only DB; no schema/migration/import/data edits; never invent or mutate Quranic text.
- Mushaf text always from `quran_words.text_uthmani`; never reconstructed from segments; segment rendering only in the word-analysis panel.
- Ayah study returns the three selected/default sources **together** in v1; only one source per kind is loaded (never all sources).
- Default sources are configuration-driven (`MushafReader:DefaultTafsirSourceKey=ar-muyassar`, `MushafReader:DefaultTranslationSourceKey=en-sahih-international`, `MushafReader:DefaultFullI3rabSourceKey=muyassar`); a missing configured/selected source yields a clear empty/error state, never a silent substitution.
- Markers placed beside the related ayah; for multi-line ayahs use the first line on the current page.
- All view state in the URL via natural Quran keys; reload/deep-link reproducibility; on wide desktop `panel` is focus state, not exclusive hiding.
- Sanitized-by-default HTML; no default `bypassSecurityTrustHtml`; DB content never altered/stripped.
- **HTTPS everywhere locally; all normal frontend data calls target `https://localhost:5015` only — no HTTP/mixed-content.**
- Arabic-default user-facing messages via centralized message keys; English property/identifier names.
- File-size review thresholds (BACKEND_STRUCTURE / FRONTEND_STRUCTURE): keep the smart shell thin by splitting into child components + facade/store.

**Scale/Scope**: Data scale is fixed: 604 pages, 9,046 lines, 83,668 words (77,432 readable + 6,236 markers), 6,236 ayahs, 128,219 morphology segments. Delivery scope: **3 backend endpoints**, **1 frontend route** with ~16 components, ~3 data-access services, ~2 state services, plus the HTTPS environment changes. No new database rows are written.

*No unresolved clarification items. Every open choice is locked by the planning report and spec (layout right-Mushaf/left-study, three-sources-together ayah study, configured default source keys, sanitized-by-default HTML, cache-after-stabilization, HTTPS-only data calls). See [research.md](./research.md) for the resolved technical approaches.*

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

The project constitution (`.specify/memory/constitution.md`) is still an unratified placeholder template. As in Feature 009, the interim governance authority is the workspace/backend/frontend rule set: `AGENTS.md`, `CODING_PRINCIPLES.md`, `Backend/.architecture/{BACKEND_STRUCTURE,CLEAN_ARCHITECTURE,API_GUIDELINES}.md`, and `Frontend/quran-dashboard-ui/.architecture/{FRONTEND_STRUCTURE,API_INTEGRATION_GUIDELINES,UI_STYLE_SYSTEM}.md`. Do not infer additional MUST rules from the placeholder constitution.

| Gate | Status | Notes |
|---|---|---|
| Clean Architecture dependency direction | PASS | Domain unchanged (no new entities); Application owns the three read queries/handlers + read abstractions + default-source resolution; Infrastructure implements EF read repositories + cache decorators + options binding; Api is thin controllers mapping results to `ApiResponse<T>`. No EF/Infrastructure usage inside controllers. |
| Backend feature/domain foldering | PASS | New code lives under `Quran/MushafReader/` in each layer (e.g., `Application/Quran/MushafReader/Queries/...`). No global `Enums`/`Models`/`DTOs`/`Helpers`/`Services` dumping folders. |
| API boundary | PASS | Three resource-oriented `GET` routes (`/api/mushaf/pages/{pageNumber}`, `/api/mushaf/ayahs/{verseKey}/study`, `/api/mushaf/words/{wordLocation}/analysis`), `ApiResponse<T>` shape, centralized Arabic messages, controlled 200/400/404 responses, global exception handler, no EF entities exposed. |
| Contract/DTO placement | PASS | Query response records live with the use case in Application; controllers wrap them in `ApiResponse<T>`. No Domain/EF entities returned. |
| Read-only & Quran data safety | PASS | No writes, no migrations, no importers; Mushaf text only from `text_uthmani`; segment fallback never fabricates text; missing data → controlled empty/error state; no invented Quranic content in messages. |
| EF migration policy | N/A | No schema changes and no migrations in this feature. |
| Frontend structure | PASS | Routeable smart shell `mushaf-reader-page` composes child presentational components; `data-access/` API services, `state/` facade/store, `models/` feature models, `mushaf.routes.ts` lazy route; URL state for page/ayah/word/segment/panel/tabs/sources; file-size thresholds respected by the split. |
| Frontend API integration | PASS | Page → facade/store → API service → backend; `ApiResponse<T>` handled in the store; loading/empty/error via `qd-` state primitives; no fabricated Quran data; request dedupe + bounded cache. |
| HTTP/content safety | PASS | Tafsir/i3rab HTML rendered via Angular's built-in sanitizer (`[innerHTML]`, no bypass); DB content never altered. |
| Secure local environment (HTTPS) | PASS | Both apps over HTTPS; frontend `apiBaseUrl=https://localhost:5015`; CORS restricted to `https://localhost:4200`; dev-time secure-URL guard blocks any non-HTTPS data request; no silent HTTP fallback. |
| Localization | PASS | Arabic-default messages via feature-owned message keys (e.g., `MushafPages.InvalidPageNumber`, `Common.NotFound`); English identifiers/property names. |

**Post-design re-check:** PASS. The Phase 1 data model and contracts preserve every boundary above and introduce no justified violations (see [data-model.md](./data-model.md) and [contracts/](./contracts/)).

## Project Structure

### Documentation (this feature)

```text
specs/011-mushaf-reader-study-context/
├── spec.md
├── plan.md                      # this file
├── research.md                  # Phase 0 output
├── data-model.md                # Phase 1 output (read models + DTOs + view models)
├── quickstart.md                # Phase 1 output (run both apps over HTTPS + smoke test)
├── contracts/                   # Phase 1 output
│   ├── mushaf-page.api.md
│   ├── ayah-study.api.md
│   ├── word-analysis.api.md
│   ├── backend-read-abstractions.md
│   └── local-https-and-frontend-integration.md
├── checklists/
│   └── requirements.md          # from /speckit-specify
└── tasks.md                     # created later by /speckit-tasks (NOT by /speckit-plan)
```

### Source Code (repository root)

```text
Backend/
  domain/QuranDashboard.Domain/
    # NO CHANGES — feature is read-only over existing entities.

  application/QuranDashboard.Application.Abstractions/Quran/MushafReader/
    IMushafPageReader.cs                 # read one page (lines, words, markers, nav summary)
    IAyahStudyReader.cs                  # read one ayah + selected tafsir/translation/full-i3rab
    IWordAnalysisReader.cs               # read one word (morphology, identity, segments, i3rab)
    MushafReaderOptions.cs               # configured default source keys + validation
    MushafReaderMessages.cs              # feature message keys (Arabic-default)

  application/QuranDashboard.Application/Quran/MushafReader/Queries/
    GetMushafPage/
      GetMushafPageQuery.cs
      GetMushafPageHandler.cs
      GetMushafPageResponse.cs           # lean page DTO (no tafsir/translation/i3rab/morphology)
    GetAyahStudy/
      GetAyahStudyQuery.cs               # verseKey + optional tafsir/translation/fullI3rab source keys
      GetAyahStudyHandler.cs             # resolves defaults; loads 3 sources together
      GetAyahStudyResponse.cs
    GetWordAnalysis/
      GetWordAnalysisQuery.cs            # wordLocation
      GetWordAnalysisHandler.cs          # rejects ayah markers; builds color-linked segments
      GetWordAnalysisResponse.cs

  infrastructure/QuranDashboard.Infrastructure/
    Persistence/Reads/Quran/MushafReader/
      EfMushafPageReader.cs
      EfAyahStudyReader.cs
      EfWordAnalysisReader.cs
      MushafReaderSql.cs                 # shared read SQL/projection helpers if needed
    Caching/Quran/MushafReader/
      CachedMushafPageReader.cs          # IMemoryCache decorator (Phase 5 — after stabilization)
      CachedAyahStudyReader.cs
      CachedWordAnalysisReader.cs
      MushafReaderCacheKeys.cs
    DependencyInjection.cs               # register readers, options binding, cache decorators

  api/QuranDashboard.Api/
    Controllers/Mushaf/
      MushafPagesController.cs           # GET /api/mushaf/pages/{pageNumber}
      MushafAyahStudyController.cs       # GET /api/mushaf/ayahs/{verseKey}/study
      MushafWordAnalysisController.cs    # GET /api/mushaf/words/{wordLocation}/analysis
    Common/ApiMessages.cs                # extend with Mushaf message values (Arabic)
    appsettings.json                     # CORS → HTTPS origin only; MushafReader defaults
    appsettings.Development.json         # CORS → HTTPS origin only; MushafReader defaults
    Properties/launchSettings.json       # default to the existing "https" profile

  tests/QuranDashboard.Tests/Quran/MushafReader/
    MushafPageReadTests.cs               # pages 1/5/604, ordering, line/marker rules
    MushafPageValidationTests.cs         # invalid/out-of-range page → controlled result
    MarkerPlacementTests.cs              # first-line rule for multi-line ayahs
    AyahStudyReadTests.cs                # defaults + explicit sources; 3 sources together
    AyahStudyGroupedEntryTests.cs        # grouped/ranged tafsir/full-i3rab metadata
    AyahStudyMissingSourceTests.cs       # missing configured/selected source → empty/error
    WordAnalysisReadTests.cs             # normal word morphology + identity + segments
    WordAnalysisMarkerRejectionTests.cs  # ayah-end marker not analyzable
    WordAnalysisSegmentFallbackTests.cs  # empty segment form → placeholder, no fabrication
    MushafReaderCacheTests.cs            # cache hit after first read; no user-state caching
    MushafReaderTestFixture.cs           # seeded Postgres fixture (Testcontainers)

Frontend/quran-dashboard-ui/
  src/environments/
    environment.development.ts           # apiBaseUrl → https://localhost:5015
  angular.json                           # serve: ssl true + sslCert/sslKey (HTTPS dev server)
  package.json                           # add "start:https" script (ng serve --ssl ...)
  src/app/core/data-access/
    secure-url.interceptor.ts            # dev guard: block any non-HTTPS / non-apiBaseUrl data call
  src/app/shared/ui/safe-html/
    safe-html.pipe.ts                    # sanitized HTML pipe (built-in sanitizer; no bypass)
  src/app/features/mushaf/
    pages/mushaf-reader-page/
      mushaf-reader-page.component.{ts,html,scss}   # smart shell / orchestrator
    components/
      mushaf-header-navigation/
      mushaf-page-area/
      mushaf-page-view/
      mushaf-line/
      mushaf-word/
      mushaf-marker/
      selected-word-section/
      segment-rendered-word/
      word-morphology-summary/
      segment-data-rows/
      selected-ayah-section/
      tafsir-card/
      translation-card/
      full-i3rab-card/
      source-selector/
    data-access/
      mushaf-pages.api.ts
      mushaf-ayah-study.api.ts
      mushaf-word-analysis.api.ts
    state/
      mushaf-reader.facade.ts            # orchestration, URL<->state, loading/empty/error
      mushaf-reader-cache.ts             # bounded request cache + concurrent dedupe + prefetch
    models/
      mushaf.models.ts                   # DTOs + view models + reader state + URL keys
    mushaf.routes.ts                     # lazy feature routes (mounted at dashboard/mushaf)
  src/app/app.routes.ts                  # add lazy route 'dashboard/mushaf'
```

**Structure Decision**: Reuse both existing solutions. Backend code lives under `Quran/MushafReader/` in each Clean Architecture layer — beside the existing `Quran/...` domains — because page/ayah/word reading is a Quran-core read concern; the read side uses dedicated read interfaces + EF read repositories (read models, not the import/write repositories). Controllers stay thin and grouped under `Controllers/Mushaf/`. Frontend code lives under `features/mushaf/` following the FRONTEND_STRUCTURE "Mushaf Reader" split example, adapted to the locked right-Mushaf/left-study layout and trimmed of out-of-scope pieces (no audio, gates, ayah-doors, display-settings). The HTTPS requirement is realized through configuration + a small reusable secure-URL interceptor in `core/data-access`, not through feature code. No Domain changes and no migrations.

## Complexity Tracking

No constitution or architecture violations.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| _(none)_ | - | - |

### Watch items (review thresholds, not violations)

- **`mushaf-reader-page` smart shell** could approach the FRONTEND_STRUCTURE soft TS threshold (300 lines). Mitigation: it only orchestrates URL↔state and composes child components; all rendering lives in children and all orchestration/data logic lives in the facade/store and data-access services.
- **`mushaf-reader.facade.ts`** owns several state slices (selection, sources, lazy loads). Mitigation: keep selection/URL mapping cohesive; the bounded cache/dedupe lives in `mushaf-reader-cache.ts`; split further by slice only if it approaches the soft threshold (400 lines).
- **EF read repositories** may carry several joins. Mitigation: one reader per resource (page/ayah/word); keep each focused; they are read services (higher size threshold) and must not own unrelated data access.
