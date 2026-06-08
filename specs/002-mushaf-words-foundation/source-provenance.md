# Source Provenance - Quran Foundation Import Sources

This document records the Phase 1 source staging contract for the Quran Mushaf Words & Layout Data Foundation feature.

Large Quran source files remain untracked because root `.gitignore` ignores `resources/`. The tracked source of truth for the staged set is this document plus `contracts/import-manifest.schema.md` and `quickstart.md`.

## Rules

- Source bytes must not be modified while assembling `resources/import-sources/quran-foundation/`.
- Page fonts are out of scope for Feature 002 and must not be copied, validated, imported, or referenced in the manifest.
- `sha256` is optional in `manifest.json`; if supplied later, importer validation must enforce it.
- The staged files are read-only import inputs. The importer must validate file presence and expected counts before reading data.

## Manifest Sources

| Key | Staged relative path | Original source path | Expected count | Count field | Join key |
|---|---|---|---:|---|---|
| `qpc-glyph` | `mushaf/qpc-v4.json` | `resources/mushaf/qpc-v4-tajweed/words/original/qpc-v4.json` | 83,668 | `expectedRecordCount` | `location` |
| `uthmani` | `words/uthmani.json` | `resources/words/with-tashkeel/original/uthmani.json` | 83,668 | `expectedRecordCount` | `location` |
| `uthmani-simple` | `words/uthmani-simple.json` | `resources/words/without-tashkeel/original/uthmani-simple.json` | 83,668 | `expectedRecordCount` | `location` |
| `imlaei-simple` | `words/imlaei-simple.json` | `resources/words/without-tashkeel/original/imlaei-simple.json` | 83,668 | `expectedRecordCount` | `location` |
| `layout` | `mushaf/qpc-v4-pages-layout.json` | `resources/mushaf/qpc-v4-tajweed/layout/jsonData/qpc-v4-pages-layout.json` | 604 pages / 9,046 lines | `expectedPageCount` / `expectedLineCount` | `wordId` |
| `surah-meta` | `metadata/quran-metadata-surah-name.json` | `resources/metadata/surah-names/original/quran-metadata-surah-name.json` | 114 | `expectedRecordCount` | `id` |
| `ayah-meta` | `metadata/quran-metadata-ayah.json` | `resources/metadata/ayahs/original/quran-metadata-ayah.json` | 6,236 | `expectedRecordCount` | `verse_key` |

## Staged Tree

```text
resources/import-sources/quran-foundation/
  mushaf/qpc-v4.json
  mushaf/qpc-v4-pages-layout.json
  words/uthmani.json
  words/uthmani-simple.json
  words/imlaei-simple.json
  metadata/quran-metadata-surah-name.json
  metadata/quran-metadata-ayah.json
  manifest.json
  README.md
```

The staged tree intentionally excludes fonts and any derived search-normalized text.
