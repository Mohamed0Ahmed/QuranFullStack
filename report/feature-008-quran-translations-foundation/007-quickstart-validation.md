# Feature 008 — Quickstart Validation

**Feature**: Quran Translations Foundation  
**Branch**: `008-quran-translations-foundation`  
**Produced by**: T081 (Phase 7 polish)  
**Date**: 2026-06-15

## Goal

Smoke-test the `import-translations` CLI verb without running a full import against the real database.

## Commands run

### 1. Unknown argument (argument validation)

```bash
cd Backend
dotnet run --project tools/QuranDashboard.DataImporter --no-build -- import-translations --bogus
```

| Item | Value |
| --- | --- |
| Exit code | 1 |
| Stderr | `Unknown argument '--bogus'.` |
| Usage printed | Yes — includes `import-translations [--source <path>] [--report-out <path>] [--force]` |

**Result**: PASS — invalid arguments are rejected before host/DI startup; no database access.

### 2. `--help` (not a supported flag)

```bash
dotnet run --project tools/QuranDashboard.DataImporter --no-build -- import-translations --help
```

| Item | Value |
| --- | --- |
| Exit code | 1 |
| Stderr | `Unknown argument '--help'.` |
| Usage printed | Yes |

**Result**: PASS — same argument-validation path; no database access. The DataImporter does not define a `--help` flag; usage is printed on unknown arguments.

## Not run (by design)

- Default `import-translations` without arguments — would attempt a real import against configured database and staged package.
- `import-translations --force` — would mutate translation tables.

Per quickstart and scope rules, full real-package import is operator-triggered and out of scope for this smoke test.

## Status

**PASS** — CLI verb is registered, argument parsing fails closed, and usage text matches quickstart.
