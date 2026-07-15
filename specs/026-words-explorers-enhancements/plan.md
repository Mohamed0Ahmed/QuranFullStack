# Implementation Plan: Words Explorers Enhancements (Word Types Parity, Filters, Statistics)

**Branch**: `026-words-explorers-enhancements` | **Date**: 2026-07-14 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `specs/026-words-explorers-enhancements/spec.md`
(clarified 2026-07-14) + the authoritative decision record at
`docs/feature-026-words-explorers-enhancements/plan.md`.

> **Precedence**: the docs/ plan is the locked decision record (Locked Decisions A1–D,
> non-goals, stop conditions). This file operationalizes it for Spec Kit execution and
> folds in the clarification outcomes. Precedence order on conflict:
> (1) the docs plan's locked decision **substance** (what to build, invariants,
> non-goals, stop conditions); (2) the spec's `## Clarifications` and terminology
> edits, which **supersede the docs plan's illustrative values** (bucket examples,
> Arabic label examples in lock D); (3) this file, which must be updated to match,
> not argued with. The docs plan itself stays untouched as the historical record.

## Summary

Three workstreams over the five read-only Words explorers:

- **A — Word Types parity**: word-identity search (scopes ALL table views and the new
  scope counts), 1000-row list pages with virtual scrolling, 100-item detail pages.
- **B — Filters**: count-range filters (preset buckets + custom min/max) on Unique
  Words/Roots/Lemmas/Stems; tri-state has-root/has-stem/has-lemma on Word Types;
  association filters (unique words by primary word type / primary root, lemmas by
  root FK, stems by *primary* root/lemma).
