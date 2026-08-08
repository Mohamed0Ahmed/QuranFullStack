# 08 — Architecture & Code Size Audit (Audit G, brief §16 + §24)

- **Branch:** `dev` — **Commit:** `72792ba9` — **Audit date:** 2026-08-08
- **Scope:** brief §16 (Audit G — Architecture & Code Size) and §24 (generated code excluded from architecture blame); mandatory questions 53–64.
- **Evidence base:** `data/loc-inventory.json`, `data/endpoint-inventory-backend.json`, `data/endpoint-consumers-frontend.json`, `data/test-inventory-backend.json`, plus direct code reading performed for this report (interface census, handler sampling, DTO chain traces, frontend similarity measurement, component reuse counts).
- **Spot-verification:** every load-bearing inventory number used here was independently recounted from `git ls-files` at the same commit before being asserted (see §1.2). The layer-value analysis in §4–§6 is new work done by reading code, not copied from the inventories.
- **Mode:** read-only audit. This report proposes and classifies; it does not instruct implementation.

---

## 1. Size accounting (Q53–Q58)

### 1.1 The headline table

Total tracked text LOC at `72792ba9`: **407,221** across **2,651 tracked files** (binary assets excluded from LOC) — `data/loc-inventory.json:3-4`. **CONFIRMED**

| Category | Files | LOC | % of 407,221 | Tag |
|---|---:|---:|---:|---|
| Backend production handwritten (`api`, `application`, `domain`, `infrastructure`, `shared`, excl. `Migrations/`) | 927 | 56,934 | 14.0% | CONFIRMED |
| Frontend production handwritten (`.ts/.html/.scss` under `src`, excl. generated + specs) | 565 | 56,221 | 13.8% | CONFIRMED |
| **Handwritten product total** | **1,492** | **113,155** | **27.8%** | CONFIRMED |
| Backend tests | 347 | 55,808 | 13.7% | CONFIRMED |
| Frontend specs | 223 | 54,145 | 13.3% | CONFIRMED |
| E2E | 22 | 1,968 | 0.5% | CONFIRMED |
| **Test total** | **592** | **111,921** | **27.5%** | CONFIRMED |
| EF migrations (27 migrations + 27 `.Designer.cs` + 1 snapshot — all generated) | 55 | 64,262 | 15.8% | CONFIRMED |
| Embedded morphology-correction JSON (`Infrastructure/Files/.../Corrections/`) | 3 | 53,367 | 13.1% | CONFIRMED |
| Swagger JSON | 1 | 12,730 | 3.1% | CONFIRMED |
| package-lock.json | 1 | 11,413 | 2.8% | CONFIRMED |
| Generated OpenAPI client (models-only) | 233 | 2,605 | 0.6% | CONFIRMED |
| Skills + agent scaffolding (`.claude`, `.agents`, `.specify`) | 104 | 13,662 | 3.4% | CONFIRMED |
| Docs/markdown (READMEs, law files, `.architecture`) | 65 | 12,740 | 3.1% | CONFIRMED |
| Historical reports (`Backend/report/**`) | 5 | 2,999 | 0.7% | CONFIRMED |
| Backend scripts | 18 | 2,019 | 0.5% | CONFIRMED |
| Backend tools (AccessAdmin + DataImporter, `.cs`) | 17 | 1,391 | 0.3% | CONFIRMED |
| Frontend support (config, testing helpers, JSON data) | 21 | 2,075 | 0.5% | CONFIRMED |
| Backend/repo config | 21 | 834 | 0.2% | CONFIRMED |
| Remainder — uncategorized (root-level docs, `.cursor` rule, e2e/config leftovers, binary assets at 0 LOC) | 23 | 2,048 | 0.5% | CONFIRMED (by subtraction; closes the accounting to 2,651 / 407,221) |

Sub-answers required by the brief:

- **Q53 total repository LOC:** 407,221 tracked text LOC. CONFIRMED (`data/loc-inventory.json:3`).
- **Q54 handwritten production LOC:** 113,155 (27.8%) — Backend 56,934 + Frontend 56,221. Backend tools add 1,391 `.cs` of thin CLI shells. CONFIRMED.
- **Q55 test LOC:** 111,921 (27.5%) — a 0.99:1 test-to-product ratio overall (`data/loc-inventory.json:964`). CONFIRMED.
- **Q56 generated LOC:** 91,010 (22.3%) = EF designer/snapshot 61,068 + migration bodies 3,194 + swagger.json 12,730 + package-lock 11,413 + generated TS client 2,605. CONFIRMED.
- **Q57 migration/snapshot LOC:** 64,262 (15.8%), of which 61,068 is `.Designer.cs`/`ModelSnapshot.cs`. CONFIRMED.
- **Q58 documentation/Skill LOC:** 26,402 (6.5%) = docs/markdown 12,740 + Skills/scaffolding 13,662; historical reports add 2,999 (0.7%). CONFIRMED.

### 1.2 Independent recount (spot-verification)

Recomputed from `git ls-files` at `72792ba9` for this report:

