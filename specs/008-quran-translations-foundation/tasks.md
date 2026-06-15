# Tasks: Quran Translations Foundation

**Input**: Design documents from `specs/008-quran-translations-foundation/`  
**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `quickstart.md`, `contracts/`  
**Tests**: Included because the plan and quickstart define required backend test areas for parser, validation, persistence, rollback, and report behavior.  
**Scope reminder**: Backend import foundation only. No frontend, API endpoints, search, startup seeding, permissions, word-by-word import, source package mutation, hand-written migrations, or database update.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel after its phase prerequisites are met because it touches different files and has no dependency on incomplete tasks.
- **[Story]**: User story label for story phases only.
- Every task includes concrete paths so the implementation model does not need to infer placement.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Prepare the feature folders, report index, and implementation guardrails without changing behavior.

- [X] T001 Create Feature 008 backend source folders from `plan.md` if missing: `Backend/domain/QuranDashboard.Domain/Quran/Translations/`, `Backend/application/QuranDashboard.Application.Abstractions/Quran/Translations/`, `Backend/application/QuranDashboard.Application/Quran/Translations/ImportTranslations/`, `Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/Translations/`, `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Configurations/Quran/Translations/`, `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Repositories/Quran/Translations/`, and `Backend/infrastructure/QuranDashboard.Infrastructure/Reports/Quran/Translations/`
- [X] T002 [P] Create Feature 008 test folder `Backend/tests/QuranDashboard.Tests/Quran/Translations/`
- [X] T003 [P] Create Feature 008 backend report folder and index `Backend/report/feature-008-quran-translations-foundation/README.md` following `Backend/report/README.md`
- [X] T004 [P] Confirm the local package files exist without modifying them: `resources/import-sources/quran-translations/README.md`, `resources/import-sources/quran-translations/manifest.json`, `resources/import-sources/quran-translations/source-display-metadata.json`, `resources/import-sources/quran-translations/package-report.md`, and `resources/import-sources/quran-translations/sources/`
- [X] T005 [P] Add a short implementation-scope note to `Backend/report/feature-008-quran-translations-foundation/001-implementation-scope.md` listing the no-frontend/no-API/no-source-mutation/no-hand-written-migration constraints

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Define entities, contracts, schema mapping, constants, and source-safe test fixture scaffolding required by every story.

**Critical**: No user story implementation starts until this phase is complete.

