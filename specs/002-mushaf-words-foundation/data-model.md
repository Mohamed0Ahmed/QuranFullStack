# Phase 1 Data Model — Quran Mushaf Words & Layout Data Foundation

Five PostgreSQL tables, all **immutable reference data** (read-only after import). Naming: DB columns `snake_case`, EF entities `PascalCase`. Types are PostgreSQL types; EF mapping notes are inline. Counts come from the data report and are **hard** import invariants (spec FR-018).

> **Convention:** `smallint` is used where values fit (≤ 32,767): surah/ayah/page/line numbers, counts. `int` is used for `quran_words.id` (max 83,668) and `quran_mushaf_lines.id`. All Arabic text columns are `text` with default collation (no `citext` — search is deferred).

---

## Relationships (overview)

```text
quran_surahs (114)
   │ 1─*           ▲ surah_number (nullable) on header lines
   ▼               │
quran_ayahs (6,236)
   │ 1─*  (ayah_id)
   ▼
quran_words (83,668) ──page_number──▶ quran_mushaf_pages (604)
        ▲                                   │ 1─*
        │ first_word_id / last_word_id      ▼
        └────────────────────────────  quran_mushaf_lines (9,046)
```

- `quran_words.line_number` is **denormalized** (no DB foreign key to lines) to avoid a circular FK with `quran_mushaf_lines.first_word_id`/`last_word_id`. Its correctness is enforced by **import validation**, not by the database (spec FR-018: "each word's stored page/line/order matches its line").

**FK insert order (acyclic):** `surahs → ayahs → pages → words → lines`.
**Force re-load:** `TRUNCATE quran_words, quran_mushaf_lines, quran_mushaf_pages, quran_ayahs, quran_surahs RESTART IDENTITY CASCADE;` inside the import transaction, then reload in the order above.

---

## 1. `quran_surahs` — 114 rows

| Column | Type | Null | Notes |
|---|---|---|---|
| `surah_number` | `smallint` | NO | **PK**, assigned (`ValueGeneratedNever`), = source `id` (1..114) |
| `name_arabic` | `text` | NO | e.g. `الفاتحة` |
| `name_simple` | `text` | NO | ASCII translit, e.g. `Al-Fatihah` |
| `name_transliteration` | `text` | NO | source `name`, e.g. `Al-Fātiĥah` |
| `revelation_place` | `text` | NO | enum `RevelationPlace` → stored as `makkah` / `madinah` (`HasConversion<string>()`) |
| `revelation_order` | `smallint` | NO | 1..114 |
| `verses_count` | `smallint` | NO | Σ across rows = **6,236** |
| `bismillah_pre` | `boolean` | NO | `false` only for surah 1 and 9 → 112 `true` |

- **Unique**: `name_arabic`. **Indexes**: PK only.
- **Source**: `metadata/quran-metadata-surah-name.json`.

## 2. `quran_ayahs` — 6,236 rows

| Column | Type | Null | Notes |
|---|---|---|---|
| `id` | `int` | NO | **PK**, assigned (`ValueGeneratedNever`), = source `id` |
| `surah_number` | `smallint` | NO | **FK** → `quran_surahs.surah_number` |
| `ayah_number` | `smallint` | NO | within surah |
| `verse_key` | `text` | NO | `"surah:ayah"`, e.g. `2:25` |
| `text_uthmani` | `text` | NO | ayah-level convenience text (different encoding from word-level — see validation note) |
| `words_count_source` | `smallint` | NO | from metadata |
| `words_count_real` | `smallint` | NO | computed = (word occurrences in ayah) − 1 marker |
| `page_from` | `smallint` | NO | computed from layout |
| `page_to` | `smallint` | NO | computed from layout |

- **Unique**: `verse_key`; `(surah_number, ayah_number)`. **FK**: `surah_number`. **Index**: `(surah_number, ayah_number)`.
- **Note**: `words_count_source == words_count_real` for all ayahs **except `37:130`** (source 4, real 3) → import **warning**, not failure.
- **Source**: `metadata/quran-metadata-ayah.json` (+ `words_count_real`/`page_from`/`page_to` derived).

## 3. `quran_mushaf_pages` — 604 rows

| Column | Type | Null | Notes |
|---|---|---|---|
| `page_number` | `smallint` | NO | **PK**, assigned (`ValueGeneratedNever`), 1..604 |
| `first_surah_number` | `smallint` | NO | derived from page's first word |
| `first_ayah_number` | `smallint` | NO | derived |
| `last_surah_number` | `smallint` | NO | derived |
| `last_ayah_number` | `smallint` | NO | derived |
| `lines_count` | `smallint` | NO | 15, except pages 1–2 = 8 |

- **Unique**: none. **Indexes**: PK only. **No enforced FK** (first/last surah/ayah are validated values).
- **Source**: `mushaf/qpc-v4-pages-layout.json` (boundaries derived). **Page fonts are out of scope — no font columns.**

## 4. `quran_mushaf_lines` — 9,046 rows (authoritative line structure)

