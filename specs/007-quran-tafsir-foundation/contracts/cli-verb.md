# Contract — Console Verb (`import-tafsirs`)

The existing `tools/QuranDashboard.DataImporter` console host gains a new local operator-only verb.
No API, UI, scheduler, or app-user permission model is part of this feature.

## Usage

```text
QuranDashboard.DataImporter import-tafsirs [--source <path>] [--report-out <path>] [--force]
```

Existing verbs remain unchanged.

## Arguments

| Argument | Required | Meaning |
|---|---|---|
| `--source <path>` | no | Local final tafsir package. Default: `resources/import-sources/quran-tafsirs/` resolved from repository root. |
| `--report-out <path>` | no | Directory for Markdown and JSON reports. Default: `resources/report/quran-tafsirs/`. |
| `--force` | no | Rebuild tafsir-owned tables only. Without it, any existing tafsir data causes refusal. |

Unknown arguments are rejected with usage text and a non-zero exit.

## Behavior

1. Resolve the source package path and report output path.
2. Verify package shape and final manifest. Final manifest means `manifestType = "quran-tafsir-import-source-package"` and `isFinalImportManifest = true`; folder location alone is not sufficient.
3. Verify approved source count, excluded source count, language counts, file set, file sizes, and sha256.
4. Require canonical ayahs to exist and resolve every referenced ayah key.
5. Refuse if tafsir target tables contain data and `--force` is absent.
6. If `--force` is present, clear and rebuild only tafsir-owned tables.
7. Store approved sources, tafsir text blocks, and source-to-ayah links.
8. Preserve tafsir text exactly as imported.
9. Run hard checks and re-verify source package unchanged state.
10. Write both required reports.
11. Accept the run only when hard checks pass and both reports are written.

## Exit behavior

| Outcome | Exit | Data kept | Report |
|---|---|---|---|
| Success | `0` | Yes | Markdown + JSON |
| Refused before write | non-zero | No new data | Console refusal; report optional only if writer is available before data work |
| Validation failure | non-zero | No partial changes | Markdown + JSON when possible |
| Report write failure after validation | non-zero | No tafsir changes kept | Console/report-write error |
| Unhandled I/O or DB error | non-zero | No accepted partial changes | Best-effort console error |

## Console summary

Success output should include:

- `sources=84`
- `ayahMappings=523824`
- `languages=33`
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
- No Quran foundation data mutation.
