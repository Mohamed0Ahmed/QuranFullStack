# Feature 007 Quran Tafsir Foundation Planning Report

**Date:** 2026-06-14  
**Scope:** Planning report only. No implementation, no Spec Kit artifacts, no Backend or Frontend source edits, no migrations, no build/test execution.  
**Feature:** Quran Tafsir Foundation  
**Canonical source package:** `resources/import-sources/quran-tafsirs/`  

---

## 1. Executive Summary

Feature 007 should establish the backend data foundation for curated Quran tafsir sources. Its job is to import the final staged package at `resources/import-sources/quran-tafsirs/`, validate it against the final manifest, resolve every approved source entry to canonical `quran_ayahs`, persist tafsir source metadata and tafsir text, and produce repeatable JSON/Markdown import reports.

The feature must not implement API endpoints, frontend screens, tafsir comparison UI, public reader behavior, search indexing, translation features, automatic startup seeding, or any mutation of Quran foundation tables. It must not copy Quran ayah text into tafsir tables; canonical Quran text remains owned by `quran_ayahs`.

This feature is needed before tafsir UI/API work because tafsir content has high provenance, licensing, identity, and ayah-resolution risk. Later readers, comparison screens, filters, and search features need a stable source catalog, clear source keys, language/direction metadata, exact source-package validation, and a durable ayah-to-tafsir lookup model before any public-facing behavior is designed.

**Current package facts from the final package report and manifest:**

| Metric | Count |
|---|---:|
| Raw inspected sources | 93 |
| Approved copied tafsir sources | 84 |
| Excluded sources | 9 |
| Arabic approved copied | 35 |
| Non-Arabic approved copied | 49 |
| Languages copied | 33 |
| Contributors referenced | 42 |

The largest risk is not structure. The package structure is stable. The largest risk is license/provenance: every source has `license = unknown` and `provenance = unknown`. That is acceptable only as an internal-use warning unless and until external publication rights are separately cleared.

---

## 2. Source Package Assessment

### Expected Package Shape

The importer should accept only the final package folder:

```text
resources/import-sources/quran-tafsirs/
├── README.md
├── manifest.json
├── package-report.md
└── sources/
    ├── ar-tabari.json
    ├── ar-qurtubi.json
    └── ...
```

The final package currently contains exactly 84 normalized source files under `sources/`. Filenames are normalized to `<sourceKey>.json`, and the copied source JSON content is preserved byte-for-byte from the curated upstream files.

The importer should treat `manifest.json` as authoritative. The older draft curation manifest under `docs/feature-007-quran-tafsir-foundation/` remains useful history, but it must not be used as the import manifest.

### Source Identification

Each tafsir source should be identified by a stable `sourceKey` from the final manifest. This key should become the database-level natural key and the import/report identity. Example values:

- `ar-tabari`
- `ar-qurtubi`
- `ar-mukhtasar`
- `en-ibn-kathir`
- `ckb-rebar`
- `ur-bayan-ul-quran-thanwi`

For each source, the importer should read and persist the manifest metadata needed for later backend/API/UI work:

| Field | Use |
|---|---|
| `sourceKey` | Stable source identity; unique natural key. |
| `languageCode` | BCP-47-style code used by the package, e.g. `ar`, `en`, `ckb`, `ur`. |
| `languageNameAr` / `languageNameEn` | Display metadata from curation. |
| `direction` | `rtl` or `ltr`; important for later Arabic-first and multilingual UI. |
| `displayNameAr` / `shortNameAr` | Arabic display names. |
| `displayNameEn` / `shortNameEn` | English display names. |
| `contributorKey` / names / type | Attribution metadata where known. |
| `resourceKind` | Must be `tafsir` for imported sources. |
| `tafsirKind` | Category such as `brief` or `detailed` where available. |
| `contentCoverageCount` | Must be 6,236 for approved sources. |
| `packageFile` | Relative path under the package. |
| `sourceFileOriginal` | Provenance path from the raw source inventory. |
| `sha256` / `fileSizeBytes` | Source integrity validation. |
| `license` / `provenance` | Currently always `unknown`; must be preserved. |