| Claim | Inventory | Recount | Verdict |
|---|---:|---:|---|
| Backend production LOC | 56,934 | 56,934 | exact match |
| EF Migrations LOC | 64,262 | 64,262 | exact match |
| Backend tests LOC | 55,808 | 55,808 | exact match |
| Frontend specs LOC / files | 54,145 / 223 | 54,145 / 223 | exact match |
| Application.Abstractions LOC / files | 5,497 / 238 | 5,497 / 238 | exact match |
| Generated client LOC / files | 2,605 / 233 | 2,602 / 233 | match (−3 LOC counting-method delta) |
| Frontend production LOC | 56,221 | 56,130 | match within 0.2% (spec-name filter delta) |

**CONFIRMED** — the LOC inventory is trustworthy at its stated definitions.

One labeling correction found while verifying: `data/loc-inventory.json:878-885` counts **96** `*Handler.cs` files (5,944 LOC). Four of those are ASP.NET authorization/middleware handler-pattern classes in the Api project (`ApiAuthorizationMiddlewareResultHandler.cs`, `OwnerAuthorizationHandler.cs`, `PermissionAuthorizationHandler.cs`, `GlobalExceptionHandler.cs`), not use-case handlers. The application use-case handler population is **92 files / 5,782 LOC, mean 63 LOC**. CONFIRMED (recount).

### 1.3 Generated code is separated from blame (§24)

Per brief §24, none of the following counts toward architecture judgment:

- **EF `.Designer.cs` + `ModelSnapshot.cs` (61,068 LOC)** — pure EF tooling output; `Backend/CLAUDE.md` already forbids hand-editing it.
- **Generated OpenAPI client (233 files / 2,605 LOC)** — deliberately *models-only*. `npm run generate:api` runs ng-openapi-gen and then `scripts/prune-generated-api.mjs`, which deletes everything except `models/` and `models.ts` (`Frontend/quran-dashboard-ui/scripts/prune-generated-api.mjs:7-8`; `data/endpoint-consumers-frontend.json` "generated_client_reality"). All HTTP access is via 16 handwritten `*.api.ts` wrappers. The 233 tiny files are a generated barrel of interface definitions, not an architecture choice to have many tiny files. **CONFIRMED**
- **swagger.json (12,730 LOC)** and **package-lock.json (11,413 LOC)** — derived artifacts.
- **Embedded morphology-correction JSON (53,367 LOC)** — this is *data*, not code. It is 93.7% of the byte-size impression of `Infrastructure/Files` and inflates "backend size" in any naive count.

**Nothing in this report's simplification candidates targets generated files.**

---

## 2. Q59 — Why does the project FEEL large?

Because the categories that dominate the line count are not product code:

- tests 111,921 (27.5%)
- generated 91,010 (22.3%)
- embedded corpus-correction data 53,367 (13.1%)

**Tests + generated + embedded data = 256,298 LOC = 62.9% of all tracked text.** Adding docs, skills, scripts, historical reports and config brings non-product content to ~70%. The handwritten product a developer or agent must actually understand is **113,155 LOC (27.8%)** — a medium-sized two-sided application, not a large one. **CONFIRMED**

(The task-orientation figure of "~65%" for generated+data+tests is consistent with this measurement; the precise value depends on whether migration bodies and reports are included. The computed value with the definitions in §1.1 is 62.9%.)

Secondary amplifiers of the "feels large" impression, measured:

