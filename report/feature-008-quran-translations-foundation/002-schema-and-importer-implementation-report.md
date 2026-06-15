# Feature 008 — Schema and Importer Implementation Report

**Feature**: Quran Translations Foundation  
**Branch**: `008-quran-translations-foundation`  
**Produced by**: T076 (Phase 7 polish)  
**Date**: 2026-06-15

## Summary

Feature 008 adds a backend-only translation import foundation: two translation-owned tables, a
`import-translations` verb on `QuranDashboard.DataImporter`, and a Clean Architecture import pipeline
from staged package files through validation, bulk persistence, and audit reports.

## Schema

### Migration

| Item | Value |
| --- | --- |
| Migration name | `AddQuranTranslations` |
| Generated file | `infrastructure/QuranDashboard.Infrastructure/Migrations/20260615112132_AddQuranTranslations.cs` |
| Designer | `20260615112132_AddQuranTranslations.Designer.cs` |
| Model snapshot | `Migrations/QuranDashboardDbContextModelSnapshot.cs` (updated) |
| Generation method | EF Core tooling only (not hand-written) |
| `database update` | Not run |

### Tables

| Table | Entity | Purpose |
| --- | --- | --- |
| `quran_translation_sources` | `TranslationSource` | One row per approved translation source (167 expected) |
| `quran_translation_ayah_entries` | `TranslationAyahEntry` | One row per source/ayah mapping with exact `text` (1,041,412 expected) |

### Key constraints

- `quran_translation_sources`: unique `source_key`; check constraints on `direction`, `translation_type`, `content_coverage_count = 6236`, and required non-empty display fields.
- `quran_translation_ayah_entries`: FK to `quran_translation_sources` and `quran_ayahs`; unique `(source_id, ayah_id)`; non-empty `text`.
- Indexes on `source_key`, `language_code`, `translation_type`, `(source_id, ayah_id)`, and `ayah_id`.

### EF configuration

- `TranslationSourceConfiguration.cs`
- `TranslationAyahEntryConfiguration.cs`
- DbSets registered in `QuranDashboardDbContext.cs`

## Import flow

```text
QuranDashboard.DataImporter import-translations [--source] [--report-out] [--force]
  └─ ImportTranslationsHandler (Application)
       ├─ ITranslationImportSource.LoadAsync
       │    ├─ TranslationManifestReader
       │    ├─ TranslationDisplayMetadataReader
       │    ├─ JsonTranslationSourceReader (per source file)
       │    └─ TranslationAssembler (verse_key → ayah_id, type flags)
       ├─ ITranslationImportWriter.AnyTargetTableHasDataAsync (re-run guard)
       └─ ITranslationImportWriter.ExecuteAcceptedImportAsync
            ├─ TranslationValidationRunner (pre/post copy checks)
            ├─ TranslationBulkCopier (FK-safe bulk insert)
            ├─ ITranslationImportReportBuilder.BuildSuccess
            ├─ TranslationImportReportEmitter → MarkdownJsonTranslationReportWriter
            └─ transaction commit (rollback on any hard-check or report-write failure)
```

### CLI defaults

- Source: `../resources/import-sources/quran-translations` (relative to `Backend/`)
- Reports: `../resources/report/quran-translations/`
- `--force`: truncate/rebuild only `quran_translation_ayah_entries` and `quran_translation_sources` after package revalidation

## Changed paths (backend repo, `main...HEAD`)

### Domain

- `domain/QuranDashboard.Domain/Quran/Translations/TranslationSource.cs`
- `domain/QuranDashboard.Domain/Quran/Translations/TranslationAyahEntry.cs`

### Application abstractions

- `application/QuranDashboard.Application.Abstractions/Quran/Translations/` — contracts, DTOs, invariants, exceptions, constants

### Application

- `application/QuranDashboard.Application/Quran/Translations/ImportTranslations/` — command, handler, result, report emitter
- `application/QuranDashboard.Application/DependencyInjection.cs` — handler registration

### Infrastructure — files/readers

- `infrastructure/QuranDashboard.Infrastructure/Files/Quran/Translations/` — manifest, display metadata, JSON source reader, assembler, import source loader, validation checks

### Infrastructure — persistence

- `infrastructure/QuranDashboard.Infrastructure/Persistence/Configurations/Quran/Translations/`
- `infrastructure/QuranDashboard.Infrastructure/Persistence/Repositories/Quran/Translations/` — bulk copier, SQL, command executor, validation runner, report builder, EF writer
- `infrastructure/QuranDashboard.Infrastructure/Persistence/QuranDashboardDbContext.cs`
- `infrastructure/QuranDashboard.Infrastructure/Migrations/20260615112132_AddQuranTranslations*.cs`

### Infrastructure — reports

- `infrastructure/QuranDashboard.Infrastructure/Reports/Quran/Translations/MarkdownJsonTranslationReportWriter.cs`

### Infrastructure — DI

- `infrastructure/QuranDashboard.Infrastructure/DependencyInjection.cs`

### Tools

- `tools/QuranDashboard.DataImporter/Program.cs` — `import-translations` verb, argument parsing, console summary

### Tests

- `tests/QuranDashboard.Tests/Quran/Translations/` — 12 test classes (+1 shared fixture), 62 tests

### Reports (this folder)

- `report/feature-008-quran-translations-foundation/001-implementation-scope.md`
- `report/feature-008-quran-translations-foundation/README.md`

## Production counts (locked)

| Metric | Expected |
| --- | ---: |
| Approved sources | 167 |
| Simple sources | 129 |
| With-footnotes sources | 38 |
| Excluded sources (report-only) | 19 |
| Languages | 83 |
| Ayahs per source | 6,236 |
| Source-to-ayah mappings | 1,041,412 |

## Status

**COMPLETE** — schema mapping, import pipeline, CLI verb, and synthetic test coverage are implemented through US1–US4.