### Approved Vs Excluded Sources

Only the 84 final approved sources should be imported. Excluded sources must remain visible in reports but must never be persisted as active tafsir sources.

The package excludes 9 inspected sources:

| Exclusion class | Count | Import behavior |
|---|---:|---|
| `excluded_incomplete_coverage` | 7 | Do not import; revisit only if a complete edition is sourced later. |
| `excluded_non_tafsir` | 1 | Do not import into tafsir tables; possible future gharib feature only. |
| `excluded_suspect_quality` | 1 | Do not import; likely stub/mislabeled source. |

The importer should hard-fail if any excluded source appears in `sources/`, if any manifest source has `includeInFutureImport = false`, or if an approved source file is missing.

### License And Provenance Warning

License/provenance is unknown for all sources. The import should not hide this. The database should persist `license_status = unknown` and `provenance_status = unknown` (or equivalent columns), and every import report should emit `TAFSIR-PROVENANCE-WARNING` as a warning, not a passively buried note.

Risk decision: internal import can proceed with this warning, but public product exposure, redistribution, export, or publishing should be blocked by later feature gates until licensing/provenance is explicitly reviewed.

---

## 3. Proposed Database Model

### Recommendation

Use a three-table model:

1. `quran_tafsir_sources`
2. `quran_tafsir_entries`
3. `quran_tafsir_ayah_entries`

The reason for three tables is the source JSON shape. Many files use grouped tafsir blocks: one text block covers multiple ayahs, while member ayahs point to the group leader. If v1 stores one expanded tafsir-text row per source/ayah, it will duplicate potentially large text blocks many times. A text-block table plus an ayah mapping table keeps tafsir text stored once while still supporting fast "all tafsirs for this ayah" queries.

### `quran_tafsir_sources`

**Purpose:** One row per imported tafsir source. Owns source identity, language, direction, contributor metadata, package provenance, and license/provenance warning metadata.

**Key columns:**

| Column | Purpose |
|---|---|
| `id` | Surrogate primary key. |
| `source_key` | Stable package key, e.g. `ar-tabari`; unique. |
| `language_code` | Source language code, e.g. `ar`, `en`, `ckb`. |
| `language_name_ar` / `language_name_en` | Manifest language labels. |
| `direction` | `rtl` or `ltr`. |
| `display_name_ar` / `short_name_ar` | Arabic display metadata. |
| `display_name_en` / `short_name_en` | English display metadata. |
| `contributor_key` | Source contributor identity when available. |
| `contributor_name_ar` / `contributor_name_en` | Contributor labels. |
| `contributor_type` | `person`, `institution`, `editorial_team`, `unknown`, etc. |
| `resource_kind` | Must be `tafsir` for imported rows. |
| `tafsir_kind` | `brief`, `detailed`, `unknown`, etc. |
| `content_coverage_count` | Expected 6,236 for imported v1 sources. |
| `package_file` | Relative package file path. |
| `source_file_original` | Raw source provenance path from manifest. |
| `sha256` | Manifest checksum. |
| `file_size_bytes` | Manifest size. |
| `license_status` | `unknown` in v1. |
| `provenance_status` | `unknown` in v1. |
| `manifest_metadata_json` | JSONB snapshot for manifest fields not modeled as columns. |
| `imported_at_utc` | Import timestamp. |

**Foreign keys:** None required in v1. A later normalized language/contributor catalog can be introduced only if API/UI needs it.

**Unique constraints:**

- Unique `source_key`.
- Optional unique `package_file`.

**Indexes:**

- `source_key` unique.
- `language_code`.
- `direction`.
- `(language_code, tafsir_kind)`.
- `contributor_key` where non-null.

**JSONB fields:**

- `manifest_metadata_json` for preserving manifest-level details without forcing premature schema expansion.

**Text storage:**

- Does not store tafsir text.
- Does not store Quran ayah text.

### `quran_tafsir_entries`

**Purpose:** One row per tafsir text block as represented by the source after resolving object records. Stores the actual tafsir content once.

