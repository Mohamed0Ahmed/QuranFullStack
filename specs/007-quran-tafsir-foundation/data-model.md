# Phase 1 Data Model — Quran Tafsir Foundation

Feature 007 adds three tafsir-owned, source-built tables. They are loaded from the local final package
`resources/import-sources/quran-tafsirs/` and keyed to existing `quran_ayahs`.

> Quran ayah text is never copied. Tafsir text is imported content and is stored exactly as imported.

## Source package summary

```text
resources/import-sources/quran-tafsirs/
  README.md
  manifest.json
  package-report.md
  sources/
    <sourceKey>.json                # 84 files
```

Fixed v1 counts:

| Metric | Count |
|---|---:|
| Approved sources | 84 |
| Excluded sources | 9 |
| Arabic approved sources | 35 |
| Non-Arabic approved sources | 49 |
| Languages | 33 |
| Source-to-ayah links | 523,824 |

## Relationships

```text
manifest.json (84 approved source records)
  └── sources/<sourceKey>.json
        ├── top-level verse_key -> object { text, ayah_keys? }
        └── top-level verse_key -> string pointer to leader verse_key

quran_ayahs (read-only)
  id (PK)
  verse_key (UNIQUE)

quran_tafsir_sources (84)
  └── quran_tafsir_entries (one row per stored tafsir text block)
        └── quran_tafsir_ayah_entries (one row per source + ayah)
              └── ayah_id -> quran_ayahs.id
```

## 1. `quran_tafsir_sources`

One row per approved imported tafsir source.

| Column | Type | Null | Notes |
|---|---|---|---|
| `id` | `int` | NO | Primary key, generated identity |
| `source_key` | `text` | NO | Stable manifest key; unique, e.g. `ar-tabari` |
| `language_code` | `text` | NO | Manifest language code, e.g. `ar`, `en`, `ckb` |
| `language_name_ar` | `text` | NO | Manifest Arabic language name |
| `language_name_en` | `text` | NO | Manifest English language name |
| `direction` | `text` | NO | `rtl` or `ltr` |
| `display_name_ar` | `text` | NO | Source display name in Arabic |
| `short_name_ar` | `text` | NO | Source short name in Arabic |
| `display_name_en` | `text` | NO | Source display name in English |
| `short_name_en` | `text` | NO | Source short name in English |
| `contributor_key` | `text` | YES | Manifest contributor identity |
| `contributor_name_ar` | `text` | YES | Contributor Arabic label |
| `contributor_name_en` | `text` | YES | Contributor English label |
| `contributor_type` | `text` | NO | `person`, `institution`, `editorial_team`, `unknown`, etc. |
| `resource_kind` | `text` | NO | Must be `tafsir` |
| `tafsir_kind` | `text` | NO | `brief`, `detailed`, or `unknown` |
| `content_coverage_count` | `smallint` | NO | Must be 6,236 for v1 imports |
| `package_file` | `text` | NO | Relative package file, e.g. `sources/ar-tabari.json` |
| `source_file_original` | `text` | NO | Raw-source provenance path from manifest |
| `sha256` | `text` | NO | Manifest checksum |
| `file_size_bytes` | `bigint` | NO | Manifest file size |
| `license_status` | `text` | NO | `unknown` in v1 |
| `provenance_status` | `text` | NO | `unknown` in v1 |
| `manifest_metadata` | `jsonb` | YES | Snapshot of manifest fields not modeled as columns |
| `imported_at_utc` | `timestamptz` | NO | Import timestamp |

**Constraints/indexes:**

- Primary key `id`.
- Unique `source_key`.
- Unique `package_file`.
- Index `language_code`.
- Index `(language_code, tafsir_kind)`.
- Check `resource_kind = 'tafsir'`.
- Check `content_coverage_count = 6236`.
- Check `direction IN ('rtl', 'ltr')`.

## 2. `quran_tafsir_entries`

One row per stored tafsir text block. Grouped sources store one text block for the leader and map member
ayahs through `quran_tafsir_ayah_entries`.

| Column | Type | Null | Notes |
|---|---|---|---|
| `id` | `bigint` | NO | Primary key, generated identity |
| `source_id` | `int` | NO | FK -> `quran_tafsir_sources.id` |
| `source_entry_key` | `text` | NO | Leader/top-level verse key for this text block |
| `leader_ayah_id` | `int` | NO | FK -> `quran_ayahs.id` |
| `tafsir_text` | `text` | NO | Exact imported tafsir text; may include inline markup |
| `covered_ayah_count` | `smallint` | NO | Number of ayahs linked to this text block |
| `covered_ayah_keys` | `jsonb` | NO | Source ayah keys covered by this block |
| `source_shape` | `text` | NO | `grouped_leader` or `flat` |
| `text_hash` | `text` | NO | Hash of exact stored tafsir text for verification/reporting |

**Constraints/indexes:**

- Primary key `id`.
- FK `source_id -> quran_tafsir_sources(id)`.
- FK `leader_ayah_id -> quran_ayahs(id)`.
- Unique `(source_id, source_entry_key)`.
- Index `leader_ayah_id`.
- Index `(source_id, leader_ayah_id)`.
- Check `covered_ayah_count >= 1`.
- Check `tafsir_text <> ''`.
- Check `source_shape IN ('grouped_leader', 'flat')`.

## 3. `quran_tafsir_ayah_entries`

One row per source and ayah. This is the future read lookup table.

