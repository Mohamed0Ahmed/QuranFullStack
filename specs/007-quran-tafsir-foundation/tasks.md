# Tasks: Quran Tafsir Foundation

**Input**: Design documents from `/projects/Dashboard/App/specs/007-quran-tafsir-foundation/`
**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `quickstart.md`, `contracts/`
**Branch**: `007-quran-tafsir-foundation`

**Tests**: Required. The feature specification defines independent tests for each user story, and the implementation is source-data-sensitive.

**Organization**: Tasks are grouped by user story so each story can be implemented and tested independently after the shared foundation is complete.

**Non-negotiable scope guards for all tasks**:

- Do not add API endpoints, controllers, frontend screens, app-user permissions, startup seeding, public reader behavior, search indexing, or translation features.
- Do not edit, normalize, rewrite, or generate files under `/projects/Dashboard/App/resources/import-sources/quran-tafsirs/`.
- Do not copy Quran ayah text into tafsir-owned records.
- Do not hand-write EF Core migrations; generate migration files only with EF tooling when executing the schema task.
- Treat execution of T015 during `/speckit-implement` as the explicit schema/migration authorization for Feature 007; outside that flow, do not generate migrations unless the user explicitly asks.
- Keep backend files under the feature/domain paths listed in `plan.md`; do not create global `Models`, `DTOs`, `Helpers`, or `Utils` folders.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel because it touches different files and does not depend on incomplete tasks.
- **[Story]**: Required only for user story phases.
- Every task includes a concrete file or folder path.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Prepare folders, references, and source-safe test scaffolding before feature code begins.

- [ ] T001 Create the Feature 007 backend report folder and README placeholder in `/projects/Dashboard/App/Backend/report/feature-007-quran-tafsir-foundation/README.md` documenting that implementation reports belong here and importer-generated reports default to `/projects/Dashboard/App/resources/report/quran-tafsirs/`.
- [ ] T002 [P] Create the domain/application/infrastructure feature folders from `plan.md` under `/projects/Dashboard/App/Backend/domain/QuranDashboard.Domain/Quran/Tafsirs/`, `/projects/Dashboard/App/Backend/application/QuranDashboard.Application.Abstractions/Quran/Tafsirs/`, `/projects/Dashboard/App/Backend/application/QuranDashboard.Application/Quran/Tafsirs/ImportTafsirs/`, `/projects/Dashboard/App/Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/Tafsirs/`, `/projects/Dashboard/App/Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Repositories/Quran/Tafsirs/`, and `/projects/Dashboard/App/Backend/infrastructure/QuranDashboard.Infrastructure/Reports/Quran/Tafsirs/`.
- [ ] T003 [P] Create the Feature 007 test folder `/projects/Dashboard/App/Backend/tests/QuranDashboard.Tests/Quran/Tafsirs/` and keep all tafsir tests there.
- [ ] T004 [P] Inspect prior importer patterns in `/projects/Dashboard/App/Backend/application/QuranDashboard.Application/Quran/Mutashabihat/ImportMutashabihat/`, `/projects/Dashboard/App/Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/Mutashabihat/`, and `/projects/Dashboard/App/Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Repositories/Quran/Mutashabihat/` before writing tafsir code.
- [ ] T005 [P] Add source-safe synthetic fixture helpers in `/projects/Dashboard/App/Backend/tests/QuranDashboard.Tests/Quran/Tafsirs/TafsirImportTestFixture.cs`; fixtures must use synthetic tafsir text and synthetic verse keys only, unless a real source excerpt is explicitly copied with traceable provenance.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Define the shared domain model, application contracts, schema mapping, and DI surface required by every user story.

**Critical**: No user story implementation should begin until this phase is complete.