For flat sources, this is usually one text block per ayah. For grouped sources, this is one text block per leader ayah, with member ayahs mapped through `quran_tafsir_ayah_entries`.

**Key columns:**

| Column | Purpose |
|---|---|
| `id` | Surrogate primary key. |
| `source_id` | FK to `quran_tafsir_sources`. |
| `source_entry_key` | Source leader/top-level verse key for the block, e.g. `1:1`. |
| `leader_ayah_id` | FK to `quran_ayahs(id)` for the leader/key that owns the text block. |
| `tafsir_text` | Imported tafsir text. May include inline HTML from source. |
| `text_format` | Recommended `html` or `plain`; source-driven. |
| `covered_ayah_count` | Number of ayahs covered by this text block. |
| `covered_ayah_keys_json` | JSONB array copied/resolved from `ayah_keys`, or single key for flat records. |
| `source_shape` | `grouped_leader`, `flat`, or equivalent. |
| `text_hash` | Optional hash for duplicate/block integrity reporting. |

**Foreign keys:**

- `source_id -> quran_tafsir_sources(id)` with cascade delete only if source re-import truncates feature-owned tables.
- `leader_ayah_id -> quran_ayahs(id)` with restrict/no action preferred for Quran foundation safety.

**Unique constraints:**

- Unique `(source_id, source_entry_key)`.

**Indexes:**

- `source_id`.
- `leader_ayah_id`.
- `(source_id, leader_ayah_id)`.
- Optional `text_hash` for diagnostics.

**JSONB fields:**

- `covered_ayah_keys_json` because grouped blocks can cover multiple ayahs, and preserving the source grouping is valuable for audit/reporting.

**Text storage:**

- Stores tafsir text because tafsir is the imported content.
- Does not store Quran ayah text.

### `quran_tafsir_ayah_entries`

**Purpose:** Lookup and coverage table. One row per `(source, ayah)` pair that connects an ayah to the tafsir text block that covers it.

This table makes the common future query direct: "give me tafsir entries for ayah X across all sources" or "give me this source's tafsir for ayah X".

**Key columns:**

| Column | Purpose |
|---|---|
| `id` | Surrogate primary key, or composite PK if preferred. |
| `source_id` | FK to `quran_tafsir_sources`. |
| `ayah_id` | FK to `quran_ayahs(id)`. |
| `tafsir_entry_id` | FK to `quran_tafsir_entries`. |
| `verse_key` | Source verse key for audit; should equal resolved `quran_ayahs.verse_key`. |
| `source_value_kind` | `leader`, `member_pointer`, or `flat`. |
| `source_leader_verse_key` | Leader key for grouped sources; same as `verse_key` for flat/single records. |
| `is_group_leader` | Boolean for grouped source audit. |
| `sort_order` | Optional Mushaf-order integer derived from `ayah_id` or source order. |

**Foreign keys:**

- `source_id -> quran_tafsir_sources(id)`.
- `ayah_id -> quran_ayahs(id)`.
- `tafsir_entry_id -> quran_tafsir_entries(id)`.

**Unique constraints:**

- Unique `(source_id, ayah_id)`.
- Optional unique `(source_id, verse_key)`.

**Indexes:**

- `(ayah_id, source_id)` for future ayah detail reads.
- `(source_id, ayah_id)` unique.
- `tafsir_entry_id` for coverage expansion.
- `(source_id, source_leader_verse_key)` for group diagnostics.

**JSONB fields:**

- None required in v1; keep this table relational because it is the primary lookup path.

**Text storage:**

- Does not store tafsir text directly; references `quran_tafsir_entries`.
- Does not store Quran ayah text.

### Explicit Non-Goals In The Model

- Do not copy `quran_ayahs.text_uthmani` into any tafsir table.
- Do not mutate `quran_ayahs`, `quran_surahs`, `quran_words`, or foundation import data.
- Do not add API-optimized search vectors or full-text indexes in Feature 007.
- Do not model translations as tafsir. These sources are tafsir/commentary resources, not Quran translation records.

---

## 4. Import Pipeline Design

### CLI Verb

Recommended CLI verb:

```text
import-tafsirs
```

