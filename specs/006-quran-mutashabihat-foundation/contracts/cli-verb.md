# Contract — Console Verb (`import-mutashabihat`)

The existing `tools/QuranDashboard.DataImporter` console host gains a **new verb**. The mutashabihat
import is operator/CI-run only — **never** exposed over HTTP (FR-020).

## Usage

```text
QuranDashboard.DataImporter import-foundation   --source <path> [--report-out <path>] [--force]
QuranDashboard.DataImporter rebuild-words       [--report-out <path>] [--force]
QuranDashboard.DataImporter import-morphology   [--source <path>] [--report-out <path>] [--force]
QuranDashboard.DataImporter generate-i3rab      [--report-out <path>] [--force]
QuranDashboard.DataImporter import-mutashabihat [--source <path>] [--report-out <path>] [--force]
```

- `import-foundation` / `rebuild-words` / `import-morphology` / `generate-i3rab` — existing verbs (unchanged).
- `import-mutashabihat` — this feature. **Source-driven**: reads two local JSON files and joins
  `quran_ayahs`.

### `import-mutashabihat` arguments

| Argument | Required | Meaning |
|---|---|---|
| `--source <path>` | no | Local staged source package. **Default = `App/resources/import-sources/mutashabihat/`** (repo-relative, resolved by the same repository-root walk as `import-morphology`). Overridable for tests/CI. |
| `--report-out <path>` | no | Directory for the Markdown+JSON report. Defaults to `resources/report/mutashabihat/`. |
| `--force` | no | Truncate **only** the three mutashabihat tables and rebuild. Without it, any non-empty target table causes refusal. |

The importer reads **only** the resolved local staged package; it **never** reads the original
`resources/mutashabihat/` working folder at runtime (FR-018). The staged package is Git-ignored /
local-only — its data files are not committed/pushed. Unknown arguments are rejected with usage text,
consistent with the existing parser (`TryParseMorphologyArguments`).

### Default source & report output

- **Source**: when `--source` is omitted, the resolved default is
  `App/resources/import-sources/mutashabihat/` (relative to the repository root), beside
  `quran-foundation/` and `quran-morphology/`. The folder must contain exactly: `manifest.json`,
  `README.md`, `mutashabihat-ul-quran/phrases.json`, `similar-ayahs/matching-ayah.json`.
- **Report**: when `--report-out` is omitted, the report goes to **`resources/report/mutashabihat/`**
  (repo-relative; created if absent).

## Behavior (maps to FRs)

1. Parse the verb; unknown/missing verb → usage text + non-zero exit.
2. Build the host (`AddApplication` + `AddInfrastructure`), resolve `ImportMutashabihatHandler`, run it.
3. Handler verifies the manifest/source files (exact file set, byte size, `sha256`) → **refuse early** on
   any mismatch (FR-019). Requires `quran_ayahs` to be populated (depends on `import-foundation`); a
   missing/empty `quran_ayahs` → refuse, write nothing (FR-023).
4. If `!--force` and any mutashabihat target table has data → **refuse**, write nothing, print the refusal
   message, exit non-zero (FR-022).
5. Otherwise invoke `IMutashabihatImportWriter.ImportAsync(source, force, expected, sourceUnchangedCheck)`
   — one transaction: truncate-if-force → `COPY` groups → occurrences → links → validation →
   `MUT-SOURCE-UNCHANGED` re-verify → commit/rollback (FR-021, FR-024). The CLI passes the production
   `MutashabihatInvariants` expected counts (814 / 3,558 / 3,557 / 1,162 / 3,552); tests pass their
   fixture's counts.
6. For an import that started (step 5), write the report via `IMutashabihatReportWriter` on both pass and
   fail (FR-032). A refusal (steps 3–4) returns earlier and writes no report.
7. Map result → exit code and console summary.

## Exit codes

| Code | Meaning |
|---|---|
| `0` | Import committed; all hard checks passed |
| non-zero | Refused (targets non-empty without `--force`, source/manifest mismatch, or missing/empty `quran_ayahs`), validation failed (rolled back), or an I/O/DB error occurred |

The handler reuses the importer's success/failure/refused result shape (`ImportMutashabihatResult` with an
`ExitCode`), mirroring `ImportMorphologyResult` (`FailureExitCode` for the early-exit/usage paths).

## Console output

- **Success:** a one-line success message plus per-table totals
  (`groups=814, occurrences=3557, links=3552, sources=1162`), and the report path.
- **Failure (import started, validation failed):** the first hard-failure message on stderr, plus the
  report path.
- **Refusal (targets non-empty without `--force`, source/manifest mismatch, or missing/empty
  `quran_ayahs`):** the refusal message on stderr; **no report is written**, so no report path is printed.

## Out of scope (verb)

No interactive prompts, no network calls, no API surface, no scheduling, no migration execution, no
read-model build. The verb does one thing: build the three mutashabihat tables from the two local source
files in one validated transaction.
