# Contract — Console Verb (`rebuild-words`)

The existing `tools/QuranDashboard.DataImporter` console host gains **verb-based**
dispatch. The rebuild is operator/CI-run only — **never** exposed over HTTP (FR-025,
FR-035).

## Usage

```text
QuranDashboard.DataImporter import-foundation --source <path> [--report-out <path>] [--force]
QuranDashboard.DataImporter rebuild-words [--report-out <path>] [--force]
```

- `import-foundation` — the existing Feature 002 import (unchanged behavior; now an
  explicit verb). Requires `--source`.
- `rebuild-words` — this feature. Reads the database; needs **no** `--source`.

### `rebuild-words` arguments

| Argument | Required | Meaning |
|---|---|---|
| `--report-out <path>` | no | Directory for the Markdown+JSON report. Defaults to a conventional report path (see below). |
| `--force` | no | Truncate **only** the four derived tables and rebuild. Without it, a non-empty target set causes refusal. |

No `--source` argument is accepted for `rebuild-words` (the source is the DB). Unknown
arguments are rejected with usage text, consistent with the existing parser.

### Default report output

When `--report-out` is omitted, the report is written to
**`resources/report/words-display/`** (resolved relative to the repository root, mirroring
the importer's default report location). This is the authoritative default; the handler
creates the directory if it does not exist.

## Behavior (maps to FRs)

1. Parse the verb; unknown/missing verb → usage text + non-zero exit.
2. Build the host (`AddApplication` + `AddInfrastructure`), resolve
   `RebuildDisplayWordsHandler`, run it.
3. Handler: if `!--force` and `AnyTargetTableHasDataAsync` → **refuse**, write nothing,
   print the refusal message, exit non-zero (FR-027).
4. Otherwise invoke `IDisplayWordsRebuilder.RebuildAsync(force, expectedReadableWords)`
   (the CLI always passes the production default 77,432) — one transaction: truncate-if-force
   → `INSERT…SELECT` ×4 → validation → commit/rollback (FR-026, FR-031).
5. For a rebuild that started (step 4), write the report via `IDisplayWordsReportWriter`
   on both pass and fail (FR-033). A refusal (step 3) returns earlier and writes no report.
6. Map result → exit code and console summary.

## Exit codes

| Code | Meaning |
|---|---|
| `0` | Rebuild committed; all hard checks passed |
| non-zero | Refused (targets non-empty, no `--force`), validation failed (rolled back), or an I/O/DB error occurred |

The handler reuses the importer's success/failure/refused result shape
(`RebuildDisplayWordsResult` with an `ExitCode`), mirroring `ImportQuranFoundationResult`.

## Console output

- **Success:** a one-line success message plus per-table totals
  (`ordered_tashkeel=77432, ordered_simple=77432, unique_tashkeel=<n>, unique_simple=<m>`)
  and the report path.
- **Failure (rebuild started, validation failed):** the first hard-failure message on
  stderr, plus the report path.
- **Refusal (targets non-empty, no `--force`):** the refusal message on stderr; **no
  report is written**, so no report path is printed.

## Out of scope (verb)

No interactive prompts, no network calls, no API surface, no scheduling. The verb does one
thing: rebuild the four derived tables from the DB.
