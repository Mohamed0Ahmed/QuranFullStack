# Backend Target Structure — Final Execution Plan

> **Status:** APPROVED FOR EXECUTION (sequential, stop-on-failure). **This document is a
> plan only.** Producing it changed no code: no files were moved, no namespaces changed,
> no migrations created, nothing committed.
>
> **Supersedes the recommendation in:**
> `Backend/report/architecture/backend-project-structure-inventory.md` §6/§7/§9 ("no
> cross-layer namespace renames now"). That deferral is **intentionally reversed** by the
> human decision recorded in §1.
>
> **Incorporates the corrections from:**
> `Backend/report/architecture/backend-target-structure-feasibility-engineering-review.md`
> (verdict: CHANGES REQUESTED). See §10 for the correction-by-correction mapping.
>
> **Authoritative references:** `CODING_PRINCIPLES.md`,
> `Backend/.architecture/BACKEND_STRUCTURE.md` (placement + file-size),
> `Backend/.architecture/CLEAN_ARCHITECTURE.md` (layers + dependency direction),
> `Backend/.architecture/API_GUIDELINES.md` (API boundary), `Backend/AGENTS.md` /
> `Backend/CLAUDE.md`.
>
> **Baseline:** `dotnet build QuranDashboard.sln` is green (0 warnings / 0 errors) at the
> time of writing. Each phase must return to that state before the next begins.

---

## 1. Final Decision

**The `DataPipelines` restructure is approved for execution now**, by explicit human
decision.

- We accept the cross-layer folder + namespace move (~230 production files) that the
  prior baseline review deferred as "high churn, low value today."
- **Rationale:** the backend/dashboard is still early and is expected to grow
  significantly (admin, runtime, and dashboard features). Performing this structural
  cleanup now — while the surface is small and well-understood — is materially cheaper
  than performing it after many more runtime features accrete on top of the current
  shape. The churn cost only rises with time.
- The engineering review correctly identified the churn risk. That risk is **accepted**,
  conditional on a disciplined execution model: **phase by phase, stop on first failure,
  build + tests + `git diff --check` after every phase, no broad all-at-once move** (§6,
  §8).

`DataPipelines` is the **final, locked name**. It is not renamed back to `Importing`
(which would mislabel the generation/rebuild workflows `GenerateI3rab` and
`RebuildDisplayWords`, neither of which is an import).

---

## 2. Accepted Risks

| Risk | Accepted because | Controlled by |
|---|---|---|
| **Wide churn (~230 files move folder + namespace)** | Cheaper now than after future growth; mechanical, not behavioral. | Sequential per-feature phases; one commit/PR per slice for revertibility; stop-on-failure. |
| **Merge conflicts with in-flight work** (notably branch `011-mushaf-reader-study-context`) | The cleanup is a one-time cost we choose to pay early. | Coordinate timing; land each phase quickly; rebase active branches between phases. |
| **Missed `using` after a namespace move → build break** | Compile-time, immediate, low impact. | `dotnet build` after every (sub-)phase; the compiler lists every missed reference. |
| **Test compile/behavior break from moved namespaces** | Mechanical; no test *logic* changes. | Targeted `dotnet test` after each slice; full suite at each layer boundary. |
| **Concern-axis shift in Application/Abstractions** (feature-first → concern-then-feature) | The runtime-vs-pipeline seam is a real bounded-context distinction worth naming; consistent with Infrastructure's existing concern-first top level. | `DataPipelines/` children are all named features/workflows — not a dumping folder. |
| **Git history/blame churn across moved files** | Acceptable trade for a clearer long-term structure. | `git log --follow` / `git blame -C` still trace through renames. |

Out-of-scope risks (EF schema, routes, response shapes) are **not** incurred — see §5 and
§9.

---

## 3. Final Target Structure

The end state after all phases. Domain and Shared are unchanged. EF configurations,
runtime reads, and caching stay out of `DataPipelines`.

### 3.1 Application.Abstractions

```text
application/QuranDashboard.Application.Abstractions/
└── Quran/
    ├── DataPipelines/
    │   ├── Foundation/                 (was Import/ — 12 files)
    │   ├── Tafsirs/                    (was Tafsirs/)
    │   ├── Translations/               (was Translations/)
    │   ├── FullI3rab/                  (was FullI3rab/)
    │   ├── Mutashabihat/               (was Mutashabihat/)
    │   ├── Navigation/                 (was Navigation/)
    │   └── Words/
    │       ├── DisplayRebuilding/      (was Words/Display/)
    │       ├── MorphologyImporting/    (was Words/Morphology/ non-Irab)
    │       └── SimpleI3rabGeneration/  (was Words/Morphology/Irab/ — 16 files, kept cohesive)
    └── MushafReader/                    (RUNTIME — stays; NO `Reading/` nesting)
        ├── IMushafPageReader.cs
        ├── IMushafAyahStudyReader.cs
        ├── IMushafSurahCatalogReader.cs
        ├── IMushafStudySourceCatalogReader.cs
        ├── IWordAnalysisReader.cs
        ├── MushafReaderOptions.cs
        └── Responses/
# DELETED: Quran/MushafPages/IMushafPageReadRepository.cs (dead) + empty MushafPages/
```

### 3.2 Application

```text
application/QuranDashboard.Application/
├── DependencyInjection.cs              (root namespace unchanged; only `using`s update)
└── Quran/
    ├── DataPipelines/
    │   ├── Foundation/                 (was Import/ImportQuranFoundation/)
    │   │   └── Validation/             (was Import/Validation/ — 14 files, kept cohesive)
    │   ├── Tafsirs/                    (was Tafsirs/ImportTafsirs/)
    │   ├── Translations/               (was Translations/ImportTranslations/)
    │   ├── FullI3rab/                  (was FullI3rab/ImportFullI3rab/)
    │   ├── Mutashabihat/               (was Mutashabihat/ImportMutashabihat/)
    │   ├── Navigation/                 (was Navigation/ImportNavigationMetadata/)
    │   └── Words/
    │       ├── DisplayRebuilding/      (was Words/RebuildDisplayWords/)
    │       ├── MorphologyImporting/    (was Words/ImportMorphology/)
    │       └── SimpleI3rabGeneration/  (was Words/GenerateI3rab/)
    └── MushafReader/Queries/Get*/      (RUNTIME — UNCHANGED)
```

### 3.3 Infrastructure

```text
infrastructure/QuranDashboard.Infrastructure/
├── DependencyInjection.cs              (root namespace unchanged; only `using`s update)
├── GlobalUsings.cs                     (unchanged)
├── Files/Quran/DataPipelines/<Feature>/        (was Files/Quran/<Feature>/)
├── Persistence/
│   ├── Configurations/Quran/...        (RUNTIME/SCHEMA — UNCHANGED, NOT under DataPipelines)
│   ├── Reads/Quran/MushafReader/       (RUNTIME — UNCHANGED)
│   └── DataPipelines/Quran/<Feature>/  (was Persistence/Repositories/Quran/<Feature>/ — write side)
├── Reports/Quran/DataPipelines/<Feature>/      (was Reports/Quran/<Feature>/)
└── Caching/Quran/MushafReader/         (RUNTIME — UNCHANGED)
```

`<Feature>` in Infrastructure follows the same final names:
`Foundation, Tafsirs, Translations, FullI3rab, Mutashabihat, Navigation,
Words/DisplayRebuilding, Words/MorphologyImporting, Words/SimpleI3rabGeneration`.

### 3.4 Api Controllers

```text
api/QuranDashboard.Api/Controllers/
├── System/
│   └── HealthController.cs                    [Route("api/health")]
├── Dashboard/
│   └── DashboardController.cs                 [Route("api/dashboard")]
└── MushafReader/
    ├── Pages/
    │   └── MushafPagesController.cs           [Route("api/mushaf/pages")]
    ├── Ayahs/
    │   └── MushafAyahStudyController.cs       [Route("api/mushaf/ayahs")]
    ├── Words/
    │   └── MushafWordAnalysisController.cs    [Route("api/mushaf/words")]
    └── Catalogs/
        ├── MushafSurahCatalogController.cs        [Route("api/mushaf/surahs")]
        └── MushafStudySourceCatalogController.cs  [Route("api/mushaf/study-sources")]
```

### 3.5 DataImporter

```text
tools/QuranDashboard.DataImporter/
├── Program.cs                          (slim: arg → verb dispatch + host composition only)
├── Import/ArgumentParsing/             (shared --source/--report-out/--force parser)
├── Import/DefaultPaths/                (feature → default source/report path map; subsumes NavigationImportPaths.cs)
└── Import/VerbRunners/                 (one runner per verb)
```

---

## 4. Namespace Policy

**`namespace follows folder`, applied uniformly** to Application.Abstractions,
Application, Infrastructure, **and Api controllers**. There is **no controller
exception**: the previous "keep old controller namespaces" option is dropped.

Rules:

1. Every moved file's `namespace` changes to match its new folder path. The codebase
   already holds `folder == namespace` everywhere (verified); this preserves that
   invariant instead of introducing drift.
2. Every `using` that referenced an old namespace updates to the new one. This is
   mechanical and tool-assisted; the compiler enforces completeness.
3. **`DependencyInjection.cs`** in Application and Infrastructure keep their **root**
   namespace (`QuranDashboard.Application` / `QuranDashboard.Infrastructure`); only their
   `using` lists change. `AddX` registration calls reference **class names**, which do
   not rename — so no registration call changes.
4. **MushafReader abstractions do not move**, so their namespace
   (`QuranDashboard.Application.Abstractions.Quran.MushafReader`) is **unchanged**. There
   is no `Reading/` sub-namespace. The five reader interfaces, `MushafReaderOptions.cs`,
   and `Responses/` stay exactly where they are (the only abstractions change here is
   deleting the dead `MushafPages/` folder — §6 Phase 1 / Phase 2e).
5. **Controller namespace map (Option A):**

   | Controller | New folder | New namespace |
   |---|---|---|
   | `HealthController` | `Controllers/System/` | `QuranDashboard.Api.Controllers.System` |
   | `DashboardController` | `Controllers/Dashboard/` | `QuranDashboard.Api.Controllers.Dashboard` |
   | `MushafPagesController` | `Controllers/MushafReader/Pages/` | `QuranDashboard.Api.Controllers.MushafReader.Pages` |
   | `MushafAyahStudyController` | `Controllers/MushafReader/Ayahs/` | `QuranDashboard.Api.Controllers.MushafReader.Ayahs` |
   | `MushafWordAnalysisController` | `Controllers/MushafReader/Words/` | `QuranDashboard.Api.Controllers.MushafReader.Words` |
   | `MushafSurahCatalogController` | `Controllers/MushafReader/Catalogs/` | `QuranDashboard.Api.Controllers.MushafReader.Catalogs` |
   | `MushafStudySourceCatalogController` | `Controllers/MushafReader/Catalogs/` | `QuranDashboard.Api.Controllers.MushafReader.Catalogs` |

   (The two catalog controllers intentionally share `Catalogs/` and therefore the
   `...MushafReader.Catalogs` namespace — both are catalog endpoints, cohesive.)

---

## 5. Controller Route Stability Policy

**No API URL changes. No response-shape changes.** This is a hard constraint.

- Every controller carries `[ApiController]` and an **explicit** `[Route("api/...")]`
  (verified for all 7). ASP.NET Core routing resolves routes from these attributes, **not**
  from folder or namespace. Moving files and renaming namespaces therefore changes **no
  URL**.
- Controller **discovery** is assembly-wide (`AddControllers()` scans the assembly), so
  namespace changes do not affect which controllers are registered.
- **Preserve every `[Route]` and every `[Http*]` (`[HttpGet]`/`[HttpPost]`/…) attribute
  and its template exactly.** The only edits permitted in Phase 5 are the file location
  and the `namespace` line.
- Response shapes (`ApiResponse<T>`, the read DTOs), status codes, and Arabic messages
  (`ApiMessages.cs`) are untouched.
- **Execution check (Phase 5):** before the move, capture the route list; after the move,
  diff it. Also grep for any consumer of the old controller namespaces
  (`using QuranDashboard.Api.Controllers`) — expected: none beyond the controllers
  themselves. A green build + the MushafReader endpoint tests + an optional endpoint
  smoke test confirm zero route drift.

---

## 6. Phase-by-Phase Execution Plan

Each phase (and sub-phase) is a **pure folder/namespace move** unless explicitly marked
behavioral, ends green (§7), and is its own commit/PR for revertibility. Execute strictly
in order; **stop on the first failure** (§8).

### Phase 1 — Safe cleanup
- Delete dead `Application.Abstractions/Quran/MushafPages/IMushafPageReadRepository.cs`
  (verified: zero consumers) and remove the now-empty `MushafPages/` folder.
- Repoint the worked example in **canonical `Backend/.architecture/CLEAN_ARCHITECTURE.md`**
  (request-flow / Application.Abstractions sections) from `IMushafPageReadRepository` /
  `MushafPageReadRepository` to a live reader (e.g. `IMushafPageReader`), or annotate it
  as illustrative — so the canonical doc does not reference a deleted type.
- Remove redundant `.gitkeep` files in folders that now have real content:
  `Domain/Quran/Tafsirs/`, `Application.Abstractions/Quran/Tafsirs/`,
  `Application/Quran/Tafsirs/ImportTafsirs/`,
  `Infrastructure/Reports/Quran/{Translations,Irab,Tafsirs}/`,
  `Infrastructure/Files/Quran/{Translations,Morphology/Irab,Tafsirs}/`,
  `Infrastructure/Persistence/Repositories/Quran/{Translations,Irab,Tafsirs}/`.
  **Guard:** delete a `.gitkeep` only after confirming its folder has ≥1 `.cs`.

### Phase 2 — Application.Abstractions `DataPipelines` (sliced per feature)
Folder == namespace, so each move implies its namespace change; update all consumers
(Infrastructure, Application, DataImporter, Tests) in the **same** slice so it ends green.

- **2a — Foundation:** `Quran/Import/` → `Quran/DataPipelines/Foundation/` (12 files).
- **2b — Tafsirs + Translations:** `Quran/Tafsirs/` → `…/DataPipelines/Tafsirs/`;
  `Quran/Translations/` → `…/DataPipelines/Translations/`.
- **2c — FullI3rab + Mutashabihat + Navigation:** each `Quran/<Feature>/` →
  `…/DataPipelines/<Feature>/`.
- **2d — Words pipelines:** `Words/Display/` → `DataPipelines/Words/DisplayRebuilding/`;
  `Words/Morphology/` (non-Irab) → `DataPipelines/Words/MorphologyImporting/`;
  `Words/Morphology/Irab/` → `DataPipelines/Words/SimpleI3rabGeneration/` (16 files, kept
  as one cohesive folder — do **not** sub-split).
- **2e — MushafReader cleanup:** confirm the five reader interfaces stay flat under
  `MushafReader/` (no `Reading/`), with `MushafReaderOptions.cs` and `Responses/`
  unchanged; confirm the dead `MushafPages/` abstraction is removed (done in Phase 1 — if
  for any reason it still exists, remove it here).

End of Phase 2: full `dotnet test` (Abstractions are consumed solution-wide).

### Phase 3 — Application `DataPipelines` (sliced per feature)
`MushafReader/Queries/` stays unchanged (update only its `using`s if a moved Abstractions
namespace forces it).

- **3a — Foundation:** `Quran/Import/ImportQuranFoundation/` →
  `Quran/DataPipelines/Foundation/`; `Quran/Import/Validation/` →
  `Quran/DataPipelines/Foundation/Validation/` (14 files, kept cohesive).
- **3b — Tafsirs + Translations:** `Tafsirs/ImportTafsirs/` → `DataPipelines/Tafsirs/`;
  `Translations/ImportTranslations/` → `DataPipelines/Translations/`.
- **3c — FullI3rab + Mutashabihat + Navigation:** `FullI3rab/ImportFullI3rab/` →
  `DataPipelines/FullI3rab/`; `Mutashabihat/ImportMutashabihat/` →
  `DataPipelines/Mutashabihat/`; `Navigation/ImportNavigationMetadata/` →
  `DataPipelines/Navigation/`.
- **3d — Words pipelines:** `Words/RebuildDisplayWords/` →
  `DataPipelines/Words/DisplayRebuilding/`; `Words/ImportMorphology/` →
  `DataPipelines/Words/MorphologyImporting/`; `Words/GenerateI3rab/` →
  `DataPipelines/Words/SimpleI3rabGeneration/`.
- Update `Application/DependencyInjection.cs` and `DataImporter/Program.cs` `using`s as
  each slice lands.

End of Phase 3: full `dotnet test`.

### Phase 4 — Infrastructure `DataPipelines` (split by concern)
EF configurations, runtime reads, and caching are **not** moved.

- **4a — Files:** `Files/Quran/<Feature>/` → `Files/Quran/DataPipelines/<Feature>/`.
- **4b — Persistence write side:** `Persistence/Repositories/Quran/<Feature>/` →
  `Persistence/DataPipelines/Quran/<Feature>/`. **EF configs in
  `Persistence/Configurations/` are NOT moved.** Review the move list against this rule
  before executing.
- **4c — Reports:** `Reports/Quran/<Feature>/` →
  `Reports/Quran/DataPipelines/<Feature>/`.
- Update `Infrastructure/DependencyInjection.cs` `using`s as each sub-phase lands.

End of Phase 4 (after 4a+4b+4c): full `dotnet test`.

### Phase 5 — Api controllers re-group (Option A; routes unchanged)
- Move the 7 controllers into the §3.4 tree.
- Update each controller `namespace` to match its new folder (§4 table).
- Preserve every `[Route]` and `[Http*]` attribute exactly. No URL/response change.

### Phase 6 — DataImporter `Program.cs` split (behavioral; test-covered)
- Extract `Import/ArgumentParsing/`, `Import/DefaultPaths/` (subsuming
  `NavigationImportPaths.cs`), and `Import/VerbRunners/` (one runner per verb).
- `Program.cs` becomes thin verb-dispatch + host composition only. No business logic
  moves in or out; behavior is covered by import integration tests that build the same
  host the CLI uses.

### Phase 7 — Optional oversized-file splits (deferred; independent)
- Only for cohesive files at/over hard thresholds: `DisplayWordsSql.cs` (554) and the
  400–463-line manifest readers/assemblers (`NavigationManifestReader`,
  `FullI3rabAssembler`, `TranslationManifestReader`, `TafsirAssembler`,
  `FullI3rabManifestReader`, `MorphologyAssembler`).
- Split **in place** by sub-responsibility within the feature folder; never via dumping
  folders. Each is its own change with its feature's tests run before and after.
- Independent of Phases 1–6; may be done before, after, or never. Do **not** bundle into a
  structural-move PR.

---

## 7. Verification Commands Per Phase

> **Docker prerequisite (mandatory):** the integration tests use **Testcontainers
> (PostgreSQL)**. **Docker must be running** before any `dotnet test`. If Docker is down,
> integration tests fail or skip — treat a skipped integration suite as a **failed gate**,
> not a pass.

> **After every (sub-)phase, always run, in order:**
> ```bash
> dotnet build QuranDashboard.sln -c Debug --nologo   # 0 warnings / 0 errors required
> git diff --check                                     # no whitespace/conflict markers
> ```
> then the slice's targeted tests below. Run the **full** suite at each layer boundary.

**Phase 1**
```bash
dotnet build QuranDashboard.sln -c Debug --nologo
dotnet test tests/QuranDashboard.Tests --nologo --filter "FullyQualifiedName~Mushaf"
```

**Phase 2 — per slice** (build + `git diff --check` first, then the slice filter)
```bash
# 2a Foundation
dotnet test tests/QuranDashboard.Tests --nologo --filter "FullyQualifiedName~Import"
# 2b Tafsirs + Translations
dotnet test tests/QuranDashboard.Tests --nologo \
  --filter "FullyQualifiedName~Tafsirs|FullyQualifiedName~Translations"
# 2c FullI3rab + Mutashabihat + Navigation
dotnet test tests/QuranDashboard.Tests --nologo \
  --filter "FullyQualifiedName~FullI3rab|FullyQualifiedName~Mutashabihat|FullyQualifiedName~Navigation"
# 2d Words pipelines
dotnet test tests/QuranDashboard.Tests --nologo \
  --filter "FullyQualifiedName~WordsDisplay|FullyQualifiedName~WordsMorphology|FullyQualifiedName~WordsSimpleI3rab"
# 2e MushafReader cleanup
dotnet test tests/QuranDashboard.Tests --nologo --filter "FullyQualifiedName~MushafReader"
```
**End of Phase 2 (layer boundary):**
```bash
dotnet test QuranDashboard.sln --nologo
```

**Phase 3 — per slice** (same slice filters as 2a–2d)
```bash
# 3a Foundation
dotnet test tests/QuranDashboard.Tests --nologo --filter "FullyQualifiedName~Import"
# 3b Tafsirs + Translations
dotnet test tests/QuranDashboard.Tests --nologo \
  --filter "FullyQualifiedName~Tafsirs|FullyQualifiedName~Translations"
# 3c FullI3rab + Mutashabihat + Navigation
dotnet test tests/QuranDashboard.Tests --nologo \
  --filter "FullyQualifiedName~FullI3rab|FullyQualifiedName~Mutashabihat|FullyQualifiedName~Navigation"
# 3d Words pipelines
dotnet test tests/QuranDashboard.Tests --nologo \
  --filter "FullyQualifiedName~WordsDisplay|FullyQualifiedName~WordsMorphology|FullyQualifiedName~WordsSimpleI3rab"
```
**End of Phase 3 (layer boundary):**
```bash
dotnet test QuranDashboard.sln --nologo
```

**Phase 4 — per sub-phase** (each moves all features; use the all-feature filter, then a
full run after 4c)
```bash
ALL_FEATURES="FullyQualifiedName~Import|FullyQualifiedName~Tafsirs|FullyQualifiedName~Translations|FullyQualifiedName~FullI3rab|FullyQualifiedName~Mutashabihat|FullyQualifiedName~Navigation|FullyQualifiedName~WordsDisplay|FullyQualifiedName~WordsMorphology|FullyQualifiedName~WordsSimpleI3rab"
# after 4a Files
dotnet test tests/QuranDashboard.Tests --nologo --filter "$ALL_FEATURES"
# after 4b Persistence write side  (re-confirm EF Configurations/ were NOT moved)
dotnet test tests/QuranDashboard.Tests --nologo --filter "$ALL_FEATURES"
# after 4c Reports
dotnet test tests/QuranDashboard.Tests --nologo --filter "$ALL_FEATURES"
```
**End of Phase 4 (layer boundary):**
```bash
dotnet test QuranDashboard.sln --nologo
```

**Phase 5 — controllers**
```bash
dotnet build QuranDashboard.sln -c Debug --nologo
git diff --check
# expect no consumers of old controller namespaces other than the controllers themselves:
grep -rn --include="*.cs" "QuranDashboard.Api.Controllers" api tests | grep -vE "/(bin|obj)/"
dotnet test tests/QuranDashboard.Tests --nologo \
  --filter "FullyQualifiedName~MushafReader|FullyQualifiedName~MushafPage|FullyQualifiedName~AyahStudy|FullyQualifiedName~WordAnalysis|FullyQualifiedName~Catalog"
# Optional: endpoint smoke test to confirm api/health, api/dashboard, api/mushaf/* are live and unchanged.
```

**Phase 6 — DataImporter split** (typo fixed: `FullyQualifiedName~Tafsirs`)
```bash
dotnet build QuranDashboard.sln -c Debug --nologo
git diff --check
dotnet test tests/QuranDashboard.Tests --nologo \
  --filter "FullyQualifiedName~Import|FullyQualifiedName~Morphology|FullyQualifiedName~Mutashabihat|FullyQualifiedName~Tafsirs|FullyQualifiedName~Translations|FullyQualifiedName~Navigation|FullyQualifiedName~FullI3rab|FullyQualifiedName~WordsSimpleI3rab|FullyQualifiedName~WordsDisplay|FullyQualifiedName~Rebuild"
# Recommended final gate for this behavioral phase:
dotnet test QuranDashboard.sln --nologo
```

**Phase 7 — per file split**
```bash
dotnet build QuranDashboard.sln -c Debug --nologo
# Run only the affected feature's assembler/manifest/report/import tests, before and after each split.
```

**Final, after the last executed phase:**
```bash
dotnet test QuranDashboard.sln --nologo
# Optional structure guard — must return nothing:
find api domain application infrastructure shared tools -type d \
  \( -name Enums -o -name Models -o -name DTOs -o -name Helpers -o -name Utils -o -name Services \) \
  -not -path '*/bin/*' -not -path '*/obj/*'
```

---

## 8. Stop-on-Failure Rules

1. **Sequential only.** Execute phases (and the sub-phases within Phase 2/3/4) strictly in
   the documented order. No broad all-at-once move.
2. **Gate after every (sub-)phase:** `dotnet build` (0 warnings / 0 errors) → `git diff
   --check` (clean) → the slice's tests green. Full `dotnet test` at each layer boundary
   (end of Phase 2, 3, 4) and as the final gate.
3. **Stop on the first failure.** If a build, a test, or `git diff --check` fails, **halt
   immediately**. Do not start the next (sub-)phase.
4. **On failure, fix-forward or revert that slice only.** Each slice is its own commit/PR,
   so a single slice can be reverted without unwinding earlier green phases.
5. **A skipped integration suite is a failure.** If Testcontainers can't start (Docker
   down), the gate has not passed — fix the environment and re-run before proceeding.
