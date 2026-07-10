# Current Local PostgreSQL Database Inventory

Date: 2026-06-29 *(full live-catalog regeneration through Feature 018; prior snapshot 2026-06-13 was Feature 005-era / 17 tables)*
Database: `quran_dashboard` on `localhost:5432`
Scope: read-only PostgreSQL catalog/data inventory plus EF mapping inspection, covering Features 002–018.
Password handling: connection password was supplied to `psql` via the `PGPASSWORD` environment variable and is **not** written to this report.

## 1. Executive summary

| Metric | Count | Notes |
|---|---:|---|
| Application schemas currently used | 1 | Only `public` for application tables. |
| Tables | 32 | 31 Quran/domain tables + EF `__EFMigrationsHistory`. |
| Columns | 329 | Includes EF migration history columns. |
| Indexes | 130 | Includes PK-backed indexes and 7 partial indexes. |
| Foreign keys | 52 | All are within `public`. |
| Unique constraints | 0 | Uniqueness is enforced through unique indexes, not separate `UNIQUE` table constraints. |
| Check constraints (explicit) | 27 | Domain/value checks on i3rab status, source enums, coverage counts, non-empty text, and the self-link guard. |
| Check constraints (information_schema, incl. `NOT NULL`) | 320 | Mostly PostgreSQL/EF-reported `NOT NULL`; the 27 above are the explicit domain checks. |
| EF migrations applied | 15 | Rows in `__EFMigrationsHistory`; see `database-reset-and-seeding-order.md` §2. |

Growth since the 2026-06-13 snapshot (17 → 32 tables): mutashabihat (006), tafsir (007), translations (008),
navigation divisions + ayah nav columns (009), full-i3rab (010), ayah-similarities (012), and segment
`root_id`/`lemma_id`/`stem_id` columns (017–018).

Very large / high-volume tables by exact row count:

- `quran_translation_ayah_entries`: 1,041,412 rows.
- `quran_tafsir_ayah_entries`: 523,824 rows.
- `quran_tafsir_entries`: 382,704 rows.
- `quran_word_morphology_segments`: 128,219 rows.
- `quran_words`: 83,668 rows.
- `quran_word_morphology`, `quran_words_ordered_simple`, `quran_words_ordered_tashkeel`: 77,432 rows each.
- `quran_full_i3rab_ayah_entries`: 24,944 rows; `quran_words_unique_tashkeel`: 21,294 rows.

Naming / structural observations (not necessarily defects):

- All domain tables are in `public` and carry a `quran_` prefix.
- Three content families share the same `*_sources` → `*_entries` → `*_ayah_entries` shape: tafsir (007),
  full-i3rab (010), and translations (008, two-table variant `*_sources` → `*_ayah_entries`). They repeat
  columns such as `source_key`, `direction`, `content_coverage_count` (`= 6236` check), `sha256`,
  `provenance_status`, `source_shape`, `source_value_kind`.
- Navigation divisions are four sibling tables keyed by their own number: `quran_juzs`, `quran_hizbs`,
  `quran_rubs`, `quran_sajdas`; `quran_ayahs` gained nullable `juz_number`/`hizb_number`/`rub_number` tags.
- Mixed transliteration conventions appear in column names: `i3rab`, `imlaei`, `uthmani`, `qpc`, `buckwalter`.
- `head_pos` (`quran_word_morphology`) vs `pos` (`quran_word_morphology_segments`) — word-level vs segment-level.
- The morphology dimension ids now exist at two grains: word-level on `quran_word_morphology`
  (`root_id`/`lemma_id`/`stem_id`) and segment-level on `quran_word_morphology_segments` (same three, added 017–018).
- EF infrastructure table `__EFMigrationsHistory` uses PascalCase columns while app tables use snake_case.

## 2. Tables inventory

| Schema | Table | Rows | Primary key | Columns | Short inferred purpose |
|---|---|---:|---|---:|---|
| public | `__EFMigrationsHistory` | 15 | `MigrationId` | 2 | EF Core migration tracking table. |
| public | `quran_ayahs` | 6,236 | `id` | 12 | Canonical ayah metadata/text, page span, and juz/hizb/rub tags. |
| public | `quran_full_i3rab_ayah_entries` | 24,944 | `id` | 9 | Ayah→full-i3rab-entry junction per source. |
| public | `quran_full_i3rab_entries` | 14,513 | `id` | 9 | Distinct full-i3rab HTML entries (grouped-leader or flat). |
| public | `quran_full_i3rab_sources` | 4 | `id` | 23 | Full-i3rab source catalogue + provenance. |
| public | `quran_hizbs` | 60 | `hizb_number` | 7 | Hizb division ranges. |
| public | `quran_i3rab_rules` | 142 | `id` | 7 | Rule catalogue for generated simple i'rab labels. |
| public | `quran_juzs` | 30 | `juz_number` | 6 | Juz division ranges. |
| public | `quran_lemmas` | 4,790 | `id` | 6 | Morphology lemma dimension, optionally linked to roots. |
| public | `quran_mushaf_lines` | 9,046 | `id` | 9 | Mushaf page-line layout and word range anchors. |
| public | `quran_mushaf_pages` | 604 | `page_number` | 6 | Mushaf page ranges and line counts. |
| public | `quran_mutashabihat_groups` | 814 | `id` | 9 | Similar-passage (mutashabihat) groups + representative span. |
| public | `quran_mutashabihat_occurrences` | 3,557 | `id` | 6 | Per-group occurrence spans. |
| public | `quran_pos_tags` | 49 | `code` | 6 | Controlled POS vocabulary and labels. |
| public | `quran_roots` | 1,642 | `id` | 6 | Morphology root dimension and usage stats. |
| public | `quran_rubs` | 240 | `rub_number` | 7 | Rub' division ranges. |
| public | `quran_sajdas` | 15 | `sajdah_number` | 4 | Sajda ayah markers (obligatory/recommended). |
| public | `quran_similar_ayah_links` | 3,552 | `id` | 7 | Directed ayah-similarity links + score/coverage. |
| public | `quran_stems` | 12,108 | `id` | 4 | Morphology stem dimension and usage stats. |
| public | `quran_surahs` | 114 | `surah_number` | 8 | Canonical surah metadata. |
| public | `quran_tafsir_ayah_entries` | 523,824 | `id` | 9 | Ayah→tafsir-entry junction per source. |
| public | `quran_tafsir_entries` | 382,704 | `id` | 9 | Distinct tafsir text entries (grouped-leader or flat). |
| public | `quran_tafsir_sources` | 84 | `id` | 25 | Tafsir source catalogue + provenance. |
| public | `quran_translation_ayah_entries` | 1,041,412 | `id` | 5 | Per-source ayah translation text. |
| public | `quran_translation_sources` | 167 | `id` | 16 | Translation source catalogue. |
| public | `quran_word_morphology` | 77,432 | `quran_word_id` | 12 | One morphology summary row per readable Quran word. |
| public | `quran_word_morphology_segments` | 128,219 | `id` | 21 | Segment-level morphology, render provenance, generated i'rab, and segment dimension ids. |
| public | `quran_words` | 83,668 | `id` | 17 | Canonical word/token stream, including ayah markers. |
| public | `quran_words_ordered_simple` | 77,432 | `word_order_in_mushaf` | 16 | Derived readable word ordering grouped by simple/imlaei key. |
| public | `quran_words_ordered_tashkeel` | 77,432 | `word_order_in_mushaf` | 16 | Derived readable word ordering grouped by tashkeel/Uthmani text. |
| public | `quran_words_unique_simple` | 14,783 | `id` | 16 | Derived unique simple/imlaei word identities and first occurrence metadata. |
| public | `quran_words_unique_tashkeel` | 21,294 | `id` | 14 | Derived unique tashkeel/Uthmani word identities and first occurrence metadata. |

