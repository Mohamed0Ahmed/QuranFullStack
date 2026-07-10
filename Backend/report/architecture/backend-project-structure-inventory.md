# Backend Project Structure Inventory & Architecture Review

> **Scope:** Backend only. **Report only.** No files were modified, moved, formatted,
> migrated, or committed. This document inventories the current `Backend/` solution
> organization and identifies structure smells so a future refactor can be planned safely.
>
> **Canonical references consulted:** `CODING_PRINCIPLES.md`,
> `Backend/.architecture/BACKEND_STRUCTURE.md` (file/folder placement + file-size
> thresholds), `Backend/.architecture/CLEAN_ARCHITECTURE.md` (layer responsibilities +
> dependency direction), `Backend/.architecture/API_GUIDELINES.md` (API boundary),
> `Backend/AGENTS.md` / `Backend/CLAUDE.md`.
>
> **Baseline verification:** `dotnet build QuranDashboard.sln` → **Build succeeded,
> 0 warnings, 0 errors** at the time of this review (net10.0).

---

## Verdict

**PASS WITH NOTES.**

The backend is **well-organized overall**. Layering respects Clean Architecture
direction, dependencies are correct at the project-reference level, and folders are
overwhelmingly grouped by **domain/feature** (`Quran/<Feature>/`), not by technical
type. There are **no global dumping folders** (`Enums/`, `Models/`, `Helpers/`,
`Utils/`, `Services/`) anywhere in the solution.

The notes below are targeted cleanups and a small number of size/ownership issues
worth addressing in a **phased, low-risk** manner — none of which are blocking.

---

## Summary

This review inspected all 8 projects in `Backend/QuranDashboard.sln`, the full folder
tree of every project (excluding `bin/`/`obj/`/`Migrations` noise unless relevant),
project references, DI wiring, the `DataImporter` CLI, and the test project. File
sizes were measured against the thresholds in `BACKEND_STRUCTURE.md` §"File Size and
Responsibility Guidelines".

What was confirmed:

- **Clean Architecture dependency direction is correct** (see §1.3).
- **Domain is independent** — no Application/Infrastructure/Api/EF references.
- **Application does not reference Infrastructure** directly.
- **Api is a thin composition root** (`Program.cs` is 15 lines; controllers delegate
  to handlers; `ApiResponse` envelope is consistent).
- **Feature-first foldering** is applied consistently across Domain, Application,
  Application.Abstractions, and Infrastructure (`Quran/{Surahs, Words, Morphology,
  Irab, Tafsirs, Translations, Mutashabihat, Navigation, FullI3rab, MushafReader,
  MushafPages, Import, Display}`).

What needs attention (detailed in §4–§7):

- One **oversized** file over a hard threshold (`DataImporter/Program.cs`, 1058 lines).
- A cluster of **manifest-reader and assembler files** in the 400–460-line range
  (soft-threshold territory) in Infrastructure.
- Two **misplaced/ownership** items: a dead empty interface and an infra-internal
  abstraction that is fine as-is but worth documenting.
- **Minor inconsistency**: Mushaf-reader read models live in two sibling namespaces
  (`MushafPages` vs `MushafReader`).

---

## 1. Solution Overview

### 1.1 Projects

The solution (`Backend/QuranDashboard.sln`) contains **8 projects** organized under
solution folders that mirror the on-disk layout:

| Project | Path | Kind | Intended responsibility |
|---|---|---|---|
| `QuranDashboard.Api` | `api/` | Web (entry) | HTTP entry point only: thin controllers, `ApiResponse` envelope, middleware, Swagger, CORS, health checks, DI composition. |
| `QuranDashboard.Application` | `application/` | Class lib | Use-case orchestration: import/generate/rebuild commands + handlers, read queries + handlers, validation, assemblers. No EF/Infra. |
| `QuranDashboard.Application.Abstractions` | `application/` | Class lib | Contracts consumed by Application and implemented by Infrastructure: source/reader/writer/report-builder interfaces + DTOs/invariants/results. |
| `QuranDashboard.Domain` | `domain/` | Class lib | Core business model: entities, value objects, domain enums. Independent. |
| `QuranDashboard.Infrastructure` | `infrastructure/` | Class lib | EF Core (`DbContext`, configurations, migrations), file readers/assemblers/import sources, report writers, caching decorators, DI wiring. |
| `QuranDashboard.Shared` | `shared/` | Class lib | Truly cross-layer primitives only (`Result`, `Error`). |
| `QuranDashboard.DataImporter` | `tools/` | Console (Exe) | CLI host for import/generate/rebuild verbs; argument parsing + host wiring. |
| `QuranDashboard.Tests` | `tests/` | Test (xunit) | Integration + unit tests; Testcontainers Postgres; embedded seed SQL. |

All target **net10.0**, `Nullable=enable`, `ImplicitUsings=enable`.

### 1.2 Project references (actual)

```text
Api                       → Application, Application.Abstractions, Infrastructure, Shared
Application               → Application.Abstractions, Domain, Shared
Application.Abstractions  → Domain, Shared
Infrastructure            → Application.Abstractions, Domain, Shared
Domain                    → (none)
Shared                    → (none)
DataImporter              → Application, Application.Abstractions, Infrastructure, Shared
Tests                     → Domain, Application, Application.Abstractions, Infrastructure, DataImporter
```

Side notes on packaging/DI:

- `Application` and `Infrastructure` both expose `InternalsVisibleTo` to `Tests`.
- `Application` pulls in `Microsoft.Extensions.DependencyInjection.Abstractions`
  (for its `AddApplication()` extension) — appropriate.
- `Infrastructure` pulls in EF Core, Npgsql, `Microsoft.Extensions.Caching.Memory`,
  and `Options.ConfigurationExtensions` — appropriate.
- `Api` carries `EFCore.Design` + EF health-check + Swagger; references
  `Infrastructure` **only for DI composition** (`AddInfrastructure`), which matches
  `API_GUIDELINES.md` §1.

### 1.3 Clean Architecture direction — MATCHES

Mapping the actual references (§1.2) against `CLEAN_ARCHITECTURE.md` "Dependency
Direction":