- [ ] T006 [P] Implement the `TafsirSource` domain entity in `/projects/Dashboard/App/Backend/domain/QuranDashboard.Domain/Quran/Tafsirs/TafsirSource.cs` with source metadata fields from `data-model.md`, no EF attributes, and no file/database logic.
- [ ] T007 [P] Implement the `TafsirEntry` domain entity in `/projects/Dashboard/App/Backend/domain/QuranDashboard.Domain/Quran/Tafsirs/TafsirEntry.cs` with leader ayah id, exact tafsir text, covered ayah count, covered ayah keys JSON, source shape, and text hash.
- [ ] T008 [P] Implement the `TafsirAyahEntry` domain entity in `/projects/Dashboard/App/Backend/domain/QuranDashboard.Domain/Quran/Tafsirs/TafsirAyahEntry.cs` with source id, ayah id, entry id, verse key, source value kind, leader verse key, group-leader flag, and sort order.
- [ ] T009 [P] Implement invariant constants and refusal messages in `/projects/Dashboard/App/Backend/application/QuranDashboard.Application.Abstractions/Quran/Tafsirs/TafsirInvariants.cs` using the locked counts `84`, `9`, `35`, `49`, `33`, `6236`, and `523824`.
- [ ] T010 [P] Implement application contract interfaces in `/projects/Dashboard/App/Backend/application/QuranDashboard.Application.Abstractions/Quran/Tafsirs/ITafsirImportSource.cs`, `/projects/Dashboard/App/Backend/application/QuranDashboard.Application.Abstractions/Quran/Tafsirs/ITafsirImportWriter.cs`, and `/projects/Dashboard/App/Backend/application/QuranDashboard.Application.Abstractions/Quran/Tafsirs/ITafsirReportWriter.cs` exactly following `contracts/tafsir-abstractions.md`.
- [ ] T011 [P] Implement application result and DTO records in `/projects/Dashboard/App/Backend/application/QuranDashboard.Application.Abstractions/Quran/Tafsirs/TafsirSourceData.cs`, `/projects/Dashboard/App/Backend/application/QuranDashboard.Application.Abstractions/Quran/Tafsirs/TafsirImportResult.cs`, `/projects/Dashboard/App/Backend/application/QuranDashboard.Application.Abstractions/Quran/Tafsirs/TafsirImportTotals.cs`, and `/projects/Dashboard/App/Backend/application/QuranDashboard.Application.Abstractions/Quran/Tafsirs/TafsirCheckResult.cs`.
- [ ] T012 [P] Implement the `ImportTafsirsCommand`, `ImportTafsirsHandler`, and `ImportTafsirsResult` shells in `/projects/Dashboard/App/Backend/application/QuranDashboard.Application/Quran/Tafsirs/ImportTafsirs/ImportTafsirsCommand.cs`, `/projects/Dashboard/App/Backend/application/QuranDashboard.Application/Quran/Tafsirs/ImportTafsirs/ImportTafsirsHandler.cs`, and `/projects/Dashboard/App/Backend/application/QuranDashboard.Application/Quran/Tafsirs/ImportTafsirs/ImportTafsirsResult.cs`; the handler must depend only on tafsir abstractions.
- [ ] T013 [P] Implement EF configurations for `quran_tafsir_sources`, `quran_tafsir_entries`, and `quran_tafsir_ayah_entries` in `/projects/Dashboard/App/Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Configurations/Quran/Tafsirs/TafsirSourceConfiguration.cs`, `/projects/Dashboard/App/Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Configurations/Quran/Tafsirs/TafsirEntryConfiguration.cs`, and `/projects/Dashboard/App/Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Configurations/Quran/Tafsirs/TafsirAyahEntryConfiguration.cs` with all constraints and indexes from `data-model.md`.
- [ ] T014 Add `DbSet` properties and configuration discovery for tafsir entities in `/projects/Dashboard/App/Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/QuranDashboardDbContext.cs`.
- [ ] T015 Generate the schema-only EF migration for the three tafsir tables with EF tooling only because `/speckit-implement` execution of this task is explicit Feature 007 migration authorization; produce files under `/projects/Dashboard/App/Backend/infrastructure/QuranDashboard.Infrastructure/Migrations/`, do not run database update, and do not hand-write or manually edit the migration or snapshot.
- [ ] T016 [P] Register tafsir services and implementations in `/projects/Dashboard/App/Backend/infrastructure/QuranDashboard.Infrastructure/DependencyInjection.cs` without changing API project behavior.
- [ ] T017 [P] Register the `ImportTafsirsHandler` application service if required by existing conventions in `/projects/Dashboard/App/Backend/application/QuranDashboard.Application/DependencyInjection.cs`.
- [ ] T018 [P] Add schema-shape tests for tafsir tables, constraints, and indexes in `/projects/Dashboard/App/Backend/tests/QuranDashboard.Tests/Quran/Tafsirs/TafsirSchemaShapeTests.cs`.