- **C — Statistics**: headline result-count ("عدد الـ…: N") on the four normal
  explorers surfacing the existing filtered `TotalCount`; a scoped four-count summary
  strip (tabs' short labels: كلمات | جذور | أصول | صيغ) on Word Types served by ONE
  new read that reuses the grouped-count SQL fragments.

Read-only throughout: no schema, no migrations, no importers, no new packages.

## Technical Context

**Language/Version**: Backend C# / .NET 10 (`net10.0`, EF Core 10 + Npgsql); Frontend TypeScript / Angular 20 standalone components + Signals.
**Primary Dependencies**: `ApiResponse<T>` + `PagedResult<T>` envelope; backend `IMemoryCache` reader decorators (`Infrastructure/Caching/Quran/Words/**`); Angular Router, RxJS `debounceTime`, Angular CDK scrolling; frontend `ApiResponseCache`; existing Words explorer component/util set.
**Storage**: existing PostgreSQL, read-only. Tables: `quran_word_morphology` (77,432), `quran_words`, `quran_words_unique_tashkeel` (21,294), `quran_words_unique_simple` (14,783), `quran_roots` (1,642), `quran_lemmas` (4,790), `quran_stems` (12,108), `quran_ayahs`, `quran_surahs`.
**Testing**: Backend xUnit + Testcontainers, source-safe seed slices (`Backend/tests/QuranDashboard.Tests/Quran/{Words,WordsRoots,WordsMorphologyExplorers,WordsWordTypes}`); Frontend Vitest under the repo worker cap.
**Target Platform**: Arabic-first RTL dashboard; scholarly-calm register (PRODUCT.md / DESIGN.md).
**Performance Goals**: 1000-row Word Types page scrolls without jank; `GroupedRowsSql` and the scope-counts read measured at worst scopes; scope counts = 1 SQL command; existing detail command budgets unchanged (grouped ayahs 3, grouped surahs 2).
**Constraints**: count-family invariant HARD (scoped word-context vs global `words_count`-backed — never mixed); URL params are user-facing contracts (fail-closed parsing); identity = clean imlaei-simple, Uthmani display-only; search text never logged; no filter may need schema (stop condition).
**Scale/Scope**: 5 pages, 6 backend read areas, 1 new endpoint, 0 migrations. All `NEEDS CLARIFICATION` resolved — see spec `## Clarifications` (buckets, stat phrasing, strip placement) and `research.md`.

## Constitution Check

*GATE: pass before Phase 0; re-checked after Phase 1 design — result unchanged.*

`.specify/memory/constitution.md` is still the unfilled template → formal compliance
**NOT EVALUABLE** (same accepted status as features 015–019). Practical governance:

| Gate | Source | Status |
|---|---|---|
| Clean Architecture layering | `Backend/CLAUDE.md`, `.architecture/BACKEND_STRUCTURE.md`, `CLEAN_ARCHITECTURE.md` | PASS — params/DTO in Abstractions, validation in Application, SQL/cache in Infrastructure, thin controllers. |
| API boundary | `Backend/.architecture/API_GUIDELINES.md` | PASS — GET-only, `ApiResponse<T>`, centralized Arabic messages, controlled 400s. |
| Quran data safety | root `CLAUDE.md`, `CODING_PRINCIPLES.md` | PASS — no writes, no invented content, canonical `text_uthmani` hydration untouched. |
| Count-family invariant | reads README (`Persistence/Reads/Quran/Words/README.md`) | PASS by design — scope counts reuse grouped SQL fragments only; C1 surfaces existing totals. |
| Frontend structure / API integration | `.architecture/FRONTEND_STRUCTURE.md`, `API_INTEGRATION_GUIDELINES.md` | PASS — page → facade → api; URL owns filter state; presentational children. |
| URL-state contract discipline | `features/words/README.md` | PASS — every new key: fail-closed parse + spec + README same commit. |
| Product/design fit | `PRODUCT.md`, `DESIGN.md` | PASS — quiet chips/stat lines, no new visual system, lock-D labels. |
| Scope / YAGNI | `CODING_PRINCIPLES.md` | PASS — explicit non-goals; counts only, no aggregations. |

**Post-design re-check (Phase 1)**: no new violations introduced by data-model or
contracts; still PASS / NOT EVALUABLE. No Complexity Tracking entries needed.

## Project Structure

### Documentation (this feature)

```text
specs/026-words-explorers-enhancements/
├── plan.md              # This file
├── spec.md              # Clarified spec (2026-07-14)
├── research.md          # Phase 0 — decisions, rationale, alternatives
├── data-model.md        # Phase 1 — entities, params, DTOs, validation
├── quickstart.md        # Phase 1 — build/test/smoke per phase
├── contracts/           # Phase 1
│   ├── word-types-api.md
│   ├── explorers-filters-api.md
│   └── frontend-url-state.md
└── tasks.md             # Created later by /speckit-tasks

docs/feature-026-words-explorers-enhancements/
└── plan.md              # Authoritative decision record (do not edit here)
```

### Source Code (repository root)

```text
Backend/
├── api/QuranDashboard.Api/
│   ├── Controllers/Words/
│   │   ├── WordTypesController.cs                 # search param, page-size defaults, scope-counts action
│   │   ├── WordTypeGroupedDetailsController.cs    # detail default 25→100
│   │   ├── UniqueWordsController.cs               # range + association params
│   │   └── RootsController.cs / LemmasController.cs / StemsController.cs  # range (+ association) params
│   └── Common/ApiMessages.cs                      # new Arabic messages
├── application/QuranDashboard.Application/Quran/Words/
│   ├── WordTypes/Queries/ (GetWordTypeRows, GetWordTypeTable, WordTypesHandlerValidation,
│   │                        + NEW GetWordTypeScopeCounts)
│   ├── Queries/GetUniqueWordsPage/                # filter params + validation
│   └── Roots|Lemmas|Stems/Queries/Get*sPage/      # filter params + validation
├── application/QuranDashboard.Application.Abstractions/Quran/Words/
│   ├── WordTypes/ (IWordTypesReader, + NEW Responses/WordTypeScopeCountsDto.cs)
│   └── IUniqueWordsReader.cs, Roots/…, Lemmas/…, Stems/…
├── infrastructure/QuranDashboard.Infrastructure/
│   ├── Persistence/Reads/Quran/Words/
│   │   ├── WordTypes/EfWordTypesReader.cs/.Sql.cs (+ NEW .ScopeCounts.cs partial)
│   │   ├── EfUniqueWordsReader.cs                 # SQL range/association predicates
│   │   ├── Roots/RootsListDerivation.cs (+ lemma/stem derivations)  # in-memory predicates
│   │   └── (shared Arabic-normalize helper extracted here)
│   └── Caching/Quran/Words/ (WordTypesCacheKeys, CachedWordTypesReader,
│                              UniqueWordsCacheKeys, CachedUniqueWordsReader)
└── tests/QuranDashboard.Tests/Quran/{Words,WordsRoots,WordsMorphologyExplorers,WordsWordTypes}/

Frontend/quran-dashboard-ui/src/app/
├── features/words/
│   ├── models/*.models.ts / *.labels.ts           # new URL keys, buckets, page-size consts, labels
│   ├── state/*-url-sync.ts (+specs), *-cache.ts, *-explorer.facade.ts, unique-words.facade.ts
│   ├── data-access/*.api.ts (+specs)
│   ├── components/
│   │   ├── word-types-table/                      # CdkVirtualScrollViewport
│   │   ├── NEW explorer-count-range-filter/       # bucket chips + مخصّص min/max
│   │   ├── NEW explorer-result-count/             # عدد الـ…: N
│   │   └── NEW word-type-scope-counts/            # four-count strip
│   └── pages/*-page/                              # toolbar wiring
└── core/api/generated/                            # regenerated client (scope-counts DTO)
```

**Structure Decision**: full-stack change inside the existing Words feature boundaries
on both sides; no new areas, no moves. New backend read follows the existing
WordTypes partial-split convention; new frontend components are presentational
children of the existing pages.

## Clarified values baked into this plan

| Item | Decision (spec `## Clarifications`) |
|---|---|
| Preset buckets (disjoint) | occurrences 1 · 2–10 · 11–100 · 101–1000 · 1001+; ayahs/surahs 1 · 2–10 · 11–50 · 51+ (≤114); word/lemma/stem sub-counts 1 · 2–5 · 6–20 · 21+; plus مخصّص custom. URL stores the actual range. |
| Result-count phrasing | Label-prefix: عدد الكلمات / عدد الجذور / عدد الصيغ المعجمية / عدد الأصول الصرفية: N |
| Four-count strip placement | Between type-filter strip and table-view tabs (filters → scope summary → tabs → table); mounted-shell invariant preserved. |
| Terminology (lock D, app terms) | root "الجذر/الجذور"; stem label "الأصل الصرفي/الأصول الصرفية"; lemma label "الصيغة المعجمية/الصيغ المعجمية". "الجذع"/"اللمّة" internal-reference only. Strip reuses the tabs' short labels verbatim: كلمات \| جذور \| أصول \| صيغ (tabs not renamed). |

## Phases (execution order; each = one commit)

### P1 — Word Types parity (A1 search + A2 1000 rows + A3 100 details)

Touches the Word Types SQL first so P2/P4 build on the final base shape.
Key mechanics (full contract: `contracts/word-types-api.md`):

- Search: optional `search` param on rows + table reads → validation (trim, empty→null,
  max length 64, log only `hasSearch`) → shared Arabic-normalize helper
  (extracted from `EfUniqueWordsReader.NormalizeArabicQuery`; Unique Words behavior
  pinned unchanged) → ONE parameterized predicate on `BaseRowsSql`'s occurrence base
  matching `quran_words_unique_tashkeel.search_text_normalized` (`ILIKE @searchPattern`,
  value-parameterized, identifiers allowlisted). Rows/count/grouped SQL inherit it;
  `.GroupedDetails.*` detail reads do NOT take search (numeric identity, already
  scoped — asymmetry documented in reads README).
- Page sizes: split `WordTypesHandlerValidation.MaxPageSize` into
  `MaxListPageSize = 1000` / `MaxDetailPageSize = 100` (protects the documented
  grouped-detail 1..100 contract); `DefaultListPageSize` 25→1000; frontend
  `WORD_TYPES_PAGE_SIZE` 25→1000; `WORD_TYPES_DETAIL_PAGE_SIZE` 25→100; the two
  controllers' `DefaultDetailPageSize` 25→100.
- `word-types-table` adopts `CdkVirtualScrollViewport` (mirror `roots-table`); mounted
  shell, skeletons, focus controller, statistic buttons unchanged.
- Frontend search: URL key `search` (fail-closed), `Subject` + `debounceTime(300)` →
  `{ search, page: null }`; input visible on all tableViews, placeholder names the
  word grain ("ابحث في الكلمات"); cache keys (frontend + backend) gain normalized
  search (empty ⇒ key unchanged).
- Perf gate (mandatory, numeric): `RowsSql`/`GroupedRowsSql` timed at `pageSize=1000`
  worst scopes (unscoped verb; stems grouped ≈ 12,108 groups). **Budget: p95 ≤ 2s per
  uncached list read** (same 2-second populate target feature 019 set); cache-entry
  growth measured; 100-ayah detail render sanity. p95 > 2s at default 1000 = hard
  failure = stop condition 4.
- Commit: `feat(words): word-types parity — search, 1000-row list, 100-row details`.

### P2 — Cheap filters + headline result count (B1 + C1) — after P1

- Range grammar `min..max` (either side omissible) per metric key; buckets are
  presentation only. Backend nullable `<metric>Min/Max` ints; `Min >= 0`,
  `Max >= Min`, else the page's new `InvalidFilter` outcome (400, Arabic message).
- Predicates: Unique Words → SQL in `BuildTashkeelQuery`/`BuildSimpleQuery`;
  Roots/Lemmas/Stems → in-memory in `*ListDerivation` (whole-summary cache untouched ⇒
  no backend cache-key change there); Word Types has-flags → allowlisted
  `IS [NOT] NULL` on `m.root_id|stem_id|lemma_id` in `BaseRowsSql` (part of the list
  scope; reshapes grouped views and P4 counts). Unique Words backend cache keys +
  Word Types keys gain the new components; all frontend list cache keys gain them.
- New shared components: `explorer-count-range-filter` (chips `aria-pressed` + مخصّص
  min/max, RTL), `explorer-result-count` (عدد الـ…: N from `listState().totalCount`;
  skeleton while loading, hidden on list error, 0 on empty). Zero new backend work
  for the stat.
- Commit: `feat(words): count-range filters + result-count stat`.

### P3 — Association filters (B2) — after P2

- Unique Words `primaryType` (catalogue-validated POS code) + `rootId`: predicates in
  the base SQL reproducing exactly the primary-selection rules of
  `LoadPrimaryWordTypesAsync`/`LoadPrimaryRootsAsync` — displayed chip and filter can
  never disagree (agreement test).
- Lemmas `rootId` (FK `QuranLemma.RootId`) and Stems `rootId`/`lemmaId` (derived
  *primary* association on the summary rows) → in-memory derivation predicates.
  Labels: "الجذر الأساسي" / "الصيغة المعجمية الأساسية"; reads README documents
  primary-not-sole.
- Pickers reuse existing list reads (roots/lemmas APIs + caches); no new endpoints.
- Commit: `feat(words): association filters (primary type/root/lemma)`.

### P4 — Word Types scoped four-count summary (C2/C3) — after P1 + P2

- ONE new read `GET api/words/word-types/scope-counts` (params = exactly the list
  scope: `type,childCode,case,tense,voice,search,hasRoot,hasStem,hasLemma`) →
  `WordTypeScopeCountsDto(WordsCount, RootsCount, StemsCount, LemmasCount)`.
- Reader partial `EfWordTypesReader.ScopeCounts.cs`: ONE SQL command — CTE over the
  scoped `BaseRowsSql` base, four aggregates byte-consistent with `RowsCountSql` and
  the three `GroupedRowsCountSql` (reuse fragments, never re-derive). Cached with a
  key containing every scope input.
- Facade loads counts on scope change only (not tableView, not page). New
  `word-type-scope-counts` strip between filter strip and tabs; labels reuse the
  existing tabs' short forms verbatim — كلمات | جذور | أصول | صيغ (tabs NOT renamed;
  spec Clarifications) — with own loading/error(إعادة المحاولة)/zero states; partial
  failure never blocks the table. Scope-counts perf budget: **p95 ≤ 2s uncached** on
  the widest scopes, 1 SQL command.
