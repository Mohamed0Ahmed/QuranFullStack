# Current Local PostgreSQL Database Inventory

Date: 2026-06-13  
Database: `quran_dashboard` on `localhost:5432`  
Scope: read-only PostgreSQL catalog/data inventory plus EF mapping inspection.  
Password handling: commands below are shown with `PGPASSWORD=***`; no password was written to this report.

## 1. Executive summary

| Metric | Count | Notes |
|---|---:|---|
| Application schemas currently used | 1 | Only `public` for application tables. |
| Tables | 17 | 16 Quran/domain tables + EF `__EFMigrationsHistory`. |
| Columns | 172 | Includes EF migration history columns. |
| Indexes | 69 | Includes PK-backed indexes and 8 partial indexes. |
| Foreign keys | 20 | All are within `public`. |
| Unique constraints | 0 | Uniqueness is enforced through unique indexes, not separate `UNIQUE` table constraints. |
| Check constraints | 149 | Mostly PostgreSQL/EF-reported `NOT NULL`; explicit domain checks are `quran_i3rab_rules.default_status` and `quran_word_morphology_segments.i3rab_status`. |

Very large / high-volume tables by exact row count:

- `quran_word_morphology_segments`: 128,219 rows.
- `quran_words`: 83,668 rows.
- `quran_word_morphology`, `quran_words_ordered_simple`, `quran_words_ordered_tashkeel`: 77,432 rows each.

Obvious naming inconsistencies to review later, not necessarily defects:

- All domain tables are in `public` and carry a `quran_` prefix; if future schemas are introduced, the prefix may become partially redundant.
- Mixed transliteration conventions appear in column names: `i3rab`, `imlaei`, `uthmani`, `qpc`, `buckwalter`.
- Count naming varies by concept: `words_count_*`, `verses_count`, `lines_count`, `occurrences_count`, `ayahs_count`, `surahs_count`.
- `head_pos` in `quran_word_morphology` and `pos` in `quran_word_morphology_segments` are closely related but named differently because one is word-level and the other is segment-level.
- EF infrastructure table `__EFMigrationsHistory` uses PascalCase columns (`MigrationId`, `ProductVersion`) while app tables use snake_case.

## 2. Tables inventory

| Schema | Table | Rows | Primary key | Columns | Short inferred purpose |
|---|---|---:|---|---:|---|
| public | `__EFMigrationsHistory` | 7 | `MigrationId` | 2 | EF Core migration tracking table. |
| public | `quran_ayahs` | 6,236 | `id` | 9 | Canonical ayah metadata/text and page span. |
| public | `quran_i3rab_rules` | 142 | `id` | 7 | Rule catalogue for generated simple i'rab labels. |
| public | `quran_lemmas` | 4,793 | `id` | 6 | Morphology lemma dimension, optionally linked to roots. |
| public | `quran_mushaf_lines` | 9,046 | `id` | 9 | Mushaf page-line layout and word range anchors. |
| public | `quran_mushaf_pages` | 604 | `page_number` | 6 | Mushaf page ranges and line counts. |
| public | `quran_pos_tags` | 49 | `code` | 6 | Controlled POS vocabulary and labels. |
| public | `quran_roots` | 1,642 | `id` | 6 | Morphology root dimension and usage stats. |
| public | `quran_stems` | 12,108 | `id` | 4 | Morphology stem dimension and usage stats. |
| public | `quran_surahs` | 114 | `surah_number` | 8 | Canonical surah metadata. |
| public | `quran_word_morphology` | 77,432 | `quran_word_id` | 12 | One morphology summary row per readable Quran word. |
| public | `quran_word_morphology_segments` | 128,219 | `id` | 18 | Segment-level morphology, render provenance, and generated i'rab. |
| public | `quran_words` | 83,668 | `id` | 17 | Canonical word/token stream, including ayah markers. |
| public | `quran_words_ordered_simple` | 77,432 | `word_order_in_mushaf` | 16 | Derived readable word ordering grouped by simple/imlaei key. |
| public | `quran_words_ordered_tashkeel` | 77,432 | `word_order_in_mushaf` | 16 | Derived readable word ordering grouped by tashkeel/Uthmani text. |
| public | `quran_words_unique_simple` | 14,783 | `id` | 16 | Derived unique simple/imlaei word identities and first occurrence metadata. |
| public | `quran_words_unique_tashkeel` | 21,294 | `id` | 14 | Derived unique tashkeel/Uthmani word identities and first occurrence metadata. |

## 3. Columns inventory

Legend for `Role`: `PK` = primary key, `FK` = foreign key, `IDX` = indexed. Category values are inferred and should be validated with feature owners before cleanup.

### `public.__EFMigrationsHistory`

| Column | Type | Nullability | Default | Role | Category |
|---|---|---|---|---|---|
| `MigrationId` | `varchar(150)` | NOT NULL | — | PK/IDX | import/audit |
| `ProductVersion` | `varchar(32)` | NOT NULL | — | — | import/audit |

### `public.quran_ayahs`