6. **No scope bleed.** A structural slice changes only folders, namespaces, and `using`s
   (plus the controller `namespace`/location in Phase 5). Behavioral edits belong only to
   Phase 6 (DataImporter) and Phase 7 (optional splits) and must not ride inside a
   structural slice.
7. **Re-verify the EF-config exclusion** explicitly at Phase 4b before moving anything.

---

## 9. Explicit Non-Goals

This refactor does **not**, and must not:

- Move EF configurations under `DataPipelines` — `Persistence/Configurations/Quran/...`
  stays as schema mapping (optional foundation regroup is out of scope here).
- Move runtime reads (`Persistence/Reads/Quran/MushafReader/`) or caching
  (`Caching/Quran/MushafReader/`) under `DataPipelines`.
- Touch **Domain** or **Shared** — both unchanged.
- Change any API URL, route template, HTTP verb, status code, response shape
  (`ApiResponse<T>` / read DTOs), or Arabic message.
- Introduce any `Reading/` sub-namespace under MushafReader abstractions.
- Rename `DataPipelines` to `Importing` or anything else — the name is locked.
- Add, change, or run EF Core migrations, or run `dotnet ef database update`.
- Introduce new projects, change project references, or alter the Clean Architecture
  dependency direction.
- Introduce generic repositories or move read DTOs out of Application.Abstractions.
- Create any global dumping folder (`Enums/`, `Models/`, `DTOs/`, `Helpers/`, `Utils/`,
  `Services/`).