- [X] T006 [P] Add schema shape tests for `quran_translation_sources` and `quran_translation_ayah_entries` in `Backend/tests/QuranDashboard.Tests/Quran/Translations/TranslationSchemaShapeTests.cs`
- [X] T007 [P] Add domain entity `TranslationSource` with only v1 persisted columns from `data-model.md` in `Backend/domain/QuranDashboard.Domain/Quran/Translations/TranslationSource.cs`
- [X] T008 [P] Add domain entity `TranslationAyahEntry` with source, ayah, optional verse key, and exact text fields in `Backend/domain/QuranDashboard.Domain/Quran/Translations/TranslationAyahEntry.cs`
- [X] T009 [P] Add EF configuration for `TranslationSource` table, indexes, required fields, and check constraints in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Configurations/Quran/Translations/TranslationSourceConfiguration.cs`
- [X] T010 [P] Add EF configuration for `TranslationAyahEntry` table, FK relationships, unique `(source_id, ayah_id)`, and indexes in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Configurations/Quran/Translations/TranslationAyahEntryConfiguration.cs`
- [X] T011 Register `TranslationSource` and `TranslationAyahEntry` DbSets and apply configurations in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/QuranDashboardDbContext.cs`
- [X] T012 Add translation invariants, expected production counts, hard check IDs, warning IDs, info IDs, and refusal messages in `Backend/application/QuranDashboard.Application.Abstractions/Quran/Translations/TranslationInvariants.cs`
- [X] T013 [P] Add translation import result, totals, check result, report, source summary, and excluded source summary records in `Backend/application/QuranDashboard.Application.Abstractions/Quran/Translations/TranslationImportResult.cs` and `Backend/application/QuranDashboard.Application.Abstractions/Quran/Translations/TranslationImportReport.cs`
- [X] T014 [P] Add source DTO records for sources, ayah entries, excluded sources, and expected counts in `Backend/application/QuranDashboard.Application.Abstractions/Quran/Translations/TranslationSourceData.cs`
- [X] T015 [P] Add focused Application abstractions in `Backend/application/QuranDashboard.Application.Abstractions/Quran/Translations/ITranslationImportSource.cs`, `Backend/application/QuranDashboard.Application.Abstractions/Quran/Translations/ITranslationImportWriter.cs`, `Backend/application/QuranDashboard.Application.Abstractions/Quran/Translations/ITranslationReportWriter.cs`, and `Backend/application/QuranDashboard.Application.Abstractions/Quran/Translations/ITranslationImportReportBuilder.cs`
- [X] T016 [P] Add `TranslationSourceException` and `TranslationValidationException` carrying check results in `Backend/application/QuranDashboard.Application.Abstractions/Quran/Translations/TranslationSourceException.cs` and `Backend/application/QuranDashboard.Application.Abstractions/Quran/Translations/TranslationValidationException.cs`
- [X] T017 [P] Add import command/result records in `Backend/application/QuranDashboard.Application/Quran/Translations/ImportTranslations/ImportTranslationsCommand.cs` and `Backend/application/QuranDashboard.Application/Quran/Translations/ImportTranslations/ImportTranslationsResult.cs`
- [X] T018 [P] Add report emitter wrapper for report-write failure handling in `Backend/application/QuranDashboard.Application/Quran/Translations/ImportTranslations/TranslationImportReportEmitter.cs`
- [X] T019 Record the planned DI mapping from abstractions to future concrete classes in `Backend/report/feature-008-quran-translations-foundation/001-implementation-scope.md` without editing `Backend/infrastructure/QuranDashboard.Infrastructure/DependencyInjection.cs`
- [X] T020 Add source-safe synthetic fixture scaffolding with fake verse keys and fake translation strings only in `Backend/tests/QuranDashboard.Tests/Quran/Translations/TranslationImportTestFixture.cs`
- [X] T021 Add helper methods to seed synthetic `quran_ayahs`, clear translation tables, create temporary source packages, and capture table snapshots in `Backend/tests/QuranDashboard.Tests/Quran/Translations/TranslationImportTestFixture.cs`
- [X] T022 After explicit user approval for migration generation, generate the EF Core schema migration under `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Migrations/` using tooling only; do not hand-write migration files or run database update

**Checkpoint**: Translation bounded context, contracts, schema mapping, planned DI mapping, and synthetic test fixture are ready.

---

## Phase 3: User Story 1 - Import Approved Translation Sources (Priority: P1) MVP

**Goal**: A valid final package import accepts 167 sources, 129 simple, 38 with-footnotes, and 1,041,412 ayah mappings with exact text preservation.

**Independent Test**: Run a valid synthetic package import through `ImportTranslationsHandler` and verify persisted source/ayah counts, type counts, distinct ayahs, exact text, and required success reports.

### Tests for User Story 1

- [X] T023 [P] [US1] Add manifest success tests for final manifest values, approved source count 167, type counts 129/38, excluded count 19, language count 83, and mapping count 1,041,412 in `Backend/tests/QuranDashboard.Tests/Quran/Translations/TranslationManifestReaderTests.cs`
- [X] T024 [P] [US1] Add display metadata success tests for final status, 167 records, required display fields, and manifest source-set alignment in `Backend/tests/QuranDashboard.Tests/Quran/Translations/TranslationDisplayMetadataReaderTests.cs`
- [X] T025 [P] [US1] Add source reader tests for object root, `{ "t": string }` values, 6,236-key set handling, and exact text preservation in `Backend/tests/QuranDashboard.Tests/Quran/Translations/TranslationSourceReaderTests.cs`
- [X] T026 [P] [US1] Add assembler tests for verse-key-to-ayah resolution, translation type by content, inline footnote flag, HTML flag, and exact text DTO output in `Backend/tests/QuranDashboard.Tests/Quran/Translations/TranslationAssemblerTests.cs`
- [X] T027 [US1] Add end-to-end successful import test for source rows, ayah rows, type totals, distinct ayahs, exact text, and success report files in `Backend/tests/QuranDashboard.Tests/Quran/Translations/TranslationImportTests.cs`

### Implementation for User Story 1

- [X] T028 [US1] Implement final manifest reader and package shape/hash/file-set validation for success paths in `Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/Translations/TranslationManifestReader.cs`
- [X] T029 [US1] Implement final display metadata reader and manifest alignment for success paths in `Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/Translations/TranslationDisplayMetadataReader.cs`
- [X] T030 [US1] Implement JSON translation source reader for object root and `{ "t": string }` source rows in `Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/Translations/JsonTranslationSourceReader.cs`
- [X] T031 [US1] Implement assembler that merges manifest and display metadata, resolves verse keys to `quran_ayahs`, and produces `TranslationSourceData` in `Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/Translations/TranslationAssembler.cs`
- [X] T032 [US1] Implement high-level source loader that coordinates manifest, display metadata, source files, ayah map, checks, and source unchanged digests in `Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/Translations/TranslationImportSource.cs`
- [X] T033 [US1] Implement bulk copy for `quran_translation_sources` and `quran_translation_ayah_entries` in FK-safe order in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Repositories/Quran/Translations/TranslationBulkCopier.cs`
- [X] T034 [US1] Implement SQL constants for source/ayah row counts, duplicate checks, source unchanged checks, table existence checks, and optional truncation in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Repositories/Quran/Translations/TranslationSql.cs`
- [X] T035 [US1] Implement command executor wrapper for database commands used by translation import persistence in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Repositories/Quran/Translations/TranslationCommandExecutor.cs`
- [X] T036 [US1] Implement validation runner success checks for post-copy source rows, ayah mappings, text unchanged, no Quran text copy, and source unchanged in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Repositories/Quran/Translations/TranslationValidationRunner.cs`
- [X] T037 [US1] Implement transaction writer for successful imports, report callback before commit, and persisted result totals in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Repositories/Quran/Translations/EfBulkTranslationImportWriter.cs`
- [X] T038 [US1] Implement `ImportTranslationsHandler` success path orchestration using Application abstractions only in `Backend/application/QuranDashboard.Application/Quran/Translations/ImportTranslations/ImportTranslationsHandler.cs`
- [X] T039 [US1] Implement minimal success report builder payload needed for acceptance in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Repositories/Quran/Translations/TranslationImportReportBuilder.cs`
- [X] T040 [US1] Implement Markdown and JSON report file writing for passing imports in `Backend/infrastructure/QuranDashboard.Infrastructure/Reports/Quran/Translations/MarkdownJsonTranslationReportWriter.cs`
- [X] T041 [US1] Add `import-translations` verb dispatch, default source path, default report path, argument parsing, and success console summary in `Backend/tools/QuranDashboard.DataImporter/Program.cs`
- [X] T042 [US1] Add DI registrations for implemented translation readers, writer, report builder, report writer, and handler in `Backend/infrastructure/QuranDashboard.Infrastructure/DependencyInjection.cs`

**Checkpoint**: MVP import path works against synthetic valid package and writes success reports.

---

## Phase 4: User Story 2 - Reject Unsafe or Out-of-Scope Sources (Priority: P2)

**Goal**: Invalid packages, incomplete display metadata, unsafe source shapes, excluded sources, duplicate mappings, unresolved ayahs, and hard-check failures are refused without partial persistence.

**Independent Test**: Mutate one synthetic package condition at a time and verify non-zero/failure result, failed `TR-*` evidence, `persisted = false`, and no translation-owned rows left behind.

### Tests for User Story 2

- [X] T043 [P] [US2] Add validation tests for missing package files, non-final manifest, wrong counts, wrong source set, wrong size, and wrong sha256 in `Backend/tests/QuranDashboard.Tests/Quran/Translations/TranslationValidationFailureTests.cs`
- [X] T044 [P] [US2] Add display metadata failure tests for missing file, invalid JSON, non-final status, wrong source count, source-set mismatch, missing required fields, empty display names, and non-final record status in `Backend/tests/QuranDashboard.Tests/Quran/Translations/TranslationDisplayMetadataReaderTests.cs`
- [X] T045 [P] [US2] Add source-shape failure tests for malformed verse keys, missing `t`, null `t`, non-string `t`, empty `t`, extra verse key, missing verse key, and invalid root shape in `Backend/tests/QuranDashboard.Tests/Quran/Translations/TranslationSourceReaderTests.cs`
- [X] T046 [P] [US2] Add excluded-source refusal tests for word-by-word, empty-text, and unattributed near-duplicate source keys in `Backend/tests/QuranDashboard.Tests/Quran/Translations/TranslationExcludedSourceTests.cs`
- [X] T047 [P] [US2] Add ayah resolution, duplicate `(source, ayah)`, and no-Quran-text-copy failure tests in `Backend/tests/QuranDashboard.Tests/Quran/Translations/TranslationValidationFailureTests.cs`
- [X] T048 [US2] Add transaction rollback test proving failed validation after writes leaves zero accepted rows in `Backend/tests/QuranDashboard.Tests/Quran/Translations/TranslationRollbackTests.cs`

### Implementation for User Story 2

- [X] T049 [US2] Add all hard check creation helpers and consistent pass/fail result construction in `Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/Translations/TranslationValidationChecks.cs`
- [X] T050 [US2] Harden `TranslationManifestReader` to emit `TR-PACKAGE-SHAPE`, `TR-MANIFEST-FINAL`, `TR-SOURCE-COUNT`, `TR-TYPE-COUNTS`, `TR-EXCLUDED-COUNT`, `TR-SOURCE-SET`, and `TR-SOURCE-HASH` failures in `Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/Translations/TranslationManifestReader.cs`
- [X] T051 [US2] Harden `TranslationDisplayMetadataReader` to emit `TR-DISPLAY-METADATA-FINAL`, `TR-DISPLAY-METADATA-SET`, and `TR-DISPLAY-METADATA-REQUIRED-FIELDS` failures in `Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/Translations/TranslationDisplayMetadataReader.cs`
- [X] T052 [US2] Harden `JsonTranslationSourceReader` to fail on invalid root shape, malformed keys, missing/non-string/empty `t`, incomplete 6,236 set, and extra verse keys in `Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/Translations/JsonTranslationSourceReader.cs`
- [X] T053 [US2] Harden `TranslationAssembler` to fail unresolved ayahs, duplicate source/ayah mappings, excluded sources, wrong translation type values, and any copied Arabic Quran text candidates in `Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/Translations/TranslationAssembler.cs`
- [X] T054 [US2] Harden `TranslationValidationRunner` to verify no excluded sources, no duplicate ayah entries, post-copy row counts, text unchanged, source unchanged, rollback on fail, and rerun guard evidence in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Repositories/Quran/Translations/TranslationValidationRunner.cs`
- [X] T055 [US2] Update `EfBulkTranslationImportWriter` to roll back all translation-owned writes on any hard-check failure or report callback failure in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Repositories/Quran/Translations/EfBulkTranslationImportWriter.cs`
- [X] T056 [US2] Update `ImportTranslationsHandler` to catch source, validation, IO, and data-shape failures, build failure/refusal results, and write JSON/Markdown failure or refusal reports through `TranslationImportReportEmitter` whenever the report output path has been resolved in `Backend/application/QuranDashboard.Application/Quran/Translations/ImportTranslations/ImportTranslationsHandler.cs`

**Checkpoint**: Unsafe and out-of-scope source conditions fail closed and never leave partial import data.

---

## Phase 5: User Story 3 - Produce Acceptance Reports (Priority: P3)

**Goal**: Every accepted or rejected run produces machine-readable and human-readable audit reports with totals, source summaries, excluded summaries, checks, warnings, errors, and final verdict.

**Independent Test**: Complete valid, invalid, and refused synthetic runs, inspect JSON and Markdown reports, and verify schema, required check IDs, warning IDs, no translation body text, and no Arabic Quran ayah text.

### Tests for User Story 3

- [X] T057 [P] [US3] Add JSON report shape tests covering success, validation failure, and refusal reports with top-level fields, totals, source summaries, excluded summaries, checks, warnings, errors, and info notes in `Backend/tests/QuranDashboard.Tests/Quran/Translations/TranslationReportShapeTests.cs`
- [X] T058 [P] [US3] Add Markdown report shape tests covering success, validation failure, and refusal reports with verdict, persisted, forced, source path, totals, hard checks, warnings, excluded sources, and reclassified sources in `Backend/tests/QuranDashboard.Tests/Quran/Translations/TranslationReportShapeTests.cs`
- [X] T059 [P] [US3] Add report content safety tests proving reports do not include translation body text or Arabic Quran ayah text in `Backend/tests/QuranDashboard.Tests/Quran/Translations/TranslationSourceSafetyTests.cs`
- [X] T060 [P] [US3] Add warning/info tests for `TR-PROVENANCE-WARNING`, `TR-INLINE-MARKUP`, `TR-LANGUAGE-COVERAGE`, and `TR-RECLASSIFIED` in `Backend/tests/QuranDashboard.Tests/Quran/Translations/TranslationReportShapeTests.cs`
- [X] T061 [US3] Add report-write failure rollback test for unwritable report path in `Backend/tests/QuranDashboard.Tests/Quran/Translations/TranslationRollbackTests.cs`

### Implementation for User Story 3

- [X] T062 [US3] Complete `TranslationImportReportBuilder` for success, validation failure, and refusal reports, including failures that occur before database writes and refusals caused by existing translation data without `--force`, in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Repositories/Quran/Translations/TranslationImportReportBuilder.cs`
- [X] T063 [US3] Ensure success reports enumerate every hard check from `FR-032`, all warning/info checks, source summaries, excluded summaries, and final verdict in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Repositories/Quran/Translations/TranslationImportReportBuilder.cs`
- [X] T064 [US3] Complete JSON and Markdown serialization with stable filenames `translation-import-report.json` and `translation-import-report.md` in `Backend/infrastructure/QuranDashboard.Infrastructure/Reports/Quran/Translations/MarkdownJsonTranslationReportWriter.cs`
- [X] T065 [US3] Update `TranslationImportReportEmitter` to write success, validation failure, and refusal reports, and to convert report-write exceptions into acceptance-critical failures without hiding the original report directory in `Backend/application/QuranDashboard.Application/Quran/Translations/ImportTranslations/TranslationImportReportEmitter.cs`
- [X] T066 [US3] Update `ImportTranslationsResult` message formatting so CLI output includes report directory, warning count, and first actionable error in `Backend/application/QuranDashboard.Application/Quran/Translations/ImportTranslations/ImportTranslationsResult.cs`
- [X] T067 [US3] Add generated report filename constants only to `Backend/application/QuranDashboard.Application.Abstractions/Quran/Translations/TranslationImportConstants.cs`

**Checkpoint**: Reports are complete enough to audit the import without reading code or source files.

---

## Phase 6: User Story 4 - Safely Replace a Previous Import (Priority: P4)

**Goal**: Normal re-runs refuse existing translation data, and explicit forced replacements revalidate first and replace only translation-owned tables atomically.

**Independent Test**: Run a successful synthetic import, attempt a second normal import, attempt a forced valid replacement, and attempt a forced invalid replacement; verify existing data is preserved or replaced exactly as specified.

### Tests for User Story 4

- [X] T068 [P] [US4] Add normal re-run refusal test for non-empty translation target tables in `Backend/tests/QuranDashboard.Tests/Quran/Translations/TranslationRefusalForceTests.cs`
- [X] T069 [P] [US4] Add forced replacement success test proving only translation tables are rebuilt and Quran foundation rows remain unchanged in `Backend/tests/QuranDashboard.Tests/Quran/Translations/TranslationRefusalForceTests.cs`
- [X] T070 [P] [US4] Add forced replacement failure test proving previous accepted translation data remains unchanged when the new package fails validation in `Backend/tests/QuranDashboard.Tests/Quran/Translations/TranslationRollbackTests.cs`

### Implementation for User Story 4

- [X] T071 [US4] Implement `AnyTargetTableHasDataAsync` for translation-owned tables in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Repositories/Quran/Translations/EfBulkTranslationImportWriter.cs`
- [X] T072 [US4] Implement force-only truncate/rebuild SQL limited to `quran_translation_ayah_entries` and `quran_translation_sources` in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Repositories/Quran/Translations/TranslationSql.cs`
- [X] T073 [US4] Ensure `EfBulkTranslationImportWriter` validates the replacement package before truncating existing translation data in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Repositories/Quran/Translations/EfBulkTranslationImportWriter.cs`
- [X] T074 [US4] Add `--force` parsing, refusal text, and forced-run console summary for `import-translations` in `Backend/tools/QuranDashboard.DataImporter/Program.cs`
- [X] T075 [US4] Ensure `ImportTranslationsHandler` refuses normal re-runs with `TR-RERUN-GUARD` evidence and accepts forced intent only through the command in `Backend/application/QuranDashboard.Application/Quran/Translations/ImportTranslations/ImportTranslationsHandler.cs`