| Rule | Required | Actual | Status |
|---|---|---|---|
| Domain depends on nothing | Domain → ∅ | Domain → ∅ | ✅ |
| Application.Abstractions → Domain/Shared only | yes | yes | ✅ |
| Application → Abstractions/Domain/Shared only | yes | yes (no Infrastructure) | ✅ |
| Infrastructure → Abstractions/Domain/Shared only | yes | yes | ✅ |
| Api → Application/Abstractions/Infrastructure/Shared | yes | yes; Infra for DI only | ✅ |
| Forbidden: Application → Infrastructure/Api | — | none | ✅ |
| Forbidden: Domain → anything | — | none | ✅ |

**Dependency direction is correct at the project-reference level.** There is no
compile-time violation of Clean Architecture boundaries in the `.csproj` graph.

> **Nuance (not a reference violation):** Application handlers do touch
> `System.IO.Path`/`Path.GetFullPath` to normalize the caller-supplied
> `--source`/`--report-out` paths (e.g. `ImportQuranFoundationHandler.cs`,
> `ImportTranslationsHandler.cs`). They do **not** read/write the filesystem
> directly — all actual I/O goes through abstractions (`IQuranImportSource`,
> `IImportReportWriter`, …). This is path *normalization*, not infra coupling, and is
> acceptable; flagged in §4 for awareness, not action.

---

## 2. Current Folder Tree Per Project

Generated from the live tree (`bin/`/`obj/` excluded; `Migrations` collapsed). Line
counts shown where ≥ 200 (soft/hard context).

### 2.1 QuranDashboard.Api  (13 files)

```text
api/QuranDashboard.Api/
├── Program.cs                         (15)   — composition root only
├── Common/
│   └── ApiMessages.cs                 (23)   — centralized Arabic message constants
├── Contracts/
│   └── ApiResponse.cs                 (27)   — ApiResponse<T> envelope (canonical)
├── Controllers/
│   ├── DashboardController.cs
│   ├── HealthController.cs
│   └── Mushaf/
│       ├── MushafAyahStudyController.cs
│       ├── MushafPagesController.cs   (49)   — thin
│       ├── MushafStudySourceCatalogController.cs
│       ├── MushafSurahCatalogController.cs
│       └── MushafWordAnalysisController.cs
├── Extensions/
│   ├── ServiceCollectionExtensions.cs (56)   — AddApiServices (Swagger/CORS/health/problem details)
│   └── WebApplicationExtensions.cs             — UseApiPipeline
├── Middleware/
│   └── GlobalExceptionHandler.cs
├── appsettings.json / appsettings.Development.json
└── Properties/launchSettings.json
```

Controllers are **feature-grouped** (`Mushaf/` subfolder). All observed controllers are
thin (≤ ~50 lines). No `DTOs/`/`Helpers/`/`Services/` dumping folders.

### 2.2 QuranDashboard.Domain  (independent)

```text
domain/QuranDashboard.Domain/
└── Quran/
    ├── Ayahs/             Ayah.cs
    ├── FullI3rab/         FullI3rabEntry.cs, FullI3rabAyahEntry.cs, FullI3rabSource.cs
    ├── MushafPages/       MushafPage.cs, MushafLine.cs, MushafLineType.cs (enum)
    ├── Mutashabihat/      MutashabihatGroup.cs, MutashabihatOccurrence.cs, SimilarAyahLink.cs
    ├── Navigation/        Juz.cs, Hizb.cs, Rub.cs, Sajda.cs, SajdahType.cs (enum)
    ├── Surahs/            Surah.cs, RevelationPlace.cs (enum)
    ├── Tafsirs/           TafsirEntry.cs, TafsirAyahEntry.cs, TafsirSource.cs   (+ .gitkeep)
    ├── Translations/      TranslationAyahEntry.cs, TranslationSource.cs
    └── Words/
        ├── QuranWord.cs, VerseKey.cs, WordLocation.cs
        ├── Display/       OrderedSimpleWord.cs, OrderedTashkeelWord.cs,
        │                  UniqueSimpleWord.cs, UniqueTashkeelWord.cs
        └── Morphology/
            ├── QuranLemma.cs, QuranRoot.cs, QuranStem.cs, WordMorphology.cs,
            │   WordMorphologySegment.cs
            ├── MorphologicalCase.cs, PosTag.cs, SegmentKind.cs,
            │   VerbTense.cs, VerbVoice.cs      (enums)
            └── Irab/     I3rabStatus.cs (enum), QuranI3rabRule.cs
```

Domain enums sit next to their owning aggregate (e.g. `MushafLineType` in
`MushafPages/`, `SajdahType` in `Navigation/`, `RevelationPlace` in `Surahs/`). This is
exactly the placement rule in `BACKEND_STRUCTURE.md`. **No `Common/` dumping** (there
is no `Common` folder at all). Largest entity files are well under the entity soft
threshold (300).

### 2.3 QuranDashboard.Application.Abstractions

