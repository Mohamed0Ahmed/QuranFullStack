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
| `import-tafsirs` | tafsir sources | `--profile curated-10\|full` `--source` `--report-out` `--force` |
| `import-translations` | translation sources | `--profile curated-10\|full` `--source` `--report-out` `--force` |
| `import-navigation-metadata` | juz/hizb/rub/sajda | `--source` `--report-out` `--force` |
| `import-full-i3rab` | full إعراب | `--source` `--report-out` `--force` |
| `generate-i3rab` | generate simplified إعراب (no source) | `--report-out` `--force` |
| `build-phrase-index` | one-shot build and activation of the derived PhraseSearch index (no source) | `--report-out` |
| `export-abwab-snapshot` | read-only Abwab relational export across the eight-table schema; excludes Linking rows and Linking-dependent inclusion-sync rows | `--output-dir` |
| `import-abwab-snapshot` | restore one verified v4 Abwab snapshot into an empty current-schema target without Linking rows | `--source` (required) `--report-out` `--allow-remote --yes` (paired remote confirmation) |
| `import-quran-topics-book` | validate or import a hierarchy-preserving Quran topics book package with full-ayah manual links | `--source` (required) `--actor-user-id` (required) `--validate-only` `--report-out` `--allow-remote --yes` (paired remote confirmation) |

Running with no verb or an unknown verb prints usage and exits non-zero.

## Defaults & sources

- When `--source` is omitted, defaults resolve under `resources/import-sources/`
  (`DataImporterDefaults`): `quran-morphology`, `quran-enriched-morphology`,
  `mutashabihat`, `quran-navigation-metadata`, and `quran-full-i3rab`. Tafsir and translation
  imports default to profile `curated-10`, which resolves `quran-tafsirs-neon-10` and
  `quran-translations-neon-10`. `--profile full` selects the untouched `quran-tafsirs` and
  `quran-translations` packages and their original full-count contracts. The selected profile
  controls expected counts even when `--source` is explicit, and every tafsir/translation JSON
  and Markdown report records the stable `curated-10` or `full` profile. `import-foundation`
  requires an explicit `--source` pointing to
  `quran-foundation` and also requires the sibling
  `masaq-corpus-aligned/masaq-search-words.dashboard-ready.json` source.
- `resources/` is **local and gitignored** — packages are not in CI/other clones. Repo root
  is auto-detected (the folder containing both `resources/` and `Backend/`).
- `export-abwab-snapshot` defaults to `resources/exports/abwab/`. It writes a timestamped
  format-v4 snapshot, a SHA-256 sidecar, and JSON plus Markdown audit reports. It never accepts
  a connection string as a CLI argument and refuses to overwrite any artifact. Earlier v3
  artifacts are legacy and require a fresh v4 export before a reset.
- `import-abwab-snapshot` requires a v4 snapshot and its adjacent `.sha256` sidecar. Reports
  default to `resources/report/abwab-snapshot-import/`; the command never rewrites either source
  artifact.
- `import-quran-topics-book` requires a format-v1 JSON source and an adjacent `.sha256` sidecar.
  Reports default to `resources/report/quran-topics-book-import/`. The source package belongs under
  `resources/import-sources/quran-topics-book/`; `--actor-user-id` identifies the active Owner used
  for required audit foreign keys and is never embedded in the source JSON. Use `--validate-only`
  first to validate the package, actor, migration head, and every `verseKey` without writing.
- A format-v1 Quran topics book source has this shape:

  ```json
  {
    "format": "quran-dashboard-quran-topics-book",
    "formatVersion": 1,
    "title": "...",
    "source": {
      "fileName": "BOOK_27464_1.pdf",
      "sha256": "<source-pdf-sha256>",
      "pdfPageFrom": 2,
      "pdfPageTo": 48
    },
    "policy": {
      "parentAyahPolicy": "direct_only",
      "groupingPolicy": "consecutive_ranges_grouped"
    },
    "sections": [
      {
        "key": "section-01",
        "name": "...",
        "order": 1,
        "doors": [
          {
            "key": "section-01.door-01",
            "parentKey": null,
            "name": "...",
            "order": 1,
            "globalOrder": 1,
            "pdfPages": [2],
            "ayahGroups": [
              { "order": 1, "kind": "single", "verseKeys": ["2:255"] },
              {
                "order": 2,
                "kind": "consecutive_range",
                "verseKeys": ["26:217", "26:218", "26:219", "26:220"]
              }
            ]
          }
        ]
      }
    ]
  }
  ```

  Door keys and parent keys are stable lowercase ASCII identifiers. Every child names its exact
  parent through `parentKey`; only root doors carry `globalOrder`. Ayah groups contain only direct
  references printed for that door: parent doors do not inherit descendant references. A `single`
  group contains exactly one verse. A `consecutive_range` contains two or more ordered, consecutive
  ayahs from one surah and imports as one grouped linking unit.
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
- `export-abwab-snapshot` uses one PostgreSQL repeatable-read, read-only transaction and a
  literal eight-table allowlist. It excludes every Linking row and `xmin`, validates the live
  table/schema allowlist, serialized counts, IDs, references, hierarchies, relation types, and
  inclusion graph before writing. Rows from `abwab_door_inclusion_unit_syncs` are derived from
  deliberately excluded `linking_units`, so they are never serialized; the snapshot and audit
  reports record their source excluded count. A validation failure writes only JSON and Markdown
  audit reports with `persisted=false`; it never writes a snapshot or checksum.