**Checkpoint**: Re-run and forced replacement behavior is safe, explicit, and transactionally verified.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Verify integration, document implementation evidence, and leave a clean handoff.

- [ ] T076 [P] Add or update backend report `Backend/report/feature-008-quran-translations-foundation/002-schema-and-importer-implementation-report.md` summarizing schema, import flow, and exact changed paths
- [ ] T077 [P] Add backend report `Backend/report/feature-008-quran-translations-foundation/003-validation-and-reporting-verification.md` summarizing all `TR-*` checks and report files
- [ ] T078 [P] Add backend report `Backend/report/feature-008-quran-translations-foundation/004-source-safety-and-scope-check.md` confirming no source package mutation, no copied Arabic Quran text, no frontend/API/search work, and no non-translation table mutation
- [ ] T079 Run Feature 008 test subset `dotnet test tests/QuranDashboard.Tests --filter "FullyQualifiedName~Quran.Translations"` from `Backend/` and record results in `Backend/report/feature-008-quran-translations-foundation/005-test-verification.md`
- [ ] T080 Run backend build `dotnet build QuranDashboard.sln --no-restore` from `Backend/` after implementation and record results in `Backend/report/feature-008-quran-translations-foundation/006-build-verification.md`
- [ ] T081 Run quickstart command smoke test for `import-translations --help` or argument validation without modifying the real database and record results in `Backend/report/feature-008-quran-translations-foundation/007-quickstart-validation.md`
- [ ] T082 Run clean-code self-check against `.claude/skills/engineering-review/references/clean-code-guard/` for Feature 008 changed backend files and record results in `Backend/report/feature-008-quran-translations-foundation/008-clean-code-self-check.md`
- [ ] T083 Run test-code self-check for `Backend/tests/QuranDashboard.Tests/Quran/Translations/` and record results in `Backend/report/feature-008-quran-translations-foundation/009-test-code-self-check.md`
- [ ] T084 Verify git diff contains no frontend files, no API controllers, no source package edits under `resources/import-sources/quran-translations/`, and no hand-written migration edits; record results in `Backend/report/feature-008-quran-translations-foundation/010-final-scope-check.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 Setup**: No dependencies.
- **Phase 2 Foundational**: Depends on Phase 1.
- **User Stories**: Depend on Phase 2 because they need entities, contracts, DbContext mapping, planned DI mapping, and fixture scaffolding; concrete DI registration happens later in T042 after the implementations exist.
- **Phase 7 Polish**: Depends on whichever user stories are implemented.

### User Story Dependencies

- **US1 (P1) Import Approved Translation Sources**: Starts after Phase 2. This is the MVP and must include minimal success reports because reports are acceptance-critical.
- **US2 (P2) Reject Unsafe or Out-of-Scope Sources**: Starts after Phase 2. Best implemented after US1 so failure paths reuse the same reader/assembler/writer flow.
- **US3 (P3) Produce Acceptance Reports**: Starts after US1 success path exists. Hardens reports for success, refusal, validation failure, warnings, and write failure.
- **US4 (P4) Safely Replace a Previous Import**: Starts after US1 writer exists. Best completed after US2 rollback behavior is in place.

### Recommended Order

```text
Phase 1 -> Phase 2 -> US1 -> US2 -> US3 -> US4 -> Phase 7
```

### Story Independence Notes

- US1 can be demonstrated with a valid synthetic package and success report files.
- US2 can be demonstrated by mutating the synthetic package and proving no rows persist.
- US3 can be demonstrated with report files from valid, refused, and failed runs.
- US4 can be demonstrated with repeated synthetic imports and table snapshots.

---

## Parallel Opportunities

- **Setup**: T002, T003, T004, and T005 can run in parallel after T001 if folders exist.
- **Foundational**: T006-T010 and T012-T018 can be split across files after folders exist; T011 depends on T007-T010.
- **US1 tests**: T023-T026 can be written in parallel using the shared fixture from T020-T021.
- **US1 implementation**: T028-T030 can run in parallel; T031 depends on T028-T030; T033-T036 can run in parallel after T012-T015 and schema mapping exist.
- **US2 tests**: T043-T047 can run in parallel because each targets a different invalid condition.
- **US3 tests**: T057-T060 can run in parallel after report DTOs exist.
- **US4 tests**: T068-T070 can run in parallel after the successful import fixture works.
- **Polish reports**: T076-T078 can run in parallel after the relevant stories are complete.

## Parallel Example: User Story 1

```bash
# Tests that can be drafted together after Phase 2:
Task T023: Backend/tests/QuranDashboard.Tests/Quran/Translations/TranslationManifestReaderTests.cs
Task T024: Backend/tests/QuranDashboard.Tests/Quran/Translations/TranslationDisplayMetadataReaderTests.cs
Task T025: Backend/tests/QuranDashboard.Tests/Quran/Translations/TranslationSourceReaderTests.cs
Task T026: Backend/tests/QuranDashboard.Tests/Quran/Translations/TranslationAssemblerTests.cs

