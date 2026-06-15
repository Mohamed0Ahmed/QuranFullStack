# Implementation Plan: Quran Translations Foundation

**Branch**: `008-quran-translations-foundation` | **Date**: 2026-06-15 | **Spec**: [spec.md](./spec.md)  
**Input**: Feature specification from `specs/008-quran-translations-foundation/spec.md`

> **Companion documents (source of truth):**
> `docs/feature-008-quran-translations-foundation/feature-008-quran-translations-foundation-planning-report.md`
> (long-form pre-Spec-Kit planning report),
> `docs/feature-008-quran-translations-foundation/translation-source-curation-report.md`
> (verified source inventory),
> `docs/feature-008-quran-translations-foundation/feature-008-decisions-addendum.md`
> (locked decisions D1-D14), and
> `resources/import-sources/quran-translations/{README.md,manifest.json,source-display-metadata.json,package-report.md}`
> (final local import-source package; every import count traces here).
> **Governance:** `AGENTS.md`, `CODING_PRINCIPLES.md`, `Backend/AGENTS.md`,
> `Backend/.architecture/{BACKEND_STRUCTURE,CLEAN_ARCHITECTURE}.md`.

## Summary

Build the **Quran Translations Foundation** backend import: a local operator-only importer that reads the
final staged package at `resources/import-sources/quran-translations/`, verifies the final manifest and
display metadata contract, resolves all translation verse keys to canonical `quran_ayahs`, stores approved
source selection metadata and exact ayah-level translation text in two translation-owned tables, and emits
Markdown + JSON audit reports.

The feature imports **167 approved ayah-level translation sources** across **83 languages**:
**129 simple** sources and **38 with-footnotes** sources, producing **1,041,412** source-to-ayah
translation entries. The **19 excluded** sources remain report-only: 11 word-by-word files, 6 empty-text
files, and 2 unattributed near-duplicate files.

The importer is a new verb on the existing backend data-import console host: `import-translations`. It is
not an API, UI, search feature, startup seeding path, permissions feature, word-by-word import, publishing
feature, or footnote parser. The feature never edits the source package, never imports excluded sources,
never copies Arabic Quran ayah text, and never mutates existing Quran foundation tables. Translation text
is stored **exactly as imported**, including inline `[[...]]` footnotes and embedded HTML. A run is accepted
only if all hard checks pass, both required report files are written, and the source package still matches
the manifest before commit.

## Technical Context

**Language/Version**: C# 13 / .NET 10 (`net10.0`)  
**Primary Dependencies**: EF Core / PostgreSQL provider already used by Infrastructure; `Npgsql` binary
`COPY` via the existing PostgreSQL connection pattern; `Microsoft.Extensions.Hosting` `10.0.0` for the
existing `QuranDashboard.DataImporter` console host; `System.Text.Json` for manifest, display metadata,
and source parsing.  
**Storage**: PostgreSQL. Two new translation-owned tables are planned:
`quran_translation_sources` and `quran_translation_ayah_entries`. Existing `quran_ayahs` is read-only and
used only to resolve `verse_key -> ayah_id`.  
**Testing**: xUnit `2.9.3`, FluentAssertions `8.2.0`, Testcontainers.PostgreSql `4.4.0` in
`Backend/tests/QuranDashboard.Tests`, plus pure unit tests for manifest/display readers, source readers,
and assembly/validation logic.  
**Target Platform**: Linux server / local backend operator environment running .NET 10.  
**Project Type**: Existing Backend Clean Architecture solution. Reuse the existing
`tools/QuranDashboard.DataImporter` project; no new project and no frontend/API project work.  
**Performance Goals**: Import 167 complete translation source files and produce **1,041,412**
source-to-ayah rows (`167 * 6,236`) in an operator-run batch. Use file-by-file parsing and bulk writes so
the import does not require holding all raw source text for all files in memory at once. This is not a
user-facing latency path.  
**Constraints**: Local staged package only; final manifest only; final display metadata only; exact file
set/size/sha256 validation; all 167 approved sources must have the exact 6,236 verse-key set; every
translation text must be non-empty string `t`; 19 excluded sources report-only; translation text stored
exactly as imported; Quran ayah text not copied; every verse key resolved to `quran_ayahs`; normal run
refuses if translation tables already contain data; `--force` rebuilds only translation-owned tables;
transactional hard-gated import; no accepted run without both report formats; no app-user permissions,
HTTP, UI, search, word-by-word import, public publishing, footnote parsing/sanitization, or startup
seeding.  
**Scale/Scope**: 167 source rows; 1,041,412 ayah-entry rows; 83 languages; 19 excluded sources;
approximately 279 MiB approved payload. `resources/` is local and Git-ignored.

*No unresolved clarification items. All open choices from the planning report and decisions addendum are
locked: 2-table model, hard no-empty-text policy, preserve-markup policy, source-level
`translation_type`, denormalized source metadata, `.fn.json` footnote marker, and final display metadata
contract.*

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