```text
application/QuranDashboard.Application.Abstractions/Quran/
├── FullI3rab/        I{FullI3rabImportSource,ImportWriter,ReportWriter,ImportReportBuilder}
│                     FullI3rabImport{Constants,Report,Result}, FullI3rabSourceData,
│                     FullI3rabInvariants, FullI3rabSourceException,
│                     FullI3rabValidationException
├── Import/           IQuranImportSource, IQuranImportWriter, IImportReportWriter,
│                     AssembledQuranData, {AyahMeta,SurahMeta,Layout,Line,WordRecord}Dto,
│                     QuranImportSourceData, QuranImportValidationResult,
│                     ImportRefusalMessages
├── Morphology reader abstractions live under Words/Morphology (see below)
├── MushafPages/      IMushafPageReadRepository.cs   ← EMPTY interface (dead; see §4)
├── MushafReader/     I{MushafPage,AyahStudy,MushafSurahCatalog,
│                     MushafStudySourceCatalog,WordAnalysis}Reader
│                     MushafReaderOptions
│                     Responses/  MushafPageResponse, AyahStudyResponse,
│                                MushafStudySourceCatalogResponse,
│                                MushafSurahCatalogResponse, WordAnalysisResponse
├── Mutashabihat/     IMutashabihatImportSource (+IMutashabihatImportWriter,
│                     +IMutashabihatReportWriter in same file),
│                     MutashabihatImportResult, MutashabihatInvariants,
│                     MutashabihatSourceData, MutashabihatSourceException
├── Navigation/       I{NavigationMetadataImport{Source,Writer},
│                     NavigationMetadata{ReportWriter,ImportReportBuilder}}
│                     Navigation{ImportConstants,MetadataImportReport,
│                     MetadataImportResult,MetadataInvariants,
│                     MetadataSourceData,MetadataSourceException,
│                     MetadataValidationException}
├── Tafsirs/          I{TafsirImportSource,ImportWriter,ReportWriter,ImportReportBuilder}
│                     TafsirImport{Constants,Report,Result}, TafsirInvariants,
│                     TafsirSourceData, TafsirSourceException,
│                     TafsirValidationException   (+ .gitkeep)
├── Translations/     (mirrors Tafsirs shape)
└── Words/
    ├── Display/      IDisplayWordsRebuilder, IDisplayWordsReportWriter,
    │                 DisplayWords{CheckResult,Invariants,RebuildResult,Totals}
    └── Morphology/
        ├── IMorphologyImportSource (+IMorphologyImportWriter, +IMorphologyReportWriter
        │                          in same file), MorphologyImportResult,
        │   MorphologyInvariants, MorphologySourceData
        └── Irab/     II3rab{Assembler,CommandExecutor,GenerationReportWriter,
                              GenerationSource,GenerationWriter,RuleCatalog}
                          I3rab{CheckResult,ExpectedCounts,GenerationResult,Invariants,
                                MorphologyReadiness,RefusalReport,RuleSeedRow,
                                SegmentInput,SegmentLabel,Warning}
```

Contracts are **feature-grouped** with no `Interfaces/` dumping folder. One structural
inconsistency: Mushaf read-models are split between two sibling namespaces —
`MushafPages/` (the dead `IMushafPageReadRepository`) and `MushafReader/` (the live
readers + `Responses/`). See §4.

### 2.4 QuranDashboard.Application

```text
application/QuranDashboard.Application/
├── DependencyInjection.cs             (43) — AddApplication() registers handlers + assemblers
└── Quran/
    ├── FullI3rab/ImportFullI3rab/     ImportFullI3rab{Command,Handler(207),Result},
    │                                   FullI3rabImportReportEmitter
    ├── Import/
    │   ├── ImportQuranFoundation/     ImportQuranFoundation{Command,Handler(87),Result},
    │   │                               QuranFoundationAssembler (313)
    │   └── Validation/                QuranImportValidator (232) + 9 check/result files
    ├── MushafReader/Queries/
    │   ├── GetAyahStudy/              GetAyahStudy{Query,Handler,Outcome}
    │   ├── GetMushafPage/             GetMushafPage{Query,Handler(34),Outcome}
    │   ├── GetMushafStudySourceCatalog/
    │   ├── GetMushafSurahCatalog/
    │   └── GetWordAnalysis/
    ├── Mutashabihat/ImportMutashabihat/
    ├── Navigation/ImportNavigationMetadata/   Handler 232
    ├── Tafsirs/ImportTafsirs/                  Handler 214  (+ .gitkeep)
    ├── Translations/ImportTranslations/        Handler 218
    └── Words/
        ├── GenerateI3rab/             GenerateI3rab{Command,Handler,Result}
        ├── ImportMorphology/          ImportMorphology{Command,Handler,Result}
        └── RebuildDisplayWords/       RebuildDisplayWords{Command,Handler,Result}
```

**Use-case folders** group `Command`/`Query` + `Handler` + `Result`/`Outcome`
together, matching the preferred structure in `BACKEND_STRUCTURE.md` §"Application
Layer". The Mushaf read side uses the `Queries/Get<UseCase>/` subfolder convention; the
import side uses `Import<Feature>/` flat. Both are cohesive; the difference is
read-vs-command convention, acceptable.

### 2.5 QuranDashboard.Infrastructure  (152 files, excl. Migrations)

