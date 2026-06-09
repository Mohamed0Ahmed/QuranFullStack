# Phase 1 Data Model — Quran Words Display Tables Foundation

Four PostgreSQL **derived, read-only** tables, rebuilt from the existing Feature 002
tables. DB columns `snake_case`; EF entities `PascalCase` under
`Domain/Quran/Words/Display/`. Types follow the Feature 002 convention: `smallint` where
values ≤ 32,767, `int` otherwise (see research R12). All Arabic text is `text` with
default collation (no normalization, no `citext`).

> **No ayah text is stored** in any table. Ayah association is by identifier only
> (`surah_number`, `ayah_number`, `verse_key`). The word-level text columns
> (`text_uthmani`, `text_uthmani_simple`, `text_imlaei_simple`) are **word** text, which
> is permitted; `text_imlaei_simple` is a **passive reference** column — no search is
> built on it (FR-005, FR-034).

---

## Sources & relationships

```text
quran_words (83,668)  ──ayah_id──▶  quran_ayahs (6,236)   [verse_key only]
   │  WHERE is_ayah_marker = false   →  77,432 readable words = the build input
   ▼
quran_words_ordered_tashkeel (77,432)   quran_words_ordered_simple (77,432)
quran_words_unique_tashkeel (~21,210)    quran_words_unique_simple (~14,783)
        (unique counts derived from the DB, not hardcoded)
```

- **Read-only inputs (never mutated):** `quran_words` (primary), `quran_ayahs`
  (`verse_key` only), `quran_surahs` (validation only).
- **FK from derived → source:** `quran_word_id` / `first_quran_word_id` →
  `quran_words.id`. No derived table references another derived table.

---

## 1. `quran_words_ordered_tashkeel` — 77,432 rows

One row per readable word; display/grouping key `text_uthmani`.

| Column | Type | Null | Notes |
|---|---|---|---|
| `word_order_in_mushaf` | `int` | NO | **PK**; contiguous `1..77,432` over readable words |
| `quran_word_id` | `int` | NO | **FK** → `quran_words.id`; **UNIQUE** |
| `location` | `text` | NO | `"s:a:w"` |
| `verse_key` | `text` | NO | from `quran_ayahs.verse_key` |
| `surah_number` | `smallint` | NO | |
| `ayah_number` | `smallint` | NO | |
| `page_number` | `smallint` | NO | |
| `line_number` | `smallint` | NO | |
| `word_order_in_ayah` | `smallint` | NO | contiguous within ayah; validated `= word_number` |
| `word_order_in_surah` | `smallint` | NO | contiguous within surah |
| `text_uthmani` | `text` | NO | **display + grouping key** (with tashkeel) |
| `text_uthmani_simple` | `text` | NO | paired form (no tashkeel) |
| `text_imlaei_simple` | `text` | NO | reference only (no search) |
| `occurrences_count` | `int` | NO | occurrences of this `text_uthmani` |
| `ayahs_count` | `smallint` | NO | distinct ayahs containing it |
| `surahs_count` | `smallint` | NO | distinct surahs containing it |

**Indexes:** PK(`word_order_in_mushaf`); UNIQUE(`quran_word_id`);
(`surah_number`, `word_order_in_surah`); (`surah_number`, `ayah_number`,
`word_order_in_ayah`).

## 2. `quran_words_ordered_simple` — 77,432 rows

One row per readable word; display/grouping key `text_uthmani_simple`.

