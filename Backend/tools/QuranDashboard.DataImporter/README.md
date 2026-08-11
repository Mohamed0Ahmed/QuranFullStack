# DataImporter CLI

**What:** the operational entry point for every Quran import / generate / rebuild.
`Program.cs` dispatches the first arg (a verb) to a `Import/VerbRunners/*` runner, which
runs the matching `application/.../Quran/DataPipelines/**` handler.

**HOW rules:** `Backend/.architecture/LOGGING_GUIDELINES.md` (run summaries). Source-data
rules: `CODING_PRINCIPLES.md` §10 Quranic Data Safety, this README's *Defaults & sources*
and *Safety* sections, and `Backend/report/README.md` when a durable report applies.

## Verbs

| Verb | Runs | Flags |
|---|---|---|
| `import-foundation` | Quran foundation (surahs/ayahs/words/layout) | `--source <path>` (required) `--report-out` `--force` |
| `rebuild-words` | rebuild display word tables | `--report-out` `--force` |
| `import-morphology` | word morphology (legacy or enriched) | `--source` `--report-out` `--force` `--enriched` |
| `validate-enriched-morphology` | dry-validate enriched source (no write) | `--source` `--report-out` |
| `import-mutashabihat` | متشابهات / similar-ayah groups | `--source` `--report-out` `--force` |
| `import-tafsirs` | tafsir sources | `--source` `--report-out` `--force` |
| `import-translations` | translation sources | `--source` `--report-out` `--force` |
| `import-navigation-metadata` | juz/hizb/rub/sajda | `--source` `--report-out` `--force` |
| `import-full-i3rab` | full إعراب | `--source` `--report-out` `--force` |
| `generate-i3rab` | generate simplified إعراب (no source) | `--report-out` `--force` |

Running with no verb or an unknown verb prints usage and exits non-zero.

## Defaults & sources

- When `--source` is omitted, defaults resolve under `resources/import-sources/`
  (`DataImporterDefaults`): `quran-morphology`, `quran-enriched-morphology`,
  `mutashabihat`, `quran-tafsirs`, `quran-translations`, `quran-navigation-metadata`,
  `quran-full-i3rab`. `import-foundation` requires an explicit `--source`.
- `resources/` is **local and gitignored** — packages are not in CI/other clones. Repo root
  is auto-detected (the folder containing both `resources/` and `Backend/`).
- Import work uses staged, canonicalized source packages under
  `resources/import-sources/<feature-or-source-name>/`. Do not import directly from random
  upstream folders when staging is required.
- Upstream source folders are provenance/read-only inputs unless the task explicitly asks to
  stage or canonicalize a package.
- Report defaults land under `resources/report/...` or `Backend/report/feature-XXX-.../`
  depending on the verb; override with `--report-out`.

## Safety

- Re-imports refuse to overwrite committed data unless `--force`; prefer a dry-validate
  (`validate-enriched-morphology`) first.
- Do not modify staged source packages from here; corrections belong in the pipeline's
  `Corrections/` (see
  `../../infrastructure/QuranDashboard.Infrastructure/Files/Quran/DataPipelines/Words/MorphologyImporting/README.md`).
- Preserve traceability from every imported or generated result back to its staged package and
  upstream provenance.

## Related

- Dev shortcut scripts: `Backend/scripts/README.md`.
- Write mechanics: `.../Persistence/DataPipelines/Quran/README.md`.