- **File-count inflation from convention, not logic.** `Application.Abstractions` is 238 files for 5,497 LOC (mean 23 LOC/file — one record/interface per file, C# convention). The generated client is 233 files for 2,605 LOC. Both make directory listings look enormous while each file is trivially readable. CONFIRMED
- **Per-operation folders.** ~84 per-operation folders under `QuranDashboard.Application` (92 handler-bearing leaves including pipeline handlers; 97 directory nodes when `Commands`/`Queries` parents and grouping dirs are counted), each operation folder holding 3–4 small files (`Handler` + `Query`/`Command` + `Outcome` [+ `Body`]), e.g. `Abwab/Commands/Doors/EditDoor/` = 4 files. Uniform and predictable, but 8 directory levels deep at the leaves. CONFIRMED
- **Backend production is 64% infrastructure** (36,576 of 56,934), and within that, importer/pipeline machinery dominates — 15 of the top 15 largest backend files are infrastructure import/persistence code (`data/loc-inventory.json:446-552, 968`). The *dashboard API itself* is small: Api 4,099 + Application 9,656 + Abstractions 5,497 + Domain 1,085 = 20,337 LOC. CONFIRMED

---

## 3. Largest areas (from the verified inventory)

Largest handwritten directories (direct, non-recursive LOC — `data/loc-inventory.json:700-856`):

| Directory | Files | LOC | Split |
|---|---:|---:|---|
| `Frontend/.../features/words/state` | 61 | 14,009 | 6,708 prod / 7,301 spec |
| `Backend/tests/.../Api/Access` | 31 | 6,776 | all test |
| `Frontend/.../features/mushaf/state` | 28 | 6,023 | 1,772 prod / 4,251 spec |
| `Frontend/.../features/abwab/state` | 25 | 5,844 | 2,915 prod / 2,929 spec |
| `Backend/tests/.../Quran/WordsMorphology` | 19 | 5,142 | all test |

Largest handwritten files by role: backend — `MorphologyValidationRunner.cs` 781, `EfAbwabDoorsWriter.cs` 757, `MorphologyAssembler.cs` 701 (all infrastructure pipeline/persistence); frontend — `_components.scss` 746, `mushaf-reader.facade.ts` 596, `access-admin.facade.ts` 545. Role populations: 114 `*.component.ts` (14,081 LOC), 15 facades (3,489), 19 controllers (4,406), 92 use-case handlers (5,782), 7 backend services (1,294). CONFIRMED (`data/loc-inventory.json:858-958` + recounts).

---

## 4. Backend layer-value analysis (Q60, Q61)

### 4.1 Interface census — Application.Abstractions

New measurement (multiline-tolerant scan over all non-generated `.cs` in Application/Infrastructure/Api/tools):

- **78 interfaces** declared in `QuranDashboard.Application.Abstractions`.
- **56 have exactly one production implementation; 22 have exactly two. Zero have none.** CONFIRMED

The 22 two-implementation interfaces decompose as:

| Group | Count | What the second implementation is |
|---|---:|---|
| Reader + `Cached*` decorator | 14 | cache decorator over the `Ef*` reader (Abwab tree/templates; 7 MushafReader readers; 5 Words explorer readers) |
| Writer + `Invalidating*` decorator | 5 | cache-invalidation decorator over the `Ef*` writer (all 5 Abwab writers) |
| Genuine host alternates | 3 | `IAccessRequestContext` (HTTP vs ambient/CLI), `IInteractiveIdentityEvidenceValidator` (JWT in Api vs unavailable in Infrastructure), `IMorphologyImportSource` (base vs enriched source) |

The decorator wiring is real composition, not ceremony: `Backend/infrastructure/QuranDashboard.Infrastructure/DependencyInjection/AbwabDependencyInjection.cs:18-53` registers each `Ef*` concrete, then binds the interface to a decorator wrapping it, with generation-stamped invalidation (`CachedAbwabTreeReader.cs:20-30`). **The 19 decorators prove the reader/writer interfaces earn their seam where they exist.** CONFIRMED

For the 56 one-implementation interfaces:

- **10 have test fakes** (e.g. `IAuthorizationStateResolver` — 3 fakes, `IOwnerBootstrapConfigurationSource` — 2, `INavigationMetadataImportSource`, `IMutashabihatImportSource`, `ICurrentUser`, `IExternalUserProfileSource`). The seam is exercised. CONFIRMED
- **46 have no test double and one implementation.** But this does **not** make them ceremony, for a structural reason verified from the `.csproj` graph: `QuranDashboard.Application` references only `Abstractions`, `Domain`, `Shared` — it cannot see Infrastructure. Every handler that touches persistence *must* consume an interface from Abstractions; the interface is the compile-time mechanism of the Application/Infrastructure boundary, not an optional indirection. Removing any of these 46 interfaces requires either moving the query into the handler (collapsing the layer) or reversing the project reference (collapsing the architecture). CONFIRMED (csproj reference graph)

**Verdict (Q60):** the Abstractions layer is a genuine boundary. Its per-interface cost is low (mean 23 LOC/file), its decorators and test fakes demonstrate real substitution at 29 of 78 seams, and the remaining 49 are load-bearing for the compile-time layering. **No interface-deletion candidates.** The only accepted cost is that a new endpoint touches ~3 projects (interface + handler + Ef implementation); that cost is assessed under Q61/Q64.

### 4.2 Handler sampling — 13 of 92 use-case handlers read in full

Sampled across features and the size spectrum (9→232 LOC): `GetPermissionCatalogueHandler` (9), `GetAbwabTreeHandler` (23), `AcceptAccessUserHandler` (24), `GetDoorRelationsHandler` (30), `GetMushafPageHandler` (34), `DeleteDoorHandler` (34), `ListAccessUsersHandler` (43), `EditDoorHandler` (54), `GetLemmaSummaryHandler` (55), `ApplyTemplateHandler` (59), `GetWordTypeRowsHandler` (86), `GetUniqueWordsPageHandler` (139), `ImportNavigationMetadataHandler` (232).

| Class | Handlers in sample | What they add beyond reader/writer call |
|---|---|---|
| Pure pass-through | 3 (`GetPermissionCatalogue`, `GetAbwabTree`, `GetDoorRelations`) | null-check + log line + null→NotFound mapping only |
| Near pass-through | 1 (`GetLemmaSummary`) | trivial `id <= 0` guard; rest is logging |
| Real validation / policy | 6 (`GetMushafPage` — 1..604 page bounds + prev/next enrichment at `GetMushafPageHandler.cs:14-30`; `AcceptAccessUser` — validation + injects `currentUser.Identity.Sub` at `AcceptAccessUserHandler.cs:15-22`; `ListAccessUsers` — search normalization/caps; `EditDoor`/`DeleteDoor` — name policy + `AbwabStaleVersionException`→outcome translation; `GetWordTypeRows` — filter/sort parsing) | input policy, security context, concurrency-outcome mapping |
| Heavy validation / orchestration | 3 (`GetUniqueWordsPage` — kind/sort/paging/association incl. async catalogue check; `ApplyTemplate` — 5-exception outcome mapping; `ImportNavigationMetadata` — full source→validate→write→report orchestration) | real orchestration |

Extrapolation (LIKELY, sample-based): **roughly a quarter of handlers are pure pass-throughs, and they are the *cheapest* handlers (9–30 LOC)**. The layer's aggregate cost is 5,782 LOC — under half the size of the words feature's spec suite alone.

Two findings that matter more than "pass-through exists":

1. **The uniform chain is load-bearing for tests and error contracts.** 83 of 85 API operations run exactly `controller → 1 handler → reader/writer` (`data/endpoint-inventory-backend.json`, handler-count histogram; the 2 exceptions are `/api/health` and `/api/dashboard/info`). Backend read tests mostly resolve *handlers* from DI rather than making HTTP calls (65 read-test classes, 16 of them HTTP-GET-only — `data/test-inventory-backend.json:951-963`, LIKELY per that inventory), and controllers switch on typed `Outcome` variants (`AbwabTreeController.cs:26-31`). Deleting the trivial handlers would break the uniformity that makes both possible, to save ~300–600 LOC. **Not proposed.**
2. **The real recurring cost inside handlers is logging ceremony, not layering.** In the sample, structured `LogInformation`/`LogWarning` blocks with `FeatureName`/`OperationName` constants are ~30–50% of the lines of every mid-size read handler (e.g. `GetLemmaSummaryHandler`: ~30 of 55 lines are logging). This pattern is additionally pinned by 4 per-feature `*LoggingTests` classes totalling 1,583 test LOC (`data/test-inventory-backend.json:1202-1216`). This is a policy-contingent simplification candidate (G5, §6). LIKELY

**Verdict (Q61):** no layer in the backend is a wholesale pass-through. The pass-through instances are individually cheap and structurally useful. The measurable recurring waste is (a) per-handler logging ceremony and (b) per-pipeline report/plumbing replication (§4.6, G6) — both *horizontal duplication*, not vertical layering.

### 4.3 Controller layer

30 controller files, 2,533 LOC (mean 84). Verified shape (`AbwabTreeController.cs`): route attribute, ETag conditional handling where cached, dispatch to handler, exhaustive `Outcome`→`ApiResponse` switch. Controllers carry the HTTP-only concerns (status codes, ETag, `ApiResponse` envelope, authorization attributes). **KEEP — this is the API contract boundary (§29: API contract integrity, OpenAPI parity).** CONFIRMED

### 4.4 DTO/model chain traces (§24 required trace, 2 endpoints)

**Trace 1 — `GET /api/abwab/tree`:**

| Step | Shape | Evidence |
|---|---|---|
| EF entities | `AbwabSection`, `AbwabDoor`, `AbwabDoorAlias` | `EfAbwabTreeReader.cs:10-22` |
| Reader projection | → `AbwabTreeDto`/`AbwabTreeSectionDto`/`AbwabTreeDoorDto` built *directly* in the reader — there is **no separate "reader row" layer** | `EfAbwabTreeReader.cs:43-57`; DTO record at `Application.Abstractions/Abwab/Responses/AbwabTreeDto.cs:3` |
| Wire envelope | `ApiResponse<AbwabTreeDto>` — same DTO instance, no re-mapping | `AbwabTreeController.cs:28-29` |
| Swagger schema | `AbwabTreeDtoApiResponse` | swagger.json (229 schemas total) |
| Generated TS | `abwab-tree-dto.ts`, `abwab-tree-door-dto.ts` (19 LOC) — generated | `core/api/generated/models/` |
| Frontend VM | `AbwabNode`/`AbwabTreeSnapshotVm` — **adds derived state** (depth, liveChildCount, liveDescendantCount, maxRelativeDepth), not a field copy | `abwab-tree.builder.ts:19-62` |

**Trace 2 — `GET /api/words/unique/{kind}` (unique words list):**

| Step | Shape | Evidence |
|---|---|---|
| EF/SQL | raw-SQL + EF query over 8 DbSets | `data/endpoint-inventory-backend.json` entry; `EfUniqueWordsReader.cs:14-66` |
| Reader | internal helper rows (`UniqueWordSummaryRow`) exist only *inside* the reader; public result is `PagedResult<UniqueWordListItemDto>` | `EfUniqueWordsReader.cs:51,66` |
| DTO | `UniqueWordListItemDto` — 11-field record in Abstractions, flows unchanged to the wire | `Responses/UniqueWordListItemDto.cs:3` |
| Cache layer | `CachedUniqueWordsReader` decorator (same DTO, no re-shape) | `CachedUniqueWordsReader.cs:6` |
| Generated TS | `unique-word-list-item-dto.ts` (16 LOC) | generated |
| Frontend model | **type re-export/alias**, not a mapped copy: `UniqueWordListItemDto as UniqueWordListItemWireDto` plus handwritten sort/filter union types | `features/words/models/unique-words.models.ts:20-28` |

**Count of distinct handwritten shapes per element: 2** (backend DTO + frontend VM where derivation exists; the words list doesn't even create a VM — it aliases the wire type). Entities and generated TS models are tooling shapes. **There is no duplicated DTO/model layering to remove: the suspected "repeated DTO/model layers" defect from brief §16 is absent in this codebase.** CONFIRMED

### 4.5 Caches — are they wrapped around trivial reads? (brief §16 "caches around trivial reads")

Checked 3 of the 14 `Cached*` readers in code:

- `CachedAbwabTreeReader` (33 LOC): generation-stamped; every Abwab write bumps the generation via `Invalidating*` writers; backs the ETag conditional-GET contract the frontend relies on (`abwab-snapshot.facade.ts:49`). Correctness-aware, not trivial. CONFIRMED
- `CachedUniqueWordsReader` (121 LOC): **bypasses the cache for search queries** (`CachedUniqueWordsReader.cs:18-21`), caches only enumerable page/detail keys, TTL-bounded (list 15 min absolute, detail 30 min sliding — `UniqueWordsCacheEntryOptions.cs:5-6`). Wraps an 8-DbSet raw-SQL aggregation — expensive read, static corpus. Justified. CONFIRMED
- `CachedMushafSurahCatalogReader` (23 LOC): the *only* cheap-query cache (114-surah catalog), but it is the highest-frequency lookup and has a 12 h TTL (`MushafReaderCacheEntryOptions.cs:6`). Cost of the decorator: 23 LOC. Not worth removing. CONFIRMED

Corpus data (words/mushaf) changes only via import pipelines, never via the API, which is why words/mushaf readers have cache decorators without invalidating writers, while Abwab (API-writable) has both. The TTLs bound staleness if an import runs against a live DB. **Verdict: no caches around trivial reads; the cache tier is one of the better-engineered parts of the backend.** One residual risk is noted in Measurement gaps (live-import staleness window). 

### 4.6 Deep folder chains / many tiny files

- ~84 per-operation folders × 3–4 files each in Application (92 handler-bearing leaves; 97 directory nodes incl. `Commands`/`Queries` parents); leaf paths are 8 levels deep. Uniform, tool-friendly, and each file is small; the cost is navigation, not comprehension. **KEEP (convention), no candidate.** CONFIRMED
- 238-file Abstractions project: one-type-per-file C# convention. **KEEP.** CONFIRMED
- Generated client 233 files: generated — excluded from blame (§1.3). CONFIRMED
- The genuinely replicated backend area is the **import-pipeline plumbing**: per pipeline (Foundation, Tafsirs, Translations, Navigation, FullI3rab, Mutashabihat, Morphology, SimpleI3rab, DisplayRebuilding) there is an `I*ImportSource` + `I*ImportWriter` + `I*ReportWriter` (+ often `I*ImportReportBuilder`) interface set, matching implementation set, and a large per-pipeline handler — mirrored on the test side by the per-pipeline RefusalForce/ValidationFailure/ReportShape/ManifestReader clusters already flagged in `data/test-inventory-backend.json:1127-1320`. This is horizontal replication of one proven pattern (G6, §6). CONFIRMED (structure), LIKELY (consolidation feasibility)

---

## 5. Frontend split-value analysis (Q62, Q63)

### 5.1 The feature-shape spectrum

The architecture does **not** force heavy scaffolding: dashboard is 3 files (53 ts / 62 html / 44 scss, no facade, no state dir), auth is 3 files. Words, at the other end, is 224 production files (133 ts / 48 html / 43 scss; 17,798 ts LOC) plus 92 spec files (24,207 LOC). Mushaf and abwab sit between. CONFIRMED (`data/loc-inventory.json:112-256` + direct listing)

### 5.2 Words state layer: one machine instantiated five times

The 39-file/6,708-LOC `words/state` production layer is a per-entity replication of the same seven-part machine — cache, url-sync, explorer.facade, detail.facade, detail.controller, detail-view.loader, detail-panel.updates — for roots, stems, lemmas and word-types (unique-words carries a 5-part drilldown variant: cache, drilldown.controller, drilldown.facade, url-sync, facade). Measured line-similarity (difflib `SequenceMatcher` ratio on whole files):

| File pair (roots/stems/lemmas trio) | LOC | Similarity |
|---|---|---:|
| `*-detail-panel.updates.ts` | 178/158/158 | 0.72–0.74 |
| `*-explorer.facade.ts` | 155/167/166 | 0.75–0.77 |
| `*-detail-view.loader.ts` | 137/145/145 | 0.60–0.65 |
| `*-url-sync.ts` | 136/166/159 | 0.50–0.58 |
| `*-detail.controller.ts` | 323/375/380 | 0.50–0.51 |
| `*-detail.facade.ts` / `*-cache.ts` | 84–89 / 43–46 | 0.47–0.52 |

The roots+stems+lemmas state slices alone are **21 files / 3,344 LOC**, of which the top two rows (6 files, 982 LOC at ~72–77% line-identical) are the most mechanical duplication measured anywhere in this audit. Shared abstractions already exist (`abstract-detail.controller.ts` 191, `abstract-route-detail.facade.ts` 71, `detail-request-lifecycle.ts` 50) — the pattern was partially unified and then re-instantiated per entity anyway. The same per-entity replication repeats in `entity-detail-overlay/adapters` (5 adapters, ~180 ts + ~150 html each) and in the five explorer table components (`lemmas/roots/stems/unique-words/word-types-table`, 688–857 LOC each incl. templates). A full single-entity slice (roots: pages + state + table + panel + lists + api + models + adapter) is **31 files / 3,786 LOC** (stems/lemmas name-matched slices: ~37–38 files / ~4,300–4,400 LOC). **CONFIRMED (measured); consolidation candidates G1/G7 in §6.**

### 5.3 Component sampling — 12 components checked for split-value

Reuse measured by counting non-spec templates containing the selector:

| Component | LOC (ts/html/scss) | Used in # templates | Split verdict |
|---|---|---:|---|
| `surah-occurrences-list` | 24/36/3 | **9** | KEEP — reuse across all 5 entities (Q62) |
| `missing-surahs-list` | 23/33/3 | **9** | KEEP (Q62) |
| `ayah-matches-list` | 94/77/118 | **9** | KEEP (Q62) |
| `word-count-chip` | 29/17/42 | 5 | KEEP |
| `words-explainer` | 30/36/65 | 5 | KEEP |
| `explorer-search-row` | 22/15/3 | 4 | KEEP |
| `roots-table` (rep. of 5 tables) | 313/296/156 | 1 each | per-entity replication — see G1/NEEDS_MEASUREMENT |
| `lemma-details-panel` (rep. of 4 panels) | 149/114/42 | 1 each | per-entity replication |
| `highlighted-ayah` | 28/11/17 | 1 | fold-back candidate (Q63) |
| `unique-words-tabs` | 33/12/3 | 1 | fold-back candidate (Q63) |
| `word-section-card` | 19/14/44 | 1 | fold-back candidate (Q63) |
| `type-distribution-list` | 44/43/3 | **0** | **DELETE_CANDIDATE — dead code (G2)** |

`type-distribution-list` has zero template usages and zero component imports repo-wide (grep across `src/app` and `e2e` for both selector and class name); the remaining mentions are its own directory, ~16 selector/class blocks in `src/styles/_explorer-detail-lists.scss` (lines 90–373 — global styling for the unrendered selector), `features/words/README.md:247` (which mentions it as display-only), and a page spec asserting it is *not* rendered (`lemmas-explorer-page.component.spec.ts:451`). CONFIRMED (statically; runtime confirmation trivial via build)

Additionally, **21 of 106 component `.scss` files are ≤5-line stubs (16 of them `:host{display:block}`)** — file multiplication from the separate-SCSS-by-default rule, cross-referenced to report 07 (styling audit) which owns that policy question. CONFIRMED

### 5.4 Page/facade/state/api split verdict

For words, the split itself is not the problem — facades isolate URL-sync, caching (48-entry LRU `ApiResponseCache` singletons, `data/endpoint-consumers-frontend.json` caching section) and request lifecycles that the 92 spec files pin extensively. The problem is that the split is **instantiated per entity instead of parameterized per entity**, so every concept costs 5× files and any cross-cutting change (e.g. a new filter behavior) is a 5-site edit. The dashboard/auth features prove the team scales the machinery down when a feature is simple. **Q62/Q63 answered per component in the table above; consolidation, not de-layering, is the correct simplification.** CONFIRMED (structure), LIKELY (consolidation net benefit)

---

## 6. Simplification candidates

Candidates answer the seven §4 questions directly or by stated reference (G4 defers to report 07; G7 inherits G1's answers). None touches the §16/§29 protected boundaries (§7). Estimated LOC figures are net of expected abstraction overhead and tagged accordingly.

### G1 — Consolidate the words per-entity state machinery (roots/stems/lemmas first)

1. **Value today:** working, spec-pinned explorer/detail state for 5 word entities; per-entity files are individually readable.
2. **Dependents:** 5 explorer pages, entity-detail overlay, 92 words spec files; URL-state restoration contracts documented in `features/words/README.md`.
3. **Risk if changed:** regression in URL sync/detail-tab behavior; genericized code can be harder to read than the duplication it replaces (the existing `abstract-detail.controller.ts` shows partial unification already hit its comfort limit).
4. **Equivalent protection:** the 24,207-LOC words spec suite pins current behavior; specs run per-feature lane (`test:feature:words`).
5. **Smallest safe step:** merge only the two highest-similarity file families — `*-detail-panel.updates.ts` (0.72–0.74) and `*-explorer.facade.ts` (0.75–0.77) across roots/stems/lemmas — into parameterized modules; leave controllers/url-sync/loaders alone initially.
6. **Verification later:** `test:feature:words` lane green + E2E words journeys; spec files themselves shrink only in a later step.
7. **Recurring cost removed:** today a sixth word entity costs ~31–38 files/~3,800–4,400 LOC + specs (roots measured, stems/lemmas name-matched — §5.2); each cross-cutting change is a 5-site edit. First step removes ~450–550 LOC / 4 files (LIKELY); full state-layer consolidation removes ~1,200–1,800 LOC / 12–15 files (NEEDS_MEASUREMENT — net of the generic engine's own size).

### G2 — Delete dead component `type-distribution-list`

1. **Value:** none found — zero production references (§5.3).
2. **Dependents:** its own spec; ~16 orphaned selector/class blocks in `src/styles/_explorer-detail-lists.scss`; a README sentence mentioning it as display-only.
3. **Risk:** near-zero; statically unreachable.
4. **Equivalent protection:** n/a (dead code).
5. **Smallest step:** remove the 4-file directory (ts/html/scss + spec); remove the orphaned `type-distribution-list` selector blocks from `src/styles/_explorer-detail-lists.scss` (cross-referenced to report 07, which owns the global-styles surface); correct `features/words/README.md:247` in the same change (README-truth rule).
6. **Verification:** build + `test:feature:words`.
7. **Recurring cost removed:** small but real — it appears in searches, README reading, and spec runtime. ~90 prod LOC + spec, 4 files (plus the orphaned SCSS blocks). CONFIRMED (dead), DELETE_CANDIDATE.

### G3 — Fold back single-use micro components (`highlighted-ayah`, `unique-words-tabs`, `word-section-card`)

1. **Value:** encapsulation; each is used exactly once.
2. **Dependents:** one parent template each.
3. **Risk:** low; purely presentational.
4. **Equivalent protection:** parent-page specs.
5. **Smallest step:** inline the template/styles into the single parent (only if the parent stays under size thresholds); otherwise keep — a 3-file component that never grows a second consumer is cheap but nonzero.
6. **Verification:** feature spec lanes.
7. **Recurring cost removed:** ~9 files; ~0 net LOC. Marginal — file-count hygiene only. LIKELY, low priority.

### G4 — Stub `.scss` shells (21 files ≤5 lines)

Owned by report 07 (styling audit) since it is a policy question (separate-SCSS-by-default rule, brief §15/§22). Recorded here because it is file multiplication evidence for Q63: −21 files, ~−60 LOC, zero risk if the `:host{display:block}` is preserved where behavioral. CONFIRMED (count), cross-referenced.

### G5 — Centralize or slim per-handler logging ceremony (policy-contingent)

1. **Value:** structured per-operation success/rejection logs with safe fields; enforced by `LOGGING_GUIDELINES.md` and pinned by 4 `*LoggingTests` classes (1,583 test LOC).
2. **Dependents:** log-shape expectations in tests; operational debugging habits (UNKNOWN — no telemetry evidence available).
3. **Risk:** losing rejection-reason visibility on reads; this is an observability decision, not a refactor.
4. **Equivalent protection:** a logging decorator/pipeline around handlers could emit the same fields generically (outcome type + query fields), preserving the contract with one implementation instead of 92.
5. **Smallest step:** decide policy first (is per-read-completion logging still wanted?); if yes, a generic outcome-logging wrapper; if no, drop `LogInformation` on successful reads only.
6. **Verification:** `*LoggingTests` rewritten against the wrapper once, not per feature; log output diff in deploy-smoke.
7. **Recurring cost removed:** ~30–50% of every future read handler's LOC plus its logging tests; retroactively ~1,500–2,000 prod LOC + ~1,600 test LOC. LIKELY (measured share), **policy decision required before any code moves**.

### G6 — Shared harness for import-pipeline report/source plumbing (long-term, guarded)

1. **Value:** each pipeline's Source/Writer/ReportBuilder/ReportWriter set enforces source validation and rollback-refusal behavior — §29 protected territory (import validation, source provenance).
2. **Dependents:** 122 pipeline test classes; DataImporter CLI; report artifacts.
3. **Risk:** HIGH if done as a flattening — these seams gate Quran-data safety.
4. **Equivalent protection:** a shared generic harness must reproduce refusal/force, validation-failure and report-shape behavior per pipeline; the per-pipeline test clusters (`data/test-inventory-backend.json:1127-1320`) would consolidate with it.
5. **Smallest step:** unify only the *report writer* implementations (9 near-identical `MarkdownJson*ReportWriter` classes) behind one generic writer; leave sources/writers per-pipeline.
6. **Verification:** pipeline lane + canonical-data lane green; report-shape tests keep byte-level expectations.
7. **Recurring cost removed:** each new pipeline currently copies ~4 interfaces + 4 implementations + 6–10 test classes; report-writer step alone ~800–1,500 LOC. LIKELY (pattern), NEEDS_MEASUREMENT (net), sequenced **after** the test-suite decisions in report 02.

### G7 — Entity-detail-overlay adapters (evaluate with G1)

5 per-entity adapter components (~180 ts + ~150 html each; `entity-detail-overlay/adapters` = 15 files / 2,815 LOC incl. specs). Same replication shape as G1; should be evaluated in the same consolidation, not separately. LIKELY, ~300–500 LOC potential.

### Explicit non-candidates (things that look like targets but are not)

| Suspect (brief §16 list) | Finding | Verdict |
|---|---|---|
| One-implementation interfaces | 56 exist; all are the compile-time layer boundary; 10 have fakes; decorators prove the seam class-wide | KEEP |
| Pass-through handlers | ~25% of handlers, 9–30 LOC each; uniformity is load-bearing for tests + outcome contracts | KEEP |
| Repeated DTO/model layers | absent — one DTO from reader to wire, frontend aliases or derives (§4.4) | no action |
| Reader abstractions mirroring EF | readers own real projections/SQL; decorated by caches | KEEP |
| Caches around trivial reads | none found; TTL-bounded, search-bypassing, generation-stamped (§4.5) | KEEP |
| Deep folder chains / tiny files | convention + generated files; navigation cost only | KEEP |
| Controllers as a layer | HTTP/contract boundary, mean 84 LOC | KEEP |

---

## 7. Boundaries preserved (§16 / §29)

No candidate in §6 touches: authentication/authorization (Api `Authorization/` handlers, policy registration), Owner/direct permissions, account status, audit (`IAccessAuditAppender`), transactions, optimistic concurrency (`AbwabStaleVersionException` translation in write handlers stays byte-for-byte), DB invariants, Quran text integrity, source provenance and import validation (G6's smallest step deliberately excludes Source/Writer seams), external identity boundaries (`IAccessRequestContext`, `IInteractiveIdentityEvidenceValidator` dual implementations), security-sensitive state, or API contract integrity (controllers and `ApiResponse` envelope untouched). G5 explicitly requires a policy decision because rejection-logging may be part of the operational security posture. 

---

## Mandatory questions answered

| Q | Answer |
|---|---|
| **53** | Total repository LOC: **407,221** tracked text LOC across 2,651 files at `72792ba9` (binaries excluded). CONFIRMED, independently recounted (§1.2). |
| **54** | Handwritten production: **113,155 LOC (27.8%)** — Backend 56,934 (of which 64% is infrastructure import/persistence machinery), Frontend 56,221; plus 1,391 LOC of CLI tool shells. CONFIRMED. |
| **55** | Test LOC: **111,921 (27.5%)** — backend 55,808, frontend specs 54,145, e2e 1,968; 0.99:1 test-to-product overall. CONFIRMED. |
| **56** | Generated LOC: **91,010 (22.3%)** — EF designer/snapshot 61,068, migration bodies 3,194, swagger.json 12,730, package-lock 11,413, generated TS client 2,605. Excluded from all architecture blame per §24. CONFIRMED. |
| **57** | Migration/snapshot LOC: **64,262 (15.8%)**, of which 61,068 is designer/snapshot files. CONFIRMED. |
| **58** | Documentation/Skill LOC: docs/markdown **12,740** + Skills/agent scaffolding **13,662** = **26,402 (6.5%)**; historical reports add 2,999. CONFIRMED. |
| **59** | It feels large because **62.9% of tracked text is tests + generated artifacts + embedded corpus data** (§2), amplified by convention-driven file-count inflation (238-file Abstractions at 23 LOC/file, 233-file generated client, ~84 op folders). The product a developer must understand is 113k LOC. CONFIRMED. |
| **60** | Genuine boundaries: Application↔Infrastructure via Abstractions (compile-time enforced by csproj references; 19 cache/invalidation decorators and 10 test-faked seams prove active substitution); Api↔Application (controllers own HTTP/auth/contract); import Source↔Writer split (file validation vs DB write — Quran-safety); host alternates (`IAccessRequestContext`, `IInteractiveIdentityEvidenceValidator`); Abwab write path's concurrency/invalidations; frontend facade/api split backing session caches + ETag revalidation. CONFIRMED (§4.1, §4.4, §4.5). |
| **61** | Pass-through: ~25% of use-case handlers (sample 13/92) are pure pass-throughs of 9–30 LOC; no whole layer is pass-through. The measurable recurring waste is horizontal: per-handler logging ceremony (~30–50% of handler LOC, LIKELY) and per-pipeline/per-entity replication — not vertical layering. CONFIRMED (sample), LIKELY (extrapolation). |
| **62** | Splits that reduce complexity: the reused presentational components (`surah-occurrences-list`, `missing-surahs-list`, `ayah-matches-list` — 9 templates each; `word-count-chip`, `words-explainer` — 5; `explorer-search-row` — 4), the facade/URL-sync/cache isolation in state layers (spec-pinned, cache-bearing), and pure derivation modules like `abwab-tree.builder.ts`. CONFIRMED (reuse counts). |
| **63** | Splits that merely multiply files: the per-entity instantiation of the words state machine (21 files/3,344 LOC for 3 entities at 47–77% line similarity), 5 per-entity overlay adapters, single-use micro components (3), one dead component (`type-distribution-list`), and 21 stub `.scss` shells. CONFIRMED (measured). |
| **64** | Realistic safe reduction: **~1,700–2,500 handwritten LOC (≈1.5–2% of product code) and ~45–65 files** via G1+G2+G3+G4+G7 without any policy change (confidence: MEDIUM — G1's net depends on abstraction overhead, NEEDS_MEASUREMENT; the sum matches the itemized §6 candidates — consolidating the five explorer tables (§5.2, 688–857 LOC each) could add more but carries no estimate here, NEEDS_MEASUREMENT); up to **~4,500 LOC + ~1,600 test LOC** if the logging policy (G5) changes, and more long-term via G6 after test-suite decisions. The honest headline: **architecture flattening is not where this repository's size lives** — the backend layer stack is thin, uniform and protective; the real recurring-cost lever is per-entity/per-pipeline replication and the size-dominant non-product categories owned by other reports. |

---

## Measurement gaps

- **G1/G6 net LOC after abstraction** — NEEDS_MEASUREMENT: a generic engine has its own size; the 1,200–1,800 (G1) and 800–1,500 (G6) figures are similarity-based estimates, not diffs of a prototype.
- **Handler logging operational value** — UNKNOWN: no production log-consumption telemetry exists in the repo; whether per-read completion logs are ever used cannot be determined statically. G5 must not proceed on this report alone.
- **Pass-through share beyond the sample** — LIKELY only: 13 of 92 handlers read in full; the ~25% pure-pass-through figure is a sample extrapolation, though the size distribution (all 9–30 LOC handlers cluster at the trivial end) supports it.
- **Cache staleness during live imports** — NEEDS_MEASUREMENT: words/mushaf `Cached*` readers rely on TTLs (15 min–12 h) if an import pipeline runs against a live API's database; whether that operational scenario occurs is not answerable from the repo.
- **`type-distribution-list` runtime reachability** — statically CONFIRMED dead; a build + words spec lane run is the named later verification (not run here: read-only audit, no test runs).
- **Mushaf feature depth** — the 62-file/4,493-LOC mushaf ts area was sized but not component-sampled at the words level of depth; its per-file mean (72 LOC) and single shared facade suggest less replication, LIKELY but unverified.
- **Frontend production LOC counting delta** — 91 LOC (0.16%) difference between inventory and recount due to spec-filename filtering; immaterial, noted for the adversarial reviewer.
