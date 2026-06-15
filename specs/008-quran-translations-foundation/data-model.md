# Phase 1 Data Model — Quran Translations Foundation

Feature 008 adds two translation-owned, source-built tables. They are loaded from the local final package
`resources/import-sources/quran-translations/` and keyed to existing `quran_ayahs`.

> Arabic Quran ayah text is never copied. Translation text is imported content and is stored exactly as
> imported.

## Source package summary

```text
resources/import-sources/quran-translations/
  README.md
  manifest.json
  source-display-metadata.json
  source-display-metadata.review.json
  package-report.md
  sources/
    <sourceKey>.json                # simple variant
    <sourceKey>.fn.json             # with-footnotes variant
```

Fixed v1 counts:

| Metric | Count |
|---|---:|
| Approved sources | 167 |
| Simple sources | 129 |
| With-footnotes sources | 38 |
| Excluded sources | 19 |
| Languages | 83 |
| Ayahs per approved source | 6,236 |
| Source-to-ayah mappings | 1,041,412 |

## Relationships

```text
manifest.json (167 approved source/file records)
source-display-metadata.json (167 final display records)
  └── sources/<sourceKey>[.fn].json
        └── verse_key -> object { "t": "<translation text>" }

quran_ayahs (read-only)
  id (PK)
  verse_key (UNIQUE)

quran_translation_sources (167)
  └── quran_translation_ayah_entries (one row per source + ayah)
        └── ayah_id -> quran_ayahs.id
```

## 1. `quran_translation_sources`

One row per approved imported translation source. Source rows are built from the manifest plus the final
display metadata contract. Display metadata wins for display labels and language labels when the manifest
contains earlier/null display values.

| Column | Type | Null | Notes |
|---|---|---|---|
| `id` | `int` | NO | Primary key, generated identity |
| `source_key` | `text` | NO | Stable manifest key; unique, e.g. `en-yusufali` |
| `language_code` | `text` | NO | Display metadata language code, e.g. `en`, `ur`, `dv` |
| `language_name_en` | `text` | NO | Final English language name |
| `language_name_ar` | `text` | NO | Final Arabic language name |
| `native_name` | `text` | YES | Native language label when available |
| `direction` | `text` | NO | `rtl` or `ltr`, source-level not language-level |
| `translation_type` | `text` | NO | `simple` or `with_footnotes` |
| `display_name_en` | `text` | NO | Required future selector label |
| `display_name_ar` | `text` | NO | Required future selector label |
| `translator_key` | `text` | YES | Inferred translator/source key when available |
| `translator_name_en` | `text` | YES | Optional; non-blocking and not the primary selector |
| `translator_name_ar` | `text` | YES | Optional; non-blocking and not the primary selector |
| `contains_inline_footnotes` | `bool` | NO | True when source text contains `[[...]]` |
| `contains_html_markup` | `bool` | NO | True when source text contains embedded HTML |
| `content_coverage_count` | `smallint` | NO | Must be 6,236 for v1 imports |

**Constraints/indexes:**

- Primary key `id`.
- Unique `source_key`.
- Index `language_code`.
- Index `(language_code, translation_type)`.
- Check `direction IN ('rtl', 'ltr')`.
- Check `translation_type IN ('simple', 'with_footnotes')`.
- Check `content_coverage_count = 6236`.
- Check required display/language/source fields are non-empty after trimming.

**Not persisted in v1 DB columns:**

- `source_file_original`
- `package_file`
- `sha256`
- `file_size_bytes`
- `license`
- `provenance`
- `manifest_metadata`
- display review confidence fields such as `needsReview`, `metadataConfidence`, and `reviewReasons`

Those values remain in `manifest.json`, `source-display-metadata.json`, and import reports.

## 2. `quran_translation_ayah_entries`

One row per approved source and Quran ayah.

| Column | Type | Null | Notes |
|---|---|---|---|
| `id` | `bigint` | NO | Primary key, generated identity |
| `source_id` | `int` | NO | FK -> `quran_translation_sources.id` |
| `ayah_id` | `int` | NO | FK -> `quran_ayahs.id` |
| `verse_key` | `text` | YES | Source verse key copy for audit/debug; must match resolved ayah when present |
| `text` | `text` | NO | Exact source `t`, including inline `[[...]]` and embedded HTML |

**Constraints/indexes:**

- Primary key `id`.
- FK `source_id -> quran_translation_sources(id)`.
- FK `ayah_id -> quran_ayahs(id)`.
- Unique `(source_id, ayah_id)`.
- Index `(ayah_id, source_id)`.
- Check `text <> ''`.