| Column | Type | Nullability | Default | Role | Category |
|---|---|---|---|---|---|
| `id` | `integer` | NOT NULL | — | PK/IDX | source/canonical |
| `surah_number` | `smallint` | NOT NULL | — | FK/IDX | relationship/FK |
| `ayah_number` | `smallint` | NOT NULL | — | IDX | source/canonical |
| `verse_key` | `text` | NOT NULL | — | IDX | source/canonical |
| `text_uthmani` | `text` | NOT NULL | — | — | source/canonical |
| `words_count_source` | `smallint` | NOT NULL | — | — | import/audit |
| `words_count_real` | `smallint` | NOT NULL | — | — | derived/search |
| `page_from` | `smallint` | NOT NULL | — | — | derived/search |
| `page_to` | `smallint` | NOT NULL | — | — | derived/search |

### `public.quran_i3rab_rules`

| Column | Type | Nullability | Default | Role | Category |
|---|---|---|---|---|---|
| `id` | `integer` | NOT NULL | — | PK/IDX | source/canonical |
| `signature_key` | `text` | NOT NULL | — | IDX | source/canonical |
| `rule_family` | `text` | NOT NULL | — | IDX | source/canonical |
| `i3rab_arabic` | `text` | NOT NULL | — | — | source/canonical |
| `default_status` | `text` | NOT NULL | — | — | import/audit |
| `description` | `text` | NULL | — | — | unclear |
| `sort_order` | `smallint` | NOT NULL | — | — | UI/cache/stat |

### `public.quran_lemmas`

| Column | Type | Nullability | Default | Role | Category |
|---|---|---|---|---|---|
| `id` | `integer` | NOT NULL | — | PK/IDX | source/canonical |
| `lemma_text` | `text` | NOT NULL | — | IDX | source/canonical |
| `lemma_buckwalter` | `text` | NULL | — | — | source/canonical |
| `root_id` | `integer` | NULL | — | FK/IDX | relationship/FK |
| `words_count` | `integer` | NOT NULL | — | — | UI/cache/stat |
| `first_word_order_in_mushaf` | `integer` | NOT NULL | — | IDX | derived/search |

### `public.quran_mushaf_lines`

| Column | Type | Nullability | Default | Role | Category |
|---|---|---|---|---|---|
| `id` | `integer` | NOT NULL | — | PK/IDX | source/canonical |
| `page_number` | `smallint` | NOT NULL | — | FK/IDX | relationship/FK |
| `line_number` | `smallint` | NOT NULL | — | IDX | source/canonical |
| `line_type` | `text` | NOT NULL | — | — | source/canonical |
| `is_centered` | `boolean` | NOT NULL | — | — | UI/cache/stat |
| `surah_number` | `smallint` | NULL | — | FK/IDX | relationship/FK |
| `first_word_id` | `integer` | NULL | — | FK/IDX | relationship/FK |
| `last_word_id` | `integer` | NULL | — | FK/IDX | relationship/FK |
| `words_count` | `smallint` | NOT NULL | — | — | UI/cache/stat |

### `public.quran_mushaf_pages`

| Column | Type | Nullability | Default | Role | Category |
|---|---|---|---|---|---|
| `page_number` | `smallint` | NOT NULL | — | PK/IDX | source/canonical |
| `first_surah_number` | `smallint` | NOT NULL | — | derived/search |
| `first_ayah_number` | `smallint` | NOT NULL | — | — | derived/search |
| `last_surah_number` | `smallint` | NOT NULL | — | — | derived/search |
| `last_ayah_number` | `smallint` | NOT NULL | — | — | derived/search |
| `lines_count` | `smallint` | NOT NULL | — | — | UI/cache/stat |

### `public.quran_pos_tags`

| Column | Type | Nullability | Default | Role | Category |
|---|---|---|---|---|---|
| `code` | `text` | NOT NULL | — | PK/IDX | source/canonical |
| `arabic_label` | `text` | NOT NULL | — | — | source/canonical |
| `english_label` | `text` | NOT NULL | — | — | source/canonical |
| `category` | `text` | NOT NULL | — | IDX | source/canonical |
| `sort_order` | `smallint` | NOT NULL | — | IDX | UI/cache/stat |
| `description` | `text` | NULL | — | — | unclear |

### `public.quran_roots`

| Column | Type | Nullability | Default | Role | Category |
|---|---|---|---|---|---|
| `id` | `integer` | NOT NULL | — | PK/IDX | source/canonical |
| `root_text` | `text` | NOT NULL | — | IDX | source/canonical |
| `root_buckwalter` | `text` | NULL | — | — | source/canonical |
| `words_count` | `integer` | NOT NULL | — | IDX | UI/cache/stat |
| `distinct_lemmas_count` | `smallint` | NOT NULL | — | — | UI/cache/stat |
| `first_word_order_in_mushaf` | `integer` | NOT NULL | — | IDX | derived/search |

### `public.quran_stems`

| Column | Type | Nullability | Default | Role | Category |
|---|---|---|---|---|---|
| `id` | `integer` | NOT NULL | — | PK/IDX | source/canonical |
| `stem_text` | `text` | NOT NULL | — | IDX | source/canonical |
| `words_count` | `integer` | NOT NULL | — | — | UI/cache/stat |
| `first_word_order_in_mushaf` | `integer` | NOT NULL | — | IDX | derived/search |

### `public.quran_surahs`