```text
infrastructure/QuranDashboard.Infrastructure/
├── DependencyInjection.cs             (169) — AddInfrastructure() + ConfigureMushafReader()
├── GlobalUsings.cs                    (10)  — EF/Npgsql/Json/Security layer-safe
├── Caching/Quran/MushafReader/        Cached{MushafPage,AyahStudy,WordAnalysis}Reader,
│                                      MushafReaderCacheKeys   (decorator pattern)
├── Files/Quran/                       (file readers / assemblers / import sources)
│   ├── FullI3rab/   FullI3rabAssembler (444), FullI3rabImportSource,
│   │                 FullI3rabManifestReader (413), FullI3rabValidationChecks,
│   │                 JsonFullI3rabSourceReader
│   ├── Import/      QuranImportSource, ManifestReader (180),
│   │                 Json{Word,Layout,Metadata}SourceReader
│   ├── Morphology/  MorphologyAssembler (412), MorphologyImportSource,
│   │                 MorphologyManifestReader (315), JsonAlignedCorpusReader,
│   │                 JsonQulReaders, BuckwalterArabicMap, SegmentArabicRenderer,
│   │                 PosTagSeed, MorphologySourceValidation
│   │   └── Irab/    I3rabAssembler, I3rabRuleCatalogSeed(.cs + SeedData),
│   │                 I3rabSeedLabelCorrections, SegmentSignatureBuilder,
│   │                 AllahLemmaMatcher   (+ .gitkeep)
│   ├── Mutashabihat/ MutashabihatAssembler (174), MutashabihatManifestReader (310),
│   │                  MutashabihatImportSource, JsonPhrasesReader, JsonSimilarAyahReader
│   ├── Navigation/  NavigationMetadataAssembler (298), NavigationManifestReader (463),
│   │                  NavigationMetadataImportSource, JsonNavigationDatasetReader (217),
│   │                  NavigationValidationChecks (190)
│   ├── Tafsirs/     TafsirAssembler (421), TafsirManifestReader (380),
│   │                  TafsirImportSource, JsonTafsirSourceReader, TafsirValidationChecks
│   └── Translations/ TranslationAssembler, TranslationManifestReader (428),
│                     TranslationImportSource, JsonTranslationSourceReader,
│                     TranslationDisplayMetadataReader (205),
│                     Translation{TypeCountValidation,ValidationChecks}
├── Migrations/                        11 migrations + ModelSnapshot (collapsed)
├── Persistence/
│   ├── QuranDashboardDbContext.cs     (54) — 30 DbSets; ApplyConfigurationsFromAssembly
│   ├── Configurations/Quran/          EF configs grouped by feature:
│   │   ├── (root)  Ayah, MushafLine, MushafPage, QuranWord, Surah configurations
│   │   ├── FullI3rab/   3 configurations
│   │   ├── Mutashabihat/ 3 configurations
│   │   ├── Navigation/  4 configurations (Hizb/Juz/Rub/Sajda)
│   │   ├── Tafsirs/     3 configurations
│   │   ├── Translations/ 2 configurations
│   │   └── Words/{Display (4), Morphology (6 incl. Irab/QuranI3rabRule)}
│   ├── Reads/Quran/MushafReader/      Ef{MushafPage,AyahStudy,MushafSurahCatalog,
│   │                                  MushafStudySourceCatalog,WordAnalysis}Reader
│   │                                  (read services; EfWordAnalysisReader 270, EfAyahStudyReader 248)
│   └── Repositories/Quran/            (write-side bulk import writers)
│       ├── FullI3rab/   EfBulkFullI3rabImportWriter, FullI3rabBulkCopier,
│       │                 FullI3rabCommandExecutor, FullI3rabSql,
│       │                 FullI3rabImportReportBuilder (200), FullI3rabValidationRunner
│       ├── Import/     EfBulkQuranImportWriter (228)
│       ├── Irab/       EfI3rabGeneration{Source,Writer}, I3rabCommandExecutor,
│       │                 I3rabSql, I3rabValidationRunner (272), I3rabSourceSnapshot,
│       │                 II3rabGenerationWriteProbe, NullI3rabGenerationWriteProbe  (+ .gitkeep)
│       ├── Morphology/ EfBulkMorphologyWriter, MorphologyBulkCopier,
│       │                 MorphologyCommandExecutor, MorphologySql,
│       │                 MorphologyImportReportBuilder, MorphologyValidationRunner,
│       │                 MorphologyImportConstants
│       ├── Mutashabihat/ (8 files incl. Sql/ReportBuilder/ValidationRunner/Session)
│       ├── Navigation/   (6 files)
│       ├── Tafsirs/      (6 files)   (+ .gitkeep)
│       ├── Translations/ (6 files)   (+ .gitkeep)
│       └── Words/Display/ DisplayWordsSql (554), SqlDisplayWordsRebuilder (409)
└── Reports/Quran/         (markdown+JSON report writers)
    ├── (root)            MarkdownJsonImportReportWriter
    ├── FullI3rab/        MarkdownJsonFullI3rabReportWriter (172)
    ├── Irab/             MarkdownJsonI3rabReportWriter (264)  (+ .gitkeep)
    ├── Morphology/       MarkdownJsonMorphologyReportWriter (188)
    ├── Mutashabihat/    MarkdownJsonMutashabihatReportWriter (211)
    ├── Navigation/       MarkdownJsonNavigationMetadataReportWriter
    ├── Tafsirs/          MarkdownJsonTafsirReportWriter (172)  (+ .gitkeep)
    ├── Translations/    MarkdownJsonTranslationReportWriter (194)  (+ .gitkeep)
    └── Words/           MarkdownJsonDisplayWordsReportWriter
```

Infrastructure is **well-partitioned by concern**: `Persistence/Configurations/`,
`Persistence/Reads/`, `Persistence/Repositories/` (write), `Files/` (readers &
assemblers), `Reports/` (writers), `Caching/` (decorators). Each is then grouped by
`Quran/<Feature>`. No mixed bag.

### 2.6 QuranDashboard.Shared

```text
shared/QuranDashboard.Shared/
└── Common/
    ├── Result.cs        (15) — Result + Result<T>
    └── Error.cs
```

Minimal and correct — exactly the cross-layer primitives allowed by
`BACKEND_STRUCTURE.md` §"Shared Layer". No feature-specific types, no helpers.

### 2.7 QuranDashboard.DataImporter  (3 files)

```text
tools/QuranDashboard.DataImporter/
├── Program.cs                  (1058) ← single oversized CLI dispatcher
├── NavigationImportPaths.cs
├── appsettings.json
```

A console host mapping verbs (`import-foundation`, `rebuild-words`,
`import-morphology`, `import-mutashabihat`, `import-tafsirs`,
`import-translations`, `import-navigation-metadata`, `import-full-i3rab`,
`generate-i3rab`) to handlers resolved from DI. No business logic — it parses args,
resolves a handler, prints totals, and writes the report path.

### 2.8 QuranDashboard.Tests

```text
tests/QuranDashboard.Tests/
├── GlobalUsings.cs
└── Quran/
    ├── FullI3rab/           (13 files)  — assembler, import, refusal/force, schema, report-shape, source-unchanged, synthetic package, schema fixture
    ├── Import/              (13 files)  — assembly derivation, force reload, imlaei keys, counts, reconstruction, validation, source helpers, fixture
    ├── MushafReader/        (16 files)  — ayah study, mushaf page, catalog, word analysis, cache, markers; embedded seed SQL + fixture + collection
    ├── Mutashabihat/        (13 files)  — assembler, import, readers, query, refusal/force, report shape + support, service-collection extensions
    ├── Navigation/          (14 files)  — assembler, dataset reader, import, isolation, manifest, write isolation, refusal, report, rollback, source path/safety, synthetic pkg, validation failure
    ├── Tafsirs/             (16 files)
    ├── Translations/        (13 files)
    ├── WordsDisplay/        (16 files)  — canonical source gate, first occurrence, idempotency, identity links, ordering, real import fixture, rebuild, refusal/force, source untouched, statistics, synthetic seed, validation
    ├── WordsMorphology/     (12 files)
    └── WordsSimpleI3rab/    (17 files)  — assembler, generation, idempotency, label correctness, refusal, rule-catalog seed, schema, source safety, validation failure, composition, displayability, signature, suffix pronoun, SQL write probe, tampering assembler
```