- `import-abwab-snapshot` verifies the exact source bytes before parsing and verifies the snapshot
  plus sidecar again before commit. It accepts only v4, the exact eight-table scope, an empty
  serialized inclusion-sync table, valid schema/count/ID/reference/hierarchy/relation contracts,
  and in-range PostgreSQL `integer` values. A v3 artifact is refused. The target must be at the
  compiled current migration head, exactly match the snapshot Abwab schema, and have all eight
  target tables empty; there is no force-overwrite flag.
- Import runs in one serializable transaction with `ACCESS EXCLUSIVE` fences over the static
  allowlist. It restores explicit IDs in validated parent-first door and template-node batches,
  leaves inclusion-sync empty, resets all eight identity sequences, and verifies post-import
  total/active/archive counts, IDs, references, schema, and migration stability before commit. It
  never reads or writes a Linking table.
- `import-quran-topics-book` is a separate one-shot path because the v4 Abwab snapshot contract
  intentionally excludes Linking. It accepts only the exact format-v1 policies above, validates
  unique keys, sibling orders and names, same-section parents, an acyclic hierarchy, PDF page bounds,
  non-overlapping direct verse references per door, and strict single/consecutive-range shapes. It
  resolves every `verseKey` against canonical local ayahs and refuses a missing reference.
- A real Quran topics book import requires all 25 tables in the Abwab-to-Linking reset closure to be
  empty and the target to match the compiled migration head. It takes serializable and
  `ACCESS EXCLUSIVE` fences, inserts sections and doors parent-first, records each printed reference
  as a confirmed `manual_mushaf_ayahs` contribution, writes full-ayah units without selected words,
  rebuilds the door-ayah projection, verifies exact counts, rechecks source bytes, and then commits.
  There is no force-overwrite flag. `--validate-only` never writes or locks the target exclusively;
  it reports non-empty target tables as a warning so source validation can still complete.
- Each printed singleton becomes one `manual_single` contribution. Each consecutive printed range
  becomes one `manual_grouped` contribution and one grouped unit. Separate printed references never
  merge merely because their verse keys happen to be adjacent after sorting.
- Loopback targets are accepted by default. A non-loopback target requires
  `--allow-remote --yes` together; the report records that authorization as a warning with only
  the masked target. Every accepted run writes timestamped JSON and Markdown success/failure
  reports with `persisted=true`, `persisted=false`, or fail-closed `persisted=unknown` after an
  ambiguous commit acknowledgement.
- Do not modify staged source packages from here; corrections belong in the owning pipeline's
  versioned `Corrections/` data.
- Preserve traceability from every imported or generated result back to its staged package and
  upstream provenance.
- The derived phrase builder is strictly one-shot. It acquires the builder fence and refuses before
  source bootstrap when any active, previous, non-failed, or child-data-bearing generation exists.
  Rebuilding requires a full database reset; `--force` is rejected. Metadata-only failed audits do
  not block a retry.
- During an eligible build, the staged rows are the only PhraseSearch data generation and the active
  pointer remains null until the short source-fenced activation. Search is unavailable during that
  first build. Activation points state to the sole ready generation and keeps the legacy
  `previous_build_id` null; the legacy `Superseded` status is not emitted operationally.
- Failure, cancellation, and abandoned status 1/2 recovery delete the attempt's child data and keep
  metadata-only failed audits. Those audits may be retained for
  `PhraseSearch:FailedBuildRetentionDays` (30 by default), and they do not count as a data
  generation.
- Foundation and display-word source mutations take the same builder fence. Their committed source
  change is followed by separately retryable PhraseSearch cleanup; a cleanup problem is surfaced as
  a persisted-success warning in the pipeline report instead of misreporting the source transaction
  as rolled back.
  `PhraseSearch:RequestTimeoutSeconds` defaults to 10.
- The disk preflight conservatively budgets one current database size as one-shot build working
  space, a second current database size for WAL, and `PhraseSearch:DiskSafetyBytes` as a margin
  (4 GiB by default). The byte formula is intentionally unchanged pending separate measurement.
  Every environment, including loopback, fails closed unless the operator supplies both
  `PhraseSearch:VerifiedDatabaseFreeBytes` and the exact
  `PhraseSearch:DatabaseStorageProofContract` value
  `operator-verified-database-filesystem-v1`; no automatic filesystem measurement runs. Reports
  contain the proof kind and byte counts but no storage paths.
- Recovery after activation requires a full database reset and verified backup restore before a new
  one-shot build; the importer exposes neither replacement builds nor previous-generation rollback.

## Related

- Dev shortcut scripts: `Backend/scripts/README.md`.
- Implementation mechanics: the corresponding source-reader and persistence pipeline code.
