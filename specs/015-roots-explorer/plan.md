# Implementation Plan: Quran Roots Explorer

**Branch**: `015-roots-explorer` | **Date**: 2026-06-23 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `specs/015-roots-explorer/spec.md`

## Summary

Add a **read-only Quran Roots Explorer** at `/dashboard/words/roots`: a split-screen page with a
roots table (8 summary columns) in the main area and a **persistent, independently-scrolling details
side panel** (no modal) exposing tabs الكلمات (sub-views بدون تشكيل / بالتشكيل), الآيات, السور
(sub-views ورد فيها / لم يذكر فيها), الصيغ المعجمية, and الأصول الصرفية. It is a structural sibling of
Feature 014 (Unique Words Explorer) and reuses its proven backend read-model/cache/logging pattern
and its frontend highlighting, surah/ayah lists, count chip, shared pagination, API-response cache,
and URL-sync patterns.

The technical approach is fully pre-researched and verified by three prior reports (see
[research.md](./research.md)): the data exists, **no migration or index is needed**, the whole
1,642-root summary aggregates in ~30–115 ms (so the list uses **compute-once + cache-whole-list**),
all root-bearing words carry both unique-word links, and **lemmas use morphology co-occurrence
semantics** (`DISTINCT lemma_id` per root), which equals the precomputed `distinct_lemmas_count`.
The authoritative, fuller design lives in
`docs/feature-015-roots-explorer/feature-015-roots-explorer-combined-implementation-plan.md`; this
plan and its sibling artifacts are the Spec-Kit-shaped extract for implementation.

## Technical Context

**Language/Version**: Backend C# / .NET 10 (EF Core 10, `ProductVersion` 10.0.x); Frontend TypeScript / Angular 20 (standalone components, Signals).
**Primary Dependencies**: ASP.NET Core (controllers, `ApiResponse<T>`), EF Core 10 + Npgsql, shared `IMemoryCache`; Angular 20, RxJS, Angular CDK (`ScrollingModule`), SCSS + Tailwind tokens (`qd-*` classes), shared `qd-pagination` + shared deep-link helper.
**Storage**: PostgreSQL `quran_dashboard` — **read-only** for this feature (tables `quran_roots`, `quran_word_morphology`, `quran_words`, `quran_words_unique_simple/tashkeel`, `quran_lemmas`, `quran_stems`, `quran_ayahs`, `quran_surahs`). In-process `IMemoryCache` for cached reads.
**Testing**: Backend xUnit + Testcontainers PostgreSQL seeded from a committed embedded SQL slice (`roots-explorer-seed.sql`); `RecordingLoggerProvider` for log assertions; `SqlCommandCountInterceptor` for bounded-query / cache-hit assertions; real-run env escape hatch. Frontend Vitest (Angular unit-test builder) with the `VITEST_MAX_FORKS` worker cap (mandatory to avoid OOM); guard `matchMedia`/`ResizeObserver` absence (jsdom) and default desktop.
**Target Platform**: Linux server (ASP.NET Core API) + modern browser (Angular dashboard SPA), Arabic-first RTL.
**Project Type**: Web application — full-stack in a 3-repo submodule workspace (workspace `App`, `Backend`, `Frontend/quran-dashboard-ui`), all on branch `015-roots-explorer`.
**Performance Goals**: whole roots-summary aggregation ~30–115 ms (verified, cached once); list first page and each detail view visible < ~1 s on a normal connection; verse matches paginated (worst root ≈ 1,879 ayahs) with no full-page freeze; bounded query counts (no N+1) on ayah reads.
**Constraints**: strictly read-only (no writes/import/pipeline/Quran-text mutation); **no migration and no new index** (verification found none required — do not add without new measured evidence); reuse the already-registered shared `IMemoryCache` with a new `roots:` key namespace (no global cache reconfiguration); lemmas = co-occurrence only; no backend IDs shown in the UI; highlight by word identity (never string replacement); Arabic RTL-first; Quran text rendering stable/unanimated; keep frontend test worker cap.
**Scale/Scope**: 1,642 roots; ~50,298 root-bearing word occurrences (27,134 root-less words excluded); 6,236 ayahs; 114 surahs; 4,793 lemmas; 12,108 stems. ~8 read-only backend endpoints; 1 new Angular page + ~6 new components (+ reuse of ~6) + 2 facades + API service + cache wrapper + URL-sync + models.

