# User Story 2 — Import integrity verification

**Feature**: 007-quran-tafsir-foundation  
**Phase**: 4 (US2)  
**Date**: 2026-06-14

## Scope

Validated refusal paths for unsafe package drift, excluded-source leakage, unresolved ayahs, invalid pointers, duplicate mappings, Quran foundation safety, and post-copy rollback with stable `TAFSIR-` hard-check identifiers.

## Verification command

```bash
cd /projects/Dashboard/App/Backend
dotnet test tests/QuranDashboard.Tests --filter "FullyQualifiedName~Quran.Tafsirs"
```

## Result

| Metric | Value |
|--------|------:|
| Total tests | 42 |
| Passed | 42 |
| Failed | 0 |
| Skipped | 0 |

## Post-review fixes (engineering review)

- **TAFSIR-TEXT-UNCHANGED**: Post-copy comparison now keys persisted rows by `(source_key, source_entry_key)` via a join on `quran_tafsir_sources`, fixing cross-source verse-key collisions in multi-source imports.
- **Multi-source regression**: `Import_persists_multi_source_shared_verse_keys_with_TAFSIR_TEXT_UNCHANGED` imports two sources sharing verse key `900:1` with distinct text and asserts `TAFSIR-TEXT-UNCHANGED` passes.
- **Manifest count checks**: Merged duplicate per-ID rows into one assertion each (manifest self-consistency + locked counts).
- **Expected counts**: Threaded explicitly through `ITafsirImportSource.LoadAsync(sourcePath, expectedCounts, ct)`; removed mutable `TafsirImportSession`.
- **DI**: Single registration path (session removed).
- **Handler**: `TafsirImportTotals.Empty`, guarded `FailedChecks.FirstOrDefault()`.
- **Empty-text test**: Asserts only `TAFSIR-NO-EMPTY-TEXT`.

## US2 test files exercised

| File | Focus |
|------|-------|
| `TafsirValidationFailureTests.cs` | Package shape, manifest finality, counts, hash/size drift, JSON shape |
| `TafsirExcludedSourceTests.cs` | Locked excluded keys refused (`TAFSIR-NO-EXCLUDED-SOURCES`) |
| `TafsirAyahResolutionTests.cs` | Ayah resolution, pointers, duplicates |
| `TafsirSourceSafetyTests.cs` | Package immutability, Quran foundation unchanged, no partial persist |

## Implementation highlights

- `TafsirInvariants` — stable hard-check ID constants and locked excluded source keys.
- `TafsirValidationException` — carries `TafsirCheckResult` list for reportable failures.
- `TafsirManifestReader` — `TAFSIR-PACKAGE-SHAPE`, `TAFSIR-MANIFEST-FINAL`, count checks, `TAFSIR-SOURCE-SET`, `TAFSIR-SOURCE-HASH`.
- `TafsirImportSource` — `TAFSIR-NO-EXCLUDED-SOURCES`, `TAFSIR-COVERAGE-COUNT`.
- `TafsirAssembler` — `TAFSIR-JSON-SHAPE`, ayah/pointer/empty/duplicate/Quran-text checks.
- `TafsirValidationRunner` — post-copy `TAFSIR-POSTCOPY-*`, `TAFSIR-TEXT-UNCHANGED`, `TAFSIR-NO-QURAN-TEXT-COPY`, `TAFSIR-SOURCE-UNCHANGED`.
- `EfBulkTafsirImportWriter` — rolls back transaction when hard checks fail (`Persisted = false`).
- `ImportTafsirsHandler` — writes failure reports for `TafsirValidationException` with check IDs.

## Contract / scope confirmations

- No Phase 5+ force/rebuild refusal behavior added beyond existing US1 skeleton.
- No Phase 6 report-detail/warning tests added.
- No API, frontend, or source package files modified.
- Hard-check IDs match `contracts/validation-report.schema.md`.

## Build

`dotnet build QuranDashboard.sln` — succeeded before test run (no new warnings).