| Column | Type | Nullability | Default | Role | Category |
|---|---|---|---|---|---|
| `surah_number` | `smallint` | NOT NULL | — | PK/IDX | source/canonical |
| `name_arabic` | `text` | NOT NULL | — | IDX | source/canonical |
| `name_simple` | `text` | NOT NULL | — | — | derived/search |
| `name_transliteration` | `text` | NOT NULL | — | — | source/canonical |
| `revelation_place` | `text` | NOT NULL | — | — | source/canonical |
| `revelation_order` | `smallint` | NOT NULL | — | — | source/canonical |
| `verses_count` | `smallint` | NOT NULL | — | — | UI/cache/stat |
| `bismillah_pre` | `boolean` | NOT NULL | — | — | source/canonical |

### `public.quran_word_morphology`

| Column | Type | Nullability | Default | Role | Category |
|---|---|---|---|---|---|
| `quran_word_id` | `integer` | NOT NULL | — | PK/FK/IDX | relationship/FK |
| `location` | `text` | NOT NULL | — | — | source/canonical |
| `head_pos` | `text` | NOT NULL | — | FK/IDX | relationship/FK |
| `segment_count` | `smallint` | NOT NULL | — | — | UI/cache/stat |
| `root_id` | `integer` | NULL | — | FK/IDX | relationship/FK |
| `lemma_id` | `integer` | NULL | — | FK/IDX | relationship/FK |
| `stem_id` | `integer` | NULL | — | FK/IDX | relationship/FK |
| `is_verb` | `boolean` | NOT NULL | — | — | derived/search |
| `verb_tense` | `text` | NULL | — | IDX | derived/search |
| `verb_voice` | `text` | NULL | — | IDX | derived/search |
| `case_feature` | `text` | NULL | — | IDX | derived/search |
| `head_features_json` | `jsonb` | NULL | — | — | derived/search |

### `public.quran_word_morphology_segments`

| Column | Type | Nullability | Default | Role | Category |
|---|---|---|---|---|---|
| `id` | `integer` | NOT NULL | — | PK/IDX | source/canonical |
| `quran_word_id` | `integer` | NOT NULL | — | FK/IDX | relationship/FK |
| `segment_location` | `text` | NOT NULL | — | — | source/canonical |
| `segment_number` | `smallint` | NOT NULL | — | IDX | source/canonical |
| `kind` | `text` | NOT NULL | — | — | source/canonical |
| `pos` | `text` | NOT NULL | — | FK/IDX | relationship/FK |
| `form_buckwalter` | `text` | NOT NULL | — | — | source/canonical |
| `form_arabic_normalized` | `text` | NULL | — | — | derived/search |
| `arabic_render_tier` | `text` | NULL | — | IDX | import/audit |
| `arabic_render_source` | `text` | NOT NULL | — | — | import/audit |
| `root_buckwalter` | `text` | NULL | — | — | source/canonical |
| `lemma_buckwalter` | `text` | NULL | — | — | source/canonical |
| `features_raw` | `text` | NOT NULL | — | — | source/canonical |
| `features_json` | `jsonb` | NULL | — | — | derived/search |
| `i3rab_arabic` | `text` | NULL | — | — | derived/search |
| `i3rab_review_reason` | `text` | NULL | — | — | import/audit |
| `i3rab_rule_id` | `integer` | NULL | — | FK/IDX | relationship/FK |
| `i3rab_status` | `text` | NOT NULL | `'unsupported'::text` | — | import/audit |

### `public.quran_words`

| Column | Type | Nullability | Default | Role | Category |
|---|---|---|---|---|---|
| `id` | `integer` | NOT NULL | — | PK/IDX | source/canonical |
| `location` | `text` | NOT NULL | — | IDX | source/canonical |
| `ayah_id` | `integer` | NOT NULL | — | FK/IDX | relationship/FK |
| `surah_number` | `smallint` | NOT NULL | — | IDX | source/canonical |
| `ayah_number` | `smallint` | NOT NULL | — | IDX | source/canonical |
| `word_number` | `smallint` | NOT NULL | — | IDX | source/canonical |
| `page_number` | `smallint` | NOT NULL | — | FK/IDX | relationship/FK |
| `line_number` | `smallint` | NOT NULL | — | IDX | source/canonical |
| `line_word_order` | `smallint` | NOT NULL | — | IDX | source/canonical |
| `qpc_glyph` | `text` | NOT NULL | — | — | source/canonical |
| `text_uthmani` | `text` | NOT NULL | — | — | source/canonical |
| `text_uthmani_simple` | `text` | NOT NULL | — | — | derived/search |
| `text_imlaei_simple` | `text` | NOT NULL | — | — | derived/search |
| `is_ayah_marker` | `boolean` | NOT NULL | — | — | source/canonical |
| `word_key_imlaei_simple` | `text` | NOT NULL | `''::text` | IDX | derived/search |
| `unique_simple_word_id` | `integer` | NULL | — | IDX | relationship/FK |
| `unique_tashkeel_word_id` | `integer` | NULL | — | IDX | relationship/FK |

### `public.quran_words_ordered_simple`