| Column | Type | Null | Notes |
|---|---|---|---|
| `id` | `int` | NO | **PK**, generated identity (surrogate) |
| `page_number` | `smallint` | NO | **FK** → `quran_mushaf_pages.page_number` |
| `line_number` | `smallint` | NO | 1..15 (1..8 on pages 1–2) |
| `line_type` | `text` | NO | enum `MushafLineType` → `ayah` / `surah_name` / `basmallah` |
| `is_centered` | `boolean` | NO | |
| `surah_number` | `smallint` | YES | set on the 114 `surah_name` lines; **FK** → `quran_surahs.surah_number` |
| `first_word_id` | `int` | YES | **FK** → `quran_words.id`; set only on `ayah` lines |
| `last_word_id` | `int` | YES | **FK** → `quran_words.id`; set only on `ayah` lines |
| `words_count` | `smallint` | NO | `last_word_id − first_word_id + 1` on ayah lines, else `0` |

- **Unique**: `(page_number, line_number)`. **Index**: `(page_number, line_number)`.
- **Type distribution (hard checks)**: `ayah` = 8,820 (all with word ids), `surah_name` = 114 (all with `surah_number`, no word ids), `basmallah` = 112 (no word ids, no `surah_number`).
- **Source**: `mushaf/qpc-v4-pages-layout.json` (`pages[n][]` line objects).

## 5. `quran_words` — 83,668 rows (the core)

| Column | Type | Null | Notes |
|---|---|---|---|
| `id` | `int` | NO | **PK**, assigned (`ValueGeneratedNever`), = source id 1..83,668, **= mushaf reading order** |
| `location` | `text` | NO | `"surah:ayah:word"`, e.g. `2:25:3` |
| `ayah_id` | `int` | NO | **FK** → `quran_ayahs.id` |
| `surah_number` | `smallint` | NO | denormalized |
| `ayah_number` | `smallint` | NO | denormalized |
| `word_number` | `smallint` | NO | position within ayah (markers are the last) |
| `page_number` | `smallint` | NO | **FK** → `quran_mushaf_pages.page_number` |
| `line_number` | `smallint` | NO | denormalized, **no FK** (validated against lines) |
| `line_word_order` | `smallint` | NO | order of this word within its line (1-based) |
| `qpc_glyph` | `text` | NO | QPC v4 glyph code; kept as a lightweight future reference for the Mushaf Reader — not rendered here (page fonts out of scope) |
| `text_uthmani` | `text` | NO | with tashkeel (display) |
| `text_uthmani_simple` | `text` | NO | no tashkeel (Uthmani spelling) |
| `text_imlaei_simple` | `text` | NO | no tashkeel (imlaei spelling) |
| `is_ayah_marker` | `boolean` | NO | `true` for exactly 6,236 rows |

- **Unique**: `location`. **FK**: `ayah_id`, `page_number`.
- **Indexes**: `UNIQUE(location)`; `(surah_number, ayah_number, word_number)`; `(page_number, line_number, line_word_order)`; **partial** `(surah_number, ayah_number, word_number) WHERE is_ayah_marker = false` (the readable-word read path).
- **NOT included** (deferred): `search_normalized_text`, `unique_word_id`, any morphology/root/i3rab columns.
- **Sources joined by `location`**: glyph `mushaf/qpc-v4.json`; text `words/{uthmani,uthmani-simple,imlaei-simple}.json`; page/line/order from `mushaf/qpc-v4-pages-layout.json` ranges.

---

## Domain value objects & enums (Domain layer, logic only)

- **`WordLocation`** (`Domain/Quran/Words/`): wraps `"s:a:w"`; validates 3 positive integer parts. Persisted as the `location` **string** column (not an EF owned type).
- **`VerseKey`** (`Domain/Quran/Words/`): wraps `"s:a"`; validates 2 parts. Persisted as the `verse_key` **string** column.
- **`RevelationPlace`** enum (`Domain/Quran/Surahs/`): `Makkah`, `Madinah` → text `makkah`/`madinah`.
- **`MushafLineType`** enum (`Domain/Quran/MushafPages/`): `Ayah`, `SurahName`, `Basmallah` → text `ayah`/`surah_name`/`basmallah`.

## Derivation rules (import must compute, not read)

1. **`line_word_order`, `page_number`, `line_number` per word**: walk layout `ayah` lines in `(page, line)` order; for each line with range `[first..last]`, assign every `id` in range to that page/line with `line_word_order` 1..n.
2. **`is_ayah_marker`**: the last `word_number` of each ayah; cross-check that the imlaei text is digits-only. Both signals must agree (else hard fail).
3. **`words_count_real`** (ayah): (count of word occurrences in the ayah) − 1.
4. **`page_from`/`page_to`** (ayah): min/max `page_number` over the ayah's words.
5. **Page boundary fields**: first/last word of the page → its ayah → surah/ayah numbers.

## Validation invariants

All hard/warning checks are enumerated in **spec FR-018** and **research.md**; the importer asserts them on the assembled in-memory model **before** persisting. Nothing is written if any hard check fails.