Tests are **feature-grouped** (`Quran/<Feature>/`), mirror the production feature
boundaries, and per-feature fixtures use Testcontainers Postgres with the real
infrastructure (real readers/writers, real `DbContext`). Embedded seed SQL lives at
`Quran/MushafReader/mushaf-reader-seed.sql`. This matches the test-code self-check
(real infrastructure where correctness matters; Quranic data stays source-safe via
synthetic packages/fixtures).

---

## 3. Current Organization by Feature / Domain

Where each capability currently lives (the home namespace/folder):

| Capability | Domain | Application.Abstractions | Application | Infra (Files) | Infra (Persistence) | Infra (Reports) | Api |
|---|---|---|---|---|---|---|---|
| **Quran foundation** (surahs/ayahs/pages/words) | `Quran/{Surahs,Ayahs,MushafPages,Words}/` | `Quran/Import/` (DTOs, `IQuranImportSource/Writer`, `IImportReportWriter`) | `Quran/Import/{ImportQuranFoundation,Validation}/` | `Files/Quran/Import/` | `Configurations/Quran/` (root), `Repositories/Quran/Import/` | `Reports/Quran/MarkdownJsonImportReportWriter` | — |
| **Morphology** | `Quran/Words/Morphology/` | `Quran/Words/Morphology/` (`IMorphologyImportSource/Writer/ReportWriter`) | `Quran/Words/ImportMorphology/` | `Files/Quran/Morphology/` | `Configurations/Quran/Words/Morphology/`, `Repositories/Quran/Morphology/` | `Reports/Quran/Morphology/` | — |
| **Simple i3rab** (generated) | `Quran/Words/Morphology/Irab/` | `Quran/Words/Morphology/Irab/` (`II3rab*`) | `Quran/Words/GenerateI3rab/` | `Files/Quran/Morphology/Irab/` | `Repositories/Quran/Irab/` | `Reports/Quran/Irab/` | — |
| **Full i3rab** | `Quran/FullI3rab/` | `Quran/FullI3rab/` | `Quran/FullI3rab/ImportFullI3rab/` | `Files/Quran/FullI3rab/` | `Configurations/Quran/FullI3rab/`, `Repositories/Quran/FullI3rab/` | `Reports/Quran/FullI3rab/` | — |
| **Mutashabihat / similar ayahs** | `Quran/Mutashabihat/` | `Quran/Mutashabihat/` | `Quran/Mutashabihat/ImportMutashabihat/` | `Files/Quran/Mutashabihat/` | `Configurations/Quran/Mutashabihat/`, `Repositories/Quran/Mutashabihat/` | `Reports/Quran/Mutashabihat/` | — |
| **Tafsir** | `Quran/Tafsirs/` | `Quran/Tafsirs/` | `Quran/Tafsirs/ImportTafsirs/` | `Files/Quran/Tafsirs/` | `Configurations/Quran/Tafsirs/`, `Repositories/Quran/Tafsirs/` | `Reports/Quran/Tafsirs/` | — |
| **Translations** | `Quran/Translations/` | `Quran/Translations/` | `Quran/Translations/ImportTranslations/` | `Files/Quran/Translations/` | `Configurations/Quran/Translations/`, `Repositories/Quran/Translations/` | `Reports/Quran/Translations/` | — |
| **Navigation metadata** | `Quran/Navigation/` | `Quran/Navigation/` | `Quran/Navigation/ImportNavigationMetadata/` | `Files/Quran/Navigation/` | `Configurations/Quran/Navigation/`, `Repositories/Quran/Navigation/` | `Reports/Quran/Navigation/` | — |
| **Mushaf reader** (read APIs) | (reuses Words/MushafPages/Surahs/Navigation) | `Quran/MushafReader/` (5 reader interfaces + options + `Responses/`) | `Quran/MushafReader/Queries/Get*/` | — | `Reads/Quran/MushafReader/` (5 Ef readers), `Caching/Quran/MushafReader/` (3 decorators) | — | `Controllers/Mushaf/*` |
| **Shared API responses / messages / exceptions** | — | — | — | — | — | — | `Api/Contracts/ApiResponse.cs`, `Api/Common/ApiMessages.cs`, `Api/Middleware/GlobalExceptionHandler.cs`, `Api/Extensions/*` |
| **Import infrastructure** | — | contracts under each feature's `Quran/<Feature>/` | — | `Files/Quran/<Feature>/` readers + assemblers + `*ImportSource` | `Repositories/Quran/<Feature>/` bulk copiers + executors + SQL + validation runners + report builders | `Reports/Quran/<Feature>/` | — |
| **Report writers** | — | `I*ReportWriter` per feature | — | — | `Repositories/Quran/<Feature>/*ImportReportBuilder` (data shaping) | `Reports/Quran/<Feature>/MarkdownJson*ReportWriter` (emission) | — |
| **CLI command executors** | — | — | handlers under `Quran/<Feature>/Import*/` | — | `Repositories/Quran/<Feature>/*CommandExecutor.cs` (write side) | — | `tools/QuranDashboard.DataImporter/Program.cs` (verb dispatch) |
| **Test fixtures** | — | — | — | — | — | — | `Tests/Quran/<Feature>/*{Fixture,TestServiceCollectionExtensions,TestSupport,Synthetic*,Collection}.cs`, embedded `mushaf-reader-seed.sql` |

**Key observation:** each capability follows a **consistent vertical slice**
(`Domain → Abstractions → Application → Files → Persistence → Reports`), which is the
target shape described in `BACKEND_STRUCTURE.md` and `CLEAN_ARCHITECTURE.md`. The two
notable deviations are (a) the split Mushaf read model namespaces, and (b) the i3rab
write-probe seam living in Infrastructure (covered in §4).

---

## 4. Structure Smells

Grouped by severity. Line counts are current `wc -l`.

### 4.1 Oversized / overloaded files (severity: medium)