| Column | Type | Nullability | Default | Role | Category |
|---|---|---|---|---|---|
| `word_order_in_mushaf` | `integer` | NOT NULL | — | PK/IDX | derived/search |
| `quran_word_id` | `integer` | NOT NULL | — | FK/IDX | relationship/FK |
| `location` | `text` | NOT NULL | — | — | derived/search |
| `verse_key` | `text` | NOT NULL | — | — | derived/search |
| `surah_number` | `smallint` | NOT NULL | — | IDX | derived/search |
| `ayah_number` | `smallint` | NOT NULL | — | IDX | derived/search |
| `page_number` | `smallint` | NOT NULL | — | — | derived/search |
| `line_number` | `smallint` | NOT NULL | — | — | derived/search |
| `word_order_in_ayah` | `smallint` | NOT NULL | — | IDX | derived/search |
| `word_order_in_surah` | `smallint` | NOT NULL | — | IDX | derived/search |
| `text_uthmani_simple` | `text` | NOT NULL | — | — | derived/search |
| `text_imlaei_simple` | `text` | NOT NULL | — | — | derived/search |
| `occurrences_count` | `integer` | NOT NULL | — | — | UI/cache/stat |
| `ayahs_count` | `smallint` | NOT NULL | — | — | UI/cache/stat |
| `surahs_count` | `smallint` | NOT NULL | — | — | UI/cache/stat |
| `word_key_imlaei_simple` | `text` | NOT NULL | `''::text` | — | derived/search |

### `public.quran_words_ordered_tashkeel`

| Column | Type | Nullability | Default | Role | Category |
|---|---|---|---|---|---|
| `word_order_in_mushaf` | `integer` | NOT NULL | — | PK/IDX | derived/search |
| `quran_word_id` | `integer` | NOT NULL | — | FK/IDX | relationship/FK |
| `location` | `text` | NOT NULL | — | — | derived/search |
| `verse_key` | `text` | NOT NULL | — | — | derived/search |
| `surah_number` | `smallint` | NOT NULL | — | IDX | derived/search |
| `ayah_number` | `smallint` | NOT NULL | — | IDX | derived/search |
| `page_number` | `smallint` | NOT NULL | — | — | derived/search |
| `line_number` | `smallint` | NOT NULL | — | — | derived/search |
| `word_order_in_ayah` | `smallint` | NOT NULL | — | IDX | derived/search |
| `word_order_in_surah` | `smallint` | NOT NULL | — | IDX | derived/search |
| `text_uthmani` | `text` | NOT NULL | — | — | source/canonical |
| `text_uthmani_simple` | `text` | NOT NULL | — | — | derived/search |
| `text_imlaei_simple` | `text` | NOT NULL | — | — | derived/search |
| `occurrences_count` | `integer` | NOT NULL | — | — | UI/cache/stat |
| `ayahs_count` | `smallint` | NOT NULL | — | — | UI/cache/stat |
| `surahs_count` | `smallint` | NOT NULL | — | — | UI/cache/stat |

### `public.quran_words_unique_simple`

| Column | Type | Nullability | Default | Role | Category |
|---|---|---|---|---|---|
| `id` | `integer` | NOT NULL | — | PK/IDX | source/canonical |
| `text_uthmani_simple` | `text` | NOT NULL | — | — | derived/search |
| `text_imlaei_simple` | `text` | NOT NULL | — | — | derived/search |
| `occurrences_count` | `integer` | NOT NULL | — | — | UI/cache/stat |
| `ayahs_count` | `smallint` | NOT NULL | — | — | UI/cache/stat |
| `surahs_count` | `smallint` | NOT NULL | — | — | UI/cache/stat |
| `first_quran_word_id` | `integer` | NOT NULL | — | FK/IDX | relationship/FK |
| `first_location` | `text` | NOT NULL | — | — | derived/search |
| `first_surah_number` | `smallint` | NOT NULL | — | — | derived/search |
| `first_ayah_number` | `smallint` | NOT NULL | — | — | derived/search |
| `first_word_order_in_mushaf` | `integer` | NOT NULL | — | IDX | derived/search |
| `first_page_number` | `smallint` | NOT NULL | — | — | derived/search |
| `first_line_number` | `smallint` | NOT NULL | — | — | derived/search |
| `qpc_glyph` | `text` | NOT NULL | `''::text` | — | source/canonical |
| `text_uthmani` | `text` | NOT NULL | `''::text` | — | source/canonical |
| `word_key_imlaei_simple` | `text` | NOT NULL | `''::text` | IDX | derived/search |

### `public.quran_words_unique_tashkeel`

| Column | Type | Nullability | Default | Role | Category |
|---|---|---|---|---|---|
| `id` | `integer` | NOT NULL | — | PK/IDX | source/canonical |
| `text_uthmani` | `text` | NOT NULL | — | IDX | source/canonical |
| `text_uthmani_simple` | `text` | NOT NULL | — | — | derived/search |
| `text_imlaei_simple` | `text` | NOT NULL | — | — | derived/search |
| `occurrences_count` | `integer` | NOT NULL | — | — | UI/cache/stat |
| `ayahs_count` | `smallint` | NOT NULL | — | — | UI/cache/stat |
| `surahs_count` | `smallint` | NOT NULL | — | — | UI/cache/stat |
| `first_quran_word_id` | `integer` | NOT NULL | — | FK/IDX | relationship/FK |
| `first_location` | `text` | NOT NULL | — | — | derived/search |
| `first_surah_number` | `smallint` | NOT NULL | — | — | derived/search |
| `first_ayah_number` | `smallint` | NOT NULL | — | — | derived/search |
| `first_word_order_in_mushaf` | `integer` | NOT NULL | — | IDX | derived/search |
| `first_page_number` | `smallint` | NOT NULL | — | — | derived/search |
| `first_line_number` | `smallint` | NOT NULL | — | — | derived/search |

