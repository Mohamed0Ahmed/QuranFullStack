# Feature 009 — Navigation Test Verification

**Feature**: Quran Navigation Metadata Foundation
**Branch**: `009-quran-navigation-metadata-foundation`
**Produced by**: T067 (Phase 7 polish)
**Date**: 2026-06-16

## Command

```bash
cd Backend
dotnet test tests/QuranDashboard.Tests --filter "FullyQualifiedName~Quran.Navigation"
```

## Result

| Metric | Value |
| --- | ---: |
| Total | 54 |
| Passed | 54 |
| Failed | 0 |
| Skipped | 0 |
| Duration | ~12 s |

**Exit code**: 0

## Test files

| File | Area |
| --- | --- |
| `NavigationImportTestFixture.cs` | Shared synthetic package + PostgreSQL seeding |
| `NavigationSyntheticPackageWriter.cs` | Synthetic package builder helper |
| `NavigationDatasetReaderTests.cs` | JSON dataset parsing |
| `NavigationAssemblerTests.cs` | `verse_mapping` expansion and hierarchy |
| `NavigationImportTests.cs` | Happy-path integration import |
| `NavigationManifestReaderTests.cs` | Manifest validation failures |
| `NavigationValidationFailureTests.cs` | `NAV-*` hard-check rejection paths |
| `NavigationRollbackTests.cs` | Transaction rollback on failure |
| `NavigationRefusalForceTests.cs` | Re-run guard and `--force` reload |
| `NavigationSourcePathTests.cs` | Default and explicit `--source` resolution |
| `NavigationIsolationTests.cs` | Non-navigation tables/columns unchanged |
| `NavigationMetadataWriteIsolationTests.cs` | Write-scope guard |
| `NavigationReportShapeTests.cs` | JSON/Markdown report contract |
| `NavigationSourceSafetyTests.cs` | No Quran text read or stored |

## Status

**PASS** — Feature 009 navigation test subset is green.