No `NEEDS CLARIFICATION` remain: the five minor open questions from the combined plan are resolved as documented defaults in the spec's **Assumptions** (and recorded in [research.md](./research.md)).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

`.specify/memory/constitution.md` is an **unfilled template (placeholders only)**. In its absence the
authoritative governance for this workspace is used as the gate source: workspace `CLAUDE.md`,
`CODING_PRINCIPLES.md`, and the `.architecture/` docs (`BACKEND_STRUCTURE.md`,
`CLEAN_ARCHITECTURE.md`, `API_GUIDELINES.md`, `LOGGING_GUIDELINES.md`, `FRONTEND_STRUCTURE.md`,
`API_INTEGRATION_GUIDELINES.md`, `UI_STYLE_SYSTEM.md`) plus the `test-guard` / clean-code-guard
self-checks.

| Gate | Source | Status |
|---|---|---|
| Clean Architecture layering (Domain → Application → Infrastructure → Api; thin controller; no EF in controllers) | `CLEAN_ARCHITECTURE.md`, `BACKEND_STRUCTURE.md` | PASS — reader interface in Abstractions, handlers in Application, EF reader + cache decorator in Infrastructure, thin controller in Api (mirrors F014). |
| Feature/bounded-context foldering (no global Enums/DTOs/Helpers dumping) | `BACKEND_STRUCTURE.md` | PASS — all new types under `Quran/Words/Roots`. |
| API boundary (`ApiResponse<T>`, Arabic messages centralized, REST routes, controlled outcomes → 200/400/404, no internal table names in routes) | `API_GUIDELINES.md` | PASS — see [contracts/roots-api.md](./contracts/roots-api.md). |
| Logging/observability (structured templates, log once at boundary, no Quran/secret/raw-search text, no vendor change) | `LOGGING_GUIDELINES.md` | PASS — handler-boundary logs of IDs/counts/`hasSearch`/elapsed only. |
| Read-only & Quran data safety (no writes, no mutation, no invented text, missing → controlled state) | workspace `CLAUDE.md`, `API_GUIDELINES.md` | PASS — every operation read-only; highlight by word ID. |
| Migrations only when proven, via EF tooling, on explicit request | `Backend/CLAUDE.md` | PASS — none required (verified plans/timings). |
| Frontend structure (feature-first, file-size thresholds, tabs+URL state, facade/store vs data-access separation, shared `qd-` primitives) | `FRONTEND_STRUCTURE.md`, `UI_STYLE_SYSTEM.md` | PASS — new page is a thin shell; URL-state tabs; shared `qd-pagination`. |
| API integration (services return `ApiResponse<T>`; facade owns orchestration/loading/empty/error; components consume page-ready state) | `API_INTEGRATION_GUIDELINES.md` | PASS. |
| Test quality (behavior not implementation; real infra where correctness matters; Quranic test data source-safe; data-driven variants) | `test-guard`, workspace `CLAUDE.md` | PASS — Testcontainers + committed slice; interceptor for query-shape behavior. |
| Coding principles / clean-code guard | `CODING_PRINCIPLES.md` | PASS — reuse over duplication; small focused units. |

**Result: PASS — no violations.** Complexity Tracking is empty.

## Project Structure

### Documentation (this feature)

```text
specs/015-roots-explorer/
├── plan.md              # This file
├── research.md          # Phase 0 — decisions/rationale/alternatives (verified)
├── data-model.md        # Phase 1 — entities, fields, relationships, count rules
├── quickstart.md        # Phase 1 — how to build/run/test the feature
├── contracts/           # Phase 1
│   ├── roots-api.md                 # HTTP read endpoints + DTOs
│   ├── backend-read-abstractions.md # IRootsReader boundary + outcomes
│   └── frontend-routing-state.md    # route, query-param state, UX/highlight/a11y rules
├── checklists/
│   └── requirements.md  # spec quality checklist (/speckit-specify output)
└── tasks.md             # Phase 2 — NOT created by /speckit-plan (use /speckit-tasks)
```

### Source Code (repository root — 3-repo submodule workspace)

