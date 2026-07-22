# Quran DB-write import pipelines

**Layer:** Infrastructure · persistence write · **HOW rules:** `Backend/.architecture/LOGGING_GUIDELINES.md`, `CLEAN_ARCHITECTURE.md`

## What this area does

The write half of every Quran import/generate/rebuild. Each domain
(`Foundation`, `FullI3rab`, `Mutashabihat`, `Navigation`, `Tafsirs`, `Translations`,
`Words/{DisplayRebuilding, MorphologyImporting, SimpleI3rabGeneration}`) takes the
assembled objects from `Files/Quran/DataPipelines/<Domain>/` and bulk-writes them into
PostgreSQL, then validates and emits a run report.

## Shared pattern (every domain repeats this shape)

| File suffix | Role |
|---|---|
| `EfBulk<Domain>*Writer` | entry point; owns the transaction/session |
| `<Domain>BulkCopier` | high-throughput COPY of rows |
| `<Domain>CommandExecutor` | runs the ordered SQL steps |
| `<Domain>Sql` | the SQL text (DDL-free; data + index/constraint ops) |
| `<Domain>ValidationRunner` | post-write integrity checks |
| `<Domain>ImportReportBuilder` | assembles the run summary emitted to `--report-out` |

`Foundation` uses a single `EfBulkQuranImportWriter`; `SimpleI3rabGeneration` also carries
an `EfI3rabGenerationSource` + write-probe (`II3rabGenerationWriteProbe`).

## Invariants / caveats (read before changing)

- **Bulk-copy, not per-row EF tracking**, for the large tables — keep it that way for
  import throughput.
- **Reset/reseed ordering matters** — foreign-key order across domains is fixed; follow
  `Backend/report/database-inventory/database-reset-and-seeding-order.md`.
- Run reports follow `LOGGING_GUIDELINES.md`; do not invent a new report shape per domain.
- **Do not hand-write migrations here** — schema comes from EF migrations under
  `../../Migrations/`; this area writes data, not DDL.
- **Route every destructive step through the safety guard** — each domain's `--force` TRUNCATE/DELETE
  goes through `QuranImportDestructiveGuard` (`Safety/README.md`): a transaction-scoped advisory lock
  plus a fail-closed FK-closure preflight so a CASCADE can never reach a table outside the `quran_*`
  domain (feature 028 US2). Add new force/reseed paths to that guard.

## Related

- Source-read half: `../../../Files/Quran/DataPipelines/<Domain>/`
  (morphology: `.../Words/MorphologyImporting/README.md`).
- Commands: `tools/QuranDashboard.DataImporter/README.md`.
- Reference: DB reset/seeding order in `Backend/report/database-inventory/` (per-feature
  import-run reports were purged; new ones are evidence-only per `Backend/report/README.md`).