Recommended arguments:

```text
--source-path resources/import-sources/quran-tafsirs
--force
--report-out-dir resources/report/quran-tafsirs
```

The default source path can resolve to `resources/import-sources/quran-tafsirs`, but the CLI should still allow an explicit path for tests and controlled runs.

### Flow

1. **Source reader**
   - Reads only the package root.
   - Requires `README.md`, `manifest.json`, `package-report.md`, and `sources/`.
   - Reads source files listed by `manifest.json`.
   - Parses source JSON as a top-level object keyed by `verse_key`.
   - Supports object values and string pointer values.

2. **Manifest validation**
   - Confirms `isFinalImportManifest == true`.
   - Confirms source count, language count, approved/excluded counts, checksum, file size, normalized filenames, and source set.
   - Confirms excluded sources are not present under `sources/`.

3. **Ayah resolution**
   - Reads `quran_ayahs` as a dictionary keyed by `verse_key`.
   - Refuses import if `quran_ayahs` is empty or does not contain the expected 6,236 verse keys.
   - Resolves every top-level key, every `ayah_keys` item, and every string pointer target to `quran_ayahs.id`.

4. **Assembler**
   - Builds source rows from manifest metadata.
   - Builds tafsir text blocks from object values containing `text`.
   - Resolves grouped member pointer rows to the leader block.
   - Builds one ayah mapping row per approved source and ayah.
   - Preserves source grouping metadata without rewriting source files.

5. **Validator**
   - Runs pre-copy hard checks before writing.
   - Runs post-copy hard checks inside the same transaction before commit.
   - Emits warnings for license/provenance and any non-blocking data characteristics.

6. **Persistence writer**
   - Uses explicit import writer infrastructure, not HTTP endpoints.
   - Writes feature-owned tables only.
   - Uses PostgreSQL transaction boundaries.
   - Bulk insert is appropriate because expected mapping rows are at least `84 * 6,236 = 523,824` ayah mappings plus source/text-block rows.

7. **Report writer**
   - Writes JSON and Markdown reports after the import attempt.
   - Recommended output path: `resources/report/quran-tafsirs/`.
   - Suggested filenames: `tafsir-import-report.json` and `tafsir-import-report.md`.

### Safe Re-Run / Force Behavior

- Without `--force`, refuse if any target tafsir table has data.
- With `--force`, truncate/rebuild only Feature 007 tafsir tables.
- Never truncate Quran foundation tables.
- Never edit source package files during import.
- Always re-check package unchanged state before commit using file set, size, and SHA-256.

### Transaction And Rollback

- Open one database transaction for truncation, insert, post-copy validation, and commit.
- If any hard check fails, roll back the transaction.
- If import fails before COPY/bulk insert, persist no rows.
- If post-copy validation fails, roll back all Feature 007 rows and write a failure report with attempted totals.
- Do not commit partial source subsets.

---

## 5. Validation Rules

Use consistent check IDs with the `TAFSIR-` prefix.

### Hard Checks