```text
Backend/                                   # .NET backend repo (submodule)
├── domain/QuranDashboard.Domain/Quran/Words/Morphology/        # existing entities (read-only): QuranRoot, QuranLemma, QuranStem, WordMorphology
├── application/QuranDashboard.Application.Abstractions/Quran/Words/Roots/
│   ├── IRootsReader.cs                     # NEW read boundary
│   ├── RootSort.cs / RootSortKeys.cs       # NEW (mushaf-order|occurrences|alpha)
│   ├── RootWordKind.cs / ...Keys.cs        # NEW (simple|tashkeel)
│   └── Responses/                          # NEW DTOs (see contracts)
├── application/QuranDashboard.Application/Quran/Words/Roots/Queries/
│   ├── GetRootsPage/ GetRootSummary/ GetRootWords/ GetRootAyahs/
│   ├── GetRootMentionedSurahs/ GetRootMissingSurahs/ GetRootLemmas/ GetRootStems/
│   │   └── {Query,Handler,Outcome}.cs each # NEW handlers (validation + logging)
├── infrastructure/QuranDashboard.Infrastructure/
│   ├── Persistence/Reads/Quran/Words/Roots/EfRootsReader.cs    # NEW EF reader (AsNoTracking)
│   ├── Caching/Quran/Words/Roots/{CachedRootsReader,RootsCacheKeys}.cs  # NEW decorator + keys
│   └── DependencyInjection/RootsDependencyInjection.cs         # NEW DI wiring
├── api/QuranDashboard.Api/Controllers/Words/RootsController.cs # NEW thin controller (api/words/roots)
└── tests/QuranDashboard.Tests/Quran/WordsRoots/                # NEW xUnit + Testcontainers + roots-explorer-seed.sql

Frontend/quran-dashboard-ui/               # Angular frontend repo (submodule)
└── src/app/features/words/
    ├── pages/roots-explorer-page/          # NEW split-screen shell
    ├── components/
    │   ├── roots-table/ root-details-panel/ root-words-list/ root-lemmas-list/ root-stems-list/  # NEW
    │   └── (reuse) highlighted-ayah/ ayah-matches-list/ surah-occurrences-list/ missing-surahs-list/ word-count-chip/
    ├── data-access/roots.api.ts            # NEW
    ├── state/{roots-explorer.facade,roots-detail.facade,roots-cache,roots-url-sync}.ts  # NEW
    ├── models/roots.models.ts              # NEW
    └── words.routes.ts                     # ADD roots route ; route-paths.ts ADD rootsRoutePath()
        # reuse: src/app/shared/ui/pagination (qd-pagination), src/app/core/caching/api-response-cache.ts,
        #        buildUniqueWordsDeepLink (unique-words-url-sync.ts)

App/ (workspace)                            # specs/, docs/, reports; submodule pointers
```

**Structure Decision**: Full-stack web application across the existing 3-repo submodule workspace.
Backend follows the F014 read-feature layering (Abstractions → Application handlers → Infrastructure
EF reader + cache decorator → thin Api controller), all under a new `Quran/Words/Roots` bounded
context. Frontend adds one routeable page + child components and two facades inside the existing
`features/words/` feature, reusing shared building blocks. No new top-level projects or features are
introduced.

## Complexity Tracking

No constitution violations — section intentionally empty.

## Phase 0 — Research

See [research.md](./research.md): consolidates the verified decisions (read-only feasibility,
no migration/index, compute-once + cache-whole-list, lemma co-occurrence, stems via morphology,
unique-word links present, highlight by word ID, F014 reuse) and the resolved defaults for the five
open questions. No unresolved `NEEDS CLARIFICATION`.

## Phase 1 — Design & Contracts

- [data-model.md](./data-model.md) — entities, the eight count rules (incl. lemma co-occurrence), and the read-only access shapes.
- [contracts/roots-api.md](./contracts/roots-api.md) — the 8 read endpoints, query/route params, DTO shapes, status codes, caching, logging.
- [contracts/backend-read-abstractions.md](./contracts/backend-read-abstractions.md) — `IRootsReader` boundary, validation ownership, outcome pattern, data-safety rules.
- [contracts/frontend-routing-state.md](./contracts/frontend-routing-state.md) — route, query-param state, count-click mapping, lazy-load rules, highlighting, accessibility, RTL/responsive.
- [quickstart.md](./quickstart.md) — build/run/test steps for backend + frontend.
- Agent context updated: the `<!-- SPECKIT START/END -->` block in workspace `CLAUDE.md` now points to this plan and its siblings.

**Post-Design Constitution Re-check**: PASS. The Phase 1 design introduces no new layering, cache,
logging, or data-safety risk: it stays within the F014-equivalent patterns, adds only a `roots:`
cache namespace, keeps all handlers read-only with controlled outcomes, and logs IDs/counts only.
Complexity Tracking remains empty.