| File | Lines | Threshold (role) | Note |
|---|---|---|---|
| `tools/QuranDashboard.DataImporter/Program.cs` | **1058** | CLI / not classified, but crosses the **1000-line** general ceiling | Single monolithic verb dispatcher. ~7 near-identical `TryParse*Arguments` blocks + `Run*Async` blocks + default-path resolvers. **Largest refactor candidate.** No business logic, so the risk is purely structural. |
| `Infrastructure/.../Repositories/Quran/Words/Display/DisplayWordsSql.cs` | 554 | Repository/read service **hard 600** | SQL string surface for display-words rebuild. Soft-ish but large; consider splitting per table (`Ordered*`, `Unique*`) if it grows. |
| `Infrastructure/.../Files/Quran/Navigation/NavigationManifestReader.cs` | 463 | file reader (no explicit hard cap; service hard 450) | Manifest parsing + validation; approaching service hard threshold. |
| `Infrastructure/.../Files/Quran/FullI3rab/FullI3rabAssembler.cs` | 444 | assembler (service hard 450) | Near hard threshold for a service. |
| `Infrastructure/.../Files/Quran/Translations/TranslationManifestReader.cs` | 428 | manifest reader | |
| `Infrastructure/.../Files/Quran/Tafsirs/TafsirAssembler.cs` | 421 | assembler | |
| `Infrastructure/.../Files/Quran/FullI3rab/FullI3rabManifestReader.cs` | 413 | manifest reader | |
| `Infrastructure/.../Files/Quran/Morphology/MorphologyAssembler.cs` | 412 | assembler | |
| `Infrastructure/.../Repositories/Quran/Words/Display/SqlDisplayWordsRebuilder.cs` | 409 | repository hard 600 | Under cap, but the largest pure C# orchestrator on the write side. |
| `Infrastructure/.../Files/Quran/Tafsirs/TafsirManifestReader.cs` | 380 | manifest reader | |
| `Infrastructure/.../Repositories/Quran/Navigation/NavigationMetadataImportReportBuilder.cs` | 350 | report builder (DTO/model soft 150 / hard 250 — but this is shaping logic) | The `*ImportReportBuilder` classes are doing DTO shaping; a few are 200–350 lines. |
| `Infrastructure/.../Files/Quran/Morphology/MorphologyManifestReader.cs` | 315 | manifest reader | |
| `Application/.../Import/ImportQuranFoundation/QuranFoundationAssembler.cs` | 313 | application/domain service hard 450 | Under cap; noting for completeness. |

Pattern: the **manifest readers** and **assemblers** in `Files/Quran/<Feature>/` are
the recurring soft-threshold cluster (300–460 lines each). They are cohesive (one
feature each), so they are justified, but they are the natural split candidates if any
single one crosses 600/1000.

### 4.2 Misplaced / duplicated abstractions (severity: low–medium)

1. **Dead empty interface** — `Application.Abstractions/Quran/MushafPages/IMushafPageReadRepository.cs`
   is `public interface IMushafPageReadRepository;` with **no members and no
   implementers/consumers** anywhere in the solution (confirmed by grep; only itself +
   the architecture doc mention it). It is leftover scaffolding from an earlier pattern
   that was superseded by the `IMushafPageReader` family under `MushafReader/`.
   **Safe to remove.**

2. **Split Mushaf read-model namespaces** — read models for the Mushaf feature are
   split across two sibling Abstractions namespaces:
   - `Quran/MushafPages/` → only the dead interface above.
   - `Quran/MushafReader/` → all 5 live readers + `MushafReaderOptions` + `Responses/`.
   The `MushafPages/` folder now carries no live content. Consolidating under
   `MushafReader/` (and dropping the dead interface) removes the confusion.

3. **`II3rabGenerationWriteProbe` lives in Infrastructure, not Abstractions** —
   `Infrastructure/.../Repositories/Quran/Irab/II3rabGenerationWriteProbe.cs` defines an
   interface consumed by `EfI3rabGenerationWriter` and registered in DI
   (`NullI3rabGenerationWriteProbe` in prod, `SqlI3rabGenerationWriteProbe` in tests).
   This is an **Infrastructure-internal test seam** (the writer is in the same
   assembly, and both projects grant `InternalsVisibleTo` to Tests). It does **not**
   violate layering (it never crosses into Application). It is fine as-is; noted only
   because the other i3rab contracts live in Application.Abstractions. No action
   required unless the probe is ever consumed by Application.

4. **Two Mushaf-page concepts, two names** — `IMushafPageReader` (live read API) vs the
   dead `IMushafPageReadRepository`. Only one is real; cleanup is part of item (1)/(2).

### 4.3 Global dumping folders — NONE

Confirmed: there are **no** `Enums/`, `Models/`, `DTOs/`, `Helpers/`, `Utils/`, or
`Services/` folders in any project. The closest things are:

- `Api/Common/` — contains only `ApiMessages.cs` (centralized Arabic message constants,
  small). This is the sanctioned "truly shared/common" location per `API_GUIDELINES.md`
  §10; **not** a dumping ground.
- `Api/Contracts/` — contains only `ApiResponse.cs` (the canonical envelope). Intentional.
- `Shared/Common/` — `Result.cs` + `Error.cs`. Exactly the allowed primitives.

### 4.4 Files grouped by technical type instead of domain — NONE

All layers group by `Quran/<Feature>/` first. Even within Infrastructure, the split is
by **concern then feature** (`Files/`, `Persistence/Configurations/`,
`Persistence/Reads/`, `Persistence/Repositories/`, `Reports/`, `Caching/`) — each
partitioned by `Quran/<Feature>/`. This is the recommended shape.

### 4.5 Misplaced DTOs / contracts — MINOR

- Mushaf `Responses/` (read models) live in **Application.Abstractions**, not in Api.
  This is a deliberate, valid choice: the handlers return these read models and the
  controllers map them into `ApiResponse<T>`. Because the API never exposes Domain
  entities directly, having the read DTOs with their reader abstractions is consistent.
  Not a smell — recorded to preempt "DTOs belong in Api" debates.
