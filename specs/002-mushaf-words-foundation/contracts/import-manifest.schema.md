# Contract — `manifest.json` (import source description)

The importer reads this file **first** and validates the whole source set against it **before** reading any data. Any missing file, count mismatch, or (when present) checksum mismatch is a **fail-fast** error: stop, report, persist nothing.

**Location**: `<source-root>/manifest.json` (default `<source-root>` = `resources/import-sources/quran-foundation/`).

## Shape

```json
{
  "version": "1",
  "generatedAt": "2026-06-08T00:00:00Z",
  "sources": [
    { "key": "qpc-glyph",   "relativePath": "mushaf/qpc-v4.json",                       "format": "json",      "expectedRecordCount": 83668, "joinKey": "location", "sha256": "<optional>" },
    { "key": "uthmani",      "relativePath": "words/uthmani.json",                       "format": "json",      "expectedRecordCount": 83668, "joinKey": "location", "sha256": "<optional>" },
    { "key": "uthmani-simple","relativePath": "words/uthmani-simple.json",               "format": "json",      "expectedRecordCount": 83668, "joinKey": "location", "sha256": "<optional>" },
    { "key": "imlaei-simple","relativePath": "words/imlaei-simple.json",                 "format": "json",      "expectedRecordCount": 83668, "joinKey": "location", "sha256": "<optional>" },
    { "key": "layout",       "relativePath": "mushaf/qpc-v4-pages-layout.json",          "format": "json",      "expectedPageCount": 604, "expectedLineCount": 9046, "joinKey": "wordId" },
    { "key": "surah-meta",   "relativePath": "metadata/quran-metadata-surah-name.json",  "format": "json",      "expectedRecordCount": 114,   "joinKey": "id" },
    { "key": "ayah-meta",    "relativePath": "metadata/quran-metadata-ayah.json",        "format": "json",      "expectedRecordCount": 6236,  "joinKey": "verse_key" }
  ]
}
```

## Field rules

| Field | Rule |
|---|---|
| `version` | Non-empty string; importer supports `"1"`. |
| `sources[].key` | One of the 7 keys above; all 7 MUST be present exactly once. |
| `relativePath` | Path relative to source root; the target MUST exist. |
| `format` | `json`. |
| `expectedRecordCount` / `expectedPageCount` / `expectedLineCount` | Integer; MUST equal the actual loaded count. |
| `joinKey` | Documentation of the join column for that source. |
| `sha256` | Optional; if present, MUST match the file's hash. |

## Failure behavior

- Missing manifest, unknown/duplicate/missing key, missing file, count mismatch, or checksum mismatch → **abort before reading data**, exit non-zero, and write a failure validation report naming the offending `key` and the expected vs observed value.
