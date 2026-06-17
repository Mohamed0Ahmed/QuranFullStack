# Feature 010 — Test Verification

**Feature:** Quran Full I'rab Foundation
**Produced by:** Phase 5 polish
**Date:** 2026-06-17

## Commands

```bash
cd Backend
dotnet test QuranDashboard.sln --filter "FullyQualifiedName~FullI3rab"
```

## Result

| Metric | Value |
| --- | --- |
| Exit code | 0 |
| Passed | 42 |
| Failed | 0 |
| Skipped | 0 |
| Duration | ~10 s |

## Test inventory (14 files: 11 test classes + 2 fixtures + 1 synthetic-package helper)

| File | Focus |
| --- | --- |
| `FullI3rabSchemaShapeTests` | Three tables, columns, indexes, check constraints |
| `FullI3rabManifestReaderTests` | Manifest validation |
| `JsonFullI3rabSourceReaderTests` | Per-file JSON reader |
| `FullI3rabAssemblerTests` | Flat, grouped leader, member pointer assembly |
| `FullI3rabValidationFailureTests` | Pre-import hard failures (no persistence) |
| `FullI3rabImportTests` | Happy-path synthetic import + post-import counts |
| `FullI3rabRefusalForceTests` | Rerun refusal without `--force` |
| `FullI3rabForceRebuildTests` | `--force` truncate-and-rebuild |
| `FullI3rabJsonReportShapeTests` | JSON report contract + provenance warning |
| `FullI3rabMarkdownReportShapeTests` | Markdown report contract + provenance warning |
| `FullI3rabSourceUnchangedTests` | Source files unchanged after import |
| `FullI3rabImportTestFixture` | Shared Testcontainers PostgreSQL fixture |
| `FullI3rabSchemaFixture` | Schema-only fixture helper |
| `FullI3rabSyntheticPackage` | Synthetic package builder |

## Status

**PASS** — all Feature 010 tests green.
