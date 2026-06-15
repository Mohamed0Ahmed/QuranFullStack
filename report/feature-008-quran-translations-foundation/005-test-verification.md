# Feature 008 — Test Verification

**Feature**: Quran Translations Foundation  
**Branch**: `008-quran-translations-foundation`  
**Produced by**: T079 (Phase 7 polish)  
**Date**: 2026-06-15

## Command

```bash
cd Backend
dotnet test tests/QuranDashboard.Tests --filter "FullyQualifiedName~Quran.Translations"
```

## Result

| Metric | Value |
| --- | ---: |
| Total | 62 |
| Passed | 62 |
| Failed | 0 |
| Skipped | 0 |
| Duration | ~49 s |

**Exit code**: 0

## Test files

| File | Area |
| --- | --- |
| `TranslationSchemaShapeTests.cs` | EF schema shape, constraints, indexes |
| `TranslationManifestReaderTests.cs` | Manifest success and count validation |
| `TranslationDisplayMetadataReaderTests.cs` | Display metadata success and failure paths |
| `TranslationSourceReaderTests.cs` | JSON source shape and `t` field rules |
| `TranslationAssemblerTests.cs` | Verse-key resolution, type flags, exact text |
| `TranslationImportTests.cs` | End-to-end successful synthetic import |
| `TranslationValidationFailureTests.cs` | Package, manifest, ayah, duplicate failures |
| `TranslationExcludedSourceTests.cs` | Excluded source refusal |
| `TranslationReportShapeTests.cs` | JSON/Markdown report contract |
| `TranslationSourceSafetyTests.cs` | Report content safety |
| `TranslationRollbackTests.cs` | Rollback and report-write failure |
| `TranslationRefusalForceTests.cs` | Re-run guard and forced replacement |

## Status

**PASS** — Feature 008 translation test subset is green.