- Change any test *logic* (only test `using`s update with the production moves).
- Bundle the optional Phase 7 file splits into a structural-move PR.

---

## 10. Summary of Corrections Made From the Engineering Review

| Review finding | Correction applied in this plan |
|---|---|
| **MAJOR-1** — `FullyMethodName~Tafsirs` typo silently dropped Tafsirs verification | Fixed to `FullyQualifiedName~Tafsirs` in the Phase 6 command (§7); added a full-suite final gate for that behavioral phase. |
| **MAJOR-2** — wide rename reversed the baseline without an acknowledged decision | §1 records the explicit human decision and rationale (early project, cheaper now than later); §2 lists the accepted risks. The plan states it intentionally supersedes inventory §6/§7/§9. |
| **MAJOR-3** — Phases 2 and 3 were big-bang despite being the widest blast radius | §6 slices Phase 2 into 2a–2e and Phase 3 into 3a–3d, per feature; each slice ends green; full suite at each layer boundary. |
| **MINOR-1** — namespace policy was inconsistent (Option A everywhere, Option B for controllers) | §4 applies `namespace follows folder` **uniformly, including controllers**; the controller exception is removed. §5 keeps routes stable via explicit attributes. |
| **MINOR-2** — redundant `MushafReader/Reading/` nesting | Dropped. §3.1/§4 keep the reader interfaces flat under `MushafReader/`; no `Reading/`. |
| **MINOR-3** — verification omitted the Docker/Testcontainers prerequisite | §7 adds a mandatory Docker prerequisite; §8 makes a skipped integration suite a failed gate. |
| **MINOR-4** — Phase 4b filter keyed on brittle concept words | §7 replaces it with deterministic feature-name filters (`$ALL_FEATURES`) and a full run after 4c. |
| **NOTE-1** — deleting the dead interface left a dangling example in canonical `CLEAN_ARCHITECTURE.md` | §6 Phase 1 adds repointing/annotating that example as an explicit step. |
| **NOTE-2** — the "5-sibling" rule is non-canonical | Treated only as a review trigger; cohesive folders (16-file `SimpleI3rabGeneration/`, 14-file `Foundation/Validation/`) are explicitly **kept**, never split on count. |
| **NOTE-3** — filtered runs only; no final full suite | §7 adds full `dotnet test` at every layer boundary and as the final gate. |
| **NOTE-4** — `Foundation` drops the "import" verb | Accepted deliberately; under `DataPipelines/` the pipeline context is implied. Name retained as `Foundation`. |

---

### Appendix — artifacts produced

This document only:
`Backend/report/architecture/backend-target-structure-final-execution-plan.md`. No code,
namespaces, migrations, or commits were changed. The earlier feasibility report and
engineering review remain in place as history; this plan is the execution-ready successor.