## 4. Constraints and indexes

Note: `NOT NULL` is represented in the column inventory. PostgreSQL catalog output included many `NOT NULL` entries as check-like constraints; below lists primary keys, foreign keys, explicit domain checks, and indexes.

### `__EFMigrationsHistory`
- PK: `PK___EFMigrationsHistory` on `MigrationId`.
- FKs: none.
- Unique constraints: none.
- Explicit checks: none.
- Indexes: `PK___EFMigrationsHistory` unique btree (`MigrationId`).

### `quran_ayahs`
- PK: `PK_quran_ayahs` on `id`.
- FKs: `surah_number -> quran_surahs(surah_number)` with cascade delete.
- Unique constraints: none.
- Explicit checks: none.
- Indexes: `PK_quran_ayahs` unique (`id`); `IX_quran_ayahs_surah_number_ayah_number` unique (`surah_number`, `ayah_number`); `IX_quran_ayahs_verse_key` unique (`verse_key`).

### `quran_i3rab_rules`
- PK: `PK_quran_i3rab_rules` on `id`.
- FKs: none.
- Unique constraints: none.
- Explicit checks: `CK_quran_i3rab_rules_default_status` allows `approved`, `needs_review`, `unsupported`.
- Indexes: `PK_quran_i3rab_rules` unique (`id`); `IX_quran_i3rab_rules_signature_key` unique (`signature_key`); `IX_quran_i3rab_rules_rule_family` (`rule_family`).

### `quran_lemmas`
- PK: `PK_quran_lemmas` on `id`.
- FKs: `root_id -> quran_roots(id)`.
- Unique constraints: none.
- Explicit checks: none.
- Indexes: `PK_quran_lemmas` unique (`id`); `IX_quran_lemmas_lemma_text` unique (`lemma_text`); `IX_quran_lemmas_first_word_order_in_mushaf` unique (`first_word_order_in_mushaf`); `IX_quran_lemmas_root_id` (`root_id`).

### `quran_mushaf_lines`
- PK: `PK_quran_mushaf_lines` on `id`.
- FKs: `page_number -> quran_mushaf_pages(page_number)` cascade; `surah_number -> quran_surahs(surah_number)`; `first_word_id -> quran_words(id)`; `last_word_id -> quran_words(id)`.
- Unique constraints: none.
- Explicit checks: none.
- Indexes: `PK_quran_mushaf_lines` unique (`id`); `IX_quran_mushaf_lines_page_number_line_number` unique (`page_number`, `line_number`); `IX_quran_mushaf_lines_first_word_id`; `IX_quran_mushaf_lines_last_word_id`; `IX_quran_mushaf_lines_surah_number`.

### `quran_mushaf_pages`
- PK: `PK_quran_mushaf_pages` on `page_number`.
- FKs: none.
- Unique constraints: none.
- Explicit checks: none.
- Indexes: `PK_quran_mushaf_pages` unique (`page_number`).

### `quran_pos_tags`
- PK: `PK_quran_pos_tags` on `code`.
- FKs: none.
- Unique constraints: none.
- Explicit checks: none.
- Indexes: `PK_quran_pos_tags` unique (`code`); `IX_quran_pos_tags_category`; `IX_quran_pos_tags_sort_order`.

### `quran_roots`
- PK: `PK_quran_roots` on `id`.
- FKs: none.
- Unique constraints: none.
- Explicit checks: none.
- Indexes: `PK_quran_roots` unique (`id`); `IX_quran_roots_root_text` unique (`root_text`); `IX_quran_roots_first_word_order_in_mushaf` unique (`first_word_order_in_mushaf`); `IX_quran_roots_words_count`.

### `quran_stems`
- PK: `PK_quran_stems` on `id`.
- FKs: none.
- Unique constraints: none.
- Explicit checks: none.
- Indexes: `PK_quran_stems` unique (`id`); `IX_quran_stems_stem_text` unique (`stem_text`); `IX_quran_stems_first_word_order_in_mushaf` unique (`first_word_order_in_mushaf`).

### `quran_surahs`
- PK: `PK_quran_surahs` on `surah_number`.
- FKs: none.
- Unique constraints: none.
- Explicit checks: none.
- Indexes: `PK_quran_surahs` unique (`surah_number`); `IX_quran_surahs_name_arabic` unique (`name_arabic`).

### `quran_word_morphology`
- PK: `PK_quran_word_morphology` on `quran_word_id`.
- FKs: `quran_word_id -> quran_words(id)` cascade; `head_pos -> quran_pos_tags(code)` restrict; `root_id -> quran_roots(id)`; `lemma_id -> quran_lemmas(id)`; `stem_id -> quran_stems(id)`.
- Unique constraints: none.
- Explicit checks: none.
- Indexes: `PK_quran_word_morphology` unique (`quran_word_id`); `IX_quran_word_morphology_quran_word_id` unique (`quran_word_id`); `IX_quran_word_morphology_head_pos`; `IX_quran_word_morphology_root_id`; `IX_quran_word_morphology_lemma_id`; `IX_quran_word_morphology_stem_id`; `IX_quran_word_morphology_case_feature`; partial `IX_quran_word_morphology_verb_tense` (`verb_tense`) where `is_verb`; partial `IX_quran_word_morphology_verb_voice` (`verb_voice`) where `is_verb`.