- `IImportReportWriter` and the per-feature `I*ReportBuilder`/`I*ReportWriter` split is
  intentional (builder = data shaping in `Repositories/`, writer = emission in
  `Reports/`). Consistent across features.

### 4.6 Application depending on filesystem / infra concerns — NONE (at the I/O level)

Application handlers reference `System.IO.Path` for **normalizing caller-supplied
strings** (`Path.GetFullPath` on `command.SourcePath`/`command.ReportOutDir`). They do
not open files, touch `DbContext`, or import Infrastructure types. All real I/O flows
through Application.Abstractions interfaces implemented in Infrastructure. **No
boundary violation.**

### 4.7 Infrastructure folders mixing unrelated features — NONE

`Files/Quran/`, `Persistence/Repositories/Quran/`, `Reports/Quran/`, and
`Persistence/Configurations/Quran/` are all sub-partitioned per feature. The only
"root-level" configuration files in `Configurations/Quran/` are the foundation
entities (`Ayah`, `MushafLine`, `MushafPage`, `QuranWord`, `Surah`) that belong to the
Quran-foundation slice rather than a later sub-feature — acceptable grouping.

### 4.8 DataImporter logic that should belong elsewhere — YES (Program.cs size)

`Program.cs` is the single structural concern. Its **content** is correctly CLI-only
(arg parsing, host build, result printing, default-path resolution) — no business logic
leaked in. The smell is **size and duplication**: 7 near-identical
`TryParse*Arguments`/`Run*Async`/`ResolveDefault*Path` blocks. This is a refactoring
candidate (extract a small `ImportVerbOptions` parser + a `VerbRunner` per verb), but
it is **low risk** because there is zero domain logic and the full behavior is covered
by the import integration tests.

### 4.9 Other notes

- `.gitkeep` files appear in a few `Tafsirs/`, `Translations/`, `Irab/` folders across
  Application/Application.Abstractions/Infrastructure/Reports. They are harmless
  placeholders; remove opportunistically once those folders have committed content
  (most already do).
- The `Quran/` namespace root is used uniformly across **all** layers. Because every
  domain concept in the product is Quranic, a single `Quran/` top-level namespace is
  reasonable today; revisit only if non-Quran bounded contexts (e.g. `Gates/`,
  `Approvals/`, `Audit/` mentioned in the architecture docs) are added later.

---

## 5. Per-Project Recommendations

| Project | Current state | Recommendation |
|---|---|---|
| **QuranDashboard.Domain** | Clean, independent, enums next to owners. | **Keep as-is.** |
| **QuranDashboard.Application.Abstractions** | Feature-grouped; 1 dead interface; split Mushaf namespaces. | **Minor cleanup.** Remove `MushafPages/IMushfPageReadRepository.cs` (dead) and let `MushafReader/` own all Mushaf read models. |
| **QuranDashboard.Application** | Use-case folders cohesive; handlers under threshold. | **Keep as-is.** (Optional: align import-side folder naming with the read-side `Queries/` convention — cosmetic only.) |
| **QuranDashboard.Infrastructure** | Well-partitioned by concern × feature; a cluster of 300–460-line manifest readers/assemblers. | **Minor cleanup now; targeted split later.** No structural move needed. Watch the manifest-reader/assembler cluster; split any file that crosses its hard threshold. The `DisplayWordsSql.cs` (554) is the closest repository file to a cap. |
| **QuranDashboard.Api** | Thin controllers, centralized messages, `ApiResponse` canonical. | **Keep as-is.** |
| **QuranDashboard.Shared** | Minimal primitives only. | **Keep as-is.** |
| **QuranDashboard.DataImporter** | Correct CLI responsibilities, but `Program.cs` is 1058 lines with heavy duplication. | **Needs feature-based reorganization (structural).** Extract a shared arg parser + per-verb runner classes. Behavior unchanged; covered by tests. |
| **QuranDashboard.Tests** | Feature-grouped, real infrastructure, source-safe synthetic data. | **Keep as-is.** |

---

## 6. Proposed Target Direction (high-level only — do not implement)

The current structure is already close to the target. The proposal below mainly
**formalizes** the existing shape and resolves the two inconsistencies in §4.2. It
keeps **domain/feature/bounded-context** as the primary axis, never technical type.

```text
Domain/Quran/Words/{Display,Morphology/{Irab}}            # keep
Domain/Quran/{Surahs,Ayahs,MushafPages,FullI3rab,
              Mutashabihat,Navigation,Tafsirs,Translations}  # keep

Application.Abstractions/Quran/Words/{Display,Morphology/Irab}   # keep
Application.Abstractions/Quran/MushafReader/{,Responses/}        # consolidate Mushaf read models here
Application.Abstractions/Quran/{Import,FullI3rab,Mutashabihat,
                                Navigation,Tafsirs,Translations} # keep
  └─ (delete) MushafPages/IMushafPageReadRepository.cs           # dead

Application/Quran/Words/{ImportMorphology,GenerateI3rab,RebuildDisplayWords}  # keep
Application/Quran/MushafReader/Queries/Get*/                                  # keep
Application/Quran/{Import/ImportQuranFoundation/Import/Validation,
                   FullI3rab,Mutashabihat,Navigation,Tafsirs,Translations}    # keep

Infrastructure/Persistence/Configurations/Quran/<Feature>/  # keep grouping
Infrastructure/Persistence/Reads/Quran/MushafReader/        # keep
Infrastructure/Persistence/Repositories/Quran/<Feature>/    # keep (write side)
Infrastructure/Files/Quran/<Feature>/                       # keep; split oversized manifest readers/assemblers in place
Infrastructure/Reports/Quran/<Feature>/                     # keep
Infrastructure/Caching/Quran/MushafReader/                  # keep (decorators)

tools/QuranDashboard.DataImporter/
  ├── Program.cs                      # slim: arg → verb dispatch only
  ├── Import/ArgumentParsing.cs       # shared --source/--report-out/--force parser
  ├── Import/VerbRunners/*.cs         # one runner per verb (Foundation, Morphology, …)
  └── Import/DefaultPaths.cs          # feature → default source/report path map
```

Non-goals (explicitly **not** proposed now):