| Column | Type | Null | Notes |
|---|---|---|---|
| `word_order_in_mushaf` | `int` | NO | **PK**; `1..77,432` |
| `quran_word_id` | `int` | NO | **FK** → `quran_words.id`; **UNIQUE** |
| `location` | `text` | NO | |
| `verse_key` | `text` | NO | |
| `surah_number` | `smallint` | NO | |
| `ayah_number` | `smallint` | NO | |
| `page_number` | `smallint` | NO | |
| `line_number` | `smallint` | NO | |
| `word_order_in_ayah` | `smallint` | NO | |
| `word_order_in_surah` | `smallint` | NO | |
| `text_uthmani_simple` | `text` | NO | **display + grouping key** (no tashkeel) |
| `text_imlaei_simple` | `text` | NO | reference only (no search) |
| `occurrences_count` | `int` | NO | occurrences of this `text_uthmani_simple` |
| `ayahs_count` | `smallint` | NO | distinct ayahs |
| `surahs_count` | `smallint` | NO | distinct surahs |

**Indexes:** as table 1.

> The ordering columns (`word_order_in_mushaf`, `_surah`, `_ayah`, `quran_word_id`,
> page/line, `verse_key`) are **identical** to table 1 for the same `quran_word_id`; only
> the display text set and the grouped statistics differ (different grouping key). Four
> tables are kept per the feature's explicit requirement.

## 3. `quran_words_unique_tashkeel` — count derived from DB (~21,210 expected, informational)

One row per distinct `text_uthmani`.

| Column | Type | Null | Notes |
|---|---|---|---|
| `id` | `int` | NO | **PK**, generated identity |
| `text_uthmani` | `text` | NO | **display + grouping key**; **UNIQUE** |
| `text_uthmani_simple` | `text` | NO | from first occurrence |
| `text_imlaei_simple` | `text` | NO | from first occurrence; reference only |
| `occurrences_count` | `int` | NO | total occurrences |
| `ayahs_count` | `smallint` | NO | distinct ayahs |
| `surahs_count` | `smallint` | NO | distinct surahs |
| `first_quran_word_id` | `int` | NO | **FK** → `quran_words.id` |
| `first_location` | `text` | NO | |
| `first_surah_number` | `smallint` | NO | |
| `first_ayah_number` | `smallint` | NO | |
| `first_word_order_in_mushaf` | `int` | NO | **UNIQUE**; stable display sort key |
| `first_page_number` | `smallint` | NO | |
| `first_line_number` | `smallint` | NO | |

**Indexes:** PK(`id`); UNIQUE(`text_uthmani`); UNIQUE(`first_word_order_in_mushaf`).
**Stable order:** `ORDER BY first_word_order_in_mushaf`.

## 4. `quran_words_unique_simple` — count derived from DB (~14,783 expected, informational)

One row per distinct `text_uthmani_simple`.

| Column | Type | Null | Notes |
|---|---|---|---|
| `id` | `int` | NO | **PK**, generated identity |
| `text_uthmani_simple` | `text` | NO | **display + grouping key**; **UNIQUE** |
| `text_imlaei_simple` | `text` | NO | from first occurrence; reference only |
| `occurrences_count` | `int` | NO | total occurrences |
| `ayahs_count` | `smallint` | NO | distinct ayahs |
| `surahs_count` | `smallint` | NO | distinct surahs |
| `first_quran_word_id` | `int` | NO | **FK** → `quran_words.id` |
| `first_location` | `text` | NO | |
| `first_surah_number` | `smallint` | NO | |
| `first_ayah_number` | `smallint` | NO | |
| `first_word_order_in_mushaf` | `int` | NO | **UNIQUE** |
| `first_page_number` | `smallint` | NO | |
| `first_line_number` | `smallint` | NO | |

**Indexes:** PK(`id`); UNIQUE(`text_uthmani_simple`); UNIQUE(`first_word_order_in_mushaf`).

---

## Derivation (must compute, not read)

A single ranked **readable base** is computed once, then reused for all four tables
(research R6). Reference SQL shape (final wording lives in `DisplayWordsSql`):

