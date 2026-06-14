# Quickstart Validation

**Feature**: 007 Quran Tafsir Foundation
**Task**: T064
**Date**: 2026-06-14
**Reference**: `specs/007-quran-tafsir-foundation/quickstart.md`

## 1. Source package presence

**Path**: `/projects/Dashboard/App/resources/import-sources/quran-tafsirs/`

| Check | Result |
| --- | --- |
| `README.md` | Present |
| `manifest.json` | Present |
| `package-report.md` | Present |
| `sources/` JSON files | **84** files |
| `manifestType` | `quran-tafsir-import-source-package` |
| `isFinalImportManifest` | `true` |
| Approved sources (`summary.copiedApprovedTafsirSources`) | 84 |
| Excluded sources (`summary.excludedSources`) | 9 |
| Arabic approved (`summary.arabicApprovedCopied`) | 35 |
| Non-Arabic approved (`summary.nonArabicApprovedCopied`) | 49 |
| Languages (`summary.languageCount`) | 33 |
| Ayah coverage (`selectionRules.contentCoverageCount`) | 6236 |

## 2. CLI verb and arguments

**Usage line** (from invalid `--help` invocation):

```text
QuranDashboard.DataImporter import-tafsirs [--source <path>] [--report-out <path>] [--force]
```

Matches quickstart §3.

**Commands attempted**:

```bash
# from Backend/ — fails: appsettings.json not in working directory
cd /projects/Dashboard/App/Backend
dotnet run --project tools/QuranDashboard.DataImporter -- import-tafsirs

# from DataImporter/ — CLI starts, DB connection attempted
cd /projects/Dashboard/App/Backend/tools/QuranDashboard.DataImporter
dotnet run -- import-tafsirs \
  --source ../../../resources/import-sources/quran-tafsirs \
  --report-out ../../../resources/report/quran-tafsirs
```

**CLI result**: Verb registered, arguments parsed, handler invoked. Import did not complete because PostgreSQL authentication failed (`28P01: password authentication failed for user "postgres"`). Default connection string in `tools/QuranDashboard.DataImporter/appsettings.json` targets `localhost:5432` with credentials not valid in this environment.

## 3. Persistence status and row-count SQL

**Status**: **Not executed** — blocked by missing/invalid local PostgreSQL credentials and no applied migration on an operator database in this environment.

Quickstart §4 SQL spot checks (`84` sources, `523824` ayah mappings, `6236` distinct ayahs, `0` excluded keys) require a successful import against a database with `quran_ayahs` foundation data.

## 4. Report paths

Default report directory per quickstart: `../resources/report/quran-tafsirs/` with canonical filenames `tafsir-import-report.md` and `tafsir-import-report.json`.

Reports were not generated in this validation run because the import did not reach the write phase.

## 5. Behavioral evidence from automated tests

End-to-end quickstart behaviors are covered by the tafsir integration test suite (Testcontainers PostgreSQL):

| Quickstart behavior | Test evidence |
| --- | --- |
| Import approved sources, text blocks, ayah links | `TafsirImportTests` |
| Exact text preservation | `TafsirImportTests`, `TafsirAssemblerTests` |
| Manifest/package validation | `TafsirManifestReaderTests`, `TafsirValidationFailureTests` |
| Excluded source refusal | `TafsirExcludedSourceTests` |
| Ayah resolution / pointer integrity | `TafsirAyahResolutionTests` |
| Normal-run refusal when tables populated | `TafsirRefusalForceTests` |
| `--force` rebuild (tafsir tables only) | `TafsirForceRebuildTests` |
| Rollback on validation failure | `TafsirRollbackTests` |
| Audit reports (JSON + Markdown) | `TafsirJsonReportShapeTests`, `TafsirMarkdownReportShapeTests` |
| Report-write failure rollback | `TafsirReportWriteFailureTests` |
| Source package and Quran foundation unchanged | `TafsirSourceSafetyTests` |

## Verdict

| Area | Status |
| --- | --- |
| Source package shape | PASS |
| CLI verb and flags | PASS |
| Live operator import + SQL spot checks | **BLOCKED** (DB credentials) |
| Automated end-to-end validation | PASS (318/318 tests) |

**Operator follow-up**: Run quickstart §3–§4 on a machine with configured `ConnectionStrings:QuranDashboardDb`, applied EF migrations including tafsir tables, and seeded `quran_ayahs` foundation data.
