# Implementation Plan: Quran Word Types Explorer

**Branch**: `019-word-types-explorer` | **Date**: 2026-06-30 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `specs/019-word-types-explorer/spec.md`

## Summary

Add a read-only **Word Types Explorer** for the existing Words hub at `/dashboard/words/types`.
The page groups Quran words by their main grammatical type: اسم, فعل, حرف وأداة, and حروف
مقطعة. It is table-first: a compact type filter picker, a paged word-context table, and an
inline-end selected-row details panel with ayahs, surah distribution, and per-occurrence analysis.

The technical approach uses the existing word-level morphology read model only:
`quran_word_morphology` joined to `quran_words` and `quran_pos_tags`. It adds a separate
WordTypes read area rather than overloading Unique Words. Counts are intentionally split into two
families: static tree/filter counts are distinct word-context row counts for unscoped type/child
nodes, while table totals/columns are scoped to the active filters and exact selected row context. No data write,
importer, migration, or new index is part of this feature.

## Technical Context

**Language/Version**: Backend C# / .NET 10 (`net10.0`, EF Core 10.x); Frontend TypeScript 5.9 / Angular 20 standalone components and Signals.  
**Primary Dependencies**: ASP.NET Core controllers and existing `ApiResponse<T>` envelope; EF Core + Npgsql; existing shared `IMemoryCache`; Angular Router, RxJS, Angular CDK, SCSS + Tailwind/`qd-*` primitives, existing Words explorer utilities/components.  
**Storage**: Existing PostgreSQL `quran_dashboard`, read-only for this feature: `quran_word_morphology`, `quran_pos_tags`, `quran_words`, `quran_words_unique_tashkeel`, `quran_roots`, `quran_lemmas`, `quran_stems`, `quran_ayahs`, and `quran_surahs`.  
**Testing**: Backend xUnit + Testcontainers PostgreSQL/source-safe seed slices, cache/log/query-count helpers where relevant; Frontend Angular unit-test builder with Vitest and the repository worker cap (`VITEST_MIN_FORKS=1`, `VITEST_MAX_FORKS=2`).  
**Target Platform**: Linux-hosted ASP.NET Core API and modern-browser Angular SPA; Arabic-first RTL dashboard with responsive narrow-screen adaptation.  
**Project Type**: Full-stack web application in the workspace root with Backend and Frontend child repositories/submodules.  
**Performance Goals**: Selecting any main type shows a populated first page within the spec target of 2 seconds; table/details avoid N+1 ayah loading; tree/list reads are cacheable and bounded; default page size is small (25 rows).  
**Constraints**: Strictly read-only; no migrations/importers/source-data mutation; no segment/prefix/suffix POS in counts; no pre-aggregated unique-word counts for type-scoped metrics; particle parent must exclude `INL`; Quran/word/search text is not logged; display stays Uthmani with tashkeel only; route state must restore the exact word-context row.  
**Scale/Scope**: Four main type buckets, all noun-category POS child nodes from the catalogue (currently `N`, `PN`, `ADJ`, `PRON`, `REL`, `DEM`, `T`, `LOC`, `TIM`, `IMPN`), three verb tense child nodes, no particle child breakdown in v1, five new read-only Word Types endpoints plus reuse of the existing per-word analysis endpoint, one Angular routeable page with focused child components/state/data-access files.

No unresolved `NEEDS CLARIFICATION` marker remains. The spec and research resolve the remaining implementation defaults, including defaulting an empty route to the `noun` main type and verifying the `PRO` POS row before implementation.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

`.specify/memory/constitution.md` is still an unfilled template with placeholders, so formal
constitution compliance is **not evaluable**. Practical governance for this plan comes from
`AGENTS.md`, `CODING_PRINCIPLES.md`, `Backend/AGENTS.md`, `Frontend/quran-dashboard-ui/AGENTS.md`,
Backend/API architecture docs, frontend API-integration rules, product/design context, and the
test/clean-code self-check guidance named by the workspace instructions.

| Gate | Source | Status |
|---|---|---|
| Clean Architecture layering | `Backend/AGENTS.md`, backend architecture rules | PASS — new read boundary in Abstractions, handlers in Application, EF/cache in Infrastructure, thin Api controller. |
| API boundary consistency | `Backend/.architecture/API_GUIDELINES.md` | PASS — read-only `GET` endpoints under `api/words/word-types`, `ApiResponse<T>` envelope, controlled `400`/`404` outcomes, centralized Arabic messages. |
| Quran data safety | `AGENTS.md`, `CODING_PRINCIPLES.md` | PASS — no writes, no importer, no invented Quran data, no frontend fallback text, marker words excluded. |
| Read-model correctness | Feature 019 spec/research | PASS — word-level `WordMorphology` only; no segment joins; no pre-aggregated all-usage unique-word counts for scoped metrics. |
| Scope and YAGNI | `CODING_PRINCIPLES.md` | PASS — particle child breakdown and lemma/stem enrichment are additive/deferrable, while catalogue-defined nominal children are included in v1. |
| Frontend structure and API integration | `Frontend/quran-dashboard-ui/AGENTS.md`, `.architecture/API_INTEGRATION_GUIDELINES.md` | PASS — routeable page delegates to facades; API service is typed; child components are presentational; URL state owns selection. |
| Product/design fit | `PRODUCT.md`, `DESIGN.md` | PASS — Arabic-first RTL, scholarly/calm, existing Words explorer visual language, no visual-system change. |
| Testing quality | `test-guard` guidance via `AGENTS.md` | PASS — behavior-focused backend/frontend tests; real infrastructure where grouped query correctness matters; no invented Quran content. |

