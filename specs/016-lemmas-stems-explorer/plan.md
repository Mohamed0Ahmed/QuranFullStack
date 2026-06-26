# Implementation Plan: Quran Lemmas & Stems Explorer

**Branch**: `016-lemmas-stems-explorer` | **Date**: 2026-06-25 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `specs/016-lemmas-stems-explorer/spec.md`

## Summary

Add two read-only morphology study pages inside the existing Words area:
`/dashboard/words/lemmas` and `/dashboard/words/stems`. Each page follows the implemented Feature 015
Roots Explorer pattern: a searchable/sortable/paginated summary table plus a persistent,
independently-scrolling detail panel with words, ayahs, surahs, type distribution, and the relevant
related morphology list. Cross-page root/lemma/stem/word/ayah links use stable identities and open in
new tabs; same-page list and panel state remains URL-driven in the current tab.

The backend uses explicit Lemmas and Stems bounded contexts rather than a generic morphology API:
Application.Abstractions read contracts and DTOs, Application query handlers/outcomes, Infrastructure
EF Core read models plus bounded cache decorators, and thin API controllers returning
`ApiResponse<T>`. The frontend adds two routeable pages under `features/words`, resource-specific API
services and URL-state helpers, thin page shells, presentation components, and list/detail facades.
Existing Roots/Unique Words/Mushaf components and deep-link helpers are reused where their contracts
already fit.

The capability report verified 4,793 lemmas and 12,108 stems, complete Arabic display values, valid
word connectivity, acceptable whole-summary aggregation costs (~217 ms lemmas / ~252 ms stems), and
no need for a migration or speculative index. The fuller implementation sequencing and file sketches
remain in
`docs/feature-016-lemmas-stems-explorer/feature-016-lemmas-stems-explorer-combined-implementation-plan.md`.

## Technical Context

**Language/Version**: Backend C# / .NET 10 (`net10.0`, EF Core 10.0.8); Frontend TypeScript 5.9 / Angular 20.3 standalone components and Signals.
**Primary Dependencies**: ASP.NET Core controllers and existing `ApiResponse<T>` envelope; EF Core 10 + Npgsql 10; shared `IMemoryCache`; Angular Router, RxJS 7.8, Angular CDK 20, SCSS + Tailwind 3.4, existing shared `qd-*` UI primitives and deep-link utilities.
**Storage**: Existing PostgreSQL `quran_dashboard`, read-only for this feature: `quran_lemmas`, `quran_stems`, `quran_roots`, `quran_word_morphology`, `quran_pos_tags`, `quran_words`, unique-word tables, `quran_ayahs`, and `quran_surahs`. In-process memory cache for bounded stable reads.
**Testing**: Backend xUnit + Testcontainers PostgreSQL with source-safe committed seed slices, existing command-count/cache/log-capture helpers, and targeted Mushaf DTO regression tests. Frontend Angular unit-test builder with Vitest 3 and the repository worker cap (`VITEST_MIN_FORKS=1`, `VITEST_MAX_FORKS=2`).
**Target Platform**: Linux-hosted ASP.NET Core API and modern-browser Angular SPA; Arabic-first RTL desktop workflow with responsive narrow-screen adaptation.
**Project Type**: Full-stack web application in the workspace root with Backend and Frontend child repositories/submodules.
**Performance Goals**: At least 95% of first catalogue pages visible within 1 second under normal conditions; active detail view visible within approximately 1 second; no N+1 ayah loading; paginated word/ayah reads; cached whole-summary lists derived once per process/reseed cycle.
**Constraints**: Strictly read-only; no migration/importer/data-pipeline/Quran-text mutation; no speculative index; canonical selection identity is numeric ID; no lexical or Quran text in logs; exact word-ID highlighting only; no generic morphology endpoint; no new visual system; cross-page study links open in new tabs; frontend route and detail state must restore safely.
**Scale/Scope**: 4,793 lemmas, 12,108 stems, 114 surahs; maximum verified lemma size 3,938 occurrences / 2,497 ayahs / 59 stems; maximum stem size 1,646 occurrences / 1,362 ayahs / 10 lemmas. Fourteen read-only morphology endpoints (seven per resource), two Angular pages, two Mushaf DTO identity additions, and additive Words hub/navigation updates.

No unresolved clarification marker remains. Product semantics, routes, URL state, count mapping,
dominant-type rules, null relationship behavior, linking behavior, and data readiness are locked by
the spec and the two Feature 016 planning reports.

## Constitution Check

*GATE: Formal constitution evaluation is unavailable while the file remains a template. The
workspace-governance gates below must pass before Phase 0 research and be re-checked after Phase 1
design.*