### `quran_word_morphology_segments`
- PK: `PK_quran_word_morphology_segments` on `id`.
- FKs: `quran_word_id -> quran_words(id)` cascade; `pos -> quran_pos_tags(code)` restrict; `i3rab_rule_id -> quran_i3rab_rules(id)` restrict.
- Unique constraints: none.
- Explicit checks: `CK_quran_word_morphology_segments_i3rab_status` allows `approved`, `needs_review`, `unsupported`.
- Indexes: `PK_quran_word_morphology_segments` unique (`id`); `IX_quran_word_morphology_segments_quran_word_id_segment_number` unique (`quran_word_id`, `segment_number`); `IX_quran_word_morphology_segments_pos`; `IX_quran_word_morphology_segments_i3rab_rule_id`; `IX_quran_word_morphology_segments_arabic_render_tier`; partial `IX_quran_word_morphology_segments_stem` (`quran_word_id`) where `kind = 'STEM'`.

### `quran_words`
- PK: `PK_quran_words` on `id`.
- FKs: `ayah_id -> quran_ayahs(id)` cascade; `page_number -> quran_mushaf_pages(page_number)` cascade.
- Unique constraints: none.
- Explicit checks: none.
- Indexes: `PK_quran_words` unique (`id`); `IX_quran_words_location` unique (`location`); `IX_quran_words_ayah_id`; `IX_quran_words_page_number_line_number_line_word_order`; `IX_quran_words_surah_ayah_word`; partial `IX_quran_words_readable_surah_ayah_word` where `is_ayah_marker = false`; partial `IX_quran_words_word_key_imlaei_simple` where `is_ayah_marker = false`; partial `IX_quran_words_unique_simple_word_id` where readable and non-null; partial `IX_quran_words_unique_tashkeel_word_id` where readable and non-null.

### `quran_words_ordered_simple`
- PK: `PK_quran_words_ordered_simple` on `word_order_in_mushaf`.
- FKs: `quran_word_id -> quran_words(id)` cascade.
- Unique constraints: none.
- Explicit checks: none.
- Indexes: `PK_quran_words_ordered_simple` unique (`word_order_in_mushaf`); `IX_quran_words_ordered_simple_quran_word_id` unique (`quran_word_id`); `IX_quran_words_ordered_simple_surah_number_ayah_number_word_or~` (`surah_number`, `ayah_number`, `word_order_in_ayah`); `IX_quran_words_ordered_simple_surah_number_word_order_in_surah` (`surah_number`, `word_order_in_surah`).

### `quran_words_ordered_tashkeel`
- PK: `PK_quran_words_ordered_tashkeel` on `word_order_in_mushaf`.
- FKs: `quran_word_id -> quran_words(id)` cascade.
- Unique constraints: none.
- Explicit checks: none.
- Indexes: `PK_quran_words_ordered_tashkeel` unique (`word_order_in_mushaf`); `IX_quran_words_ordered_tashkeel_quran_word_id` unique (`quran_word_id`); `IX_quran_words_ordered_tashkeel_surah_number_ayah_number_word_~` (`surah_number`, `ayah_number`, `word_order_in_ayah`); `IX_quran_words_ordered_tashkeel_surah_number_word_order_in_sur~` (`surah_number`, `word_order_in_surah`).

### `quran_words_unique_simple`
- PK: `PK_quran_words_unique_simple` on `id`.
- FKs: `first_quran_word_id -> quran_words(id)` cascade.
- Unique constraints: none.
- Explicit checks: none.
- Indexes: `PK_quran_words_unique_simple` unique (`id`); `IX_quran_words_unique_simple_word_key_imlaei_simple` unique (`word_key_imlaei_simple`); `IX_quran_words_unique_simple_first_word_order_in_mushaf` unique (`first_word_order_in_mushaf`); `IX_quran_words_unique_simple_first_quran_word_id`.

### `quran_words_unique_tashkeel`
- PK: `PK_quran_words_unique_tashkeel` on `id`.
- FKs: `first_quran_word_id -> quran_words(id)` cascade.
- Unique constraints: none.
- Explicit checks: none.
- Indexes: `PK_quran_words_unique_tashkeel` unique (`id`); `IX_quran_words_unique_tashkeel_text_uthmani` unique (`text_uthmani`); `IX_quran_words_unique_tashkeel_first_word_order_in_mushaf` unique (`first_word_order_in_mushaf`); `IX_quran_words_unique_tashkeel_first_quran_word_id`.

## 5. EF mapping cross-check

Inspected EF files:

- `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/QuranDashboardDbContext.cs`
- `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Configurations/Quran/**`
- Latest `QuranDashboardDbContextModelSnapshot.cs`
- Raw SQL files under `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Repositories/Quran/**`

Findings:

| Check | Result |
|---|---|
| Tables configured in EF but missing in DB | None found. The 16 app tables configured with `ToTable(...)` are present. |
| DB tables not mapped in EF | `__EFMigrationsHistory` is not a domain `DbSet`, expected for EF infrastructure. No unmapped app table found. |
| Columns configured in EF but missing in DB | None found by comparing configuration column names with `information_schema.columns`. |
| DB columns not mapped in EF | None found for app tables. `__EFMigrationsHistory.ProductVersion` is EF infrastructure, not app mapping. |
| Schema mapping | EF `ToTable(...)` calls do not specify schema; tables resolve to `public`. |