**Checkpoint**: Domain entities, application contracts, EF mapping, DI registration, and schema tests exist. User story implementation can now begin.

---

## Phase 3: User Story 1 - Import approved tafsir package (Priority: P1) MVP

**Goal**: Import the final curated tafsir package into approved source records, exact tafsir text blocks, and canonical ayah links.

**Independent Test**: Run a passing import against a source-safe fixture shaped like the final package and verify approved source count, language counts, source-to-ayah links, grouped text blocks, exact text preservation, and no copied Quran ayah text.

### Tests for User Story 1

- [ ] T019 [P] [US1] Add manifest reader tests for final package shape, `manifestType = "quran-tafsir-import-source-package"`, `isFinalImportManifest = true`, approved/excluded counts, language counts, source metadata, file size, and sha256 fields in `/projects/Dashboard/App/Backend/tests/QuranDashboard.Tests/Quran/Tafsirs/TafsirManifestReaderTests.cs`.
- [ ] T020 [P] [US1] Add source reader tests for object values, string pointer values, grouped `ayah_keys`, inline markup preservation, empty text refusal, and malformed JSON in `/projects/Dashboard/App/Backend/tests/QuranDashboard.Tests/Quran/Tafsirs/TafsirSourceReaderTests.cs`.
- [ ] T021 [P] [US1] Add assembler tests for resolving synthetic verse keys to ayah ids, storing grouped text once, expanding every covered ayah to an ayah-link DTO, computing exact text hash, and never carrying Quran ayah text in `/projects/Dashboard/App/Backend/tests/QuranDashboard.Tests/Quran/Tafsirs/TafsirAssemblerTests.cs`.
- [ ] T022 [P] [US1] Add happy-path import integration tests with PostgreSQL/Testcontainers in `/projects/Dashboard/App/Backend/tests/QuranDashboard.Tests/Quran/Tafsirs/TafsirImportTests.cs` covering source rows, text-block rows, ayah-link rows, distinct ayahs, and exact text preservation.

### Implementation for User Story 1