## Excluded by design

- No persisted records for the 19 excluded sources; they are report-only.
- No word-by-word import.
- No copied Arabic Quran ayah text.
- No source text normalization, sanitization, footnote parsing, or plain-text derivative.
- No separate language, contributor, footnote, license, provenance, or import-run-history tables in v1.
- No API/search/read-model tables.

## Import derivation

1. Read final `manifest.json`.
2. Read final `source-display-metadata.json`.
3. Verify package shape, final manifest, final display metadata, source-set alignment, file size, and sha256.
4. Load `quran_ayahs` as a read-only `verse_key -> ayah_id` map.
5. For each approved source:
   - Parse root object keyed by verse key.
   - Require exactly 6,236 canonical verse keys.
   - Require every value to be an object with non-empty string `t`.
   - Resolve every verse key to `quran_ayahs`.
   - Preserve text exactly.
   - Merge source metadata from manifest and final display metadata.
6. Bulk write sources and ayah entries in FK-safe order.
7. Run hard checks inside the transaction.
8. Re-verify the package unchanged state.
9. Write reports and accept the run only if hard checks pass and reports are available.

## Validation invariants

| ID | Severity | Requirement |
|---|---|---|
| `TR-PACKAGE-SHAPE` | hard | Package root contains `README.md`, `manifest.json`, `package-report.md`, `source-display-metadata.json`, and `sources/`. |
| `TR-MANIFEST-FINAL` | hard | Manifest is final translation import manifest. |
| `TR-DISPLAY-METADATA-FINAL` | hard | Display metadata is present, final, has 167 records, and every record is final display-ready. |
| `TR-DISPLAY-METADATA-SET` | hard | Display metadata `sourceKey` set exactly matches manifest approved source set. |
| `TR-DISPLAY-METADATA-REQUIRED-FIELDS` | hard | Required display metadata fields are present and non-empty, including `displayNameEn` and `displayNameAr`. |
| `TR-SOURCE-COUNT` | hard | Approved source count is 167. |
| `TR-TYPE-COUNTS` | hard | Type counts are simple 129 and with-footnotes 38. |
| `TR-EXCLUDED-COUNT` | hard | Excluded source count is 19. |
| `TR-SOURCE-SET` | hard | `sources/` files exactly match manifest approved package files. |
| `TR-SOURCE-HASH` | hard | Every approved file size and sha256 matches manifest. |
| `TR-NO-EXCLUDED-SOURCES` | hard | Excluded and word-by-word sources are not importable and not persisted. |
| `TR-JSON-SHAPE` | hard | Each source root is an object and every value is `{ "t": string }`. |
| `TR-COVERAGE-COUNT` | hard | Every approved source has the exact 6,236 verse-key set. |
| `TR-NO-EMPTY-TEXT` | hard | No approved source has empty, null, missing, or non-string `t`. |
| `TR-AYAH-KEYS-RESOLVE` | hard | Every verse key resolves to canonical ayah. |
| `TR-NO-DUPLICATE-AYAH-ENTRY` | hard | No duplicate `(source, ayah)` mapping. |
| `TR-TEXT-UNCHANGED` | hard | Stored translation text matches imported source text exactly. |
| `TR-NO-QURAN-TEXT-COPY` | hard | Translation-owned records do not contain copied Arabic Quran ayah text. |
| `TR-POSTCOPY-SOURCE-ROWS` | hard | Persisted source rows = 167. |
| `TR-POSTCOPY-AYAH-MAPPINGS` | hard | Persisted ayah mappings = 1,041,412. |
| `TR-SOURCE-UNCHANGED` | hard | Source files still match manifest before acceptance. |
| `TR-REPORT-WRITTEN` | hard | Required Markdown and JSON reports are written before run acceptance. |
| `TR-ROLLBACK-ON-FAIL` | hard | Any hard-check failure rolls back all translation-owned writes. |
| `TR-RERUN-GUARD` | hard | Normal re-run refuses non-empty target tables; forced replacement revalidates before replacing. |
| `TR-PROVENANCE-WARNING` | warning | License/provenance unknown for all imported sources; internal use only. |
| `TR-INLINE-MARKUP` | info | Inline `[[...]]` and embedded HTML are preserved exactly. |
| `TR-LANGUAGE-COVERAGE` | info | Source count by language, direction, and type. |
| `TR-RECLASSIFIED` | info | Three sources were reclassified from simple folder placement to `with_footnotes` by content. |
