# Contract — Console Verb (`import-morphology`)

The existing `tools/QuranDashboard.DataImporter` console host gains a **third verb**. The morphology
import is operator/CI-run only — **never** exposed over HTTP (FR-001, FR-037).

## Usage

```text
QuranDashboard.DataImporter import-foundation --source <path> [--report-out <path>] [--force]
QuranDashboard.DataImporter rebuild-words [--report-out <path>] [--force]
QuranDashboard.DataImporter import-morphology [--source <path>] [--report-out <path>] [--force]
```

- `import-foundation` — Feature 002 import (unchanged).
- `rebuild-words` — Feature 003 DB-to-DB rebuild (unchanged).
- `import-morphology` — this feature. **Source-driven**: reads local JSON files and joins `quran_words`.

### `import-morphology` arguments

| Argument | Required | Meaning |
|---|---|---|
| `--source <path>` | no | Local source folder. **Default = `App/resources/import-sources/quran-morphology/`** (repo-relative). Overridable for tests/CI. |
| `--report-out <path>` | no | Directory for the Markdown+JSON report. Defaults to `resources/report/words-morphology/`. |
| `--force` | no | Truncate **only** the six morphology tables and rebuild. Without it, a non-empty target set causes refusal. |

The importer reads **only** the resolved local source tree; it **never** reads the external
`~/Desktop/.../resources/morphology` workspace, and runtime has **no dependency** on that path
(FR-002). The local source folder is Git-ignored / local-only — its data files are not committed/pushed
(FR-003). Unknown arguments are rejected with usage text, consistent with the existing parser.

### Default source & report output

- **Source**: when `--source` is omitted, the resolved default is
  `App/resources/import-sources/quran-morphology/` (relative to the repository root), beside
  `quran-foundation/`. The folder must contain exactly: `manifest.json`, `README.md`,
  `corpus/quranic-corpus-morphology-qpc-aligned.json`, `corpus/corpus-qpc-location-alignment-map.json`,
  `qul/word-root.json`, `qul/word-lemma.json`, `qul/word-stem-corrected-arabic.json`.
- **Report**: when `--report-out` is omitted, the report goes to **`resources/report/words-morphology/`**
  (repo-relative; created if absent).

## Behavior (maps to FRs)

1. Parse the verb; unknown/missing verb → usage text + non-zero exit.
2. Build the host (`AddApplication` + `AddInfrastructure`), resolve `ImportMorphologyHandler`, run it.
3. Handler verifies the manifest/source files (presence, counts, size/`sha256`) → refuse early on
   mismatch (FR-004, FR-036). Requires `quran_words` to be populated (depends on `import-foundation`;
   FR-006).
4. If `!--force` and any morphology target table has data → **refuse**, write nothing, print the refusal
   message, exit non-zero (FR-032).
5. Otherwise invoke `IMorphologyImportWriter.ImportAsync(source, force, expectedReadableWords)` — one
   transaction: truncate-if-force → seed POS → `COPY` dimensions → morphology → segments → validation →
   commit/rollback (FR-027, FR-031). The CLI always passes the production default
   `MorphologyInvariants.ExpectedReadableWords` (77,432); tests pass their fixture's readable count.
6. For an import that started (step 5), write the report via `IMorphologyReportWriter` on both pass and
   fail (FR-030). A refusal (steps 3–4) returns earlier and writes no report.
7. Map result → exit code and console summary.

## Exit codes

| Code | Meaning |
|---|---|
| `0` | Import committed; all hard checks passed |
| non-zero | Refused (targets non-empty without `--force`, or source/manifest mismatch), validation failed (rolled back), or an I/O/DB error occurred |

The handler reuses the importer's success/failure/refused result shape (`ImportMorphologyResult` with an
`ExitCode`), mirroring `ImportQuranFoundationResult`.

## Console output

- **Success:** a one-line success message plus per-table totals
  (`morphology=77432, segments=<n>, roots=<r>, lemmas=<l>, stems=<s>, pos_tags=<p>`), the tier
  distribution, and the report path.
- **Failure (import started, validation failed):** the first hard-failure message on stderr, plus the
  report path.
- **Refusal (targets non-empty without `--force`, or source/manifest mismatch):** the refusal message on
  stderr; **no report is written**, so no report path is printed.

## Out of scope (verb)

No interactive prompts, no network calls, no API surface, no scheduling, no migration execution. The verb
does one thing: build the six morphology tables from the local source files in one validated transaction.
