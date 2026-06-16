---
description: "Task list for Quran Navigation Metadata Foundation (Feature 009)"
---

# Tasks: Quran Navigation Metadata Foundation

**Input**: Design documents from `specs/009-quran-navigation-metadata-foundation/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md (all present)
**Tests**: INCLUDED — this is a correctness-critical Quran data import; the spec + plan define a full testing strategy.

## How to read this file (READ FIRST)

- This feature is **one backend importer**, built as **sequential hardening layers**. Implement phases **in order**: Setup → Foundational → US1 → US2 → US3 → US4 → Polish. Within a phase, tasks marked **[P]** touch different files and may run in parallel.
- **The fastest, safest way to implement almost every file is to copy the matching existing `Quran/Translations` file and rename `Translation*` → `NavigationMetadata*` (or `Translation` → `Navigation`), then adjust fields per `data-model.md` and `contracts/`.** Each task names its **PATTERN** file to copy.
- All paths are relative to repo root `/projects/Dashboard/App/`. Backend root is `Backend/`.
- **Hard safety rules (never violate):** never read or store Quran ayah text from the sources; never modify `quran_ayahs.text_uthmani` or any non-navigation column; only ever write the 4 new tables + the 3 new `quran_ayahs` columns; read source files only, never edit them.
- **Canonical numbers:** juz=30, hizb=60, rub=240, sajda=15, ayahs=6236; sajda split = 11 optional / 4 required.
- After each phase, run `dotnet build Backend/QuranDashboard.sln` and the phase's tests; do not advance on red.

## Format: `[ID] [P?] [Story] Description`

- **[P]** = parallelizable (different file, no incomplete dependency).
- **[Story]** = US1/US2/US3/US4 (user-story phases only).

---

## Phase 1: Setup

**Purpose**: Confirm baseline and create empty folders. No behavior yet.

- [ ] T001 Confirm the working branch is `009-quran-navigation-metadata-foundation` in BOTH `App` and `Backend`, then run `dotnet build Backend/QuranDashboard.sln` and confirm it is green before any change.
- [ ] T002 [P] Create empty folders (no files yet): `Backend/domain/QuranDashboard.Domain/Quran/Navigation/`, `Backend/application/QuranDashboard.Application.Abstractions/Quran/Navigation/`, `Backend/application/QuranDashboard.Application/Quran/Navigation/ImportNavigationMetadata/`, `Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/Navigation/`, `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Configurations/Quran/Navigation/`, `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Repositories/Quran/Navigation/`, `Backend/infrastructure/QuranDashboard.Infrastructure/Reports/Quran/Navigation/`, `Backend/tests/QuranDashboard.Tests/Quran/Navigation/`.
- [ ] T003 [P] Confirm the staged package exists at `resources/import-sources/quran-navigation-metadata/` (manifest.json + sources/{juz,hizb,rub,sajda}.json). If absent, note that real-data tasks (T028 real run, T068) are skipped; unit/integration tasks use synthetic fixtures and still run.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Domain entities, schema, and Application-boundary contracts shared by ALL user stories.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete. See `data-model.md` and `contracts/navigation-abstractions.md` for exact field lists.

### Domain (entities are pure POCOs — no EF, no IO)

- [ ] T004 [P] Create `SajdahType` enum `{ Required, Optional }` in `Backend/domain/QuranDashboard.Domain/Quran/Navigation/SajdahType.cs`. PATTERN: `Domain/Quran/Surahs/RevelationPlace.cs`.
- [ ] T005 [P] Create `Juz` entity (`JuzNumber` short PK, `VersesCount` short, `FirstAyahId`/`LastAyahId` int, `FirstVerseKey`/`LastVerseKey` string) in `Backend/domain/QuranDashboard.Domain/Quran/Navigation/Juz.cs`. PATTERN: `Domain/Quran/Surahs/Surah.cs`. Fields per data-model.md §quran_juzs.
- [ ] T006 [P] Create `Hizb` entity (adds `JuzNumber` short FK to Juz fields) in `Backend/domain/QuranDashboard.Domain/Quran/Navigation/Hizb.cs`. Fields per data-model.md §quran_hizbs.
- [ ] T007 [P] Create `Rub` entity (adds `HizbNumber` short FK) in `Backend/domain/QuranDashboard.Domain/Quran/Navigation/Rub.cs`. Fields per data-model.md §quran_rubs.
- [ ] T008 [P] Create `Sajda` entity (`SajdahNumber` short PK, `AyahId` int, `VerseKey` string, `SajdahType` enum) in `Backend/domain/QuranDashboard.Domain/Quran/Navigation/Sajda.cs`. Fields per data-model.md §quran_sajdas.
- [ ] T009 Extend existing `Ayah` entity with three NULLABLE props `short? JuzNumber`, `short? HizbNumber`, `short? RubNumber` in `Backend/domain/QuranDashboard.Domain/Quran/Ayahs/Ayah.cs`. Do NOT change or remove any existing property.

### Application.Abstractions contracts (records + interfaces; see contracts/navigation-abstractions.md)

- [ ] T010 [P] Create `NavigationExpectedCounts` record + `NavigationMetadataInvariants` (counts 30/60/240/15/6236, sajda 4 required/11 optional, refusal messages, all `NAV-*` check-id constants, `WarningVerseCountMatch`, `WarningSajdaDistribution`) in `.../Application.Abstractions/Quran/Navigation/NavigationMetadataInvariants.cs`. PATTERN: `Application.Abstractions/Quran/Translations/TranslationInvariants.cs`. Use exactly the ids in contracts/validation-report.schema.md.
- [ ] T011 [P] Create `NavigationMetadataSourceData` (parsed juz/hizb/rub/sajda records with their `verse_mapping`, plus per-file sha256/size — **NO ayah text field**) in `.../Quran/Navigation/NavigationMetadataSourceData.cs`. PATTERN: `Translations/TranslationSourceData.cs`.
- [ ] T012 [P] Create `NavigationImportTotals(int Juz,int Hizb,int Rub,int Sajda,int AyahsTagged)` + `NavigationMetadataImportResult` (Persisted, Forced, RunAtUtc, Totals, Checks, Errors) in `.../Quran/Navigation/NavigationMetadataImportResult.cs`. PATTERN: `Translations/TranslationImportResult.cs`.
- [ ] T013 [P] Create `NavigationMetadataImportReport` record (all report fields from contracts/validation-report.schema.md: verdict, persisted, forced, sourcePath, totals, ayahCoverage, checks, warnings, errors, noQuranAyahTextReadOrStored) in `.../Quran/Navigation/NavigationMetadataImportReport.cs`. PATTERN: `Translations/TranslationImportReport.cs`.
- [ ] T014 [P] Create `NavigationMetadataSourceException` and `NavigationMetadataValidationException` (carries failed `NAV-*` checks {Id,Expected,Observed}) in `.../Quran/Navigation/NavigationMetadataSourceException.cs` and `.../NavigationMetadataValidationException.cs`. PATTERN: `Translations/TranslationSourceException.cs`, `Translations/TranslationValidationException.cs`.
- [ ] T015 [P] Create `INavigationMetadataImportSource` (`LoadAsync(sourcePath, expected, ct)`, `SourceUnchangedAsync(sourcePath, ct)`) in `.../Quran/Navigation/INavigationMetadataImportSource.cs`. PATTERN: `Translations/ITranslationImportSource.cs`.
- [ ] T016 [P] Create `INavigationMetadataImportWriter` (`AnyTargetTableHasDataAsync`, `ExecuteAcceptedImportAsync(...)` with the exact signature in contracts/navigation-abstractions.md) in `.../Quran/Navigation/INavigationMetadataImportWriter.cs`. PATTERN: `Translations/ITranslationImportWriter.cs`.
- [ ] T017 [P] Create `INavigationMetadataReportWriter` and `INavigationMetadataImportReportBuilder` in `.../Quran/Navigation/INavigationMetadataReportWriter.cs` and `.../INavigationMetadataImportReportBuilder.cs`. PATTERN: `Translations/ITranslationReportWriter.cs`, `Translations/ITranslationImportReportBuilder.cs`.

### EF Core schema (Infrastructure)

- [ ] T018 [P] Create `JuzConfiguration` (`ToTable("quran_juzs")`, PK `juz_number` value-generated-never, columns + FK indexes per data-model.md) in `.../Infrastructure/Persistence/Configurations/Quran/Navigation/JuzConfiguration.cs`. PATTERN: `Configurations/Quran/SurahConfiguration.cs`.
- [ ] T019 [P] Create `HizbConfiguration` (`quran_hizbs`, FK `juz_number` → `quran_juzs`) in `.../Navigation/HizbConfiguration.cs`. PATTERN: SurahConfiguration.cs + AyahConfiguration.cs (FK).
- [ ] T020 [P] Create `RubConfiguration` (`quran_rubs`, FK `hizb_number` → `quran_hizbs`) in `.../Navigation/RubConfiguration.cs`.
- [ ] T021 [P] Create `SajdaConfiguration` (`quran_sajdas`, unique `ayah_id`, `sajdah_type` value-conversion enum→`"required"`/`"optional"`) in `.../Navigation/SajdaConfiguration.cs`. PATTERN: SurahConfiguration.cs `RevelationPlace` HasConversion block.
- [ ] T022 Extend existing `AyahConfiguration` to map `juz_number`/`hizb_number`/`rub_number` as nullable `smallint` columns with non-unique indexes (additive only) in `.../Persistence/Configurations/Quran/AyahConfiguration.cs`.
- [ ] T023 Register `DbSet<Juz>`, `DbSet<Hizb>`, `DbSet<Rub>`, `DbSet<Sajda>` in `.../Infrastructure/Persistence/QuranDashboardDbContext.cs` (mirror how translation entities are registered).
- [ ] T024 Generate the ADDITIVE migration `AddQuranNavigationMetadata` using EF tooling ONLY, via the repo's canonical wrapper from the Backend root: `./scripts/add-mig AddQuranNavigationMetadata`. That wrapper runs `dotnet ef migrations add AddQuranNavigationMetadata --project infrastructure/QuranDashboard.Infrastructure --startup-project api/QuranDashboard.Api --context QuranDashboardDbContext --output-dir Migrations`. The **startup project MUST be `api/QuranDashboard.Api`** — the established convention for every prior migration (features 002/003/004/006); do NOT use `QuranDashboard.DataImporter` as the startup project. Do NOT hand-edit migration/snapshot files; do NOT run `database update` unless explicitly requested. Verify it adds only the 4 tables + 3 nullable ayah columns. Report the generated file names and build status.

**Checkpoint**: `dotnet build` green; schema + contracts compile. User stories can begin.

---

## Phase 3: User Story 1 - Make every ayah navigable by juz/hizb/rub + list sajda (Priority: P1) 🎯 MVP

**Goal**: Running `import-navigation-metadata` against a good package populates the 4 tables, tags all 6,236 ayahs with juz/hizb/rub, records the 15 sajda with type, and exits 0 with reports written.

**Independent Test**: Seed synthetic `quran_ayahs`, run the importer against the synthetic good package → assert counts 30/60/240/15, every ayah has non-null juz/hizb/rub, sajda list = 15 with correct types, exit 0.

### Tests for User Story 1 (write FIRST; expect FAIL before implementation)

- [ ] T025 [P] [US1] Create `NavigationImportTestFixture` that builds a SMALL synthetic package + manifest AND a matching small `NavigationExpectedCounts` (a few juz/hizb/rub/sajda over a small synthetic surah/ayah world), and seeds a matching small synthetic `quran_ayahs` set via Testcontainers PostgreSQL. Synthetic tests inject this small `ExpectedCounts` via `ImportNavigationMetadataCommand.ExpectedCounts` so synthetic data is NEVER measured against the production invariants. File `Backend/tests/QuranDashboard.Tests/Quran/Navigation/NavigationImportTestFixture.cs`. PATTERN: `tests/Quran/Translations/TranslationImportTestFixture.cs`.
- [ ] T026 [P] [US1] Write `NavigationDatasetReaderTests` (parse juz/hizb/rub/sajda JSON into records; sajda type parsed) in `.../Quran/Navigation/NavigationDatasetReaderTests.cs`.
- [ ] T027 [P] [US1] Write `NavigationAssemblerTests`: `verse_mapping` expands to correct per-ayah assignments; every ayah covered exactly once per division type; hierarchy hizb→juz and rub→hizb derived by containment; computed `verses_count` matches range size. File `.../Quran/Navigation/NavigationAssemblerTests.cs`.
- [ ] T028 [P] [US1] Write `NavigationImportTests` happy-path integration using the SMALL synthetic fixture + its injected `ExpectedCounts`: assert the fixture's small counts, every seeded ayah tagged (non-null juz/hizb/rub), the fixture's sajda list, exit 0, reports exist. (The production counts 30/60/240/15/6236 are asserted ONLY by the gated real-package test T068, which uses the default `ExpectedCounts = NavigationMetadataInvariants.Production`.) File `.../Quran/Navigation/NavigationImportTests.cs`.

### Implementation for User Story 1

- [ ] T029 [US1] Implement `NavigationManifestReader` (read `manifest.json`; expose packageType, isFinalImportManifest, sourceFiles[{relativePath,datasetKey,recordCount,sha256,sizeBytes}], expectedCounts) in `.../Infrastructure/Files/Quran/Navigation/NavigationManifestReader.cs`. PATTERN: `Files/Quran/Translations/TranslationManifestReader.cs`.
- [ ] T030 [US1] Implement `JsonNavigationDatasetReader` (parse the 4 keyed-object JSON files into typed records incl. `verse_mapping` as `{surah:"from-to"}`; sajda type string) in `.../Files/Quran/Navigation/JsonNavigationDatasetReader.cs`. PATTERN: `Translations/JsonTranslationSourceReader.cs`. NEVER read any `text` field.
- [ ] T031 [US1] Implement `NavigationMetadataAssembler` (expand each division's `verse_mapping` into the set of `verse_key`s; compute per-ayah juz/hizb/rub; compute each division's `verses_count` from its ranges; derive hizb→juz and rub→hizb by range containment) in `.../Files/Quran/Navigation/NavigationMetadataAssembler.cs`. PATTERN: `Translations/TranslationAssembler.cs`. See data-model.md §Derivation.
- [ ] T032 [US1] Implement `NavigationMetadataImportSource` in `.../Files/Quran/Navigation/NavigationMetadataImportSource.cs`. `LoadAsync` is responsible ONLY for: package-root loading, manifest validation, file-set validation, sha256/size/recordCount validation, JSON parsing, and required source-field validation; it returns parsed `NavigationMetadataSourceData` with **no Quran ayah text** and performs **no database access**. `LoadAsync` MUST NOT resolve `verse_key`s against `quran_ayahs`, expand `verse_mapping`, or run coverage/gap-overlap/hierarchy validation — those run later (assembler T031 in-memory; validator T046 against `quran_ayahs`). `SourceUnchangedAsync` recomputes sha256/size. PATTERN: `Translations/TranslationImportSource.cs` (keep only its file/manifest responsibilities).
- [ ] T033 [US1] Implement `NavigationMetadataSql` (parameterized SQL/const for: probe-target-has-data, insert juz/hizb/rub/sajda rows, `UPDATE quran_ayahs SET juz_number/hizb_number/rub_number`, and the force truncate/null statements) in `.../Infrastructure/Persistence/Repositories/Quran/Navigation/NavigationMetadataSql.cs`. PATTERN: `Repositories/Quran/Translations/TranslationSql.cs`.
- [ ] T034 [US1] Implement `NavigationMetadataBulkCopier` (bulk insert the 345 header rows; bulk `UPDATE` the 6236 ayah nav columns) in `.../Repositories/Quran/Navigation/NavigationMetadataBulkCopier.cs`. PATTERN: `Translations/TranslationBulkCopier.cs`.
- [ ] T035 [US1] Implement `NavigationMetadataCommandExecutor` (open one transaction; orchestrate clear(if force)→insert→update→checks→source-unchanged→commit/rollback) in `.../Repositories/Quran/Navigation/NavigationMetadataCommandExecutor.cs`. PATTERN: `Translations/TranslationCommandExecutor.cs`.
- [ ] T036 [US1] Implement `EfBulkNavigationMetadataImportWriter` (`AnyTargetTableHasDataAsync`; `ExecuteAcceptedImportAsync` happy path: persist + return totals/checks) in `.../Repositories/Quran/Navigation/EfBulkNavigationMetadataImportWriter.cs`. PATTERN: `Translations/EfBulkTranslationImportWriter.cs`.
- [ ] T037 [US1] Create `ImportNavigationMetadataCommand(SourcePath, Force, ExpectedCounts?, ReportOutDir)` and `ImportNavigationMetadataResult` (Succeeded/Message/ExitCode/Totals/ReportOutDir/WarningCount + Success/Failure/Refused factories, `FailureExitCode` const) in `.../Application/Quran/Navigation/ImportNavigationMetadata/ImportNavigationMetadataCommand.cs` and `.../ImportNavigationMetadataResult.cs`. PATTERN: the matching `ImportTranslations*` files.
- [ ] T038 [US1] Implement `ImportNavigationMetadataHandler` happy path (Load → if !Force && AnyTargetTableHasData → refuse → ExecuteAcceptedImport → Success) in `.../ImportNavigationMetadata/ImportNavigationMetadataHandler.cs`. PATTERN: `ImportTranslations/ImportTranslationsHandler.cs`.
- [ ] T039 [US1] Implement `NavigationMetadataImportReportEmitter` (write success report via the report writer) in `.../ImportNavigationMetadata/NavigationMetadataImportReportEmitter.cs`. PATTERN: `ImportTranslations/TranslationImportReportEmitter.cs`.
- [ ] T040 [US1] Register all navigation services (source, writer, report writer, report builder, handler) in `.../Infrastructure/DependencyInjection.cs` (mirror translation registrations).
- [ ] T041 [US1] Add the `import-navigation-metadata` verb to `Backend/tools/QuranDashboard.DataImporter/Program.cs`: dispatch case, `RunImportNavigationMetadataAsync`, `TryParseNavigationArguments` (`--source`/`--report-out`/`--force`), and add the usage line. PATTERN: the `import-translations` verb block. Console success summary: `juz=30, hizb=60, rub=240, sajda=15, ayahsTagged=6236, warnings=N`.

**Checkpoint**: MVP works — good package imports, ayahs tagged, exit 0, reports written. T026–T028 green.

---

## Phase 4: User Story 2 - Reject any import that is not provably correct (Priority: P2)

**Goal**: Broken input (wrong counts, hash/size mismatch, unresolved verse key, gap/overlap, incomplete coverage, broken hierarchy, bad sajda type) is rejected with a clear `NAV-*` reason and ZERO writes.

**Independent Test**: Feed deliberately broken packages → importer aborts before/at validation, DB unchanged, failure report names the failed check.

### Tests for User Story 2 (write FIRST)

- [ ] T042 [P] [US2] Write `NavigationManifestReaderTests` (reject wrong `packageType`, `isFinalImportManifest=false`, missing/extra file, sha256 mismatch, size mismatch, count mismatch) in `.../Quran/Navigation/NavigationManifestReaderTests.cs`.
- [ ] T043 [P] [US2] Write `NavigationValidationFailureTests` (each fails with the right `NAV-*` id, no writes): juz gap, rub overlap, unresolved sajda verse_key, invalid sajda type, an ayah left untagged, hizb spanning two juz. File `.../Quran/Navigation/NavigationValidationFailureTests.cs`.
- [ ] T044 [P] [US2] Write `NavigationRollbackTests` (a hard-check failure inside the transaction → full rollback; tables empty, ayah columns null) in `.../Quran/Navigation/NavigationRollbackTests.cs`.

### Implementation for User Story 2

- [ ] T045 [US2] Implement `NavigationValidationChecks` (pure functions: count check, coverage-exactly-once per type, no gaps/overlaps, hierarchy containment, verse-key resolvable, sajda-type allowed, ayah-columns-complete) returning check results in `.../Infrastructure/Files/Quran/Navigation/NavigationValidationChecks.cs`. PATTERN: `Translations/TranslationValidationChecks.cs`.
- [ ] T046 [US2] Implement `NavigationMetadataValidationRunner` in `.../Persistence/Repositories/Quran/Navigation/NavigationMetadataValidationRunner.cs`. Using the assembler output and the existing `quran_ayahs` data, it resolves every `verse_key` to `quran_ayahs.id` and runs all hard `NAV-*` checks (verse-keys-resolve, range-coverage per type = all ayahs once, no gaps/overlaps, hierarchy containment, sajda-type, ayah-columns-complete), builds the checks list, and throws `NavigationMetadataValidationException` with failed checks. This is where `quran_ayahs` DB resolution + coverage + hierarchy live (NOT in `LoadAsync`). PATTERN: `Translations/TranslationValidationRunner.cs`.
- [ ] T047 [US2] Put the FILE-BASED rejections into `NavigationMetadataImportSource.LoadAsync` (wrong `packageType` / non-final manifest, wrong/missing/extra file set, sha256/size/recordCount mismatch, malformed JSON, missing required fields, sajda type ∉ {required,optional}) → throw `NavigationMetadataSourceException` / `NavigationMetadataValidationException`. Do NOT put any `quran_ayahs`/DB check here — the `AyahsMissing` precondition and verse_key resolution live in the persistence/validator flow (T048/T046). Extends T032 file.
- [ ] T048 [US2] Gate persistence in `NavigationMetadataCommandExecutor`/`EfBulkNavigationMetadataImportWriter`: first require `quran_ayahs` non-empty (else `AyahsMissing`), then run the validation runner (T046, which resolves verse_keys + coverage + hierarchy against `quran_ayahs`) inside the transaction BEFORE commit; on any hard failure, roll back and return `Persisted=false` with checks/errors. Extends T035/T036 files.
- [ ] T049 [US2] Implement the handler failure paths (catch validation/source exceptions → write failure/refusal report → non-zero exit; first failed `NAV-*` in message). Extends `ImportNavigationMetadataHandler.cs` (T038). PATTERN: `ImportTranslationsHandler.cs` catch blocks.

**Checkpoint**: Good package still imports (US1 stays green); every broken package is rejected with no writes. T042–T044 green.

---

## Phase 5: User Story 3 - Configurable source + safe, repeatable, isolated runs (Priority: P2)

**Goal**: `--source` selects the package root (default = workspace-relative staged path, no absolute path); rerun without `--force` refuses and changes nothing; `--force` atomically reloads ONLY navigation-owned data; no other data is ever touched; a source changed mid-run aborts.

**Independent Test**: run with explicit `--source` (reads it) and without (uses default root); run twice (2nd refuses, 0 changes); run `--force` (result == fresh import); verify other tables/columns unchanged; simulate source change → abort.

### Tests for User Story 3 (write FIRST)

- [ ] T050 [P] [US3] Write `NavigationRefusalForceTests` (rerun guard refuses when any nav table OR any ayah nav column is populated; `--force` reload yields identical state to fresh import) in `.../Quran/Navigation/NavigationRefusalForceTests.cs`.
- [ ] T051 [P] [US3] Write source-path tests (no `--source` → resolves to `resources/import-sources/quran-navigation-metadata` package root; explicit `--source` honored; missing dir → clear error, no writes) — add to `.../Quran/Navigation/NavigationImportTests.cs` or a new `NavigationSourcePathTests.cs`.
- [ ] T052 [P] [US3] Write isolation test (after import, surah/ayah-text/page/line/word/tafsir/translation/mutashabihat/morphology/i3rab rows are byte-identical to pre-run; only nav tables + 3 ayah columns changed) in `.../Quran/Navigation/NavigationIsolationTests.cs`.

### Implementation for User Story 3

- [ ] T053 [US3] Add `ResolveDefaultNavigationSourcePath()` (`<repo-root>/resources/import-sources/quran-navigation-metadata`) and `ResolveDefaultNavigationReportDir()` (`<repo-root>/Backend/report/feature-009-quran-navigation-metadata-foundation`) using the existing `ResolveRepositoryRoot()`; default `sourcePath ??=` and `reportOutDir ??=` in the verb. Extends `Program.cs` (T041). Never hard-code an absolute path.
- [ ] T054 [US3] Ensure `AnyTargetTableHasDataAsync` returns true if ANY of the 4 tables is non-empty OR any `quran_ayahs.juz_number/hizb_number/rub_number` is populated. Extends `EfBulkNavigationMetadataImportWriter.cs` (T036).
- [ ] T055 [US3] Implement the `--force` reload: within the transaction, clear the 4 nav tables and reset the 3 ayah columns to NULL, then reload. Add the truncate/null SQL to `NavigationMetadataSql.cs` (T033) and call it from the executor (T035) only when `force` is true.
- [ ] T056 [US3] Wire the `sourceUnchangedCheck` callback so the source sha256/size is re-verified just before commit; if changed, roll back (`NAV-SOURCE-UNCHANGED` fails). Extends executor/writer (T035/T036) + handler wiring (T038), mirroring the translations `sourceUnchangedCheck` parameter.
- [ ] T057 [US3] Add an isolation guard/assert in the executor so the import only ever writes the 4 nav tables and the 3 ayah columns (no other SQL touches other tables). Extends `NavigationMetadataCommandExecutor.cs` (T035).

**Checkpoint**: configurable source + rerun guard + `--force` + isolation all work. T050–T052 green. US1 and US2 stay green.

---

## Phase 6: User Story 4 - Auditable import report (Priority: P3)

**Goal**: Every run writes a complete Markdown + JSON report (verdict, persisted, forced, resolved source path, per-dataset totals, ayah-coverage summary, per-check results, warnings, errors, explicit "no Quran ayah text read or stored"). Acceptance requires both reports written.

**Independent Test**: run a passing and a failing case → both report formats exist, fields match the actual outcome (contracts/validation-report.schema.md), the no-Quran-text assertion is present.

### Tests for User Story 4 (write FIRST)

- [ ] T058 [P] [US4] Write `NavigationReportShapeTests` (JSON has verdict/persisted/forced/sourcePath/totals/ayahCoverage/checks/warnings/errors/noQuranAyahTextReadOrStored; Markdown has the matching sections + closing no-text statement) in `.../Quran/Navigation/NavigationReportShapeTests.cs`.
- [ ] T059 [P] [US4] Write `NavigationSourceSafetyTests` (the importer never reads a `text`/`text_uthmani` field from sources; the report's `noQuranAyahTextReadOrStored` is true; `quran_ayahs.text_uthmani` unchanged) in `.../Quran/Navigation/NavigationSourceSafetyTests.cs`.

### Implementation for User Story 4

- [ ] T060 [US4] Implement `NavigationMetadataImportReportBuilder` (`BuildCandidateSuccess`, `BuildValidationFailure`, `BuildRefusal` → full `NavigationMetadataImportReport`, incl. ayah-coverage summary and no-text flag) in `.../Persistence/Repositories/Quran/Navigation/NavigationMetadataImportReportBuilder.cs`. PATTERN: `Translations/TranslationImportReportBuilder.cs`.
- [ ] T061 [US4] Implement `MarkdownJsonNavigationMetadataReportWriter` (writes both `.json` and `.md` to the report dir per contracts/validation-report.schema.md) in `.../Infrastructure/Reports/Quran/Navigation/MarkdownJsonNavigationMetadataReportWriter.cs`. PATTERN: `Reports/Quran/Translations/MarkdownJsonTranslationReportWriter.cs`.
- [ ] T062 [US4] Emit the two warning checks: `NAV-VERSE-COUNT-MATCH` (source `verses_count` ≠ stored computed count → warn, carry source value) and `NAV-SAJDA-DISTRIBUTION` (split ≠ 11 optional / 4 required → warn). Wire into the report builder (T060) + validation runner (T046).
- [ ] T063 [US4] Ensure acceptance requires both reports written (`NAV-REPORT-WRITTEN`): if report write fails after validation, keep no navigation changes (`ReportRequired` message), per contracts/cli-verb.md exit table. Extends handler/emitter (T038/T039).

**Checkpoint**: full audit reports for success/refusal/failure. T058–T059 green. All four stories green.

---

## Phase 7: Polish & Cross-Cutting

- [ ] T064 [P] Run `dotnet build Backend/QuranDashboard.sln` and apply `dotnet format`; resolve warnings introduced by this feature.
- [ ] T065 [P] Run the clean-code-guard self-check (naming/functions/SOLID/DRY/KISS) against the new files per `.claude/skills/engineering-review/references/clean-code-guard/`; keep C# `I`-prefixed interfaces.
- [ ] T066 [P] Run the test-guard self-check on the new tests (test behavior not implementation; real DTOs/entities; real PostgreSQL where correctness matters; Quranic test data stays source-safe — no ayah text).
- [ ] T067 Run `dotnet test Backend/tests/QuranDashboard.Tests --filter FullyQualifiedName~Quran.Navigation` and confirm all green.
- [ ] T068 (Gated: requires the staged package present AND the T024 migration already APPLIED to the target database with explicit authorization — generating the migration in T024 is NOT sufficient; `dotnet ef database update` / `./scripts/update-db` is a separate, explicitly-authorized step) Real run: `dotnet run --project Backend/tools/QuranDashboard.DataImporter -- import-navigation-metadata` (uses default `ExpectedCounts = Production`); verify quickstart.md SQL (30/60/240/15, 0 untagged ayahs, 15 sajda, `text_uthmani` unchanged) and that reports show `verdict=accepted`, `persisted=true`, coverage complete.
- [ ] T069 Write the backend completion/real-run report under `Backend/report/feature-009-quran-navigation-metadata-foundation/` (follow `Backend/report/README.md` naming).
- [ ] T070 Final full-suite `dotnet test Backend/tests/QuranDashboard.Tests` green; confirm no existing (foundation/words/tafsir/translation/etc.) tests regressed.

---

## Dependencies & Execution Order

### Phase dependencies

- **Setup (P1)** → no deps.
- **Foundational (P2)** → after Setup. **BLOCKS all user stories** (entities, schema, contracts).
- **US1 (P3)** → after Foundational. The MVP.
- **US2 (P4)** → after US1 (extends the same handler/writer/executor/source files; adds the validation runner).
- **US3 (P5)** → after US1 (extends Program.cs/writer/executor; independent of US2 but easiest after it).
- **US4 (P6)** → after US1 (report builder/writer; uses checks from US2 and totals from US1/US3).
- **Polish (P7)** → after all desired stories.

> **Important for this feature**: unlike a typical multi-feature app, these stories are **sequential hardening layers of ONE importer**, not parallel features. US2–US4 EXTEND files created in US1 (handler, writer, executor, source, Program.cs, report). Implement strictly in order P1→P2→P3→P4. Do not parallelize across these four stories.

### Within a phase

- Tests are written first and should FAIL before the matching implementation.
- Domain/contracts (`[P]`) before infrastructure that uses them.
- Readers/assembler before the source; source + writer before the handler; handler before the CLI verb.

### Parallel opportunities

- **Foundational**: T004–T008 (domain), T010–T017 (contracts), T018–T021 (EF configs) are each `[P]` — different files. T009/T022/T023/T024 edit shared/existing files (not `[P]`).
- **Within US1 tests**: T025–T028 `[P]`.
- **Within each later story**: only the test tasks are `[P]`; the implementation tasks mostly extend US1 files (not `[P]`).

---

## Parallel Example: Foundational domain + contracts

```bash
# Domain entities (different files, no deps) in parallel:
Task: "T004 SajdahType enum"   Task: "T005 Juz"   Task: "T006 Hizb"
Task: "T007 Rub"               Task: "T008 Sajda"