- [ ] T023 [P] [US1] Implement final package manifest parsing in `/projects/Dashboard/App/Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/Tafsirs/TafsirManifestReader.cs`; it must read only the supplied package path and must not modify source files.
- [ ] T024 [P] [US1] Implement source JSON parsing in `/projects/Dashboard/App/Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/Tafsirs/JsonTafsirSourceReader.cs` for root verse-key objects, object values with `text` and optional `ayah_keys`, and string pointers to leader verse keys.
- [ ] T025 [US1] Implement tafsir assembly in `/projects/Dashboard/App/Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/Tafsirs/TafsirAssembler.cs`, including verse-key resolution to `quran_ayahs`, grouped block expansion, duplicate source/ayah detection, source-shape classification, and text hashing.
- [ ] T026 [US1] Implement the `ITafsirImportSource` facade in `/projects/Dashboard/App/Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/Tafsirs/TafsirImportSource.cs`, combining manifest validation, file-set validation, source hash validation, source parsing, ayah lookup, and `SourceUnchangedAsync`.
- [ ] T027 [P] [US1] Implement SQL constants and table names for tafsir bulk import in `/projects/Dashboard/App/Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Repositories/Quran/Tafsirs/TafsirSql.cs`.
- [ ] T028 [US1] Implement FK-safe bulk copy for source rows, text-block rows, and ayah-link rows in `/projects/Dashboard/App/Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Repositories/Quran/Tafsirs/TafsirBulkCopier.cs`.
- [ ] T029 [US1] Implement the first passing-path import writer in `/projects/Dashboard/App/Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Repositories/Quran/Tafsirs/EfBulkTafsirImportWriter.cs`; it must write only tafsir-owned tables and must not mutate `quran_ayahs`.
- [ ] T030 [US1] Complete `ImportTafsirsHandler` orchestration in `/projects/Dashboard/App/Backend/application/QuranDashboard.Application/Quran/Tafsirs/ImportTafsirs/ImportTafsirsHandler.cs`, wiring source load, expected counts, import writer, source-unchanged check, and report callback.
- [ ] T031 [US1] Add the `import-tafsirs` local operator verb and arguments `--source`, `--report-out`, and `--force` to `/projects/Dashboard/App/Backend/tools/QuranDashboard.DataImporter/Program.cs` while preserving all existing verbs.
- [ ] T032 [US1] Verify User Story 1 with `dotnet test tests/QuranDashboard.Tests --filter "FullyQualifiedName~Quran.Tafsirs"` from `/projects/Dashboard/App/Backend` and record the result in `/projects/Dashboard/App/Backend/report/feature-007-quran-tafsir-foundation/001-us1-import-foundation-verification.md`.

**Checkpoint**: User Story 1 imports approved tafsir fixture data into tafsir-owned tables, resolves canonical ayahs, preserves tafsir text exactly, and exposes only the local console verb.

---

## Phase 4: User Story 2 - Protect import integrity and scope (Priority: P2)

**Goal**: Refuse unsafe package drift, excluded-source leakage, unresolved ayahs, invalid pointers, duplicate mappings, and Quran foundation mutation.

**Independent Test**: Tamper synthetic packages and canonical ayah fixtures, then verify the import refuses without accepted tafsir data and reports stable `TAFSIR-` hard-check failures.

### Tests for User Story 2

- [ ] T033 [P] [US2] Add validation failure tests for missing package files, wrong `manifestType`, `isFinalImportManifest = false`, wrong approved/excluded/language counts, changed file size, changed sha256, malformed source JSON, and wrong top-level ayah count in `/projects/Dashboard/App/Backend/tests/QuranDashboard.Tests/Quran/Tafsirs/TafsirValidationFailureTests.cs`.
- [ ] T034 [P] [US2] Add excluded-source refusal tests for the locked excluded keys `ar-wajiz`, `ar-durr-al-manthur`, `ar-ibn-al-qayyim`, `ar-ibn-uthaymeen`, `ar-baydawi`, `ar-suddi`, `ar-muyassar-fi-al-gharib`, `id-saadi`, and `tr-ibn-kathir` in `/projects/Dashboard/App/Backend/tests/QuranDashboard.Tests/Quran/Tafsirs/TafsirExcludedSourceTests.cs`.
- [ ] T035 [P] [US2] Add ayah and pointer integrity tests for unresolved verse keys, unresolved grouped pointer targets, empty resolved text, and duplicate source/ayah mappings in `/projects/Dashboard/App/Backend/tests/QuranDashboard.Tests/Quran/Tafsirs/TafsirAyahResolutionTests.cs`.
- [ ] T036 [P] [US2] Add source safety tests that assert source package files are not modified and Quran foundation ayah text is not inserted, updated, deleted, or copied by tafsir import code in `/projects/Dashboard/App/Backend/tests/QuranDashboard.Tests/Quran/Tafsirs/TafsirSourceSafetyTests.cs`.

### Implementation for User Story 2

