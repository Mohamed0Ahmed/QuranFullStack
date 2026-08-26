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
| `build-phrase-index` | build and atomically activate the derived PhraseSearch index (no source) | `--report-out` `--force` |
| `rollback-phrase-index` | atomically reactivate the compatible previous PhraseSearch generation | none |

Running with no verb or an unknown verb prints usage and exits non-zero.

## Defaults & sources

- When `--source` is omitted, defaults resolve under `resources/import-sources/`
  (`DataImporterDefaults`): `quran-morphology`, `quran-enriched-morphology`,
  `mutashabihat`, `quran-tafsirs`, `quran-translations`, `quran-navigation-metadata`,
  `quran-full-i3rab`. `import-foundation` requires an explicit `--source` pointing to
  `quran-foundation` and also requires the sibling
  `masaq-corpus-aligned/masaq-search-words.dashboard-ready.json` source.
- `resources/` is **local and gitignored** — packages are not in CI/other clones. Repo root
  is auto-detected (the folder containing both `resources/` and `Backend/`).
- Import work uses staged, canonicalized source packages under
  `resources/import-sources/<feature-or-source-name>/`. Do not import directly from random
  upstream folders when staging is required.
- Upstream source folders are provenance/read-only inputs unless the task explicitly asks to
  stage or canonicalize a package.
- Report defaults land under `resources/report/...` or `Backend/report/feature-XXX-.../`
  depending on the verb; override with `--report-out`.
- `build-phrase-index` writes one Markdown and one JSON report under
  `resources/report/quran-phrase-search/<build-id>/` by default. It requires the Quran
  foundation and both rebuilt exact word-identity links, initializes a null PhraseSearch
  source fingerprint under the shared source lock, and refuses an unapproved fingerprint.

## Safety

- Re-imports refuse to overwrite committed data unless `--force`; prefer a dry-validate
  (`validate-enriched-morphology`) first.
- Do not modify staged source packages from here; corrections belong in the owning pipeline's
  versioned `Corrections/` data.
- Preserve traceability from every imported or generated result back to its staged package and
  upstream provenance.
- The derived phrase builder stages a complete generation before a short source-fenced activation.
  Without `--force`, it refuses to replace an active generation. With `--force`, the active
  generation remains readable until the replacement passes every hard check and activates.
  Eligible superseded-generation cleanup runs only after activation commits. A cleanup failure
  leaves the new active and compatible previous generation intact and is surfaced in the Active
  report and command message as `post-activation-cleanup-failed`.
- `PhraseSearch:CleanupGraceMinutes` defaults to 15, `PhraseSearch:FailedBuildRetentionDays`
  defaults to 30, and `PhraseSearch:RequestTimeoutSeconds` defaults to 10. Cleanup configuration
  is rejected unless the grace is longer than the request timeout.
- The disk preflight budgets one current database size for the additional generation and indexes,
  a second current database size for WAL, and `PhraseSearch:DiskSafetyBytes` as a margin (4 GiB by
  default). A loopback database is measured on the PostgreSQL data filesystem. A remote database
  fails closed unless the operator supplies both `PhraseSearch:VerifiedDatabaseFreeBytes` and the
  exact `PhraseSearch:DatabaseStorageProofContract` value
  `operator-verified-database-filesystem-v1`; reports contain the proof kind and byte counts but no
  storage paths.
- `rollback-phrase-index` takes the same source fence, recomputes the current semantic source
  fingerprint, requires a format-compatible generation with both readiness flags, and swaps the
  active and previous pointers with their statuses in one transaction. It refuses when no compatible
  previous generation exists.

## Related

- Dev shortcut scripts: `Backend/scripts/README.md`.
- Implementation mechanics: the corresponding source-reader and persistence pipeline code.