The project constitution (`.specify/memory/constitution.md`) is still an unratified placeholder template.
For Feature 008, the explicit interim governance authority is the workspace/backend rule set:
`AGENTS.md`, `CODING_PRINCIPLES.md`, `Backend/AGENTS.md`, `Backend/.architecture/BACKEND_STRUCTURE.md`,
and `Backend/.architecture/CLEAN_ARCHITECTURE.md`. Do not infer additional constitution MUST rules from
the placeholder constitution. Ratifying a real constitution remains a separate `/speckit-constitution`
concern.

| Gate | Status | Notes |
|---|---|---|
| Clean Architecture dependency direction | PASS | Domain owns pure translation entities; Application orchestrates import via abstractions; Infrastructure implements file readers, assembler, persistence, validation, and report writing; console host is composition/dispatch only. |
| Feature/domain foldering | PASS | New types live under `Quran/Translations/` and `Quran/Translations/ImportTranslations/` feature folders. No global `Models`, `DTOs`, `Helpers`, or technical dumping folders. |
| Import source safety | PASS | Reads only `resources/import-sources/quran-translations/`; source package is never modified; source package is re-verified before commit. |
| Quranic data safety | PASS | Translation text is imported as source content; Arabic Quran ayah text is never copied; `quran_ayahs` and prior foundation data are read-only. |
| EF migration policy | PASS | Planning allows schema changes only; migration must be generated by EF tooling later and only on explicit implementation/migration request. No hand-written migrations in this planning phase. |
| API boundary | N/A | No API endpoints, controllers, or `ApiResponse` behavior in Feature 008. |
| Operator-only scope | PASS | Console import only; no app-user permissions, frontend, public API, search, or public reader behavior. |
| Reporting/audit gate | PASS | Accepted import requires both JSON and Markdown reports. Report write failure means no accepted translation changes are kept. |
| Display metadata contract | PASS | Import acceptance depends on the final `source-display-metadata.json`; required app-facing display names are import-blocking. |

**Post-design re-check:** PASS. The generated data model and contracts preserve the same boundaries and
introduce no justified violations.

## Project Structure

### Documentation (this feature)

```text
specs/008-quran-translations-foundation/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── cli-verb.md
│   ├── translation-abstractions.md
│   └── validation-report.schema.md
├── checklists/
│   └── requirements.md
└── tasks.md                 # created later by /speckit-tasks, not by /speckit-plan
```

### Source Code (repository root)

```text
Backend/
  domain/QuranDashboard.Domain/Quran/Translations/
    TranslationSource.cs
    TranslationAyahEntry.cs

  application/QuranDashboard.Application.Abstractions/Quran/Translations/
    ITranslationImportSource.cs
    ITranslationImportWriter.cs
    ITranslationReportWriter.cs
    ITranslationImportReportBuilder.cs
    TranslationSourceData.cs
    TranslationImportResult.cs
    TranslationImportReport.cs
    TranslationImportConstants.cs
    TranslationInvariants.cs
    TranslationSourceException.cs
    TranslationValidationException.cs

  application/QuranDashboard.Application/Quran/Translations/ImportTranslations/
    ImportTranslationsCommand.cs
    ImportTranslationsHandler.cs
    ImportTranslationsResult.cs
    TranslationImportReportEmitter.cs

  infrastructure/QuranDashboard.Infrastructure/
    Files/Quran/Translations/
      TranslationManifestReader.cs
      TranslationDisplayMetadataReader.cs
      JsonTranslationSourceReader.cs
      TranslationAssembler.cs
      TranslationImportSource.cs
      TranslationValidationChecks.cs
    Persistence/Configurations/Quran/Translations/
      TranslationSourceConfiguration.cs
      TranslationAyahEntryConfiguration.cs
    Persistence/Repositories/Quran/Translations/
      EfBulkTranslationImportWriter.cs
      TranslationBulkCopier.cs
      TranslationCommandExecutor.cs
      TranslationImportReportBuilder.cs
      TranslationSql.cs
      TranslationValidationRunner.cs
    Reports/Quran/Translations/
      MarkdownJsonTranslationReportWriter.cs
    Persistence/QuranDashboardDbContext.cs
    DependencyInjection.cs

  tools/QuranDashboard.DataImporter/
    Program.cs                      # add import-translations verb only

  tests/QuranDashboard.Tests/Quran/Translations/
    TranslationSchemaShapeTests.cs
    TranslationManifestReaderTests.cs
    TranslationDisplayMetadataReaderTests.cs
    TranslationSourceReaderTests.cs
    TranslationAssemblerTests.cs
    TranslationImportTests.cs
    TranslationValidationFailureTests.cs
    TranslationExcludedSourceTests.cs
    TranslationRefusalForceTests.cs
    TranslationRollbackTests.cs
    TranslationReportShapeTests.cs
    TranslationSourceSafetyTests.cs
    TranslationImportTestFixture.cs
```

**Structure Decision**: Reuse the existing backend solution and `QuranDashboard.DataImporter` console host.
The feature belongs under `Quran/Translations` because it is an ayah-level Quran research content
foundation beside `Ayahs`, `Words`, `Mutashabihat`, and `Tafsirs`. No frontend/API folders are touched.
Contracts document the operator CLI and Application-boundary abstractions; they do not define public HTTP
endpoints.

## Complexity Tracking

No constitution or architecture violations.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| _(none)_ | - | - |
