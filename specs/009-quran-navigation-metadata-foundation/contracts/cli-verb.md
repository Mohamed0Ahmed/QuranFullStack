# Contract — Console Verb (`import-navigation-metadata`)

The existing `tools/QuranDashboard.DataImporter` console host gains a new local operator-only verb.
No API, UI, scheduler, search, startup seeding, reader, or app-user permission model is part of this feature.

## Usage

```text
QuranDashboard.DataImporter import-navigation-metadata [--source <path>] [--report-out <path>] [--force]
```

Existing verbs (`import-foundation`, `rebuild-words`, `import-morphology`, `import-mutashabihat`,
`import-tafsirs`, `import-translations`, `generate-i3rab`) remain unchanged.

## Arguments

| Argument | Required | Meaning |
|---|---|---|
| `--source <path>` | no | The navigation-metadata package **root** directory (the folder containing `manifest.json` and `sources/`). Default: `resources/import-sources/quran-navigation-metadata/` resolved from repository root. Must NOT be a hard-coded absolute path; default is resolved via the existing `ResolveRepositoryRoot()` pattern. |
| `--report-out <path>` | no | Directory for Markdown and JSON reports. Default: `Backend/report/feature-009-quran-navigation-metadata-foundation/` resolved from repository root. |
| `--force` | no | Clear and reload navigation-owned data only (the four nav tables + the three `quran_ayahs` nav columns). Without it, any already-populated navigation target causes refusal. |

Unknown arguments are rejected with usage text and a non-zero exit before report output is required
(matches the existing `TryParse…Arguments` behavior). When `--source` is supplied it must resolve to an
existing directory; otherwise the default package root is used.

## Behavior

1. Resolve the source **package root** path and the report output path.
2. Verify package shape: `manifest.json` exists and `sources/{quran-metadata-juz,quran-metadata-hizb,quran-metadata-rub,quran-metadata-sajda}.json` are present, and only those.
3. Verify the **final manifest**: `packageType = "quran-navigation-metadata-import-source-package"` and `isFinalImportManifest = true`. Folder location alone is not sufficient.
4. Verify per-file `sha256`, `sizeBytes`, and `recordCount` against the manifest; verify expected counts juz=30, hizb=60, rub=240, sajda=15.
5. Verify required fields per dataset and that `sajdah_type ∈ {required, optional}`.
6. Require canonical ayahs to exist (`quran_ayahs` non-empty) and resolve every referenced `verse_key`.
7. Parse each division's `verse_mapping`; verify per-type coverage of all 6,236 ayahs exactly once (no gaps/overlaps) and the derived hierarchy (hizb→juz, rub→hizb).
8. Refuse if any navigation target table is non-empty OR any `quran_ayahs` nav column is populated, and `--force` is absent.
9. If `--force` is present, clear and reload only navigation-owned data after package validation.
10. Persist the four header tables (verse counts = computed range counts) and update `juz_number`/`hizb_number`/`rub_number` on all 6,236 ayahs.
11. Run hard checks; re-verify the source package is unchanged (sha256/size) before commit.
12. Never read or persist Quran ayah text from the sources; never mutate `quran_ayahs.text_uthmani`.
13. Write both required reports.
14. Accept the run only when all hard checks pass and both reports are written; otherwise roll back fully.

## Exit behavior

| Outcome | Exit | Data kept | Report |
|---|---|---|---|
| Success | `0` | Yes | Markdown + JSON |
| Refused before write (targets populated, no `--force`; or `quran_ayahs` empty) | non-zero | No new data | Markdown + JSON after report path is resolved |
| Validation failure (counts/hash/coverage/hierarchy/verse-keys/sajda-type) | non-zero | No partial changes | Markdown + JSON after report path is resolved |
| Report write failure after validation | non-zero | No navigation changes kept | Console/report-write error |
| Source changed mid-run (pre-commit re-check fails) | non-zero | No accepted changes (rollback) | Markdown + JSON |
| Unhandled early startup, path-resolution, I/O, or DB error | non-zero | No accepted partial changes | Best-effort console error; reports if writer context available |

## Console summary

Success output should include:

- `juz=30`
- `hizb=60`
- `rub=240`
- `sajda=15`
- `ayahsTagged=6236`
- `warnings` count
- `forced=true` line when `--force` was used (nav-owned tables + ayah nav columns cleared and rebuilt after package validation)
- report directory

Failure/refusal output should include the first actionable refusal or hard-check failure (its `NAV-*` id)
and whether any data was persisted.

## Out of scope

- No API endpoint, frontend/admin UI, public reader, or search indexing.
- No startup seeding.
- No source package editing.
- No re-import of surah or ayah metadata; no reading/copying of Quran ayah text.
- No `quran_ayahs.text_uthmani` mutation; no `quran_words`/tafsir/translation/mutashabihat/morphology/i3rab changes.
- No ruku, manzil, or audio metadata.