| Column | Type | Null | Notes |
|---|---|---|---|
| `id` | `bigint` | NO | Primary key, generated identity |
| `source_id` | `int` | NO | FK -> `quran_tafsir_sources.id` |
| `ayah_id` | `int` | NO | FK -> `quran_ayahs.id` |
| `tafsir_entry_id` | `bigint` | NO | FK -> `quran_tafsir_entries.id` |
| `verse_key` | `text` | NO | Source verse key for audit; must match resolved ayah |
| `source_value_kind` | `text` | NO | `leader`, `member_pointer`, or `flat` |
| `source_leader_verse_key` | `text` | NO | Leader verse key for grouped source; same as verse key for flat |
| `is_group_leader` | `bool` | NO | True when the ayah owns the text block |
| `sort_order` | `int` | NO | Mushaf-order sort derived from canonical ayah identity |

**Constraints/indexes:**

- Primary key `id`.
- FK `source_id -> quran_tafsir_sources(id)`.
- FK `ayah_id -> quran_ayahs(id)`.
- FK `tafsir_entry_id -> quran_tafsir_entries(id)`.
- Unique `(source_id, ayah_id)`.
- Unique `(source_id, verse_key)`.
- Index `(ayah_id, source_id)`.
- Index `tafsir_entry_id`.
- Check `source_value_kind IN ('leader', 'member_pointer', 'flat')`.

## Excluded by design

- No persisted records for the 9 excluded sources; they are report-only.
- No copied Quran ayah text.
- No source text normalization, sanitization, or plain-text derivative.
- No language/contributor catalog tables in v1.
- No import-run history table in v1; reports are the audit record.
- No API/search/read-model tables.

## Import derivation

1. Read final manifest and approved source metadata.
2. Verify package shape, file set, file size, and sha256 for all approved source files.
3. Load `quran_ayahs` as a read-only `verse_key -> ayah_id` map.
4. For each source file:
   - Parse the root object keyed by verse key.
   - For object values, create a tafsir text block.
   - For string values, resolve the pointer to the leader text block.
   - Expand every covered ayah into `quran_tafsir_ayah_entries`.
   - Reject unresolved pointers, unresolved ayahs, empty text, or duplicate source/ayah mappings.
5. Bulk write sources, text blocks, and ayah links in FK-safe order.
6. Run hard checks inside the transaction.
7. Re-verify the package unchanged state.
8. Write reports and accept the run only if hard checks pass and reports are available.

## Validation invariants

| ID | Severity | Requirement |
|---|---|---|
| `TAFSIR-PACKAGE-SHAPE` | hard | Package root contains `README.md`, `manifest.json`, `package-report.md`, and `sources/`. |
| `TAFSIR-MANIFEST-FINAL` | hard | Manifest is final import manifest. |
| `TAFSIR-SOURCE-COUNT` | hard | Approved source count is 84. |
| `TAFSIR-EXCLUDED-COUNT` | hard | Excluded source count is 9. |
| `TAFSIR-ARABIC-SOURCE-COUNT` | hard | Arabic approved source count is 35. |
| `TAFSIR-NON-ARABIC-SOURCE-COUNT` | hard | Non-Arabic approved source count is 49. |
| `TAFSIR-SOURCE-SET` | hard | `sources/` files exactly match manifest approved package files. |
| `TAFSIR-SOURCE-HASH` | hard | Every file size and sha256 matches manifest. |
| `TAFSIR-NO-EXCLUDED-SOURCES` | hard | Excluded sources are not importable and not persisted. |
| `TAFSIR-COVERAGE-COUNT` | hard | Every approved source reports coverage 6,236. |
| `TAFSIR-JSON-SHAPE` | hard | Each source root is an object with 6,236 ayah keys. |
| `TAFSIR-AYAH-KEYS-RESOLVE` | hard | Every ayah key and pointer target resolves to `quran_ayahs`. |
| `TAFSIR-POINTERS-RESOLVE` | hard | Every string pointer resolves to a text-owning entry in the same source. |
| `TAFSIR-NO-EMPTY-TEXT` | hard | Every source/ayah resolves to non-empty tafsir text. |
| `TAFSIR-NO-DUPLICATE-AYAH-ENTRY` | hard | No duplicate `(source, ayah)` mapping. |
| `TAFSIR-TEXT-UNCHANGED` | hard | Stored tafsir text matches source text exactly. |
| `TAFSIR-NO-QURAN-TEXT-COPY` | hard | Tafsir tables do not store copied Quran ayah text. |
| `TAFSIR-POSTCOPY-SOURCE-ROWS` | hard | Persisted source rows = 84. |
| `TAFSIR-POSTCOPY-AYAH-MAPPINGS` | hard | Persisted ayah mappings = 523,824. |
| `TAFSIR-SOURCE-UNCHANGED` | hard | Source files still match manifest before acceptance. |
| `TAFSIR-REPORT-WRITTEN` | hard | Required Markdown and JSON reports are written before run acceptance. |
| `TAFSIR-PROVENANCE-WARNING` | warning | License/provenance unknown for all imported sources. |
| `TAFSIR-INLINE-MARKUP` | info | Source text may contain inline markup; preserved exactly. |
| `TAFSIR-LANGUAGE-COVERAGE` | info | Source count by language/direction. |
| `TAFSIR-TEXT-BLOCK-COUNT` | info | Text block count by source. |