- [ ] T037 [US2] Harden package-shape and manifest-final validation in `/projects/Dashboard/App/Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/Tafsirs/TafsirManifestReader.cs` with hard check IDs `TAFSIR-PACKAGE-SHAPE`, `TAFSIR-MANIFEST-FINAL`, `TAFSIR-SOURCE-COUNT`, `TAFSIR-EXCLUDED-COUNT`, `TAFSIR-ARABIC-SOURCE-COUNT`, and `TAFSIR-NON-ARABIC-SOURCE-COUNT`; `TAFSIR-MANIFEST-FINAL` must require `manifestType = "quran-tafsir-import-source-package"` and `isFinalImportManifest = true`.
- [ ] T038 [US2] Harden file-set, file-size, sha256, excluded-source, and coverage-count validation in `/projects/Dashboard/App/Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/Tafsirs/TafsirImportSource.cs` with hard check IDs `TAFSIR-SOURCE-SET`, `TAFSIR-SOURCE-HASH`, `TAFSIR-NO-EXCLUDED-SOURCES`, and `TAFSIR-COVERAGE-COUNT`.
- [ ] T039 [US2] Harden JSON shape, ayah resolution, pointer resolution, empty text, duplicate mapping, exact text, and no-Quran-text checks in `/projects/Dashboard/App/Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/Tafsirs/TafsirAssembler.cs`.
- [ ] T040 [US2] Implement post-copy validation runner in `/projects/Dashboard/App/Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Repositories/Quran/Tafsirs/TafsirValidationRunner.cs` for `TAFSIR-POSTCOPY-SOURCE-ROWS`, `TAFSIR-POSTCOPY-AYAH-MAPPINGS`, `TAFSIR-TEXT-UNCHANGED`, `TAFSIR-NO-QURAN-TEXT-COPY`, and `TAFSIR-SOURCE-UNCHANGED`.
- [ ] T041 [US2] Implement rollback-on-hard-failure behavior in `/projects/Dashboard/App/Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Repositories/Quran/Tafsirs/EfBulkTafsirImportWriter.cs`; failed validation must return or raise a failure with `Persisted = false`.
- [ ] T042 [US2] Verify User Story 2 with `dotnet test tests/QuranDashboard.Tests --filter "FullyQualifiedName~Quran.Tafsirs"` from `/projects/Dashboard/App/Backend` and record the result in `/projects/Dashboard/App/Backend/report/feature-007-quran-tafsir-foundation/002-us2-integrity-verification.md`.

**Checkpoint**: User Story 2 refuses unsafe input, keeps excluded sources report-only, protects Quran foundation data, and leaves no accepted partial tafsir changes.

---

## Phase 5: User Story 3 - Re-run safely with explicit operator intent (Priority: P3)

**Goal**: Make repeated imports safe by refusing accidental appends and allowing explicit rebuilds of tafsir-owned tables only.

**Independent Test**: Import once, run again without `--force` and expect refusal, then run with `--force` and verify only tafsir-owned data is replaced.

### Tests for User Story 3

- [ ] T043 [P] [US3] Add refusal tests for a normal second run when tafsir tables already contain data in `/projects/Dashboard/App/Backend/tests/QuranDashboard.Tests/Quran/Tafsirs/TafsirRefusalForceTests.cs`.
- [ ] T044 [P] [US3] Add force rebuild tests that seed tafsir data and Quran ayah data, run with `--force`, and assert only `quran_tafsir_sources`, `quran_tafsir_entries`, and `quran_tafsir_ayah_entries` are replaced in `/projects/Dashboard/App/Backend/tests/QuranDashboard.Tests/Quran/Tafsirs/TafsirForceRebuildTests.cs`.
- [ ] T045 [P] [US3] Add rollback tests for validation failure after a forced rebuild starts, excluding report-write failure cases owned by US4, in `/projects/Dashboard/App/Backend/tests/QuranDashboard.Tests/Quran/Tafsirs/TafsirRollbackTests.cs`.

### Implementation for User Story 3