**Pre-design workspace-governance result: PASS.** Formal constitution status remains **NOT
EVALUABLE** until the project adopts a completed constitution. This is accepted for Feature 019 and
does not block implementation. No violation requires Complexity Tracking.

## Project Structure

### Documentation (this feature)

```text
specs/019-word-types-explorer/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── word-types-api.md
│   ├── backend-read-abstractions.md
│   └── frontend-routing-state.md
├── checklists/
│   └── requirements.md
└── tasks.md                 # Created later by /speckit-tasks, not by this command
```

### Source Code (repository root)

```text
Backend/
├── application/QuranDashboard.Application.Abstractions/Quran/Words/WordTypes/
│   ├── IWordTypesReader.cs
│   ├── WordTypeFilter.cs
│   ├── WordTypeSort.cs
│   ├── WordTypeRowIdentity.cs
│   └── Responses/
│       ├── WordTypeTreeDto.cs
│       ├── WordTypeRowDto.cs
│       ├── WordTypeSummaryDto.cs
│       ├── WordTypeAyahMatchDto.cs
│       └── WordTypeSurahsResponse.cs
├── application/QuranDashboard.Application/Quran/Words/WordTypes/Queries/
│   ├── GetWordTypeTree/
│   ├── GetWordTypeRows/
│   ├── GetWordTypeSummary/
│   ├── GetWordTypeAyahs/
│   └── GetWordTypeSurahs/
├── infrastructure/QuranDashboard.Infrastructure/
│   ├── Persistence/Reads/Quran/Words/WordTypes/
│   │   ├── EfWordTypesReader.cs
│   │   └── WordTypeGrouping.cs
│   ├── Caching/Quran/Words/WordTypes/
│   │   ├── CachedWordTypesReader.cs
│   │   └── WordTypesCacheKeys.cs
│   └── DependencyInjection/WordTypesDependencyInjection.cs
├── api/QuranDashboard.Api/
│   ├── Controllers/Words/WordTypesController.cs
│   └── Common/ApiMessages.cs
└── tests/QuranDashboard.Tests/Quran/WordsWordTypes/

Frontend/quran-dashboard-ui/
└── src/app/
    ├── core/navigation/route-paths.ts
    └── features/words/
        ├── pages/word-types-explorer-page/
        ├── components/
        │   ├── word-type-filter/
        │   ├── word-types-table/
        │   └── word-type-details-panel/
        ├── data-access/word-types.api.ts
        ├── models/word-types.models.ts / word-types.labels.ts
        ├── state/
        │   ├── word-types-cache.ts
        │   ├── word-types-url-sync.ts
        │   ├── word-types-explorer.facade.ts
        │   └── word-types-detail.facade.ts
        ├── utils/word-type-ayah-match.mapper.ts
        └── words.routes.ts
```

**Structure Decision**: Add a new explicit `WordTypes` bounded context inside the existing Quran
Words feature. Backend placement mirrors Roots/Lemmas/Stems and stays read-only. Frontend adds one
routeable sibling page under `features/words`, reusing existing table keyboard/focus utilities,
highlighted ayah, ayah/surah list, missing-surah list, count-chip, pagination, cache, and URL-sync
patterns where their contracts fit. No new top-level projects or global abstractions are introduced.

## Complexity Tracking

No workspace-governance violations. Section intentionally empty.

## Phase 0 — Research

[research.md](./research.md) records the resolved decisions:

- word-level `quran_word_morphology` is the only source for types, filters, and counts;
- the four main buckets are derived from `quran_pos_tags.category`, `IsVerb`, and `HeadPos`, with
  `INL` excluded from the particle parent;
- counts are recomputed and cached, never read from all-usage unique-word aggregates; E1 tree counts
  stay unscoped by secondary filters while table totals/details honor active filters;
- v1 ships every noun-category POS child from the catalogue and the three verb tense children;
- root enrichment ships first, while lemma/stem enrichment is deferrable if winner queries are not
  low-risk;
- `contextCode` is mandatory to address a word-context row across endpoints and URL state;
- Uthmani-with-tashkeel is the only display mode;
- `PRO` catalogue data must be verified before implementation;
- empty route state defaults to `noun`.

No unresolved research item remains.

## Phase 1 — Design & Contracts

- [data-model.md](./data-model.md): existing read-only entities, derived tree/row concepts, grouping
  keys, count semantics, winner enrichment, validation rules, and URL-state notes.
- [contracts/word-types-api.md](./contracts/word-types-api.md): five read-only HTTP endpoints,
  `ApiResponse<T>`/`PagedResult<T>` response contracts, status codes, row-context rules, and reuse of
  existing word analysis.
- [contracts/backend-read-abstractions.md](./contracts/backend-read-abstractions.md): `IWordTypesReader`
  boundary, filter/value objects, validation ownership, outcomes, query rules, caching, and logging.
- [contracts/frontend-routing-state.md](./contracts/frontend-routing-state.md): route, query params,
  normalization, selection/deep-link behavior, loading rules, accessibility, and required frontend
  tests.
- [quickstart.md](./quickstart.md): build/test/run commands and acceptance checkpoints.
- `AGENTS.md`: active Spec Kit context updated to Feature 019 and this plan.

**Post-design workspace-governance re-check: PASS.** Formal constitution alignment remains **NOT
EVALUABLE** because the constitution is still an unfilled template. Phase 1 preserves layer
boundaries, explicit resource ownership, API envelope/localization, safe logging, URL-driven state,
source-safe tests, and read-only Quran data handling. It introduces no schema change, external
dependency, global cache configuration, visual-system change, or premature shared abstraction.
Complexity Tracking remains empty.
