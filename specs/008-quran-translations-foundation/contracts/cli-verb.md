# Contract — Console Verb (`import-translations`)

The existing `tools/QuranDashboard.DataImporter` console host gains a new local operator-only verb.
No API, UI, scheduler, search, startup seeding, word-by-word import, or app-user permission model is part
of this feature.

## Usage

```text
QuranDashboard.DataImporter import-translations [--source <path>] [--report-out <path>] [--force]
```

Existing verbs remain unchanged.

## Arguments

| Argument | Required | Meaning |
|---|---|---|
| `--source <path>` | no | Local final translation package. Default: `resources/import-sources/quran-translations/` resolved from repository root. |
| `--report-out <path>` | no | Directory for Markdown and JSON reports. Default: `Backend/report/feature-008-quran-translations-foundation/` resolved from repository root. |
| `--force` | no | Rebuild translation-owned tables only. Without it, any existing translation data causes refusal. |

Unknown arguments are rejected with usage text and a non-zero exit before report output is required.

## Behavior

1. Resolve the source package path and report output path.
2. Verify package shape and final manifest. Final manifest means `manifestType = "quran-translation-import-source-package"` and `isFinalImportManifest = true`; folder location alone is not sufficient.
3. Verify final display metadata. Final display metadata means `metadataType = "quran-translation-source-display-metadata"`, `status = "final"`, `sourceCount = 167`, and all records are complete and aligned with the manifest source set.
4. Verify approved source count, per-type counts, excluded count, language count, file set, file sizes, and sha256.
5. Require canonical ayahs to exist and resolve every referenced verse key.
6. Refuse if translation target tables contain data and `--force` is absent.
7. If `--force` is present, clear and rebuild only translation-owned tables after package validation.
8. Store approved sources and source-to-ayah translation entries.
9. Preserve translation text exactly as imported.
10. Run hard checks and re-verify source package unchanged state.
11. Write both required reports.
12. Accept the run only when hard checks pass and both reports are written.

## Exit behavior

| Outcome | Exit | Data kept | Report |
|---|---|---|---|
| Success | `0` | Yes | Markdown + JSON |
| Refused before write | non-zero | No new data | Markdown + JSON after report output path is resolved |
| Validation failure | non-zero | No partial changes | Markdown + JSON after report output path is resolved |
| Report write failure after validation | non-zero | No translation changes kept | Console/report-write error |
| Unhandled early startup, path-resolution, I/O, or DB error | non-zero | No accepted partial changes | Best-effort console error; Markdown + JSON only if report output and writer context are available |

## Console summary

Success output should include:

- `sources=167`
- `ayahMappings=1041412`
- `languages=83`
- `types=simple:129,with_footnotes:38`
- `warnings` count
- report directory

Failure/refusal output should include the first actionable refusal or hard-check failure and whether any
data was persisted.

## Out of scope

- No API endpoint.
- No frontend or admin UI.
- No public reader.
- No search indexing.
- No startup seeding.
- No source package editing.
- No word-by-word import.
- No footnote parsing/sanitization.
- No Quran foundation data mutation.