- [ ] T046 [US3] Implement `AnyTargetTableHasDataAsync` and normal-run refusal in `/projects/Dashboard/App/Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Repositories/Quran/Tafsirs/EfBulkTafsirImportWriter.cs` using the `TafsirInvariants.TargetsNotEmpty` message.
- [ ] T047 [US3] Implement force rebuild clearing for tafsir-owned tables only in `/projects/Dashboard/App/Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Repositories/Quran/Tafsirs/TafsirSql.cs` and `/projects/Dashboard/App/Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Repositories/Quran/Tafsirs/EfBulkTafsirImportWriter.cs`.
- [ ] T048 [US3] Ensure forced rebuild uses a single transaction boundary and never clears or rewrites non-tafsir tables in `/projects/Dashboard/App/Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Repositories/Quran/Tafsirs/EfBulkTafsirImportWriter.cs`.
- [ ] T049 [US3] Ensure CLI parsing passes `--force` correctly into `ImportTafsirsCommand` and prints actionable refusal text in `/projects/Dashboard/App/Backend/tools/QuranDashboard.DataImporter/Program.cs`.
- [ ] T050 [US3] Verify User Story 3 with `dotnet test tests/QuranDashboard.Tests --filter "FullyQualifiedName~Quran.Tafsirs"` from `/projects/Dashboard/App/Backend` and record the result in `/projects/Dashboard/App/Backend/report/feature-007-quran-tafsir-foundation/003-us3-rerun-verification.md`.

**Checkpoint**: User Story 3 prevents accidental duplicate imports and supports explicit tafsir-only rebuilds with rollback on failure.

---

## Phase 6: User Story 4 - Produce audit-ready import reports (Priority: P4)

**Goal**: Produce Markdown and JSON audit reports for successful, refused, and failed import attempts, including all counts, checks, warnings, errors, and persistence status.

**Independent Test**: Generate passing, refused, failed, and report-write-failure cases, then inspect report contents and persistence status.

### Tests for User Story 4

- [ ] T051 [P] [US4] Add JSON report shape tests for `runAtUtc`, `verdict`, `persisted`, `forced`, `sourcePath`, `totals`, `sourceSummaries`, `excludedSourceSummaries`, `checks`, `warnings`, `errors`, and `infoNotes` in `/projects/Dashboard/App/Backend/tests/QuranDashboard.Tests/Quran/Tafsirs/TafsirJsonReportShapeTests.cs`.
- [ ] T052 [P] [US4] Add Markdown report shape tests for verdict, persisted flag, source path, totals table, hard checks table, warnings, and excluded-source table in `/projects/Dashboard/App/Backend/tests/QuranDashboard.Tests/Quran/Tafsirs/TafsirMarkdownReportShapeTests.cs`.
- [ ] T053 [P] [US4] Add warning tests for `TAFSIR-PROVENANCE-WARNING`, `TAFSIR-MODERN-WORKS-WARNING`, `TAFSIR-INLINE-MARKUP`, `TAFSIR-LANGUAGE-COVERAGE`, and `TAFSIR-TEXT-BLOCK-COUNT` in `/projects/Dashboard/App/Backend/tests/QuranDashboard.Tests/Quran/Tafsirs/TafsirWarningTests.cs`.
- [ ] T054 [P] [US4] Add report-write failure tests that make the report output path unwritable and assert no accepted tafsir changes remain after validation passes in `/projects/Dashboard/App/Backend/tests/QuranDashboard.Tests/Quran/Tafsirs/TafsirReportWriteFailureTests.cs`.

### Implementation for User Story 4

