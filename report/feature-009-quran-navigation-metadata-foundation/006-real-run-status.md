# Feature 009 — Real Import Run Status

**Feature**: Quran Navigation Metadata Foundation
**Branch**: `009-quran-navigation-metadata-foundation`
**Produced by**: T068 (Phase 7 polish — gated)
**Date**: 2026-06-16

## Gate checklist

| Requirement | Status |
| --- | --- |
| Staged package at `resources/import-sources/quran-navigation-metadata/` | **PRESENT** (`manifest.json` + `sources/`) |
| Migration `AddQuranNavigationMetadata` generated (T024) | **DONE** (EF-generated) |
| Migration applied to target PostgreSQL (`dotnet ef database update` / `./scripts/update-db`) | **NOT RUN** — requires explicit operator authorization |
| Feature 002 foundation import (`quran_ayahs` populated) | **ASSUMED** for operator environment; not verified in this session |

## Decision

**SKIPPED in this implementation session** per `tasks.md` T068 and workspace rules:

- `dotnet ef database update` is a separate, explicitly-authorized step.
- The real-package import against production counts (30/60/240/15/6236) must run only after the
  migration is applied to the operator's target database.

## Operator next steps (when authorized)

```bash
# 1. Apply migration (only when explicitly authorized):
cd Backend && ./scripts/update-db

# 2. Run the import with production expected counts:
dotnet run --project Backend/tools/QuranDashboard.DataImporter -- import-navigation-metadata

# 3. Verify per quickstart.md SQL and copy generated reports into this folder.
```

Expected success console summary:

```text
juz=30, hizb=60, rub=240, sajda=15, ayahsTagged=6236, warnings=0
```

## Status

**PENDING** — gated real run not executed. Synthetic/integration tests (54) and full suite (434) are
green without the real-package run.

---

## Update — Authorized real run completed (2026-06-16)

Operator authorized the gated step. Both actions were executed against the target PostgreSQL
(`quran_dashboard`, `Host=localhost;Port=5432`):

1. **Migration applied** — `20260616095937_AddQuranNavigationMetadata` via
   `./scripts/update-db` (Api startup project). Recorded in `__EFMigrationsHistory`.
2. **Import executed** — `import-navigation-metadata` (default `ExpectedCounts = Production`).

Console summary (matches the expected line above):

```text
juz=30, hizb=60, rub=240, sajda=15, ayahsTagged=6236, warnings=0
```

Post-run database verification (quickstart.md SQL):

| Check | Result |
| --- | --- |
| `quran_juzs` / `quran_hizbs` / `quran_rubs` / `quran_sajdas` row counts | 30 / 60 / 240 / 15 |
| `quran_ayahs` untagged (juz/hizb/rub NULL) | 0 / 0 / 0 of 6236 |
| Sajda type split | 11 optional / 4 required |
| Orphan hizbs / rubs (broken hierarchy) | 0 / 0 |
| `quran_ayahs.text_uthmani` emptied/nulled | 0 (text untouched) |

Generated importer report (`navigation-metadata-import-report.json`): `verdict=accepted`,
`persisted=true`, `forced=false`, `ayahCoverage.complete=true`,
`noQuranAyahTextReadOrStored=true`, zero failed hard checks, zero warnings, zero errors.

**Status: DONE** — gated real run (T068) complete and verified.