- No new projects, no solution-folder reorganization.
- No namespace renames beyond deleting the dead interface.
- No moving read DTOs out of Application.Abstractions.
- No introducing a generic repository — the focused per-feature reader/writer/source
  abstractions are the intended pattern per `CLEAN_ARCHITECTURE.md`.

---

## 7. Risk Assessment

| Candidate | Classification | Why |
|---|---|---|
| Delete `MushafPages/IMushfPageReadRepository.cs` (dead interface) | **Safe now** | Zero references; build is green; covered by the fact that nothing uses it. |
| Consolidate Mushaf read models under `MushafReader/` (after the delete) | **Safe now** | Only affects the already-empty `MushafPages/` folder. |
| Remove stray `.gitkeep` files in folders that now have content | **Safe now** | Cosmetic; no code impact. |
| Split oversized manifest readers/assemblers (`NavigationManifestReader`, `FullI3rabAssembler`, `TranslationManifestReader`, `TafsirAssembler`, …) | **Needs tests first** | Each is covered by per-feature assembler/manifest-reader tests; run them after each split. Low logical risk (cohesive), but the split must be mechanical and per-feature. |
| Split `DisplayWordsSql.cs` (554) and `SqlDisplayWordsRebuilder.cs` (409) | **Defer until after current feature** | Large SQL surface; touch only if it crosses a hard cap or a change is already needed there. |
| Refactor `DataImporter/Program.cs` (1058 → per-verb runners) | **Safe now / needs tests after** | Pure structural; behavior is fully exercised by import integration tests. Do it as an isolated, mechanical change with the test suite green before and after. |
| Move `II3rabGenerationWriteProbe` to Abstractions | **Do not touch unless necessary** | It is an Infrastructure-internal seam used only by the writer in the same assembly + tests via `InternalsVisibleTo`. Moving it adds a public surface for no current benefit. |
| Any namespace/rename across `Quran/` | **Do not touch unless necessary** | High churn, low value today; revisit only when a non-Quran bounded context is introduced. |

---

## 8. Verification Suggestions (run after any future refactor)

Run these from `Backend/`:

1. **Build the whole solution (must stay 0 warnings / 0 errors):**
   ```bash
   dotnet build QuranDashboard.sln -c Debug --nologo
   ```
2. **Full test suite (Testcontainers requires a runnable Docker daemon):**
   ```bash
   dotnet test QuranDashboard.sln --nologo
   ```
   Per-feature focus during a scoped change, e.g. for the DataImporter refactor:
   ```bash
   dotnet test tests/QuranDashboard.Tests --filter "FullyQualifiedName~Import|FullyQualifiedName~Morphology|FullyQualifiedName~Mutashabihat|FullyQualifiedName~Tafsirs|FullyQualifiedName~Translations|FullyQualifiedName~Navigation|FullyQualifiedName~FullI3rab|FullyQualifiedName~WordsSimpleI3rab|FullyQualifiedName~WordsDisplay"
   ```
3. **Confirm DI wiring still resolves** (the refactored verbs must still resolve their
   handlers via `AddApplication()` + `AddInfrastructure()`). The existing import
   integration tests already spin up the host and resolve handlers, so a green run
   proves the composition root.
4. **After deleting the dead interface:** rebuild + `dotnet test` — if green, no
   consumer existed (already confirmed by grep).
5. **After splitting a manifest reader / assembler:** run that feature's
   `*AssemblerTests` + `*ManifestReaderTests` + the corresponding `*ImportTests`
   (they assert source-safety, validation, and report shape).
6. **Optional static check — no new dumping folders introduced:**
   ```bash
   # Should return nothing:
   find api domain application infrastructure shared tools \
     -type d \( -name Enums -o -name Models -o -name DTOs \
                -o -name Helpers -o -name Utils -o -name Services \) \
     -not -path '*/bin/*' -not -path '*/obj/*'
   ```
7. **Optional size gate — flag any file crossing a threshold after refactor:**
   ```bash
   find api domain application infrastructure tools \
     -name '*.cs' -not -path '*/bin/*' -not -path '*/obj/*' \
     -not -path '*/Migrations/*' | xargs wc -l | sort -rn | head -20
   ```

---

## 9. Suggested Phased Refactor Plan (planning only — not executed)

Sequenced for smallest blast radius first. Each phase ends with build + tests green.

- **Phase 0 — Verify baseline.** Capture current build + test state (already green).
- **Phase 1 — Dead-code removal (safe).** Delete
  `Application.Abstractions/Quran/MushafPages/IMushafPageReadRepository.cs`; remove the
  now-empty `MushafPages/` folder from Abstractions so all Mushaf read models live under
  `MushafReader/`. Remove stray `.gitkeep` files where folders have real content.
- **Phase 2 — DataImporter structural refactor (safe, test-covered).** Extract
  `tools/QuranDashboard.DataImporter/Import/{ArgumentParsing,VerbRunners/,DefaultPaths}`
  and slim `Program.cs` to pure verb dispatch. No behavior change.
- **Phase 3 — Size-threshold splits (per feature, needs tests).** For any manifest
  reader/assembler that approaches/exceeds its hard threshold, split in place by
  sub-responsibility (e.g. manifest parsing vs. validation vs. assembly), keeping the
  file in its feature folder. Prioritize by current size:
  `NavigationManifestReader` (463) → `FullI3rabAssembler` (444) →
  `TranslationManifestReader` (428) → `TafsirAssembler` (421) →
  `FullI3rabManifestReader` (413) → `MorphologyAssembler` (412).
- **Phase 4 — Watch items (defer).** `DisplayWordsSql.cs` (554) and
  `SqlDisplayWordsRebuilder.cs` (409): split only if a change is already required there
  or if they cross a hard cap.
- **Phase 5 — Not recommended now.** Any cross-layer namespace rename, moving
  `II3rabGenerationWriteProbe`, or introducing new projects/bounded contexts. Revisit
  only when driven by a real new feature (e.g. `Gates/`, `Approvals/`).

---

### Appendix — changed files reviewed

None. This was a **report-only** review. No files were modified, moved, formatted,
migrated, or committed. The single artifact produced is this document at
`Backend/report/architecture/backend-project-structure-inventory.md`.