Raw SQL dependencies identified:

- Foundation import writes `quran_surahs`, `quran_ayahs`, `quran_mushaf_pages`, `quran_words`, `quran_mushaf_lines` via binary `COPY`, and can `TRUNCATE` the foundation set (`EfBulkQuranImportWriter.cs`).
- Display word rebuild derives and writes `quran_words_ordered_*`, `quran_words_unique_*`, and updates `quran_words.unique_*_word_id` (`DisplayWordsSql.cs`).
- Morphology import writes `quran_pos_tags`, `quran_roots`, `quran_lemmas`, `quran_stems`, `quran_word_morphology`, `quran_word_morphology_segments`, and can `TRUNCATE` the morphology set (`MorphologyBulkCopier.cs`, `MorphologySql.cs`).
- I'rab generation upserts `quran_i3rab_rules`, stages segment results in a temp table, updates `quran_word_morphology_segments.i3rab_*`, and validates/report-reads several morphology/i'rab columns (`I3rabSql.cs`, `I3rabValidationRunner.cs`, `EfI3rabGenerationWriter.cs`).

## 6. Potential cleanup candidates

No deletion is recommended from this inventory alone. The entries below are candidates for deeper review only.

| Candidate | Why it may be unnecessary | Evidence | Risk | Must verify before deleting |
|---|---|---|---|---|
| `quran_word_morphology_segments.i3rab_review_reason` | Currently carries no data in this local DB. | Read-only profile: 128,219 total rows, 0 non-null, 128,219 null. Raw SQL writes it only when staged i'rab review reasons exist. | Medium | Confirm workflow will never persist `needs_review`/`unsupported` explanations; check reports/tests depending on validation messages. |
| `quran_word_morphology_segments.arabic_render_source` | Current values are uniform, so it may be provenance metadata rather than query data. | Distribution: 128,219 rows all `buckwalter-transliteration`. `MorphologySql.CheckSegSourceValid` validates this exact value. | High | Confirm no future render source will be introduced; replace validation/report provenance if removed. |
| `quran_ayahs.words_count_source` and `quran_ayahs.words_count_real` | These look like source-vs-derived audit counts and may duplicate computable data from `quran_words`. | Code search found configuration/import references; no obvious query usage outside import/model files during this pass. | Medium | Verify import validation/reporting requirements and whether historical source discrepancies must remain inspectable. |
| `quran_ayahs.page_from` and `quran_ayahs.page_to` | Page span is derivable from word/page layout. | Code search found configuration/import references; no obvious read usage outside import/model files during this pass. | Medium | Verify UI/API page navigation needs and source reconciliation needs. |
| `quran_word_morphology.location` | Duplicates `quran_words.location` via `quran_word_id`. | `MorphologySql.CheckLocationIdMismatch` exists specifically to validate equality with `quran_words.location`. | High | Confirm importer validation and human-readable diagnostics do not need direct morphology location; update mismatch checks if removed. |
| `quran_words_ordered_*` placement/text/count columns | Many columns duplicate `quran_words`/`quran_ayahs` plus computed counts. | `DisplayWordsSql` derives these tables from `quran_words` and `quran_ayahs`. They are optimized read models. | High | Identify frontend/API read paths, expected query latency, rebuild cost, and whether a view/materialized view is preferable. |
| `quran_words_unique_*` first occurrence columns | First occurrence metadata can be recomputed from `quran_words_ordered_*`/`quran_words`. | `DisplayWordsSql` derives all unique-word first occurrence fields. | High | Verify lookup/search UX and indexing needs; measure recompute/query cost. |
| `quran_words.unique_simple_word_id` and `quran_words.unique_tashkeel_word_id` | They are link-cache columns to derived unique-word tables and are null for ayah markers. | Profile: 83,668 rows total, 77,432 non-null, 6,236 null; non-null count equals readable word count. `DisplayWordsSql` updates these after rebuilding unique tables. | High | Confirm no read path needs O(1) jump from canonical word to unique identity; evaluate join alternatives. |

Zero-row tables: none. Exact counts show `quran_pos_tags` has 49 rows and `__EFMigrationsHistory` has 7 rows; earlier approximate `pg_class.reltuples` estimates for these tables were stale.

## 7. Potential schema split proposal

This is a future organization proposal only. Do not implement without a migration plan, EF schema mapping plan, and raw SQL rewrite plan.