| Check ID | Requirement |
|---|---|
| `TAFSIR-PACKAGE-SHAPE` | Package root contains `README.md`, `manifest.json`, `package-report.md`, and `sources/`. |
| `TAFSIR-MANIFEST-FINAL` | `manifest.json` is the final import manifest, not the draft curation manifest. |
| `TAFSIR-SOURCE-COUNT` | Approved source count is exactly 84. |
| `TAFSIR-EXCLUDED-COUNT` | Excluded source count is exactly 9. |
| `TAFSIR-ARABIC-SOURCE-COUNT` | Arabic approved source count is exactly 35. |
| `TAFSIR-NON-ARABIC-SOURCE-COUNT` | Non-Arabic approved source count is exactly 49. |
| `TAFSIR-SOURCE-SET` | Files under `sources/` exactly match manifest `packageFile` values. |
| `TAFSIR-SOURCE-FILES-READABLE` | Every approved source file is readable and valid JSON. |
| `TAFSIR-SOURCE-HASH` | Every approved source file matches manifest `sha256` and `fileSizeBytes`. |
| `TAFSIR-SOURCE-KEY-UNIQUE` | Every approved `sourceKey` is unique. |
| `TAFSIR-NO-EXCLUDED-SOURCES` | No excluded, incomplete, non-tafsir, or suspect-quality source is copied/imported. |
| `TAFSIR-RESOURCE-KIND` | Every imported source has `resourceKind = tafsir`. |
| `TAFSIR-COVERAGE-COUNT` | Every imported source has `contentCoverageCount = 6236`. |
| `TAFSIR-JSON-SHAPE` | Each source JSON root is an object with exactly 6,236 top-level verse keys. |
| `TAFSIR-AYAH-KEY-FORMAT` | Every top-level key, pointer, and `ayah_keys` item uses valid `surah:ayah` form. |
| `TAFSIR-AYAH-KEYS-RESOLVE` | Every referenced verse key resolves to `quran_ayahs`. |
| `TAFSIR-POINTERS-RESOLVE` | Every string pointer points to a valid text-owning entry in the same source. |
| `TAFSIR-NO-EMPTY-TEXT` | Every approved source/ayah resolves to non-empty tafsir text after pointer resolution. |
| `TAFSIR-NO-DUPLICATE-AYAH-ENTRY` | No duplicate `(source_id, ayah_id)` mapping rows are produced. |
| `TAFSIR-SOURCE-UNCHANGED` | Source package file set, sizes, and hashes are unchanged before commit. |
| `TAFSIR-NO-QURAN-TEXT-COPY` | Tafsir tables do not store copied Quran ayah text; entries reference `quran_ayahs`. |
| `TAFSIR-POSTCOPY-SOURCE-ROWS` | Persisted source rows equal 84. |
| `TAFSIR-POSTCOPY-AYAH-MAPPINGS` | Persisted ayah mapping rows equal 523,824. |

### Warnings

| Check ID | Warning |
|---|---|
| `TAFSIR-PROVENANCE-WARNING` | License/provenance is unknown for all imported sources. |
| `TAFSIR-MODERN-WORKS-WARNING` | Modern or translated works may need explicit rights review before publishing. |
| `TAFSIR-INLINE-HTML-WARNING` | Some source text may contain inline HTML; v1 stores it but does not render/sanitize for public UI. |
| `TAFSIR-LARGE-SOURCE-WARNING` | Large tafsir files require bulk import and careful memory behavior. |

### Informational Checks

| Check ID | Information |
|---|---|
| `TAFSIR-LANGUAGE-COVERAGE` | Report source count by language. |
| `TAFSIR-DIRECTION-COVERAGE` | Report RTL/LTR source counts. |
| `TAFSIR-TAFSIR-KIND-COVERAGE` | Report brief/detailed/unknown source counts. |
| `TAFSIR-GROUPED-SOURCE-COUNT` | Report how many sources use grouped pointers versus flat object values. |
| `TAFSIR-TEXT-BLOCK-COUNT` | Report text block rows produced per source. |

---

## 6. Report Design

The importer should write both JSON and Markdown reports.

Recommended output path:

```text
resources/report/quran-tafsirs/
```

Recommended files:

```text
tafsir-import-report.json
tafsir-import-report.md
```

### JSON Report Contents

The JSON report should include:

- `runAtUtc`
- `verdict`: `pass`, `fail`, or `refused`
- `persisted`: boolean
- `forced`: boolean
- `sourcePath`
- `manifestSummary`
- `totals`
- `sourceSummaries`
- `excludedSourceSummaries`
- `languageSummaries`
- `importedRows`
- `checks`
- `warnings`
- `errors`
- `infoNotes`

Suggested totals:

| Field | Meaning |
|---|---|
| `approvedSourceCount` | Expected 84. |
| `excludedSourceCount` | Expected 9. |
| `arabicSourceCount` | Expected 35. |
| `nonArabicSourceCount` | Expected 49. |
| `languageCount` | Expected 33. |
| `sourceRowsWritten` | Rows in `quran_tafsir_sources`. |
| `textBlockRowsWritten` | Rows in `quran_tafsir_entries`. |
| `ayahMappingRowsWritten` | Rows in `quran_tafsir_ayah_entries`; expected 523,824. |
| `distinctAyahsReferenced` | Expected 6,236 if every ayah is covered. |