```sql
-- base: readable words + verse_key + the three ranks
WITH readable AS (
  SELECT w.id, w.location, w.ayah_id, w.surah_number, w.ayah_number,
         w.word_number, w.page_number, w.line_number,
         w.text_uthmani, w.text_uthmani_simple, w.text_imlaei_simple,
         a.verse_key
  FROM quran_words w
  JOIN quran_ayahs a ON a.id = w.ayah_id
  WHERE w.is_ayah_marker = false
),
ranked AS (
  SELECT r.*,
         ROW_NUMBER() OVER (ORDER BY id)                                AS word_order_in_mushaf,
         ROW_NUMBER() OVER (PARTITION BY surah_number ORDER BY id)      AS word_order_in_surah,
         ROW_NUMBER() OVER (PARTITION BY ayah_id ORDER BY word_number)  AS word_order_in_ayah
  FROM readable r
),
stats_tashkeel AS (
  SELECT text_uthmani,
         COUNT(*)                      AS occurrences_count,
         COUNT(DISTINCT ayah_id)       AS ayahs_count,
         COUNT(DISTINCT surah_number)  AS surahs_count
  FROM ranked GROUP BY text_uthmani
)
-- ordered tashkeel = ranked ⋈ stats_tashkeel (on text_uthmani)
-- ordered simple   = ranked ⋈ stats_simple   (on text_uthmani_simple)
-- unique tashkeel  = DISTINCT ON (text_uthmani)        ranked ⋈ stats_tashkeel, first row by word_order_in_mushaf
-- unique simple    = DISTINCT ON (text_uthmani_simple) ranked ⋈ stats_simple,   first row by word_order_in_mushaf
```

- `stats_simple` is the analogue of `stats_tashkeel` grouped on `text_uthmani_simple`.
- `occurrences_count = COUNT(*)`, `ayahs_count = COUNT(DISTINCT ayah_id)`,
  `surahs_count = COUNT(DISTINCT surah_number)` per group (FR-016–018).
- First occurrence = the group's row with `MIN(word_order_in_mushaf)` (FR-023).

## Domain types

Four plain entities (data carriers, no behavior) in
`Domain/Quran/Words/Display/`: `OrderedTashkeelWord`, `OrderedSimpleWord`,
`UniqueTashkeelWord`, `UniqueSimpleWord`. No new value objects or enums are required;
`location`/`verse_key` are stored as plain strings (consistent with `quran_words`).

## Validation invariants (enforced before commit — see contracts/validation-report.schema.md)

| Id | Invariant |
|---|---|
| ORD-COUNT | each ordered table has exactly `expectedReadableWords` rows (production default 77,432) |
| ORD-READABLE | readable-word count in `quran_words` = `expectedReadableWords` (production default 77,432) and = ordered row count |
| ORD-NO-MARKERS | no `quran_word_id` maps to `is_ayah_marker = true` |
| ORD-BIJECTION | ordered `quran_word_id` is one-to-one with readable words (`COUNT(DISTINCT)` = `expectedReadableWords`, production default 77,432) |
| ORD-MUSHAF-CONTIG | `word_order_in_mushaf`: MIN=1, MAX=`expectedReadableWords`, COUNT(DISTINCT)=`expectedReadableWords` (production default 77,432) |
| ORD-SURAH-CONTIG | per surah: MIN=1, MAX=COUNT(*), no gaps |
| ORD-AYAH-CONTIG | per ayah: MIN=1, contiguous, equals `word_number` ordering |
| UNQ-COUNT | each unique table rows = `COUNT(DISTINCT text)` over readable words (reported) |
| STAT-MATCH | `occurrences/ayahs/surahs_count` match grouping over readable words; `Σ occurrences_count` (unique) = `expectedReadableWords` (production default 77,432) |
| FIRST-OCC | each unique row's `first_*` = the earliest `word_order_in_mushaf` row of its group |
| SRC-UNTOUCHED | `quran_words`/`quran_ayahs`/`quran_surahs` row counts unchanged across the run |
| UNQ-EXPECT *(warning)* | unique counts ≈ 21,210 / ≈ 14,783 — informational only, never a hard fail |

Any hard check failing ⇒ rollback (write nothing) + failure report + non-zero exit
(FR-031, FR-032).