# Application contracts (different files) in parallel:
Task: "T010 NavigationMetadataInvariants"  Task: "T011 NavigationMetadataSourceData"
Task: "T015 INavigationMetadataImportSource"  Task: "T016 INavigationMetadataImportWriter"
```

---

## Implementation Strategy

### MVP first (US1 only)

1. Phase 1 Setup → 2. Phase 2 Foundational (CRITICAL, blocks everything) → 3. Phase 3 US1.
4. **STOP and VALIDATE**: run the importer on the (synthetic, then real) good package; confirm 30/60/240/15, all 6236 ayahs tagged, exit 0, reports written.
5. The MVP is a usable navigation-metadata import.

### Incremental hardening

1. US1 → working import (MVP).
2. US2 → it now safely rejects every bad input (no partial writes).
3. US3 → configurable source + safe re-run/force + isolation.
4. US4 → full audit reports.
5. Polish → format, guards, real run, completion report.

---

## Notes

- `[P]` = different files, no incomplete dependency. `[USx]` = traceability to the spec user story.
- **Copy the matching `Quran/Translations` file** named in each task's PATTERN; rename `Translation*`→`NavigationMetadata*`; adjust fields per `data-model.md` / `contracts/`.
- Never read or store Quran ayah text; never touch `quran_ayahs.text_uthmani` or any non-nav column; write only the 4 tables + 3 ayah columns; read source files only.
- Migrations: EF tooling only; no hand-written migration/snapshot edits; no `database update` without explicit request.
- Commit after each phase or logical group (only when the user asks). Stop at any checkpoint to validate a story independently.
