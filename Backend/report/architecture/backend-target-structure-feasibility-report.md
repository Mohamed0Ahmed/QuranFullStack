# Backend Target Structure — Feasibility Report

> **Scope:** Backend only. **Report only.** No files were moved, renamed, formatted,
> migrated, or committed. No namespaces were changed. No logic was changed. This report
> evaluates the *proposed target structure* for safety, usefulness, and mechanical
> feasibility, and proposes a phased plan.
>
> **Canonical references:** `CODING_PRINCIPLES.md`,
> `Backend/.architecture/BACKEND_STRUCTURE.md` (placement + file-size thresholds),
> `Backend/.architecture/CLEAN_ARCHITECTURE.md` (layers + dependency direction),
> `Backend/.architecture/API_GUIDELINES.md` (API boundary), `Backend/AGENTS.md` /
> `Backend/CLAUDE.md`, and the prior
> `Backend/report/architecture/backend-project-structure-inventory.md`.
>
> **Baseline:** `dotnet build QuranDashboard.sln` was green (0 warnings / 0 errors) at
> the time of the inventory. This report does not change that.

---

## 0. Executive Verdict

**FEASIBLE — recommended, with two caveats and one renaming note.**

The proposed target structure is **safe, useful, and mechanically feasible**. It does
not change behavior, routes, dependencies, or the Clean Architecture direction. It
formalizes a **runtime-vs-data-pipeline separation** that the current code already
implements but does not yet *name* consistently.

Key feasibility conclusions:

1. **Namespaces track folders exactly** in every project (verified: every
   `namespace X` equals its folder path). This makes folder moves equivalent to
   namespace moves and keeps the refactor **mechanical and predictable**.
2. **Routes are folder-independent.** Every controller carries an explicit
   `[Route("api/...")]` attribute, so moving controller files between folders changes
   **no URL**. (§9)
3. **EF configurations stay out of `DataPipelines`** as required — confirmed they live
   in `Persistence/Configurations/`, which the target preserves. (§3.3)
4. **Caveat A (size):** the restructure touches a large number of files across 3 layers
   and ~9 features. Each individual move is trivial, but the *aggregate* is wide. This
   is why the plan is **phase-per-layer-per-concern** with focused tests between.
5. **Caveat B (test churn):** tests are **feature-grouped by the current namespace
   path** (`Tests/Quran/<Feature>/`), and several test files reference the production
   namespaces directly. Phase 2/3/4 will require corresponding test `using` updates.
   This is mechanical but must be planned, not skipped.
6. **Naming note:** `DataPipelines` is a **better top-level name than `Importing`** for
   this codebase, because the bucket mixes **import, generation, and rebuild** — not
   only importing. `Importing` would mislabel `GenerateI3rab` and `RebuildDisplayWords`.
   (§8)

The structure honors all 8 target principles (§1.1) and the 5-sibling cohesion rule
(§7).

---

## 1. Target Principles vs. Current Reality

### 1.1 Principle compliance

| # | Target principle | Current state | Target honors it? |
|---|---|---|---|
| 1 | Keep Clean Architecture dependency direction unchanged | Correct today (inventory §1.3). Target moves files **within** existing layers only. | ✅ Yes — no project reference changes. |
| 2 | Keep domain/feature/bounded-context organization | Feature-first everywhere (`Quran/<Feature>/`). Target keeps `Quran/<Feature>/` and only inserts a **concern axis** (`DataPipelines` vs runtime). | ✅ Yes. |
| 3 | No dumping folders (Enums/Models/DTOs/Helpers/Utils/Services) | None exist today (inventory §4.3/§4.4). Target introduces none. | ✅ Yes. |
| 4 | Separate data-prep workflows from runtime/API reads | Currently mixed **only implicitly**: runtime read contracts (`MushafReader/`) sit as siblings of pipeline contracts (`Import/`, `Words/...`, `Tafsirs/`, …). Target makes the split **explicit** via `DataPipelines/`. | ✅ Yes — this is the primary value of the refactor. |
| 5 | Do not mix import/generation/rebuild with runtime readers/controllers | Already true at the file level (no controller imports a pipeline type); target enforces it at the **folder** level. | ✅ Yes. |
| 6 | EF configurations remain schema mapping; do not move under DataPipelines | EF configs live in `Persistence/Configurations/Quran/...`. Target keeps them there and explicitly **not** under `DataPipelines`. | ✅ Yes. |
| 7 | Controllers thin, grouped by runtime feature | Controllers are thin and feature-grouped (`Controllers/Mushaf/`). Target refines the grouping into runtime sub-features. | ✅ Yes. |
| 8 | Do not change behavior | Pure folder/namespace moves; no logic edits. | ✅ Yes (by construction). |

### 1.2 What the target actually changes (summary)

- Inserts one new concern axis — `DataPipelines/` — between `Quran/` and `<Feature>/`
  for all **import / generation / rebuild** code in Application.Abstractions,
  Application, and Infrastructure (`Files`, `Persistence` write side, `Reports`).
- Leaves **runtime** code (`MushafReader/`, EF `Configurations/`, `Reads/`,
  `Caching/`) exactly where it is.
- Removes one dead file and redundant `.gitkeep`s.
- Re-groups Api controllers by runtime sub-feature (URLs unchanged).
- Splits the oversized `DataImporter/Program.cs` into parser / default-paths /
  verb-runners.