Each check result should include:

- `id`
- `severity`: `hard`, `warning`, or `info`
- `expected`
- `observed`
- `passed`
- `details`

### Markdown Report Contents

The Markdown report should be human-readable and include:

1. Verdict and run metadata.
2. Source package path and manifest identity.
3. Counts and imported row totals.
4. Source summary table by language/source key.
5. Excluded source table and reasons.
6. Hard check table.
7. Warnings table, especially license/provenance.
8. Failure cases and rollback status.
9. Notes on source package immutability.

Failure reports should say whether rows were persisted. A failed post-copy validation should clearly state that attempted totals reflect a rolled-back transaction.

---

## 7. Scope Boundaries

Feature 007 must remain backend data foundation only.

Out of scope:

- No API endpoints.
- No frontend.
- No tafsir comparison UI.
- No search indexing.
- No translations feature.
- No mutation of Quran foundation data.
- No editing source package during import.
- No automatic DB seeding at startup.
- No public reader behavior.
- No license-clearance workflow beyond preserving warnings.
- No migration name should be invented in this planning report.

In scope for the future implementation phase:

- Domain entities for tafsir sources, tafsir text blocks, and ayah mappings.
- Application command/handler for explicit import.
- Infrastructure source reader, assembler, validator, persistence writer, and report writer.
- Tests for source parsing, manifest validation, ayah resolution, force/refusal behavior, rollback, and report shape.

---

## 8. Spec Kit Readiness

### Recommended Feature Title

Quran Tafsir Foundation

### Proposed Spec Name

`007-quran-tafsir-foundation`

### User Stories Suitable For `/speckit.specify`

1. As a backend maintainer, I can import the final curated tafsir package so the database contains stable tafsir source metadata and tafsir text linked to canonical ayahs.
2. As a backend maintainer, I can re-run the tafsir import safely with explicit `--force` so feature-owned tafsir tables are rebuilt without mutating Quran foundation tables.
3. As a reviewer, I can inspect JSON and Markdown import reports so I can verify source counts, excluded sources, ayah resolution, warnings, and rollback status.
4. As a future API/UI implementer, I can query tafsir content by `ayah_id` and source so later tafsir reader or comparison features have a stable backend foundation.
5. As a product owner, I can see license/provenance warnings preserved in source metadata and reports so internal import is not confused with public publishing clearance.

### Open Clarification Questions

1. Should v1 persist `quran_tafsir_entries` as a separate text-block table, as recommended here, or intentionally expand tafsir text into one row per source/ayah despite duplicated grouped text?
2. Should language and contributor metadata remain denormalized on `quran_tafsir_sources` for v1, or should the implementation create separate `quran_tafsir_languages` and `quran_tafsir_contributors` tables immediately?
3. Should inline HTML be stored exactly as source text in `tafsir_text` for v1, with sanitization/rendering deferred to API/UI features?
4. Should public/API exposure later be blocked when `license_status = unknown`, or is that decision reserved for a separate publishing policy feature?

### Recommended Decisions To Lock Before `/speckit.specify`

- Lock source package path: `resources/import-sources/quran-tafsirs/`.
- Lock v1 counts: 84 approved, 9 excluded, 35 Arabic, 49 non-Arabic, 33 languages.
- Lock rule: excluded sources are report-only and must not be imported.
- Lock rule: tafsir text is imported content and must be stored; Quran ayah text must not be copied.
- Lock rule: every tafsir entry must resolve to `quran_ayahs` by verse key and persist an `ayah_id` relationship.
- Lock rule: license/provenance unknown is an explicit warning and not publication clearance.
- Lock CLI import as explicit operator action, not startup seeding and not HTTP/API behavior.
- Lock report output path recommendation: `resources/report/quran-tafsirs/`.

**Planning verdict:** Ready for `/speckit.specify` after the data-model granularity decision is confirmed. The staged source package is stable enough for specification, but the license/provenance warning must remain visible through the entire implementation.