- [ ] T055 [US4] Implement import report building with source summaries, excluded-source summaries, language summaries, totals, hard checks, warnings, errors, and info notes in `/projects/Dashboard/App/Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Repositories/Quran/Tafsirs/TafsirImportReportBuilder.cs`.
- [ ] T056 [US4] Implement Markdown and JSON report writing in `/projects/Dashboard/App/Backend/infrastructure/QuranDashboard.Infrastructure/Reports/Quran/Tafsirs/MarkdownJsonTafsirReportWriter.cs` with canonical filenames `tafsir-import-report.md` and `tafsir-import-report.json`.
- [ ] T057 [US4] Include all required stable check IDs from `contracts/validation-report.schema.md` in `/projects/Dashboard/App/Backend/application/QuranDashboard.Application.Abstractions/Quran/Tafsirs/TafsirInvariants.cs` or a tafsir-owned constants file under `/projects/Dashboard/App/Backend/application/QuranDashboard.Application.Abstractions/Quran/Tafsirs/`.
- [ ] T058 [US4] Ensure refused-before-write and failed-validation attempts produce reports whenever the report writer is available in `/projects/Dashboard/App/Backend/application/QuranDashboard.Application/Quran/Tafsirs/ImportTafsirs/ImportTafsirsHandler.cs`.
- [ ] T059 [US4] Ensure report-write failure after validation passes rolls back the database transaction before commit in `/projects/Dashboard/App/Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Repositories/Quran/Tafsirs/EfBulkTafsirImportWriter.cs`.
- [ ] T060 [US4] Ensure successful CLI output includes `sources=84`, `ayahMappings=523824`, `languages=33`, warning count, and report directory in `/projects/Dashboard/App/Backend/tools/QuranDashboard.DataImporter/Program.cs`.
- [ ] T061 [US4] Verify User Story 4 with `dotnet test tests/QuranDashboard.Tests --filter "FullyQualifiedName~Quran.Tafsirs"` from `/projects/Dashboard/App/Backend` and record the result in `/projects/Dashboard/App/Backend/report/feature-007-quran-tafsir-foundation/004-us4-reporting-verification.md`.

**Checkpoint**: User Story 4 provides audit-ready Markdown and JSON reports for success, refusal, validation failure, and report-write failure.

---

## Final Phase: Polish & Cross-Cutting Concerns

**Purpose**: Validate the complete feature against contracts, quickstart, architecture, and source-data safety.

- [ ] T062 [P] Run the full backend build from `/projects/Dashboard/App/Backend` and record the command, result, and any warnings in `/projects/Dashboard/App/Backend/report/feature-007-quran-tafsir-foundation/005-build-verification.md`.
- [ ] T063 Run the full backend test suite from `/projects/Dashboard/App/Backend` and record the command, result, and any skipped tests in `/projects/Dashboard/App/Backend/report/feature-007-quran-tafsir-foundation/006-test-verification.md`.
- [ ] T064 Validate the implemented CLI against `/projects/Dashboard/App/specs/007-quran-tafsir-foundation/quickstart.md` and record the command outputs, report paths, row-count SQL results, and persistence status in `/projects/Dashboard/App/Backend/report/feature-007-quran-tafsir-foundation/007-quickstart-validation.md`.
- [ ] T065 [P] Run an architecture self-check against `/projects/Dashboard/App/Backend/.architecture/BACKEND_STRUCTURE.md` and `/projects/Dashboard/App/Backend/.architecture/CLEAN_ARCHITECTURE.md`, then record any deviations or `PASS` in `/projects/Dashboard/App/Backend/report/feature-007-quran-tafsir-foundation/008-architecture-self-check.md`.
- [ ] T066 [P] Run a source-safety self-check confirming `/projects/Dashboard/App/resources/import-sources/quran-tafsirs/` was not modified and Quran foundation data is read-only, then record evidence in `/projects/Dashboard/App/Backend/report/feature-007-quran-tafsir-foundation/009-source-safety-check.md`.
- [ ] T067 [P] Confirm no Feature 007 API/frontend/public-reader/search/startup-seeding files were added by scanning `/projects/Dashboard/App/Backend/api/`, `/projects/Dashboard/App/Frontend/quran-dashboard-ui/`, and `/projects/Dashboard/App/Backend/tools/QuranDashboard.DataImporter/`, then record the result in `/projects/Dashboard/App/Backend/report/feature-007-quran-tafsir-foundation/010-scope-check.md`.
- [ ] T068 Update `/projects/Dashboard/App/Backend/report/feature-007-quran-tafsir-foundation/README.md` with final report index links for build, tests, quickstart validation, architecture check, source-safety check, and scope check.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 Setup**: No dependencies.
- **Phase 2 Foundational**: Depends on Phase 1 and blocks every user story.
- **Phase 3 US1**: Depends on Phase 2; this is the MVP import foundation.
- **Phase 4 US2**: Depends on Phase 2 and can begin after US1 parser/import skeleton exists.
- **Phase 5 US3**: Depends on Phase 2 and the import writer from US1.
- **Phase 6 US4**: Depends on Phase 2 and can be developed alongside US2/US3 after result/check records exist.
- **Final Phase**: Depends on all desired user stories being complete.