# Reader implementations that can start together:
Task T028: Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/Translations/TranslationManifestReader.cs
Task T029: Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/Translations/TranslationDisplayMetadataReader.cs
Task T030: Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/Translations/JsonTranslationSourceReader.cs
```

## Parallel Example: User Story 2

```bash
Task T043: Backend/tests/QuranDashboard.Tests/Quran/Translations/TranslationValidationFailureTests.cs
Task T044: Backend/tests/QuranDashboard.Tests/Quran/Translations/TranslationDisplayMetadataReaderTests.cs
Task T045: Backend/tests/QuranDashboard.Tests/Quran/Translations/TranslationSourceReaderTests.cs
Task T046: Backend/tests/QuranDashboard.Tests/Quran/Translations/TranslationExcludedSourceTests.cs
```

## Parallel Example: User Story 3

```bash
Task T057: Backend/tests/QuranDashboard.Tests/Quran/Translations/TranslationReportShapeTests.cs
Task T059: Backend/tests/QuranDashboard.Tests/Quran/Translations/TranslationSourceSafetyTests.cs
Task T062: Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Repositories/Quran/Translations/TranslationImportReportBuilder.cs
Task T064: Backend/infrastructure/QuranDashboard.Infrastructure/Reports/Quran/Translations/MarkdownJsonTranslationReportWriter.cs
```

## Parallel Example: User Story 4

```bash
Task T068: Backend/tests/QuranDashboard.Tests/Quran/Translations/TranslationRefusalForceTests.cs
Task T070: Backend/tests/QuranDashboard.Tests/Quran/Translations/TranslationRollbackTests.cs
Task T072: Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Repositories/Quran/Translations/TranslationSql.cs
Task T074: Backend/tools/QuranDashboard.DataImporter/Program.cs
```

---

## Implementation Strategy

### MVP First (US1 Only)

1. Complete Phase 1 setup.
2. Complete Phase 2 foundational contracts, entities, schema mapping, planned DI mapping, and fixtures.
3. Complete Phase 3 US1.
4. Stop and validate US1 with `TranslationImportTests`.
5. Do not proceed to real package/database import until synthetic tests pass and reports are written.

### Incremental Delivery

1. **US1**: Successful valid import with counts and success reports.
2. **US2**: Fail-closed validation and rollback for bad packages and unsafe data.
3. **US3**: Full audit report schema and warning/info coverage.
4. **US4**: Safe refusal and forced replacement behavior.
5. **Polish**: Build/test verification and backend reports.

### Cheaper-Model Guardrails

- Always read `specs/008-quran-translations-foundation/spec.md`, `plan.md`, `data-model.md`, and `contracts/` before editing code.
- Use Feature 007 Tafsir import as a pattern, but do not copy the three-table model; translations use exactly two v1 tables.
- Use `source-display-metadata.json` for final display names and required display fields.
- Keep `packageFile`, `sha256`, `fileSizeBytes`, `license`, `provenance`, and review confidence data in DTO/report evidence only, not v1 DB columns.
- Preserve `t` text exactly; never parse, strip, sanitize, normalize, or split inline footnotes/HTML.
- Never copy Arabic Quran ayah text into translation-owned records.
- Never modify `resources/import-sources/quran-translations/`.
- Never hand-write migration files; use EF tooling only after explicit migration approval.

## Task Count Summary

| Area | Count |
|---|---:|
| Setup | 5 |
| Foundational | 17 |
| US1 Import approved sources | 20 |
| US2 Reject unsafe/out-of-scope sources | 14 |
| US3 Produce reports | 11 |
| US4 Safe replacement | 8 |
| Polish/cross-cutting | 9 |
| **Total** | **84** |