Nothing in the target requires a new project, a new package, or a dependency-direction
change.

---

## 2. Per-Project Classification

Legend: **MATCH** = already matches · **SAFE** = safe mechanical move (build + focused
tests) · **TESTS** = needs focused tests because logic/surface is touched · **DEFER** =
risky/defer · **NOT REC** = not recommended.

| Project | Classification | Rationale |
|---|---|---|
| `QuranDashboard.Domain` | **MATCH** | Target does not touch Domain. No change. |
| `QuranDashboard.Shared` | **MATCH** | Target does not touch Shared. No change. |
| `QuranDashboard.Application.Abstractions` | **SAFE** (Phase 2) | Move pipeline contracts under `DataPipelines/<Feature>/`; keep `MushafReader/` runtime contracts; delete dead `MushafPages/` interface. Folder=namespace, so mechanical. |
| `QuranDashboard.Application` | **SAFE** (Phase 3) | Move pipeline use-cases under `DataPipelines/<Feature>/`; keep `MushafReader/Queries/`. Mechanical. |
| `QuranDashboard.Infrastructure` | **SAFE, but wide** (Phase 4, three sub-phases) | Largest blast radius: `Files/`, `Persistence` write side, `Reports/` all get a `DataPipelines/` insert. EF `Configurations/`, `Reads/`, `Caching/` untouched. Each sub-phase independently safe. |
| `QuranDashboard.Api` | **SAFE** (Phase 5) | Controller folder re-group only; explicit `[Route]` attributes keep all URLs stable. |
| `QuranDashboard.DataImporter` | **TESTS** (Phase 6) | `Program.cs` split is structural but behavioral; covered by import integration tests. Verify after. |
| `QuranDashboard.Tests` | **TESTS** (rides with Phases 2–4) | Test files reference production namespaces; `using` updates follow each production phase. No test *logic* changes. |
| Oversized cohesive files (Phase 7) | **DEFER** | Only split where useful; not part of the structural refactor. |

---

## 3. Current → Target, Layer by Layer

### 3.1 Application.Abstractions

**Current (top-level under `Quran/`):** `Import/`, `FullI3rab/`, `Mutashabihat/`,
`Navigation/`, `Tafsirs/`, `Translations/`, `Words/{Display,Morphology/{,Irab}}`,
`MushafReader/{,Responses/}`, and the dead `MushafPages/`.

**Target:** pipeline contracts → `Quran/DataPipelines/<Feature>/`; runtime read
contracts stay in `Quran/MushafReader/{Reading,Responses}/`; delete `MushafPages/`.

**Classification: SAFE (Phase 2).** All files keep their *contents*; only the folder
(and therefore the namespace tail) changes.

Exact moves (folder = namespace, so each move implies the namespace change in §5):

| From (`Quran/...`) | To (`Quran/DataPipelines/...`) | Files |
|---|---|---|
| `Import/` | `Foundation/` | 12 files: `IQuranImportSource`, `IQuranImportWriter`, `IImportReportWriter`, `AssembledQuranData`, `{AyahMeta,SurahMeta,Layout,Line,WordRecord}Dto`, `QuranImportSourceData`, `QuranImportValidationResult`, `ImportRefusalMessages` |
| `Tafsirs/` | `Tafsirs/` | 11 files (incl. `.gitkeep`) |
| `Translations/` | `Translations/` | 11 files |
| `Navigation/` | `Navigation/` | 11 files |
| `FullI3rab/` | `FullI3rab/` | 11 files |
| `Mutashabihat/` | `Mutashabihat/` | 5 files |
| `Words/Display/` | `Words/DisplayRebuilding/` | 6 files |
| `Words/Morphology/` (+ non-Irab) | `Words/MorphologyImporting/` | 4 files (`IMorphologyImportSource`, `MorphologyImportResult`, `MorphologyInvariants`, `MorphologySourceData`) |
| `Words/Morphology/Irab/` | `Words/SimpleI3rabGeneration/` | 16 files (the `I3rab*` / `II3rab*` contracts) |

**Runtime (stay, reorganize):**

| From | To | Note |
|---|---|---|
| `MushafReader/` (5 reader interfaces + `MushafReaderOptions.cs`) | `MushafReader/Reading/` | Target groups reader interfaces under `Reading/`. |
| `MushafReader/Responses/` | `MushafReader/Responses/` | Unchanged. |

**Delete (Phase 1):** `Quran/MushafPages/IMushafPageReadRepository.cs` (empty, zero
references — confirmed in inventory §4.2 and re-confirmed by grep: only itself + the
architecture doc mention it).

> **Caveat — folder-size rule (§7):** `Words/Morphology/Irab/` has **16** sibling
> files. After the move it becomes `DataPipelines/Words/SimpleI3rabGeneration/` with
> the same 16. Per the cohesion rule this is a **single cohesive responsibility**
> (simple-iʿrab generation contracts), so it should **stay as one folder** — do not
> sub-split. See §7.

### 3.2 Application

**Current:** pipeline use-cases under `Quran/{Import,FullI3rab,Mutashabihat,Navigation,
Tafsirs,Translations,Words/{GenerateI3rab,ImportMorphology,RebuildDisplayWords}}/`;
runtime reads under `Quran/MushafReader/Queries/Get*/`.