`.specify/memory/constitution.md` is an unfilled template, so formal constitution alignment is
**NOT EVALUABLE** for this feature. The enforceable workspace governance gates are the root
`AGENTS.md`,
`CODING_PRINCIPLES.md`, Backend and Frontend `AGENTS.md`, and the relevant `.architecture/` documents,
with `PRODUCT.md` and `DESIGN.md` governing frontend product/visual decisions.

| Gate | Source | Status |
|---|---|---|
| Clean Architecture dependency direction and thin API boundary | `Backend/.architecture/CLEAN_ARCHITECTURE.md`, `BACKEND_STRUCTURE.md`, `API_GUIDELINES.md` | PASS — explicit read interfaces/DTOs in Application.Abstractions; handlers in Application; EF/cache/DI in Infrastructure; controllers only bind, call, and map outcomes. |
| Domain/feature-first placement with no technical dumping folders | `BACKEND_STRUCTURE.md`, `FRONTEND_STRUCTURE.md` | PASS — backend stays under `Quran/Words/Lemmas` and `Quran/Words/Stems`; frontend remains under `features/words`. |
| Stable and localized API contract | `API_GUIDELINES.md` | PASS — resource-oriented GET routes, English properties, centralized Arabic messages, controlled 200/400/404 outcomes, `ApiResponse<T>`. |
| Safe structured observability | `LOGGING_GUIDELINES.md` | PASS — Application logs IDs, operation, paging, counts, booleans, and measured duration; never Quran/lexical/raw-search text or payloads. |
| Quranic data safety and read-only behavior | `AGENTS.md`, `CODING_PRINCIPLES.md` | PASS — no writes, invented text, normalization of stored Quran content, migration, importer, or pipeline. |
| Evidence before schema/index changes | Backend `AGENTS.md` | PASS — capability analysis found no migration or index requirement; both are out of scope absent new measured evidence and explicit approval. |
| Frontend state and API separation | `FRONTEND_STRUCTURE.md`, `API_INTEGRATION_GUIDELINES.md` | PASS — routeable shells compose child components; facades own URL/API/loading state; API services return typed `ApiResponse<T>`. |
| Existing design system and RTL behavior | `PRODUCT.md`, `DESIGN.md`, `UI_STYLE_SYSTEM.md` | PASS — reuse Roots layout and `qd-*` primitives; no new palette/tokens; logical RTL layout; stable unanimated Quran text. |
| Accessible and inspectable navigation | Feature spec, frontend architecture | PASS — count controls/tabs/rows are keyboard-operable; cross-page destinations are real safe new-tab anchors; active state is non-color-only. |
| Test quality and source safety | `CODING_PRINCIPLES.md`, test-code self-check rules | PASS — behavior-focused tests, real PostgreSQL for query correctness, source-safe Quran slices, data-driven validation variants. |
| Focused implementation without premature generic abstractions | `CODING_PRINCIPLES.md` | PASS — explicit Lemmas/Stems contracts; shared extraction only for already-proven truly common UI/DTO primitives. |

**Pre-design workspace-governance result: PASS.** Formal constitution status remains **NOT
EVALUABLE** until the project adopts a completed constitution. No workspace-governance violation
requires Complexity Tracking.

## Project Structure

### Documentation (this feature)

```text
specs/016-lemmas-stems-explorer/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── morphology-explorer-api.md
│   ├── backend-read-abstractions.md
│   └── frontend-routing-state.md
├── checklists/
│   └── requirements.md
└── tasks.md                 # Created later by /speckit-tasks, not by this command
```

### Source Code (repository root)

