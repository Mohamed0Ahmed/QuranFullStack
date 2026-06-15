# User Story 3 — Safe re-run verification

**Feature**: 007-quran-tafsir-foundation  
**Phase**: 5 (US3)  
**Date**: 2026-06-14

## Scope

Validated refusal on accidental second import, explicit `--force` rebuild of tafsir-owned tables only, Quran foundation preservation, and transaction rollback when post-copy validation fails after a forced rebuild starts.

## Verification command

```bash
cd /projects/Dashboard/App/Backend
dotnet test tests/QuranDashboard.Tests --filter "FullyQualifiedName~Quran.Tafsirs"
```

## Result

| Metric | Value |
|--------|------:|
| Total tests | 48 |
| Passed | 48 |
| Failed | 0 |
| Skipped | 0 |

## US3 test files added

| File | Focus |
|------|-------|
| `TafsirRefusalForceTests.cs` | Second run without `--force` refused with `TargetsNotEmpty`; `AnyTargetTableHasDataAsync` |
| `TafsirForceRebuildTests.cs` | `--force` replaces tafsir content; Quran foundation snapshot unchanged |
| `TafsirRollbackTests.cs` | Failed forced rebuild rolls back; `persisted=false`, `forced=true` in report |

## Implementation highlights

- `EfBulkTafsirImportWriter.AnyTargetTableHasDataAsync` — refuses normal runs when any tafsir table has rows.
- `EfBulkTafsirImportWriter.ExecuteAcceptedImportAsync` — `TRUNCATE` tafsir tables on `--force` inside a single transaction; rolls back on validation failure.
- `TafsirSql.TruncateTafsirTables` — clears only `quran_tafsir_ayah_entries`, `quran_tafsir_entries`, `quran_tafsir_sources`.
- `ImportTafsirsHandler` — pre-write refusal via `TargetsNotEmpty`; passes `command.Force` to the writer.
- `Program.cs` — `import-tafsirs` parses `--force` and prints actionable refusal text on stderr.

## Contract / scope confirmations

- No Phase 6 report-detail/warning enhancements added.
- No API, frontend, search, or startup seeding behavior added.
- Quran foundation tables are not truncated or rewritten during force rebuild.
- Report-write failure rollback remains US4 scope; US3 rollback tests exclude that path.