**Target:** pipeline → `Quran/DataPipelines/<Feature>/`; runtime stays.

**Classification: SAFE (Phase 3).** Mechanical; `DependencyInjection.cs` `using` lines
follow the namespace moves (§5).

| From (`Quran/...`) | To (`Quran/DataPipelines/...`) |
|---|---|
| `Import/ImportQuranFoundation/` (+ `Import/Validation/`) | `Foundation/` with nested `Validation/` |
| `FullI3rab/ImportFullI3rab/` | `FullI3rab/` |
| `Mutashabihat/ImportMutashabihat/` | `Mutashabihat/` |
| `Navigation/ImportNavigationMetadata/` | `Navigation/` |
| `Tafsirs/ImportTafsirs/` | `Tafsirs/` |
| `Translations/ImportTranslations/` | `Translations/` |
| `Words/RebuildDisplayWords/` | `Words/DisplayRebuilding/` |
| `Words/ImportMorphology/` | `Words/MorphologyImporting/` |
| `Words/GenerateI3rab/` | `Words/SimpleI3rabGeneration/` |

`Quran/MushafReader/Queries/Get*/` — **unchanged** (already matches target).

> Note on the target's `Foundation/Validation/` nesting: the current
> `Import/Validation/` folder (14 sibling files) is **cohesive** (all foundation import
> validation checks). Per §7 it should stay as one `Validation/` folder under
> `Foundation/`; do not split by check type.

### 3.3 Infrastructure

Target keeps the **concern-first** top level (`Files`, `Persistence`, `Reports`,
`Caching`) and inserts `DataPipelines/` **only on the data-prep side**. EF
configurations, runtime reads, and caching are untouched.

#### 3.3a `Files/Quran/` → `Files/Quran/DataPipelines/<Feature>/` (Phase 4a — SAFE)

| From (`Files/Quran/...`) | To (`Files/Quran/DataPipelines/...`) | Sibling count |
|---|---|---|
| `Import/` | `Foundation/` | 5 |
| `FullI3rab/` | `FullI3rab/` | 5 |
| `Mutashabihat/` | `Mutashabihat/` | 5 |
| `Navigation/` | `Navigation/` | 5 |
| `Tafsirs/` | `Tafsirs/` | 5 |
| `Translations/` | `Translations/` | 7 |
| `Morphology/` (+ non-Irab) | `Words/MorphologyImporting/` | 9 |
| `Morphology/Irab/` | `Words/SimpleI3rabGeneration/` | 6 |

All cohesive single-responsibility folders; no sub-split needed (§7).

#### 3.3b `Persistence/` write side → `Persistence/DataPipelines/Quran/<Feature>/` (Phase 4b — SAFE)

Current write side lives in `Persistence/Repositories/Quran/<Feature>/`. Target renames
the concern to `Persistence/DataPipelines/Quran/<Feature>/`. EF configurations and
reads are **not** in this set.

| From (`Persistence/Repositories/Quran/...`) | To (`Persistence/DataPipelines/Quran/...`) | Sibling count |
|---|---|---|
| `Import/` | `Foundation/` | 1 (`EfBulkQuranImportWriter`) |
| `FullI3rab/` | `FullI3rab/` | 6 |
| `Mutashabihat/` | `Mutashabihat/` | 8 |
| `Navigation/` | `Navigation/` | 6 |
| `Tafsirs/` | `Tafsirs/` | 6 |
| `Translations/` | `Translations/` | 6 |
| `Morphology/` | `Words/MorphologyImporting/` | 7 |
| `Irab/` | `Words/SimpleI3rabGeneration/` | 8 |
| `Words/Display/` | `Words/DisplayRebuilding/` | 2 |

> **IMPORTANT — do NOT move EF configurations here.** The target is explicit
> (principle 6): `Persistence/Configurations/Quran/...` stays put and is only
> optionally regrouped by feature (§3.3d). The write-side bulk-copiers / SQL /
> validation-runners / report-builders are the only contents of
> `Persistence/DataPipelines/`.

#### 3.3c `Reports/Quran/` → `Reports/Quran/DataPipelines/<Feature>/` (Phase 4c — SAFE)

| From (`Reports/Quran/...`) | To (`Reports/Quran/DataPipelines/...`) |
|---|---|
| (root) `MarkdownJsonImportReportWriter.cs` | `Foundation/` |
| `FullI3rab/` | `FullI3rab/` |
| `Mutashabihat/` | `Mutashabihat/` |
| `Navigation/` | `Navigation/` |
| `Tafsirs/` | `Tafsirs/` |
| `Translations/` | `Translations/` |
| `Morphology/` | `Words/MorphologyImporting/` |
| `Irab/` | `Words/SimpleI3rabGeneration/` |
| `Words/` (`MarkdownJsonDisplayWordsReportWriter`) | `Words/DisplayRebuilding/` |

#### 3.3d `Persistence/Configurations/Quran/` — **MATCH, optional regroup** (not a DataPipelines move)

Current: a mix of root-level foundation configs (`Ayah`, `MushafLine`, `MushafPage`,
`QuranWord`, `Surah`) plus per-feature subfolders. Target allows grouping the root
foundation configs under `Configurations/Quran/Foundation/`. This is **optional** and
**low value** (only 5 root files, all cohesive as "foundation schema"). Classification:
**optional / defer** unless doing it for symmetry.