### User Story Dependencies

- **US1 (P1)**: Core import path; required before the feature is useful.
- **US2 (P2)**: Builds on US1 source parsing and writer paths, but each integrity scenario is independently testable.
- **US3 (P3)**: Builds on US1 writer/CLI paths, but force/refusal behavior is independently testable.
- **US4 (P4)**: Uses result/check data from US1-US3 and owns report acceptance behavior, including report-write failure rollback after validation passes.

### Within Each User Story

- Write the listed tests first and confirm they fail for missing behavior.
- Implement source readers before assemblers.
- Implement assemblers before writers.
- Implement writers before CLI integration.
- Verify each story with its checkpoint task before moving to the next priority story.

---

## Parallel Execution Examples

### User Story 1

```text
Parallel test tasks:
- T019 TafsirManifestReaderTests.cs
- T020 TafsirSourceReaderTests.cs
- T021 TafsirAssemblerTests.cs
- T022 TafsirImportTests.cs

Parallel implementation tasks after tests are written:
- T023 TafsirManifestReader.cs
- T024 JsonTafsirSourceReader.cs
- T027 TafsirSql.cs
```

### User Story 2

```text
Parallel test tasks:
- T033 TafsirValidationFailureTests.cs
- T034 TafsirExcludedSourceTests.cs
- T035 TafsirAyahResolutionTests.cs
- T036 TafsirSourceSafetyTests.cs
```

### User Story 3

```text
Parallel test tasks:
- T043 TafsirRefusalForceTests.cs
- T044 TafsirForceRebuildTests.cs
- T045 TafsirRollbackTests.cs
```

### User Story 4

```text
Parallel test tasks:
- T051 TafsirJsonReportShapeTests.cs
- T052 TafsirMarkdownReportShapeTests.cs
- T053 TafsirWarningTests.cs
- T054 TafsirReportWriteFailureTests.cs
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 setup.
2. Complete Phase 2 foundation.
3. Complete Phase 3 US1.
4. Stop and validate `T032` before adding stricter failure, rerun, or report-detail behavior.

### Incremental Delivery

1. **US1**: Approved package import, exact tafsir text, canonical ayah links, local CLI verb.
2. **US2**: Integrity refusal and source/Quran safety.
3. **US3**: Safe re-run and force rebuild behavior.
4. **US4**: Audit-ready report detail and report-write failure acceptance gate.
5. **Final**: Build, test, quickstart, architecture, source-safety, and scope checks.

### Cheaper-Model Implementation Notes

- Read `spec.md`, `plan.md`, `data-model.md`, `contracts/`, and `quickstart.md` before starting each phase.
- Do not infer new counts; use the locked constants in `TafsirInvariants`.
- Do not invent Quran text or tafsir content in tests; use clearly synthetic fixture strings.
- Do not commit data without reports; `contracts/tafsir-abstractions.md` requires reports before transaction commit.
- If a task mentions EF migration generation, use EF tooling and report the generated files; do not manually create migration files.