- Equality test matrix: four counts == the four tableViews' `TotalCount` for identical
  scopes (types × children × case/tense/voice × search × has-flags); single-command
  budget pinned via `SqlCommandCountInterceptor`.
- Commit: `feat(words): word-types scoped four-count summary`.

## Tests, READMEs, verification (per phase — carried from the decision record)

- Tests per phase as listed in the docs plan (backend: search/caps/filters/equality/
  command budgets under the four Words test areas; frontend: url-sync fail-closed
  grammar, cache-key inclusion, component states, page-size pins).
- README updates in the SAME commit as the contract they document:
  `features/words/README.md` + reads README every phase; `docs/contracts/` untouched
  (pointer index already defers to these READMEs).
- Cross-phase checks: count-family audit, normalization equivalence pin, ordering
  untouched, command budgets (3/2/1), deploy-smoke after P1 and P4.

## Risks, rollback, stop conditions

Carried verbatim from the decision record (`docs/feature-026-words-explorers-enhancements/plan.md`
§Risks / §Rollback / §Stop conditions): five stop conditions (schema-needing filter;
search predicate breaking grouped-summary byte-equivalence; scope counts unable to
reuse grouped fragments; hard perf failure at default 1000; anything touching
importer/migrations/Quran source). Each phase is a single revertible commit; all new
params optional — old URLs stay valid.

## Progress Tracking

- [x] Phase 0: research.md complete — all unknowns resolved (3 via spec Clarifications)
- [x] Phase 1: data-model.md, contracts/ (3 files), quickstart.md generated
- [x] Constitution check: pre- and post-design PASS (formal status NOT EVALUABLE — unfilled template, accepted)
- [ ] Phase 2: tasks.md (`/speckit-tasks` — not part of this command)
- [x] Agent-context update: `<!-- SPECKIT START/END -->` block added to root
  `CLAUDE.md` and `AGENTS.md` pointing at this plan and the docs decision record.