#### 3.3e `Persistence/Reads/Quran/MushafReader/` and `Caching/Quran/MushafReader/` — **MATCH**

Unchanged by target. 5 read services + 4 caching decorators stay.

### 3.4 Api Controllers — **SAFE (Phase 5), URLs unchanged**

Current: `Controllers/{DashboardController, HealthController, Mushaf/}`. Target
regroups by runtime sub-feature. Because every controller has an explicit
`[Route("api/...")]`, **no URL changes**:

| Controller | Current `[Route]` | Current folder | Target folder |
|---|---|---|---|
| `HealthController` | `api/health` | `Controllers/` | `Controllers/System/` |
| `DashboardController` | `api/dashboard` | `Controllers/` | `Controllers/Dashboard/` |
| `MushafPagesController` | `api/mushaf/pages` | `Controllers/Mushaf/` | `Controllers/MushafReader/Pages/` |
| `MushafAyahStudyController` | `api/mushaf/ayahs` | `Controllers/Mushaf/` | `Controllers/MushafReader/Ayahs/` |
| `MushafWordAnalysisController` | `api/mushaf/words` | `Controllers/Mushaf/` | `Controllers/MushafReader/Words/` |
| `MushafSurahCatalogController` | `api/mushaf/surahs` | `Controllers/Mushaf/` | `Controllers/MushafReader/Catalogs/` |
| `MushafStudySourceCatalogController` | `api/mushaf/study-sources` | `Controllers/Mushaf/` | `Controllers/MushafReader/Catalogs/` |

(See §9 for the route-stability proof and the `ApiController` discovery note.)

### 3.5 DataImporter — **TESTS (Phase 6)**

Current: a single 1058-line `Program.cs`. Target: thin `Program.cs` +
`Import/{ArgumentParsing,DefaultPaths,VerbRunners}/`. No business logic enters or
leaves; behavior is fully covered by import integration tests. Details in §6/§10.

---

## 4. Exact Files That Would Move

Compiled from the live tree. (Counts exclude `.gitkeep` unless noted. Full per-file
lists live in the inventory §2; only the *change* is summarized here.)

- **Application.Abstractions — pipeline contracts:** `Import/` (12) + `Tafsirs/` (10 +
  keep) + `Translations/` (10) + `Navigation/` (10) + `FullI3rab/` (10) +
  `Mutashabihat/` (4) + `Words/Display/` (6) + `Words/Morphology/` non-Irab (4) +
  `Words/Morphology/Irab/` (16) → `DataPipelines/...`. ~72 files.
- **Application.Abstractions — runtime:** 5 reader interfaces +
  `MushafReaderOptions.cs` → `MushafReader/Reading/`. 6 files.
- **Application — pipeline use-cases:** `Import/ImportQuranFoundation/` (4) +
  `Import/Validation/` (14) + 5 feature `Import<Feature>/` folders (3–4 each) + 3
  `Words/` use-cases (3 each) → `DataPipelines/...`. ~45 files.