| Proposed schema | Current tables that might belong there | Why | Migration risk | Raw SQL / EF impact |
|---|---|---|---|---|
| `quran_core` | `quran_surahs`, `quran_ayahs`, `quran_mushaf_pages`, `quran_mushaf_lines`, `quran_words` | Canonical Quran structure, tokens, and layout. | High because many FKs and imports depend on these tables. | EF `ToTable` schemas; all import/display/morphology/i'rab SQL table references need schema qualification. |
| `quran_words` | `quran_words_ordered_simple`, `quran_words_ordered_tashkeel`, `quran_words_unique_simple`, `quran_words_unique_tashkeel` | Derived/read-model word identity and ordering tables. | Medium/high because rebuild SQL truncates/inserts/updates across these and `quran_core.quran_words`. | `DisplayWordsSql` is heavily impacted; FK cross-schema references must be explicit. |
| `quran_morphology` | `quran_pos_tags`, `quran_roots`, `quran_lemmas`, `quran_stems`, `quran_word_morphology`, `quran_word_morphology_segments` | Morphology dimensions and segment-level source/derived data. | High due to bulk `COPY`, validation SQL, and cross-links to `quran_words`. | Morphology `COPY`, validation, EF configurations, and tests need schema-qualified references. |
| `quran_i3rab` | `quran_i3rab_rules`; optionally i'rab columns could stay on `quran_morphology.quran_word_morphology_segments` or move to a separate result table later. | Separates generated i'rab rule catalogue from raw morphology. | Medium if only rules move; high if segment i'rab columns are normalized out. | `I3rabSql` upsert/update/report SQL and FK from segments require updates. |
| `importing` | No current permanent app table beyond EF history; future import runs/reports could live here. | Keeps operational import metadata separate if persisted later. | Low for future-only, high if repurposing current audit columns. | Additive if future-only; otherwise changes import/report code. |
| `admin` / `content` | No current tables in this DB snapshot. | Useful if non-Quran admin/content publishing tables are introduced. | Low if future-only. | No current impact. |

## 8. Final recommendation

- Physical schema splitting: **later, not now**. The current database is cohesive and small enough operationally; the larger risk is raw SQL and EF mapping churn. Split schemas when stable module boundaries and read/write ownership are clearer, especially after the Word Simple I'rab Foundation feature stabilizes.
- Obvious safe column deletions now: **none**. Several columns are denormalized or all-null/all-same in this local DB, but they are tied to imports, validation, reports, or read-model performance.
- Areas needing deeper review before cleanup:
  - Display/read-model tables (`quran_words_ordered_*`, `quran_words_unique_*`) and their latency requirements.
  - I'rab workflow status/review columns, especially whether `needs_review`/`unsupported` states will be retained.
  - Import/audit columns on `quran_ayahs` and morphology render provenance columns.
  - Raw SQL contract surface before any schema split; table names are embedded in multiple SQL constants.

## Verification

### Commands used

All database commands were read-only catalog/data reads (`SELECT` only):

```bash
PGPASSWORD=*** psql -h localhost -p 5432 -U postgres -d quran_dashboard -v ON_ERROR_STOP=1 -A -t -c "SELECT jsonb_pretty(... schemas/tables from pg_class/information_schema ...);"
PGPASSWORD=*** psql -h localhost -p 5432 -U postgres -d quran_dashboard -v ON_ERROR_STOP=1 -A -t -c "SELECT jsonb_pretty(... columns from information_schema/pg_attribute ...);"
PGPASSWORD=*** psql -h localhost -p 5432 -U postgres -d quran_dashboard -v ON_ERROR_STOP=1 -A -t -c "SELECT jsonb_pretty(... constraints and indexes from pg_constraint/pg_indexes ...);"
PGPASSWORD=*** psql -h localhost -p 5432 -U postgres -d quran_dashboard -v ON_ERROR_STOP=1 -A -F '|' -c "SELECT ... inventory totals ...;"
PGPASSWORD=*** psql -h localhost -p 5432 -U postgres -d quran_dashboard -v ON_ERROR_STOP=1 -A -F '|' -c "SELECT ... exact count(*) by table ...;"
PGPASSWORD=*** psql -h localhost -p 5432 -U postgres -d quran_dashboard -v ON_ERROR_STOP=1 -A -F '|' -c "SELECT ... table summaries with PK/columns ...;"
PGPASSWORD=*** psql -h localhost -p 5432 -U postgres -d quran_dashboard -v ON_ERROR_STOP=1 -A -F '|' -c "SELECT ... compact constraints and indexes ...;"
PGPASSWORD=*** psql -h localhost -p 5432 -U postgres -d quran_dashboard -v ON_ERROR_STOP=1 -A -F '|' -c "SELECT ... nullable-column profiles ...;"
PGPASSWORD=*** psql -h localhost -p 5432 -U postgres -d quran_dashboard -v ON_ERROR_STOP=1 -A -F '|' -c "SELECT ... empty/default-valued profiles ...;"
PGPASSWORD=*** psql -h localhost -p 5432 -U postgres -d quran_dashboard -v ON_ERROR_STOP=1 -A -F '|' -c "SELECT ... categorical morphology status profiles ...;"
```

Repository/file inspection commands and tools used:

```bash
git status --short
git status --short   # from Backend repo
```

Specialized read/search tools were used to inspect `AGENTS.md`, `Backend/AGENTS.md`, `CODING_PRINCIPLES.md`, EF configurations, model snapshot, and raw SQL files. No source files were edited.

### Final git status

Workspace repo (`/projects/Dashboard/App`):

```text
 ? Backend
```

Backend repo (`/projects/Dashboard/App/Backend`):

```text
?? report/database-inventory/
```

### Safety confirmation

- Database data changed: **No**. Only read-only `SELECT` SQL was executed.
- Migrations created or modified: **No**.
- Source code changed: **No**.
- Files intentionally created/updated: `Backend/report/database-inventory/current-database-inventory.md` only.