```text
Backend/
├── application/QuranDashboard.Application.Abstractions/Quran/
│   ├── Words/Lemmas/
│   │   ├── ILemmasReader.cs
│   │   ├── LemmaSort.cs
│   │   ├── LemmaWordKind.cs
│   │   └── Responses/
│   ├── Words/Stems/
│   │   ├── IStemsReader.cs
│   │   ├── StemSort.cs
│   │   ├── StemWordKind.cs
│   │   └── Responses/
│   └── MushafReader/Responses/WordAnalysisResponse.cs          # Add lemma/stem IDs
├── application/QuranDashboard.Application/Quran/Words/
│   ├── Lemmas/Queries/
│   │   ├── GetLemmasPage/
│   │   ├── GetLemmaSummary/
│   │   ├── GetLemmaWords/
│   │   ├── GetLemmaAyahs/
│   │   ├── GetLemmaMentionedSurahs/
│   │   ├── GetLemmaMissingSurahs/
│   │   └── GetLemmaStems/
│   └── Stems/Queries/
│       ├── GetStemsPage/
│       ├── GetStemSummary/
│       ├── GetStemWords/
│       ├── GetStemAyahs/
│       ├── GetStemMentionedSurahs/
│       ├── GetStemMissingSurahs/
│       └── GetStemLemmas/
├── infrastructure/QuranDashboard.Infrastructure/
│   ├── Persistence/Reads/Quran/
│   │   ├── Words/Lemmas/
│   │   ├── Words/Stems/
│   │   └── MushafReader/EfWordAnalysisReader.cs                # Map additive IDs
│   ├── Caching/Quran/Words/Lemmas/
│   ├── Caching/Quran/Words/Stems/
│   └── DependencyInjection/
│       ├── LemmasDependencyInjection.cs
│       └── StemsDependencyInjection.cs
├── api/QuranDashboard.Api/
│   ├── Controllers/Words/LemmasController.cs
│   ├── Controllers/Words/StemsController.cs
│   └── Common/ApiMessages.cs
└── tests/QuranDashboard.Tests/Quran/
    ├── WordsMorphologyExplorers/                              # Shared Feature 016 fixture/tests
    └── MushafReader/                                          # DTO identity regression

Frontend/quran-dashboard-ui/
└── src/app/
    ├── core/navigation/
    │   └── route-paths.ts
    ├── features/words/
    │   ├── pages/
    │   │   ├── lemmas-explorer-page/
    │   │   └── stems-explorer-page/
    │   ├── components/
    │   │   ├── lemmas-table/
    │   │   ├── stems-table/
    │   │   ├── lemma-details-panel/
    │   │   ├── stem-details-panel/
    │   │   ├── lemma-words-list/
    │   │   ├── stem-words-list/
    │   │   ├── lemma-stems-list/
    │   │   ├── stem-lemmas-list/
    │   │   └── type-distribution-list/
    │   ├── data-access/
    │   │   ├── lemmas.api.ts
    │   │   └── stems.api.ts
    │   ├── models/
    │   │   ├── lemmas.models.ts / lemmas.labels.ts
    │   │   └── stems.models.ts / stems.labels.ts
    │   ├── state/
    │   │   ├── lemmas-url-sync.ts / stems-url-sync.ts
    │   │   ├── lemmas-explorer.facade.ts / stems-explorer.facade.ts
    │   │   ├── lemmas-detail.facade.ts / stems-detail.facade.ts
    │   │   └── bounded cache/load helpers split when thresholds require
    │   └── words.routes.ts
    └── features/mushaf/
        ├── models/mushaf.models.ts
        └── components/
            ├── selected-word-section/
            └── word-morphology-summary/
```

**Structure Decision**: Keep both resources inside the existing Quran Words bounded context while
retaining explicit contracts and handlers per resource. Backend placement mirrors implemented Roots,
with no new Domain entities because the existing morphology model is authoritative. Frontend adds two
routeable sibling pages and resource-specific state/data-access files under `features/words`; existing
shared list/highlight/surah/pagination/deep-link components are reused. If detail facades approach the
400-line soft threshold, split loader/update helpers following the implemented Roots pattern rather
than allowing oversized generated services.

## Complexity Tracking

No constitution violations. Section intentionally empty.

## Phase 0 — Research

[research.md](./research.md) records the resolved decisions:

- existing data is sufficient and remains read-only;
- numeric IDs are canonical identities;
- explicit Lemmas/Stems APIs are safer than a generic morphology endpoint;
- whole-summary compute-once caching is bounded and evidence-supported;
- dominant type and dominant stem relationships use count then Mushaf-order tie-breaks;
- exact ayah highlighting uses Quran word IDs;
- no migration/index/importer is required;
- Roots/Unique Words/Mushaf reuse and new-tab linking behavior are fixed;
- tests use real PostgreSQL query behavior and source-safe fixtures.

No unresolved research item remains.

## Phase 1 — Design & Contracts

- [data-model.md](./data-model.md): read models, relationships, count rules, validation, and state transitions.
- [contracts/morphology-explorer-api.md](./contracts/morphology-explorer-api.md): fourteen GET endpoints, response DTOs, status codes, caching, logging, and additive Mushaf identity contract.
- [contracts/backend-read-abstractions.md](./contracts/backend-read-abstractions.md): `ILemmasReader` / `IStemsReader`, validation ownership, outcomes, query rules, and data-safety constraints.
- [contracts/frontend-routing-state.md](./contracts/frontend-routing-state.md): routes, query state, count mapping, deep links, lazy loading, responsive/a11y behavior, and Mushaf integration.
- [quickstart.md](./quickstart.md): build, test, run, and acceptance checkpoints.
- `AGENTS.md`: active Spec Kit context updated to Feature 016 and this plan.

**Post-design workspace-governance re-check: PASS.** Formal constitution alignment remains **NOT
EVALUABLE** because the constitution is still an unfilled template. Phase 1 preserves layer
boundaries, explicit resource ownership, API envelope/localization, safe logging, URL-driven state,
source-safe tests, and read-only Quran data handling. It introduces no schema change, external
dependency, global cache configuration, visual-system change, or premature shared abstraction.
Complexity Tracking remains empty.