- **Infrastructure/Files/Quran/** pipeline readers/assemblers: ~48 files →
  `DataPipelines/<Feature>/`.
- **Infrastructure/Persistence/Repositories/Quran/** write side: ~50 files →
  `Persistence/DataPipelines/Quran/<Feature>/`. (EF configs excluded.)
- **Infrastructure/Reports/Quran/** writers: ~10 files →
  `Reports/Quran/DataPipelines/<Feature>/`.
- **Api/Controllers/** : 7 files re-grouped (no content change beyond optional
  namespace — see §5/§9).
- **DataImporter/Program.cs** : 1 file → split into ~4–6 files (Phase 6).

**Totals:** ~230 production files change folder (and namespace tail). Each move is
trivial; the volume is the only reason to phase it.

---

## 5. Expected Namespace Changes

**Rule observed in the codebase: folder path == namespace.** Verified across
Application.Abstractions, Application, and Infrastructure (every `namespace
QuranDashboard.<Layer>.<...>` matches its folder). Therefore each folder move above
implies exactly one namespace-segment insertion/rename.

Representative mappings (full set follows the same pattern):

| Layer | Old namespace tail | New namespace tail |
|---|---|---|
| Abstractions | `…Abstractions.Quran.Import` | `…Abstractions.Quran.DataPipelines.Foundation` |
| Abstractions | `…Abstractions.Quran.Tafsirs` | `…Abstractions.Quran.DataPipelines.Tafsirs` |
| Abstractions | `…Abstractions.Quran.Words.Display` | `…Abstractions.Quran.DataPipelines.Words.DisplayRebuilding` |
| Abstractions | `…Abstractions.Quran.Words.Morphology` | `…Abstractions.Quran.DataPipelines.Words.MorphologyImporting` |
| Abstractions | `…Abstractions.Quran.Words.Morphology.Irab` | `…Abstractions.Quran.DataPipelines.Words.SimpleI3rabGeneration` |
| Abstractions | `…Abstractions.Quran.MushafReader` (readers) | `…Abstractions.Quran.MushafReader.Reading` |
| Application | `…Application.Quran.Import.ImportQuranFoundation` | `…Application.Quran.DataPipelines.Foundation` |
| Application | `…Application.Quran.Import.Validation` | `…Application.Quran.DataPipelines.Foundation.Validation` |
| Application | `…Application.Quran.Words.GenerateI3rab` | `…Application.Quran.DataPipelines.Words.SimpleI3rabGeneration` |
| Application | `…Application.Quran.Words.ImportMorphology` | `…Application.Quran.DataPipelines.Words.MorphologyImporting` |
| Application | `…Application.Quran.Words.RebuildDisplayWords` | `…Application.Quran.DataPipelines.Words.DisplayRebuilding` |
| Application | `…Application.Quran.Tafsirs.ImportTafsirs` | `…Application.Quran.DataPipelines.Tafsirs` |
| Infrastructure/Files | `…Files.Quran.Import` | `…Files.Quran.DataPipelines.Foundation` |
| Infrastructure/Files | `…Files.Quran.Morphology.Irab` | `…Files.Quran.DataPipelines.Words.SimpleI3rabGeneration` |
| Infrastructure/Persistence | `…Persistence.Repositories.Quran.Morphology` | `…Persistence.DataPipelines.Quran.Words.MorphologyImporting` |
| Infrastructure/Persistence | `…Persistence.Repositories.Quran.Irab` | `…Persistence.DataPipelines.Quran.Words.SimpleI3rabGeneration` |
| Infrastructure/Persistence | `…Persistence.Repositories.Quran.Words.Display` | `…Persistence.DataPipelines.Quran.Words.DisplayRebuilding` |
| Infrastructure/Reports | `…Reports.Quran` (import writer) | `…Reports.Quran.DataPipelines.Foundation` |
| Infrastructure/Reports | `…Reports.Quran.Irab` | `…Reports.Quran.DataPipelines.Words.SimpleI3rabGeneration` |

**Namespace policy choice (decide before Phase 2):**

- **Option A — namespace follows folder (recommended).** Each moved file's `namespace`
  changes to match the new folder. Maximally consistent with the existing convention
  and aids navigation. Cost: every `using` that referenced the old namespace updates
  (mechanical, tool-assisted).
- **Option B — keep old namespaces, move files only.** Possible but **not
  recommended**: it breaks the folder==namespace invariant the codebase has held
  everywhere, creating permanent drift between path and namespace. Reject unless there
  is an explicit reason.

**Api controllers:** ASP.NET Core discovers controllers by assembly scan + `[ApiController]`
+ `[Route]`, **not** by namespace or folder. Controller namespaces *may* be left
unchanged even when files move folders (Phase 5 can be a pure file move with no
namespace edit). This is the one layer where Option B is acceptable. See §9.

**Special:** `QuranDashboard.Infrastructure` `DependencyInjection.cs` and
`QuranDashboard.Application` `DependencyInjection.cs` declare their own `namespace
QuranDashboard.<Layer>` (root), which is unchanged; only their `using` lists update.

---

## 6. DI Registration Updates Likely Needed

Both DI files register types by **concrete class name** (not by string), so a namespace
move does not break registration resolution — but the `using` directives at the top of
each DI file must follow the namespace moves (Option A).

- **`Application/DependencyInjection.cs`** — 15 `using QuranDashboard.Application.Quran.…`
  lines (verified). After Phase 3, the pipeline `using`s become
  `…Quran.DataPipelines.<Feature>`; the 5 MushafReader `using`s are unchanged.
- **`Infrastructure/DependencyInjection.cs`** — ~33 `using` lines spanning Abstractions
  + Infrastructure feature namespaces (verified). After Phases 2 and 4, the pipeline
  `using`s update; the `MushafReader` reader/caching `using`s are unchanged.
- **No `AddX` registration *calls* change** — they reference class names that do not
  rename. Only `using` directives change.
- **`Api/Program.cs` / `AddApiServices`** — unchanged (no feature namespaces).
- **`DataImporter/Program.cs`** — its `using QuranDashboard.Application.Quran.…` lines
  follow Phase 3; Phase 6 then re-shapes the file internally.

**Test-side DI extensions:** `Tests/Quran/Mutashabihat/MutashabihatTestServiceCollectionExtensions.cs`
and `Tests/Quran/WordsMorphology/MorphologyTestServiceCollectionExtensions.cs` reference
production namespaces; their `using`s update with the corresponding production phase.

---

## 7. Folders With > 5 Sibling `.cs` Files — Cohesion Classification

Rule: keep if cohesive; split only if multiple responsibilities; never split by file
count alone; never use generic names. Current counts (live):

| Folder | # | Classification | Reason / action |
|---|---|---|---|
| `Application.Abstractions/Quran/Words/Morphology/Irab` | 16 | **cohesive — keep** | All simple-iʿrab generation contracts (one responsibility). After move → `DataPipelines/Words/SimpleI3rabGeneration/`, still one folder. |
| `Application/Quran/Import/Validation` | 14 | **cohesive — keep** | All foundation-import validation checks + verdict/severity/id constants. After move → `DataPipelines/Foundation/Validation/`. |
| `Application.Abstractions/Quran/Import` | 12 | **cohesive — keep** | Foundation import contracts/DTOs. After move → `DataPipelines/Foundation/`. |
| `Application.Abstractions/Quran/{Translations,Tafsirs,Navigation,FullI3rab}` | 11 each | **cohesive — keep** | One feature each: source/writer/report-builder interfaces + invariants/results/exceptions. |
| `Infrastructure/Files/Quran/Morphology` | 9 | **cohesive — keep** | Morphology file readers + assembler + seed. |
| `Infrastructure/Persistence/Repositories/Quran/{Mutashabihat,Irab,Morphology}` | 8/8/7 | **cohesive — keep** | One feature's write side (copier + executor + SQL + validation + report builder). |
| `Infrastructure/.../Repositories/Quran/{Translations,Tafsirs,Navigation,FullI3rab}` | 6 each | **cohesive — keep** | Same shape. |
| `Infrastructure/Persistence/Configurations/Quran/Words/Morphology` | 6 | **cohesive — keep** | EF configs for morphology entities. Not a DataPipelines move. |
| `Infrastructure/Files/Quran/{Translations,Morphology/Irab,...}` | 6–7 | **cohesive — keep** | One feature each. |
| `Application.Abstractions/Quran/Words/Display` | 6 | **cohesive — keep** | Display-words rebuild contracts. |
| `Application.Abstractions/Quran/MushafReader` (root) | 6 | **split recommended (target-driven)** | 5 reader interfaces + `Options`. Target moves the 5 readers into `Reading/` — this is a *responsibility* split (reader contracts vs options), not a count split. `MushafReaderOptions.cs` may stay at `MushafReader/` root or move with readers; recommend keeping it at root (it configures the whole feature). |
| `Infrastructure/Persistence/Reads/Quran/MushafReader` | 5 | **cohesive — keep** | 5 runtime read services. |
| `Infrastructure/Persistence/Configurations/Quran` (root) | 5 | **cohesive — keep (optional regroup)** | Foundation entity configs. Optionally → `Foundation/` for symmetry; low value. |
| `Infrastructure/Files/Quran/{Tafsirs,Navigation,Mutashabihat,Import,FullI3rab}` | 5 each | **cohesive — keep** | One feature each. |
| `Application.Abstractions/Quran/MushafReader/Responses` | 5 | **cohesive — keep** | Runtime read DTOs. |
| `Api/Controllers/Mushaf` | 5 | **split (target-driven)** | Target splits by runtime sub-feature (Pages/Ayahs/Words/Catalogs). Responsibility-driven, not count-driven. |

**Verdict for §7:** no folder is over-count *and* multi-responsibility in a way that
demands splitting for its own sake. The only splits are the **target-driven
responsibility splits** (`MushafReader/Reading/`, Api sub-features), which are
justified by principle 4/7, not by the 5-file rule.

---

## 8. Is `DataPipelines` a Better Name Than `Importing`?

**Yes — for this codebase.** Recommendation: **use `DataPipelines`.**

Reasoning:

- The bucket contains **three** kinds of data-preparation work, not one:
  - **Import** (`ImportQuranFoundation`, `ImportMorphology`, `ImportTafsirs`,
    `ImportTranslations`, `ImportNavigationMetadata`, `ImportMutashabihat`,
    `ImportFullI3rab`) — reads external source packages and persists.
  - **Generation** (`GenerateI3rab`) — *derives* simple-iʿrab from already-imported
    morphology + a rule catalog. It does not import an external source.
  - **Rebuild** (`RebuildDisplayWords`) — *derives* display word tables from
    already-imported words. Also not an import.
- `Importing` would **mislabel** `GenerateI3rab` and `RebuildDisplayWords`, which are
  transformation/rebuild steps, not imports. That mislabeling would push future
  contributors to force new derivations under an `Import*` name.
- `DataPipelines` is neutral and accurately spans **ingest → validate → persist →
  derive → rebuild → report**. It also reads well at every layer
  (`Application.DataPipelines.…`, `Infrastructure.Persistence.DataPipelines.Quran.…`).
- It is **not** a dumping folder (principle 3): every child is a named feature
  (`Foundation/`, `Tafsirs/`, …) or a named workflow
  (`Words/DisplayRebuilding/`, `Words/SimpleI3rabGeneration/`).

**Sub-folder naming** inside `DataPipelines/Words/` follows the target's preferred
gerund style (`DisplayRebuilding`, `MorphologyImporting`, `SimpleI3rabGeneration`),
which describes *what the pipeline does* — better than the current `Display` /
`GenerateI3rab` / `ImportMorphology` names that mix noun and verb forms.

> Counter-consideration: `DataPipelines` is slightly more abstract than `Importing`. If
> the team strongly prefers a shorter concrete name, `DataPrep` is an acceptable
> alternative that still covers import+generate+rebuild. `Importing` is **not**
> recommended.

---

## 9. Controller Target Structure & Route/URL Stability

**Routes stay unchanged. Proof:**

- Every controller is decorated with `[ApiController]` and an **explicit**
  `[Route("api/...")]` attribute (verified for all 7 controllers). ASP.NET Core routing
  uses these attributes; it does **not** derive routes from the controller's folder or
  namespace.
- Moving `MushafPagesController.cs` from `Controllers/Mushaf/` to
  `Controllers/MushafReader/Pages/` therefore leaves `GET /api/mushaf/pages/{n}`
  identical. Same for all others (table in §3.4).
- `[HttpGet]`/`[HttpPost]` action templates are likewise unaffected.

**Controller discovery is assembly-wide:** `services.AddControllers()` in
`AddApiServices` registers all controllers regardless of namespace, so namespace
changes (if any) do not affect discovery.

**Recommendation for Phase 5:**

- **Keep controller namespaces unchanged** (Option B is acceptable here). Controllers
  already use `namespace QuranDashboard.Api.Controllers.Mushaf`; the simplest safe move
  is to physically relocate the files into the new subfolders **without** editing
  namespaces, so Phase 5 is a pure file move. If full folder==namespace consistency is
  desired later, it can be a separate, optional follow-up.
- Re-grouping into `MushafReader/{Pages,Ayahs,Words,Catalogs}/` is a
  **responsibility** split (sub-feature areas of the runtime reader), satisfying
  principle 7. The two catalog controllers share `Catalogs/` because they are both
  catalog endpoints — cohesive.

**No API contract change:** response shapes (`ApiResponse<T>`, the read DTOs), status
codes, and Arabic messages (`ApiMessages.cs`) are untouched.

---

## 10. Phased Refactor Plan (smallest safe phases first)

Each phase is independently shippable, ends with build + the prescribed tests green,
and is a **pure move/rename** unless noted. Run `git diff --check` after every phase.

### Phase 1 — Safe cleanup (SAFE, no tests strictly required but run them)

- Delete `Application.Abstractions/Quran/MushafPages/IMushafPageReadRepository.cs`
  (dead, zero references) and the now-empty `MushafPages/` folder.
- Remove redundant `.gitkeep` files in folders that **now have real content**:
  - `Domain/Quran/Tafsirs/.gitkeep`
  - `Application.Abstractions/Quran/Tafsirs/.gitkeep`
  - `Application/Quran/Tafsirs/ImportTafsirs/.gitkeep`
  - `Infrastructure/Reports/Quran/{Translations,Irab,Tafsirs}/.gitkeep`
  - `Infrastructure/Files/Quran/{Translations,Morphology/Irab,Tafsirs}/.gitkeep`
  - `Infrastructure/Persistence/Repositories/Quran/{Translations,Irab,Tafsirs}/.gitkeep`
  - (Keep `.gitkeep` only where a folder is genuinely empty; none remain after this list
    — verify each folder has ≥1 `.cs` before deleting its keep file.)

**Verify:**
```bash
dotnet build QuranDashboard.sln -c Debug --nologo
dotnet test tests/QuranDashboard.Tests --nologo --filter "FullyQualifiedName~Mushaf"
```

### Phase 2 — Application.Abstractions DataPipelines (SAFE)

- Decide namespace policy (recommend **Option A**: namespace follows folder).
- Move pipeline contracts under `Quran/DataPipelines/<Feature>/` per §3.1; move
  Mushaf reader interfaces into `MushafReader/Reading/` (keep `MushafReaderOptions.cs`
  at `MushafReader/` root; `Responses/` unchanged).
- Update `namespace` declarations (Option A) and all `using`s across the solution that
  referenced the moved Abstractions namespaces (Infrastructure DI, Application
  handlers, Tests).

**Verify:** build + full test suite (Abstractions are consumed widely).
```bash
dotnet build QuranDashboard.sln -c Debug --nologo
dotnet test QuranDashboard.sln --nologo
```

### Phase 3 — Application DataPipelines (SAFE)

- Move pipeline use-cases under `Quran/DataPipelines/<Feature>/` per §3.2; keep
  `MushafReader/Queries/` unchanged.
- Update `Application/DependencyInjection.cs` `using`s; update `DataImporter/Program.cs`
  `using`s; update test `using`s.

**Verify:**
```bash
dotnet build QuranDashboard.sln -c Debug --nologo
dotnet test tests/QuranDashboard.Tests --nologo \
  --filter "FullyQualifiedName~Import|FullyQualifiedName~Morphology|FullyQualifiedName~Mutashabihat|FullyQualifiedName~Tafsirs|FullyQualifiedName~Translations|FullyQualifiedName~Navigation|FullyQualifiedName~FullI3rab|FullyQualifiedName~WordsSimpleI3rab|FullyQualifiedName~WordsDisplay"
```

### Phase 4 — Infrastructure DataPipelines, concern-by-concern (SAFE each)

Split into three independent sub-phases so each is small and independently testable.

**4a — `Files/Quran/` → `Files/Quran/DataPipelines/<Feature>/`**
```bash
dotnet build QuranDashboard.sln -c Debug --nologo
dotnet test tests/QuranDashboard.Tests --nologo --filter "FullyQualifiedName~Assembler|FullyQualifiedName~ManifestReader|FullyQualifiedName~SourceReader|FullyQualifiedName~SourceSafety"
```

**4b — `Persistence/Repositories/Quran/` → `Persistence/DataPipelines/Quran/<Feature>/`**
*(EF configurations in `Persistence/Configurations/` are NOT moved.)*
```bash
dotnet build QuranDashboard.sln -c Debug --nologo
dotnet test tests/QuranDashboard.Tests --nologo \
  --filter "FullyQualifiedName~Import|FullyQualifiedName~Rollback|FullyQualifiedName~Isolation|FullyQualifiedName~Idempotency|FullyQualifiedName~Rebuild|FullyQualifiedName~Generation|FullyQualifiedName~Schema"
```

**4c — `Reports/Quran/` → `Reports/Quran/DataPipelines/<Feature>/`**
```bash
dotnet build QuranDashboard.sln -c Debug --nologo
dotnet test tests/QuranDashboard.Tests --nologo --filter "FullyQualifiedName~ReportShape|FullyQualifiedName~ValidationFailure|FullyQualifiedName~Refusal"
```

After 4a+4b+4c, run the **full** suite once to catch any cross-consumer `using`:
```bash
dotnet test QuranDashboard.sln --nologo
```

### Phase 5 — Api Controllers re-group (SAFE, URLs unchanged)

- Move controller files into `Controllers/{System,Dashboard,MushafReader/{Pages,Ayahs,Words,Catalogs}}/`.
- **Recommend: do not change controller namespaces** (Option B) — pure file move.
  Keeps the phase trivial and guarantees zero route/discovery risk.

**Verify:**
```bash
dotnet build QuranDashboard.sln -c Debug --nologo
dotnet test tests/QuranDashboard.Tests --nologo --filter "FullyQualifiedName~MushafReader|FullyQualifiedName~MushafPage|FullyQualifiedName~AyahStudy|FullyQualifiedName~WordAnalysis|FullyQualifiedName~Catalog"
# Optional: smoke-test the API endpoints to confirm routes are live.
```

### Phase 6 — DataImporter `Program.cs` split (TESTS)

- Extract `Import/ArgumentParsing/` (shared `--source/--report-out/--force` parser),
  `Import/DefaultPaths/` (feature → default source/report path map; subsumes
  `NavigationImportPaths.cs`), and `Import/VerbRunners/` (one runner per verb).
- `Program.cs` becomes thin verb-dispatch + host composition only.
- No business logic moves in or out; behavior is fully covered by import integration
  tests.

**Verify:**
```bash
dotnet build QuranDashboard.sln -c Debug --nologo
dotnet test tests/QuranDashboard.Tests --nologo \
  --filter "FullyQualifiedName~Import|FullyQualifiedName~Morphology|FullyQualifiedName~Mutashabihat|FullyMethodName~Tafsirs|FullyQualifiedName~Translations|FullyQualifiedName~Navigation|FullyQualifiedName~FullI3rab|FullyQualifiedName~WordsSimpleI3rab|FullyQualifiedName~WordsDisplay|FullyQualifiedName~Rebuild"
```
> (Note: the import tests resolve handlers through the same host builder the CLI uses,
> so a green run exercises the re-wired composition root.)

### Phase 7 — Optional oversized-file splits (DEFER)

- Only for cohesive files that approach/exceed hard thresholds (inventory §4.1):
  `DisplayWordsSql.cs` (554), the 400–460-line manifest readers/assemblers
  (`NavigationManifestReader`, `FullI3rabAssembler`, `TranslationManifestReader`,
  `TafsirAssembler`, `FullI3rabManifestReader`, `MorphologyAssembler`).
- Split **in place** by sub-responsibility (parsing vs validation vs assembly) within
  the feature folder; never via dumping folders. Each split is its own change with its
  feature's tests run before and after.
- This phase is **independent** of Phases 1–6 and can be done before, after, or never.

---

## 11. Risks & Mitigations

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Missed `using` after namespace move → build break | Med | Low (compile-time, immediate) | Build after each phase; compiler lists every missed reference. |
| Missed `using` in a test file → test compile break | Med | Low | `dotnet test` compile step catches it; tests are compiled in the same solution. |
| Accidentally moving an EF configuration into `DataPipelines` | Low | High (schema mapping lost from its logical home) | Phase 4b explicitly excludes `Persistence/Configurations/`; review the move list against §3.3b before executing. |
| Controller route drift | Very low | High (API break) | Routes are attribute-based (§9); Phase 5 is a pure file move with no namespace edit recommended. Add an endpoint smoke test. |
| Behavioral drift in DataImporter split | Low | Med | Phase 6 changes no logic; import integration tests (which build the host) cover the verbs. |
| Wide single commit vs. phased commits | Process | Med | Keep phases as separate commits/PRs so each is revertible and reviewable. |
| Namespace policy inconsistency (Option A vs B) | Med | Low–Med | Decide Option A globally in Phase 2; allow Option B **only** for Api controllers (Phase 5). Document the decision. |

---

## 12. Recommendation

Proceed with the refactor in the **seven phases above**, all under the **Option A
(namespace follows folder)** policy except Api controllers (Option B, pure file move).
Use **`DataPipelines`** as the concern name (not `Importing`). Sequence:

1. Cleanup (dead interface + `.gitkeep`) →
2. Abstractions DataPipelines →
3. Application DataPipelines →
4. Infrastructure (4a Files → 4b Persistence write side → 4c Reports) →
5. Api controller re-group (URLs fixed) →
6. DataImporter `Program.cs` split →
7. (Optional) oversized-file splits.

The result formalizes the runtime/data-pipeline boundary the code already obeys,
introduces no dumping folders, preserves all routes and behavior, and keeps every
phase independently buildable and testable.

---

### Appendix — artifacts produced

This document only: `Backend/report/architecture/backend-target-structure-feasibility-report.md`.
No files were moved, renamed, edited, formatted, migrated, or committed.