## 3. Columns inventory

Legend for `Role`: `PK` = primary key, `FK` = foreign key, `IDX` = covered by a non-PK index.
(The prior revision's speculative per-column "category" tags were dropped in favor of factual role flags.)

### `public.__EFMigrationsHistory`

| Column | Type | Nullability | Default | Role |
|---|---|---|---|---|
| `MigrationId` | `varchar(150)` | NOT NULL | — | PK |
| `ProductVersion` | `varchar(32)` | NOT NULL | — | — |

### `public.quran_ayahs`

| Column | Type | Nullability | Default | Role |
|---|---|---|---|---|
| `id` | `integer` | NOT NULL | — | PK |
| `surah_number` | `smallint` | NOT NULL | — | FK, IDX |
| `ayah_number` | `smallint` | NOT NULL | — | IDX |
| `verse_key` | `text` | NOT NULL | — | IDX |
| `text_uthmani` | `text` | NOT NULL | — | — |
| `words_count_source` | `smallint` | NOT NULL | — | — |
| `words_count_real` | `smallint` | NOT NULL | — | — |
| `page_from` | `smallint` | NOT NULL | — | — |
| `page_to` | `smallint` | NOT NULL | — | — |
| `hizb_number` | `smallint` | NULL | — | FK, IDX |
| `juz_number` | `smallint` | NULL | — | FK, IDX |
| `rub_number` | `smallint` | NULL | — | FK, IDX |

### `public.quran_full_i3rab_ayah_entries`

| Column | Type | Nullability | Default | Role |
|---|---|---|---|---|
| `id` | `bigint` | NOT NULL | — | PK |
| `source_id` | `integer` | NOT NULL | — | FK, IDX |
| `ayah_id` | `integer` | NOT NULL | — | FK, IDX |
| `entry_id` | `bigint` | NOT NULL | — | FK, IDX |
| `verse_key` | `text` | NOT NULL | — | IDX |
| `source_value_kind` | `text` | NOT NULL | — | — |
| `source_leader_verse_key` | `text` | NOT NULL | — | — |
| `is_group_leader` | `boolean` | NOT NULL | — | — |
| `sort_order` | `integer` | NOT NULL | — | — |

### `public.quran_full_i3rab_entries`

| Column | Type | Nullability | Default | Role |
|---|---|---|---|---|
| `id` | `bigint` | NOT NULL | — | PK |
| `source_id` | `integer` | NOT NULL | — | FK, IDX |
| `source_entry_key` | `text` | NOT NULL | — | IDX |
| `leader_ayah_id` | `integer` | NOT NULL | — | FK, IDX |
| `i3rab_html` | `text` | NOT NULL | — | — |
| `covered_ayah_count` | `smallint` | NOT NULL | — | — |
| `covered_ayah_keys` | `jsonb` | NOT NULL | — | — |
| `source_shape` | `text` | NOT NULL | — | — |
| `text_hash` | `text` | NOT NULL | — | — |

### `public.quran_full_i3rab_sources`

| Column | Type | Nullability | Default | Role |
|---|---|---|---|---|
| `id` | `integer` | NOT NULL | — | PK |
| `source_key` | `text` | NOT NULL | — | IDX |
| `display_name_ar` | `text` | NOT NULL | — | — |
| `short_name_ar` | `text` | NOT NULL | — | — |
| `display_name_en` | `text` | NOT NULL | — | — |
| `short_name_en` | `text` | NOT NULL | — | — |
| `language_code` | `text` | NOT NULL | — | — |
| `direction` | `text` | NOT NULL | — | — |
| `contributor_name_ar` | `text` | NULL | — | — |
| `contributor_name_en` | `text` | NULL | — | — |
| `resource_kind` | `text` | NOT NULL | — | — |
| `markup_format` | `text` | NOT NULL | — | — |
| `has_quran_quotation_markup` | `boolean` | NOT NULL | — | — |
| `content_coverage_count` | `smallint` | NOT NULL | — | — |
| `package_file` | `text` | NOT NULL | — | IDX |
| `source_file_original` | `text` | NOT NULL | — | — |
| `sha256` | `text` | NOT NULL | — | — |
| `file_size_bytes` | `bigint` | NOT NULL | — | — |
| `license_status` | `text` | NOT NULL | — | — |
| `provenance_status` | `text` | NOT NULL | — | — |
| `usage_scope` | `text` | NOT NULL | — | — |
| `manifest_metadata` | `jsonb` | NULL | — | — |
| `imported_at_utc` | `timestamptz` | NOT NULL | — | — |

### `public.quran_hizbs`

| Column | Type | Nullability | Default | Role |
|---|---|---|---|---|
| `hizb_number` | `smallint` | NOT NULL | — | PK |
| `juz_number` | `smallint` | NOT NULL | — | FK, IDX |
| `verses_count` | `smallint` | NOT NULL | — | — |
| `first_ayah_id` | `integer` | NOT NULL | — | FK, IDX |
| `last_ayah_id` | `integer` | NOT NULL | — | FK, IDX |
| `first_verse_key` | `text` | NOT NULL | — | — |
| `last_verse_key` | `text` | NOT NULL | — | — |

### `public.quran_i3rab_rules`

| Column | Type | Nullability | Default | Role |
|---|---|---|---|---|
| `id` | `integer` | NOT NULL | — | PK |
| `signature_key` | `text` | NOT NULL | — | IDX |
| `rule_family` | `text` | NOT NULL | — | IDX |
| `i3rab_arabic` | `text` | NOT NULL | — | — |
| `default_status` | `text` | NOT NULL | — | — |
| `description` | `text` | NULL | — | — |
| `sort_order` | `smallint` | NOT NULL | — | — |

### `public.quran_juzs`

| Column | Type | Nullability | Default | Role |
|---|---|---|---|---|
| `juz_number` | `smallint` | NOT NULL | — | PK |
| `verses_count` | `smallint` | NOT NULL | — | — |
| `first_ayah_id` | `integer` | NOT NULL | — | FK, IDX |
| `last_ayah_id` | `integer` | NOT NULL | — | FK, IDX |
| `first_verse_key` | `text` | NOT NULL | — | — |
| `last_verse_key` | `text` | NOT NULL | — | — |

### `public.quran_lemmas`

| Column | Type | Nullability | Default | Role |
|---|---|---|---|---|
| `id` | `integer` | NOT NULL | — | PK |
| `lemma_text` | `text` | NOT NULL | — | IDX |
| `lemma_buckwalter` | `text` | NULL | — | — |
| `root_id` | `integer` | NULL | — | FK, IDX |
| `words_count` | `integer` | NOT NULL | — | — |
| `first_word_order_in_mushaf` | `integer` | NOT NULL | — | IDX |

### `public.quran_mushaf_lines`

| Column | Type | Nullability | Default | Role |
|---|---|---|---|---|
| `id` | `integer` | NOT NULL | — | PK |
| `page_number` | `smallint` | NOT NULL | — | FK, IDX |
| `line_number` | `smallint` | NOT NULL | — | IDX |
| `line_type` | `text` | NOT NULL | — | — |
| `is_centered` | `boolean` | NOT NULL | — | — |
| `surah_number` | `smallint` | NULL | — | FK, IDX |
| `first_word_id` | `integer` | NULL | — | FK, IDX |
| `last_word_id` | `integer` | NULL | — | FK, IDX |
| `words_count` | `smallint` | NOT NULL | — | — |

### `public.quran_mushaf_pages`

| Column | Type | Nullability | Default | Role |
|---|---|---|---|---|
| `page_number` | `smallint` | NOT NULL | — | PK |
| `first_surah_number` | `smallint` | NOT NULL | — | — |
| `first_ayah_number` | `smallint` | NOT NULL | — | — |
| `last_surah_number` | `smallint` | NOT NULL | — | — |
| `last_ayah_number` | `smallint` | NOT NULL | — | — |
| `lines_count` | `smallint` | NOT NULL | — | — |

### `public.quran_mutashabihat_groups`

| Column | Type | Nullability | Default | Role |
|---|---|---|---|---|
| `id` | `integer` | NOT NULL | — | PK |
| `source_group_id` | `integer` | NOT NULL | — | IDX |
| `representative_ayah_id` | `integer` | NOT NULL | — | FK, IDX |
| `representative_word_from` | `smallint` | NOT NULL | — | — |
| `representative_word_to` | `smallint` | NOT NULL | — | — |
| `occurrence_count` | `smallint` | NOT NULL | — | — |
| `distinct_ayah_count` | `smallint` | NOT NULL | — | — |
| `distinct_surah_count` | `smallint` | NOT NULL | — | — |
| `raw_source_counts` | `jsonb` | NULL | — | — |

### `public.quran_mutashabihat_occurrences`

| Column | Type | Nullability | Default | Role |
|---|---|---|---|---|
| `id` | `integer` | NOT NULL | — | PK |
| `group_id` | `integer` | NOT NULL | — | FK, IDX |
| `ayah_id` | `integer` | NOT NULL | — | FK, IDX |
| `word_from` | `smallint` | NOT NULL | — | IDX |
| `word_to` | `smallint` | NOT NULL | — | IDX |
| `is_representative` | `boolean` | NOT NULL | `false` | — |

### `public.quran_pos_tags`

| Column | Type | Nullability | Default | Role |
|---|---|---|---|---|
| `code` | `text` | NOT NULL | — | PK |
| `arabic_label` | `text` | NOT NULL | — | — |
| `english_label` | `text` | NOT NULL | — | — |
| `category` | `text` | NOT NULL | — | IDX |
| `sort_order` | `smallint` | NOT NULL | — | IDX |
| `description` | `text` | NULL | — | — |

### `public.quran_roots`

| Column | Type | Nullability | Default | Role |
|---|---|---|---|---|
| `id` | `integer` | NOT NULL | — | PK |
| `root_text` | `text` | NOT NULL | — | IDX |
| `root_buckwalter` | `text` | NULL | — | — |
| `words_count` | `integer` | NOT NULL | — | IDX |
| `distinct_lemmas_count` | `smallint` | NOT NULL | — | — |
| `first_word_order_in_mushaf` | `integer` | NOT NULL | — | IDX |

### `public.quran_rubs`

| Column | Type | Nullability | Default | Role |
|---|---|---|---|---|
| `rub_number` | `smallint` | NOT NULL | — | PK |
| `hizb_number` | `smallint` | NOT NULL | — | FK, IDX |
| `verses_count` | `smallint` | NOT NULL | — | — |
| `first_ayah_id` | `integer` | NOT NULL | — | FK, IDX |
| `last_ayah_id` | `integer` | NOT NULL | — | FK, IDX |
| `first_verse_key` | `text` | NOT NULL | — | — |
| `last_verse_key` | `text` | NOT NULL | — | — |

### `public.quran_sajdas`

| Column | Type | Nullability | Default | Role |
|---|---|---|---|---|
| `sajdah_number` | `smallint` | NOT NULL | — | PK |
| `ayah_id` | `integer` | NOT NULL | — | FK, IDX |
| `verse_key` | `text` | NOT NULL | — | — |
| `sajdah_type` | `text` | NOT NULL | — | — |

### `public.quran_similar_ayah_links`

| Column | Type | Nullability | Default | Role |
|---|---|---|---|---|
| `id` | `integer` | NOT NULL | — | PK |
| `source_ayah_id` | `integer` | NOT NULL | — | FK, IDX |
| `target_ayah_id` | `integer` | NOT NULL | — | FK, IDX |
| `score` | `smallint` | NOT NULL | — | — |
| `coverage` | `smallint` | NOT NULL | — | — |
| `matched_words_count` | `smallint` | NOT NULL | — | — |
| `match_words` | `jsonb` | NOT NULL | — | — |

### `public.quran_stems`

| Column | Type | Nullability | Default | Role |
|---|---|---|---|---|
| `id` | `integer` | NOT NULL | — | PK |
| `stem_text` | `text` | NOT NULL | — | IDX |
| `words_count` | `integer` | NOT NULL | — | — |
| `first_word_order_in_mushaf` | `integer` | NOT NULL | — | IDX |

### `public.quran_surahs`

| Column | Type | Nullability | Default | Role |
|---|---|---|---|---|
| `surah_number` | `smallint` | NOT NULL | — | PK |
| `name_arabic` | `text` | NOT NULL | — | IDX |
| `name_simple` | `text` | NOT NULL | — | — |
| `name_transliteration` | `text` | NOT NULL | — | — |
| `revelation_place` | `text` | NOT NULL | — | — |
| `revelation_order` | `smallint` | NOT NULL | — | — |
| `verses_count` | `smallint` | NOT NULL | — | — |
| `bismillah_pre` | `boolean` | NOT NULL | — | — |

### `public.quran_tafsir_ayah_entries`

| Column | Type | Nullability | Default | Role |
|---|---|---|---|---|
| `id` | `bigint` | NOT NULL | — | PK |
| `source_id` | `integer` | NOT NULL | — | FK, IDX |
| `ayah_id` | `integer` | NOT NULL | — | FK, IDX |
| `tafsir_entry_id` | `bigint` | NOT NULL | — | FK, IDX |
| `verse_key` | `text` | NOT NULL | — | IDX |
| `source_value_kind` | `text` | NOT NULL | — | — |
| `source_leader_verse_key` | `text` | NOT NULL | — | — |
| `is_group_leader` | `boolean` | NOT NULL | — | — |
| `sort_order` | `integer` | NOT NULL | — | — |

### `public.quran_tafsir_entries`

| Column | Type | Nullability | Default | Role |
|---|---|---|---|---|
| `id` | `bigint` | NOT NULL | — | PK |
| `source_id` | `integer` | NOT NULL | — | FK, IDX |
| `source_entry_key` | `text` | NOT NULL | — | IDX |
| `leader_ayah_id` | `integer` | NOT NULL | — | FK, IDX |
| `tafsir_text` | `text` | NOT NULL | — | — |
| `covered_ayah_count` | `smallint` | NOT NULL | — | — |
| `covered_ayah_keys` | `jsonb` | NOT NULL | — | — |
| `source_shape` | `text` | NOT NULL | — | — |
| `text_hash` | `text` | NOT NULL | — | — |

### `public.quran_tafsir_sources`

| Column | Type | Nullability | Default | Role |
|---|---|---|---|---|
| `id` | `integer` | NOT NULL | — | PK |
| `source_key` | `text` | NOT NULL | — | IDX |
| `language_code` | `text` | NOT NULL | — | IDX |
| `language_name_ar` | `text` | NOT NULL | — | — |
| `language_name_en` | `text` | NOT NULL | — | — |
| `direction` | `text` | NOT NULL | — | — |
| `display_name_ar` | `text` | NOT NULL | — | — |
| `short_name_ar` | `text` | NOT NULL | — | — |
| `display_name_en` | `text` | NOT NULL | — | — |
| `short_name_en` | `text` | NOT NULL | — | — |
| `contributor_key` | `text` | NULL | — | — |
| `contributor_name_ar` | `text` | NULL | — | — |
| `contributor_name_en` | `text` | NULL | — | — |
| `contributor_type` | `text` | NOT NULL | — | — |
| `resource_kind` | `text` | NOT NULL | — | — |
| `tafsir_kind` | `text` | NOT NULL | — | IDX |
| `content_coverage_count` | `smallint` | NOT NULL | — | — |
| `package_file` | `text` | NOT NULL | — | IDX |
| `source_file_original` | `text` | NOT NULL | — | — |
| `sha256` | `text` | NOT NULL | — | — |
| `file_size_bytes` | `bigint` | NOT NULL | — | — |
| `license_status` | `text` | NOT NULL | — | — |
| `provenance_status` | `text` | NOT NULL | — | — |
| `manifest_metadata` | `jsonb` | NULL | — | — |
| `imported_at_utc` | `timestamptz` | NOT NULL | — | — |

### `public.quran_translation_ayah_entries`

| Column | Type | Nullability | Default | Role |
|---|---|---|---|---|
| `id` | `bigint` | NOT NULL | — | PK |
| `source_id` | `integer` | NOT NULL | — | FK, IDX |
| `ayah_id` | `integer` | NOT NULL | — | FK, IDX |
| `verse_key` | `text` | NULL | — | — |
| `text` | `text` | NOT NULL | — | — |

### `public.quran_translation_sources`

| Column | Type | Nullability | Default | Role |
|---|---|---|---|---|
| `id` | `integer` | NOT NULL | — | PK |
| `source_key` | `text` | NOT NULL | — | IDX |
| `language_code` | `text` | NOT NULL | — | IDX |
| `language_name_en` | `text` | NOT NULL | — | — |
| `language_name_ar` | `text` | NOT NULL | — | — |
| `native_name` | `text` | NULL | — | — |
| `direction` | `text` | NOT NULL | — | — |
| `translation_type` | `text` | NOT NULL | — | IDX |
| `display_name_en` | `text` | NOT NULL | — | — |
| `display_name_ar` | `text` | NOT NULL | — | — |
| `translator_key` | `text` | NULL | — | — |
| `translator_name_en` | `text` | NULL | — | — |
| `translator_name_ar` | `text` | NULL | — | — |
| `contains_inline_footnotes` | `boolean` | NOT NULL | — | — |
| `contains_html_markup` | `boolean` | NOT NULL | — | — |
| `content_coverage_count` | `smallint` | NOT NULL | — | — |

### `public.quran_word_morphology`

| Column | Type | Nullability | Default | Role |
|---|---|---|---|---|
| `quran_word_id` | `integer` | NOT NULL | — | PK, FK |
| `location` | `text` | NOT NULL | — | — |
| `head_pos` | `text` | NOT NULL | — | FK, IDX |
| `segment_count` | `smallint` | NOT NULL | — | — |
| `root_id` | `integer` | NULL | — | FK, IDX |
| `lemma_id` | `integer` | NULL | — | FK, IDX |
| `stem_id` | `integer` | NULL | — | FK, IDX |
| `is_verb` | `boolean` | NOT NULL | — | — |
| `verb_tense` | `text` | NULL | — | IDX (partial: where `is_verb`) |
| `verb_voice` | `text` | NULL | — | IDX (partial: where `is_verb`) |
| `case_feature` | `text` | NULL | — | IDX |
| `head_features_json` | `jsonb` | NULL | — | — |

### `public.quran_word_morphology_segments`

| Column | Type | Nullability | Default | Role |
|---|---|---|---|---|
| `id` | `integer` | NOT NULL | — | PK |
| `quran_word_id` | `integer` | NOT NULL | — | FK, IDX |
| `segment_location` | `text` | NOT NULL | — | — |
| `segment_number` | `smallint` | NOT NULL | — | IDX |
| `kind` | `text` | NOT NULL | — | IDX (partial: where `kind = 'STEM'`) |
| `pos` | `text` | NOT NULL | — | FK, IDX |
| `form_buckwalter` | `text` | NOT NULL | — | — |
| `form_arabic_normalized` | `text` | NULL | — | — |
| `arabic_render_tier` | `text` | NULL | — | IDX |
| `arabic_render_source` | `text` | NOT NULL | — | — |
| `root_buckwalter` | `text` | NULL | — | — |
| `lemma_buckwalter` | `text` | NULL | — | — |
| `features_raw` | `text` | NOT NULL | — | — |
| `features_json` | `jsonb` | NULL | — | — |
| `i3rab_arabic` | `text` | NULL | — | — |
| `i3rab_review_reason` | `text` | NULL | — | — |
| `i3rab_rule_id` | `integer` | NULL | — | FK, IDX |
| `i3rab_status` | `text` | NOT NULL | `'unsupported'::text` | — |
| `lemma_id` | `integer` | NULL | — | FK, IDX |
| `root_id` | `integer` | NULL | — | FK, IDX |
| `stem_id` | `integer` | NULL | — | FK, IDX |

### `public.quran_words`

| Column | Type | Nullability | Default | Role |
|---|---|---|---|---|
| `id` | `integer` | NOT NULL | — | PK |
| `location` | `text` | NOT NULL | — | IDX |
| `ayah_id` | `integer` | NOT NULL | — | FK, IDX |
| `surah_number` | `smallint` | NOT NULL | — | IDX |
| `ayah_number` | `smallint` | NOT NULL | — | IDX |
| `word_number` | `smallint` | NOT NULL | — | IDX |
| `page_number` | `smallint` | NOT NULL | — | FK, IDX |
| `line_number` | `smallint` | NOT NULL | — | IDX |
| `line_word_order` | `smallint` | NOT NULL | — | IDX |
| `qpc_glyph` | `text` | NOT NULL | — | — |
| `text_uthmani` | `text` | NOT NULL | — | — |
| `text_uthmani_simple` | `text` | NOT NULL | — | — |
| `text_imlaei_simple` | `text` | NOT NULL | — | — |
| `is_ayah_marker` | `boolean` | NOT NULL | — | — |
| `word_key_imlaei_simple` | `text` | NOT NULL | `''::text` | IDX (partial: where not marker) |
| `unique_simple_word_id` | `integer` | NULL | — | IDX (partial) |
| `unique_tashkeel_word_id` | `integer` | NULL | — | IDX (partial) |

### `public.quran_words_ordered_simple`

| Column | Type | Nullability | Default | Role |
|---|---|---|---|---|
| `word_order_in_mushaf` | `integer` | NOT NULL | — | PK |
| `quran_word_id` | `integer` | NOT NULL | — | FK, IDX |
| `location` | `text` | NOT NULL | — | — |
| `verse_key` | `text` | NOT NULL | — | — |
| `surah_number` | `smallint` | NOT NULL | — | IDX |
| `ayah_number` | `smallint` | NOT NULL | — | IDX |
| `page_number` | `smallint` | NOT NULL | — | — |
| `line_number` | `smallint` | NOT NULL | — | — |
| `word_order_in_ayah` | `smallint` | NOT NULL | — | IDX |
| `word_order_in_surah` | `smallint` | NOT NULL | — | IDX |
| `text_uthmani_simple` | `text` | NOT NULL | — | — |
| `text_imlaei_simple` | `text` | NOT NULL | — | — |
| `occurrences_count` | `integer` | NOT NULL | — | — |
| `ayahs_count` | `smallint` | NOT NULL | — | — |
| `surahs_count` | `smallint` | NOT NULL | — | — |
| `word_key_imlaei_simple` | `text` | NOT NULL | `''::text` | — |

### `public.quran_words_ordered_tashkeel`

| Column | Type | Nullability | Default | Role |
|---|---|---|---|---|
| `word_order_in_mushaf` | `integer` | NOT NULL | — | PK |
| `quran_word_id` | `integer` | NOT NULL | — | FK, IDX |
| `location` | `text` | NOT NULL | — | — |
| `verse_key` | `text` | NOT NULL | — | — |
| `surah_number` | `smallint` | NOT NULL | — | IDX |
| `ayah_number` | `smallint` | NOT NULL | — | IDX |
| `page_number` | `smallint` | NOT NULL | — | — |
| `line_number` | `smallint` | NOT NULL | — | — |
| `word_order_in_ayah` | `smallint` | NOT NULL | — | IDX |
| `word_order_in_surah` | `smallint` | NOT NULL | — | IDX |
| `text_uthmani` | `text` | NOT NULL | — | — |
| `text_uthmani_simple` | `text` | NOT NULL | — | — |
| `text_imlaei_simple` | `text` | NOT NULL | — | — |
| `occurrences_count` | `integer` | NOT NULL | — | — |
| `ayahs_count` | `smallint` | NOT NULL | — | — |
| `surahs_count` | `smallint` | NOT NULL | — | — |

### `public.quran_words_unique_simple`

| Column | Type | Nullability | Default | Role |
|---|---|---|---|---|
| `id` | `integer` | NOT NULL | — | PK |
| `text_uthmani_simple` | `text` | NOT NULL | — | — |
| `text_imlaei_simple` | `text` | NOT NULL | — | — |
| `occurrences_count` | `integer` | NOT NULL | — | — |
| `ayahs_count` | `smallint` | NOT NULL | — | — |
| `surahs_count` | `smallint` | NOT NULL | — | — |
| `first_quran_word_id` | `integer` | NOT NULL | — | FK, IDX |
| `first_location` | `text` | NOT NULL | — | — |
| `first_surah_number` | `smallint` | NOT NULL | — | — |
| `first_ayah_number` | `smallint` | NOT NULL | — | — |
| `first_word_order_in_mushaf` | `integer` | NOT NULL | — | IDX |
| `first_page_number` | `smallint` | NOT NULL | — | — |
| `first_line_number` | `smallint` | NOT NULL | — | — |
| `qpc_glyph` | `text` | NOT NULL | `''::text` | — |
| `text_uthmani` | `text` | NOT NULL | `''::text` | — |
| `word_key_imlaei_simple` | `text` | NOT NULL | `''::text` | IDX |

### `public.quran_words_unique_tashkeel`

| Column | Type | Nullability | Default | Role |
|---|---|---|---|---|
| `id` | `integer` | NOT NULL | — | PK |
| `text_uthmani` | `text` | NOT NULL | — | IDX |
| `text_uthmani_simple` | `text` | NOT NULL | — | — |
| `text_imlaei_simple` | `text` | NOT NULL | — | — |
| `occurrences_count` | `integer` | NOT NULL | — | — |
| `ayahs_count` | `smallint` | NOT NULL | — | — |
| `surahs_count` | `smallint` | NOT NULL | — | — |
| `first_quran_word_id` | `integer` | NOT NULL | — | FK, IDX |
| `first_location` | `text` | NOT NULL | — | — |
| `first_surah_number` | `smallint` | NOT NULL | — | — |
| `first_ayah_number` | `smallint` | NOT NULL | — | — |
| `first_word_order_in_mushaf` | `integer` | NOT NULL | — | IDX |
| `first_page_number` | `smallint` | NOT NULL | — | — |
| `first_line_number` | `smallint` | NOT NULL | — | — |

## 4. Constraints and indexes

`NOT NULL` is represented in the column inventory (§3). Below: primary keys, foreign keys (with on-delete
action), explicit domain checks, and indexes (unique/partial noted). 130 indexes total; 7 are partial.

### `__EFMigrationsHistory`
- PK: `MigrationId`. FKs: none. Checks: none. Indexes: PK only.

### `quran_ayahs`
- PK: `id`.
- FKs: `surah_number → quran_surahs` (cascade); `juz_number → quran_juzs` (restrict); `hizb_number → quran_hizbs` (restrict); `rub_number → quran_rubs` (restrict).
- Checks: none.
- Indexes: PK; unique (`surah_number`,`ayah_number`); unique (`verse_key`); (`juz_number`); (`hizb_number`); (`rub_number`).

### `quran_full_i3rab_ayah_entries`
- PK: `id`.
- FKs: `source_id → quran_full_i3rab_sources` (cascade); `ayah_id → quran_ayahs` (cascade); `entry_id → quran_full_i3rab_entries` (cascade).
- Checks: `source_value_kind ∈ {leader, member_pointer, flat}`.
- Indexes: PK; (`ayah_id`,`source_id`); (`entry_id`); unique (`source_id`,`ayah_id`); unique (`source_id`,`verse_key`).

### `quran_full_i3rab_entries`
- PK: `id`.
- FKs: `source_id → quran_full_i3rab_sources` (cascade); `leader_ayah_id → quran_ayahs` (cascade).
- Checks: `covered_ayah_count ≥ 1`; `i3rab_html <> ''`; `source_shape ∈ {grouped_leader, flat}`.
- Indexes: PK; (`leader_ayah_id`); (`source_id`,`leader_ayah_id`); unique (`source_id`,`source_entry_key`).

### `quran_full_i3rab_sources`
- PK: `id`.
- FKs: none.
- Checks: `content_coverage_count = 6236`; `direction ∈ {rtl, ltr}`; `language_code = 'ar'`; `license_status = 'unknown'`; `markup_format = 'html'`; `provenance_status = 'unknown'`; `resource_kind = 'full_i3rab'`; `usage_scope = 'internal-only-until-cleared'`.
- Indexes: PK; unique (`package_file`); unique (`source_key`).

### `quran_hizbs`
- PK: `hizb_number`.
- FKs: `juz_number → quran_juzs` (restrict); `first_ayah_id → quran_ayahs` (restrict); `last_ayah_id → quran_ayahs` (restrict).
- Checks: none.
- Indexes: PK; (`first_ayah_id`); (`juz_number`); (`last_ayah_id`).

### `quran_i3rab_rules`
- PK: `id`.
- FKs: none.
- Checks: `default_status ∈ {approved, needs_review, unsupported}`.
- Indexes: PK; unique (`signature_key`); (`rule_family`).

### `quran_juzs`
- PK: `juz_number`.
- FKs: `first_ayah_id → quran_ayahs` (restrict); `last_ayah_id → quran_ayahs` (restrict).
- Checks: none.
- Indexes: PK; (`first_ayah_id`); (`last_ayah_id`).

### `quran_lemmas`
- PK: `id`.
- FKs: `root_id → quran_roots` (no action).
- Checks: none.
- Indexes: PK; unique (`lemma_text`); unique (`first_word_order_in_mushaf`); (`root_id`).

### `quran_mushaf_lines`
- PK: `id`.
- FKs: `page_number → quran_mushaf_pages` (cascade); `surah_number → quran_surahs` (no action); `first_word_id → quran_words` (no action); `last_word_id → quran_words` (no action).
- Checks: none.
- Indexes: PK; unique (`page_number`,`line_number`); (`first_word_id`); (`last_word_id`); (`surah_number`).

### `quran_mushaf_pages`
- PK: `page_number`. FKs: none. Checks: none. Indexes: PK only.

### `quran_mutashabihat_groups`
- PK: `id`.
- FKs: `representative_ayah_id → quran_ayahs` (cascade).
- Checks: none.
- Indexes: PK; (`representative_ayah_id`); unique (`source_group_id`).

### `quran_mutashabihat_occurrences`
- PK: `id`.
- FKs: `group_id → quran_mutashabihat_groups` (cascade); `ayah_id → quran_ayahs` (cascade).
- Checks: none.
- Indexes: PK; (`ayah_id`); unique (`group_id`,`ayah_id`,`word_from`,`word_to`).

### `quran_pos_tags`
- PK: `code`. FKs: none. Checks: none. Indexes: PK; (`category`); (`sort_order`).

### `quran_roots`
- PK: `id`. FKs: none. Checks: none.
- Indexes: PK; unique (`root_text`); unique (`first_word_order_in_mushaf`); (`words_count`).

### `quran_rubs`
- PK: `rub_number`.
- FKs: `hizb_number → quran_hizbs` (restrict); `first_ayah_id → quran_ayahs` (restrict); `last_ayah_id → quran_ayahs` (restrict).
- Checks: none.
- Indexes: PK; (`first_ayah_id`); (`hizb_number`); (`last_ayah_id`).

### `quran_sajdas`
- PK: `sajdah_number`.
- FKs: `ayah_id → quran_ayahs` (restrict).
- Checks: none.
- Indexes: PK; unique (`ayah_id`).

### `quran_similar_ayah_links`
- PK: `id`.
- FKs: `source_ayah_id → quran_ayahs` (cascade); `target_ayah_id → quran_ayahs` (cascade).
- Checks: `source_ayah_id <> target_ayah_id` (no self-link).
- Indexes: PK; unique (`source_ayah_id`,`target_ayah_id`); (`target_ayah_id`).

### `quran_stems`
- PK: `id`. FKs: none. Checks: none.
- Indexes: PK; unique (`stem_text`); unique (`first_word_order_in_mushaf`).

### `quran_surahs`
- PK: `surah_number`. FKs: none. Checks: none. Indexes: PK; unique (`name_arabic`).

### `quran_tafsir_ayah_entries`
- PK: `id`.
- FKs: `source_id → quran_tafsir_sources` (cascade); `ayah_id → quran_ayahs` (cascade); `tafsir_entry_id → quran_tafsir_entries` (cascade).
- Checks: `source_value_kind ∈ {leader, member_pointer, flat}`.
- Indexes: PK; (`ayah_id`,`source_id`); unique (`source_id`,`ayah_id`); unique (`source_id`,`verse_key`); (`tafsir_entry_id`).

### `quran_tafsir_entries`
- PK: `id`.
- FKs: `source_id → quran_tafsir_sources` (cascade); `leader_ayah_id → quran_ayahs` (cascade).
- Checks: `covered_ayah_count ≥ 1`; `source_shape ∈ {grouped_leader, flat}`; `tafsir_text <> ''`.
- Indexes: PK; (`leader_ayah_id`); (`source_id`,`leader_ayah_id`); unique (`source_id`,`source_entry_key`).

### `quran_tafsir_sources`
- PK: `id`.
- FKs: none.
- Checks: `content_coverage_count = 6236`; `direction ∈ {rtl, ltr}`; `resource_kind = 'tafsir'`.
- Indexes: PK; (`language_code`); (`language_code`,`tafsir_kind`); unique (`package_file`); unique (`source_key`).

### `quran_translation_ayah_entries`
- PK: `id`.
- FKs: `source_id → quran_translation_sources` (cascade); `ayah_id → quran_ayahs` (cascade).
- Checks: `text <> ''`.
- Indexes: PK; (`ayah_id`,`source_id`); unique (`source_id`,`ayah_id`).

### `quran_translation_sources`
- PK: `id`.
- FKs: none.
- Checks: `content_coverage_count = 6236`; `direction ∈ {rtl, ltr}`; `translation_type ∈ {simple, with_footnotes}`; required-fields non-empty (`source_key`, `language_code`, `language_name_en/ar`, `direction`, `translation_type`, `display_name_en/ar`).
- Indexes: PK; (`language_code`); (`language_code`,`translation_type`); unique (`source_key`).

### `quran_word_morphology`
- PK: `quran_word_id`.
- FKs: `quran_word_id → quran_words` (cascade); `head_pos → quran_pos_tags` (restrict); `root_id → quran_roots` (no action); `lemma_id → quran_lemmas` (no action); `stem_id → quran_stems` (no action).
- Checks: none.
- Indexes: PK / unique (`quran_word_id`); (`head_pos`); (`root_id`); (`lemma_id`); (`stem_id`); (`case_feature`); partial (`verb_tense`) where `is_verb`; partial (`verb_voice`) where `is_verb`.

### `quran_word_morphology_segments`
- PK: `id`.
- FKs: `quran_word_id → quran_words` (cascade); `pos → quran_pos_tags` (restrict); `i3rab_rule_id → quran_i3rab_rules` (restrict); `root_id → quran_roots` (restrict); `lemma_id → quran_lemmas` (restrict); `stem_id → quran_stems` (restrict).
- Checks: `i3rab_status ∈ {approved, needs_review, unsupported}`.
- Indexes: PK; unique (`quran_word_id`,`segment_number`); (`pos`); (`i3rab_rule_id`); (`arabic_render_tier`); (`root_id`); (`lemma_id`); (`stem_id`); partial (`quran_word_id`) where `kind = 'STEM'`.

### `quran_words`
- PK: `id`.
- FKs: `ayah_id → quran_ayahs` (cascade); `page_number → quran_mushaf_pages` (cascade).
- Checks: none.
- Indexes: PK; unique (`location`); (`ayah_id`); (`page_number`,`line_number`,`line_word_order`); (`surah_number`,`ayah_number`,`word_number`); partial readable (`surah_number`,`ayah_number`,`word_number`) where not marker; partial (`word_key_imlaei_simple`) where not marker; partial (`unique_simple_word_id`) where readable & non-null; partial (`unique_tashkeel_word_id`) where readable & non-null.

### `quran_words_ordered_simple`
- PK: `word_order_in_mushaf`.
- FKs: `quran_word_id → quran_words` (cascade).
- Checks: none.
- Indexes: PK; unique (`quran_word_id`); (`surah_number`,`ayah_number`,`word_order_in_ayah`); (`surah_number`,`word_order_in_surah`).

### `quran_words_ordered_tashkeel`
- PK: `word_order_in_mushaf`.
- FKs: `quran_word_id → quran_words` (cascade).
- Checks: none.
- Indexes: PK; unique (`quran_word_id`); (`surah_number`,`ayah_number`,`word_order_in_ayah`); (`surah_number`,`word_order_in_surah`).

### `quran_words_unique_simple`
- PK: `id`.
- FKs: `first_quran_word_id → quran_words` (cascade).
- Checks: none.
- Indexes: PK; unique (`word_key_imlaei_simple`); unique (`first_word_order_in_mushaf`); (`first_quran_word_id`).

### `quran_words_unique_tashkeel`
- PK: `id`.
- FKs: `first_quran_word_id → quran_words` (cascade).
- Checks: none.
- Indexes: PK; unique (`text_uthmani`); unique (`first_word_order_in_mushaf`); (`first_quran_word_id`).

## 5. EF mapping cross-check

- All 31 application tables resolve to the `public` schema (EF `ToTable(...)` calls do not specify a schema);
  `__EFMigrationsHistory` is EF infrastructure, not a domain `DbSet`.
- `__EFMigrationsHistory` holds **15** rows — one per applied migration (`QuranFoundationSchema` … `AddSegmentStemId`).
  See `database-reset-and-seeding-order.md` §2 for the ordered migration list and feature attribution.
- Raw SQL ownership by importer pipeline (unchanged verb set; see the seeding runbook §3):
  - Foundation import writes `quran_surahs`, `quran_ayahs`, `quran_mushaf_pages`, `quran_words`, `quran_mushaf_lines`.
  - Display word rebuild derives `quran_words_ordered_*`, `quran_words_unique_*`, and updates `quran_words.unique_*_word_id` (Feature 013 assigns the unique ids deterministically — no `IDENTITY`).
  - Morphology import writes `quran_pos_tags`, `quran_roots`, `quran_lemmas`, `quran_stems`, `quran_word_morphology`, `quran_word_morphology_segments`, including the segment `root_id`/`lemma_id` (017) and `stem_id` (018) dimension ids.
  - I'rab generation upserts `quran_i3rab_rules` and updates `quran_word_morphology_segments.i3rab_*`.
  - Mutashabihat (006), tafsir (007), translations (008), navigation divisions (009), full-i3rab (010), and ayah similarities (012) each own their `quran_*` table family.

## 6. Potential cleanup candidates

No deletion is recommended from this inventory alone. This pass refreshed the structural catalog
(tables/columns/indexes/FKs/checks) but did **not** re-run per-column null-distribution profiling for the
new (006–018) tables; candidates that depend on value distribution are carried forward from the
2026-06-13 revision and must be re-profiled before any action.

| Candidate | Why it may be unnecessary | Risk | Must verify before deleting |
|---|---|---|---|
| `quran_word_morphology_segments.i3rab_review_reason` | Carried no data in the prior profile. | Medium | Re-profile null distribution; confirm `needs_review`/`unsupported` explanations are never persisted. |
| `quran_word_morphology_segments.arabic_render_source` | Prior profile uniform (`buckwalter-transliteration`); `MorphologySql.CheckSegSourceValid` validates the exact value. | High | Confirm no future render source; update validation if removed. |
| `quran_ayahs.words_count_source` / `words_count_real` | Look like source-vs-derived audit counts. | Medium | Verify import validation/reporting needs. |
| `quran_ayahs.page_from` / `page_to` | Page span derivable from layout. | Medium | Verify UI/API page-navigation and reconciliation needs. |
| `quran_word_morphology.location` | Duplicates `quran_words.location` via `quran_word_id`; `MorphologySql.CheckLocationIdMismatch` validates equality. | High | Confirm importer diagnostics do not need it; update mismatch check if removed. |
| `quran_words_ordered_*` / `quran_words_unique_*` denormalized columns | Read-model duplication of `quran_words`/`quran_ayahs` plus computed counts. | High | Identify read-path latency/rebuild-cost needs before touching. |
| `quran_words.unique_simple_word_id` / `unique_tashkeel_word_id` | Link-cache columns to derived unique tables; null for ayah markers (6,236 of 83,668). | High | Confirm no O(1) canonical→unique jump is needed. |

Zero-row tables: none. Smallest populated tables are `quran_full_i3rab_sources` (4) and `quran_sajdas` (15).

## 7. Potential schema split proposal

Future organization proposal only — do not implement without a migration plan, EF schema-mapping plan, and
raw-SQL rewrite plan. Now that the content families have grown, candidate schemas are clearer:

| Proposed schema | Tables | Why |
|---|---|---|
| `quran_core` | `quran_surahs`, `quran_ayahs`, `quran_mushaf_pages`, `quran_mushaf_lines`, `quran_words` | Canonical structure, tokens, layout. |
| `quran_navigation` | `quran_juzs`, `quran_hizbs`, `quran_rubs`, `quran_sajdas` | Division metadata referencing `quran_ayahs`. |
| `quran_word_models` | `quran_words_ordered_*`, `quran_words_unique_*` | Derived read-model word identity/ordering. |
| `quran_morphology` | `quran_pos_tags`, `quran_roots`, `quran_lemmas`, `quran_stems`, `quran_word_morphology`, `quran_word_morphology_segments` | Morphology dimensions + segment data. |
| `quran_i3rab` | `quran_i3rab_rules`, `quran_full_i3rab_sources`, `quran_full_i3rab_entries`, `quran_full_i3rab_ayah_entries` | Generated-rule catalogue + full-i'rab content. |
| `quran_content` | `quran_tafsir_*`, `quran_translation_*`, `quran_mutashabihat_*`, `quran_similar_ayah_links` | Per-ayah scholarly/derived content families. |

Risk is high across the board because bulk `COPY`, validation SQL, and EF configurations embed bare table
names; cross-schema FKs to `quran_ayahs`/`quran_words` would all need qualification.

## 8. Final recommendation

- Physical schema splitting: **later, not now.** The DB is cohesive; the dominant risk remains raw-SQL and EF
  mapping churn. Revisit once module read/write ownership is stable.
- Obvious safe column deletions now: **none.** Several columns are denormalized or (in the prior profile)
  all-null/uniform, but they are tied to imports, validation, reports, or read-model performance.
- Next deeper-review items: re-profile null/value distributions for the 006–018 tables; confirm the
  `content_coverage_count = 6236` invariant across all three content families; review render-provenance and
  i'rab review columns before any cleanup; map the raw-SQL table-name surface before any schema split.

## Verification

### Commands used

All database commands were read-only catalog/data reads (`SELECT` only); the password was passed via the
`PGPASSWORD` environment variable, not embedded in any command or written to this report:

```bash
PGPASSWORD=[REDACTED] psql -h localhost -p 5432 -U postgres -d quran_dashboard -A -F '|' -t -c "SELECT ... base tables + pk + column counts ..."
PGPASSWORD=[REDACTED] psql -h localhost -p 5432 -U postgres -d quran_dashboard -A -F '|' -t -c "SELECT count(*) ... exact per-table row counts (UNION ALL) ..."
PGPASSWORD=[REDACTED] psql -h localhost -p 5432 -U postgres -d quran_dashboard -A -F '|' -t -c "SELECT ... summary metrics: tables/columns/indexes/fk/unique/checks/partial ..."
PGPASSWORD=[REDACTED] psql -h localhost -p 5432 -U postgres -d quran_dashboard -A -F '|' -t -c "SELECT ... explicit CHECK constraint defs + all FK defs (pg_get_constraintdef) ..."
PGPASSWORD=[REDACTED] psql -h localhost -p 5432 -U postgres -d quran_dashboard -A -F '|' -t -c "SELECT ... per-column type/nullability/default (pg_attribute/format_type) ..."
PGPASSWORD=[REDACTED] psql -h localhost -p 5432 -U postgres -d quran_dashboard -A -F '|' -t -c "SELECT ... all index definitions (pg_indexes) ..."
```

EF inspection used read/search tools over `Migrations/`, `Program.cs`, and EF configurations. No source files
were edited during this inventory.

### Safety confirmation

- Database data changed: **No.** Only read-only `SELECT` catalog/data reads were executed.
- Migrations created or modified: **No.**
- Source code changed: **No.**
- Files intentionally created/updated: `Backend/report/database-inventory/current-database-inventory.md` only.
