# Unique Word Display Tables — Database Audit Report

> Feature 003 `words-display-tables` — audit of the two **unique** display tables only.
> Report-only. No code, schema, data, migrations, tests, or normalization were changed.

| | |
|---|---|
| **Audit date** | 2026-06-09 |
| **Database** | `quran_dashboard` @ `localhost:5432` (Postgres 18.4) |
| **Connection** | configured local connection from .NET user-secrets (`ConnectionStrings:QuranDashboardDb`); password read from secrets, **not printed or committed** |
| **Tables audited** | `quran_words_unique_tashkeel`, `quran_words_unique_simple` |
| **Reference tables** | `quran_words_ordered_tashkeel`, `quran_words_ordered_simple` (read-only, for cross-checks) |
| **Rebuild report** | `resources/report/words-display/words-display-report.md` (current rebuild: unique tashkeel 21,294 / unique simple 15,826) |

---

## 1. Verdict

**PASS WITH NOTES.**

- ✅ No duplicate unique keys in either table (DB-enforced and re-verified by query).
- ✅ Row counts exactly match the current rebuild output (21,294 / 15,826).
- ✅ Full bijection and occurrence consistency with the ordered tables (0 missing, 0 occurrence mismatches).
- ✅ Every stored first-occurrence field matches the earliest row derived from the ordered tables (0 mismatches across all 7 fields, both tables).
- ⚠️ **Notes (no data-integrity bug):** a number of unique keys contain spaces. Almost all are faithful Quranic annotation marks (waqf/pause symbols, rub‑el‑hizb `۞`, sajdah `۩`) carried from the QPC Uthmani source. Two are genuine multi-token words (`ال ياسين`, and the source segmentation of `دائرة`). The only review-worthy item is **normalization completeness**: the *simple* form strips some waqf marks (`ۖ`, `ۚ`) but keeps others (`ۗ`, `۞`, `۩`), so 520 simple keys carry a residual mark and 383 of them duplicate — *in letters only* — an existing clean key (e.g. `الله ۗ` alongside `الله`). This is a review question, not a uniqueness or consistency failure.

---

## 2. Scope

This audit inspects only the two unique display tables and validates them against the
already-populated ordered tables. It covers: row counts vs the rebuild, intra-table key
duplication, null/empty/whitespace and multi-token text quality, tashkeel→simple collapse
behaviour, referential + occurrence consistency with the ordered tables, and first-occurrence
correctness.

It does **not** re-run the rebuild, modify or normalize any data, change schema, or create
migrations. All queries are read-only `SELECT`s.

---

## 3. Source Tables

### `quran_words_unique_tashkeel` — one row per distinct fully-vocalized (Uthmani) form

Key columns: `text_uthmani` (**unique key**), `text_uthmani_simple`, `text_imlaei_simple`,
`occurrences_count`, `ayahs_count`, `surahs_count`, and first-occurrence fields
(`first_quran_word_id`, `first_location`, `first_surah_number`, `first_ayah_number`,
`first_word_order_in_mushaf`, `first_page_number`, `first_line_number`).

Relevant constraints (DB-enforced):

- `UNIQUE (text_uthmani)`
- `UNIQUE (first_word_order_in_mushaf)`

### `quran_words_unique_simple` — one row per distinct simple (Uthmani-simple) form

Key columns: `text_uthmani_simple` (**unique key**), `text_imlaei_simple`,
`occurrences_count`, `ayahs_count`, `surahs_count`, and the same first-occurrence fields.

Relevant constraints (DB-enforced):

- `UNIQUE (text_uthmani_simple)`
- `UNIQUE (first_word_order_in_mushaf)`

> Because the text key is a `UNIQUE` index in both tables, exact-duplicate keys are
> structurally impossible; Section 5 confirms this empirically.

---

## 4. Row Counts

| Table | Rows (DB) | Rebuild expected/current | Match |
|---|---:|---:|:---:|
| `quran_words_unique_tashkeel` | **21,294** | 21,294 | ✅ |
| `quran_words_unique_simple` | **15,826** | 15,826 | ✅ |

Supporting sanity checks:

| Check | Value | Expected | Match |
|---|---:|---:|:---:|
| `SUM(occurrences_count)` — unique tashkeel | 77,432 | 77,432 (readable words) | ✅ |
| `SUM(occurrences_count)` — unique simple | 77,432 | 77,432 (readable words) | ✅ |
| `quran_words_ordered_tashkeel` rows | 77,432 | 77,432 | ✅ |
| `quran_words_ordered_simple` rows | 77,432 | 77,432 | ✅ |

Both unique tables match the current rebuild exactly, and each table's occurrence counts sum
to the full readable-word total (77,432), so no occurrences are lost or double-counted.

---

## 5. Duplicate Audit

Query: `GROUP BY <text_key> HAVING COUNT(*) > 1`.

| Table | Text key column | Duplicate groups | Duplicated rows |
|---|---|---:|---:|
| `quran_words_unique_tashkeel` | `text_uthmani` | **0** | **0** |
| `quran_words_unique_simple` | `text_uthmani_simple` | **0** | **0** |

**No duplicate unique keys in either table.** Consistent with the `UNIQUE` indexes on the text
keys. (Different tashkeel and simple totals are expected and are analysed in Section 7 — not a
duplication problem.)

---

## 6. Null / Empty / Whitespace Audit

### 6.1 Hard text-quality checks (on the unique key column)

| Check | unique_tashkeel (`text_uthmani`) | unique_simple (`text_uthmani_simple`) |
|---|---:|---:|
| NULL values | 0 | 0 |
| Empty string `''` | 0 | 0 |
| Whitespace-only | 0 | 0 |
| Leading/trailing whitespace | 0 | 0 |
| Internal double space (`'%  %'`) | 1 | 0 |
| Contains a space (multi-token) | 2,893 | 520 |

No nulls, empties, whitespace-only, or trimmable edge whitespace. ✅

**One double-space row** (tashkeel): `أَنفُسِهِمْ  ۖ` at `11:31:27` (1 occurrence) — a stray extra
space before a waqf mark. Cosmetic; flagged for review.

### 6.2 Multi-token (space-containing) classification

A token is a "word token" if it contains an Arabic base letter (U+0621–U+064A). Tokens that are
standalone Quranic annotation symbols are not word tokens.

| Table | Space-containing forms | Mark-artifact (word + Quranic symbol) | Genuine multi-word |
|---|---:|---:|---:|
| unique_tashkeel | 2,893 | 2,891 | 2 |
| unique_simple | 520 | 518 | 2 |

**Mark artifacts (expected / faithful to source).** The vast majority are a word followed or
preceded by a Quranic annotation symbol from the QPC Uthmani text, e.g. `ٱللَّهِ ۚ`, `ٱللَّهِ ۖ`,
`۞ إِنَّ`. These are part of the source rendering, not data errors.

**Genuine multi-word forms (only 2, each 1 occurrence):**

| Tashkeel | Simple | Location | Classification |
|---|---|---|---|
| `إِلْ يَاسِينَ` | `ال ياسين` | `37:130:3` | **Known / expected** — the named segmentation case (Āl Yāsīn) |
| `دَآئِرَ ةٌۭ ۚ` | `دائر ةۭ` | `5:52:12` | **Needs review** — source word-segmentation of `دائرة` (the closing `ة` rendered as a separate token); faithful to source, not invented |

**Residual annotation marks that survive into the *simple* keys.** The simple normalization
removes some waqf marks (`ۖ` U+06D6, `ۚ` U+06DA — absent from all simple keys) but **retains**
others. Distinct non-letter symbols found inside the 520 space-containing simple keys:

| Symbol | Code point | Name (approx.) | Appears in keys |
|---|---|---|---:|
| `ۗ` | U+06D7 | waqf / pause mark | 391 |
| `۞` | U+06DE | start of rub‑el‑hizb (ornament) | 110 |
| `ۭ` | U+06ED | small low meem | 57 |
| `۟` | U+06DF | small high rounded zero | 30 |
| `۩` | U+06E9 | place of sajdah | 14 |
| `ۜ` | U+06DC | small high seen | 4 |
| `‏` | **U+200F** | **right-to-left mark (invisible bidi control)** | 1 |
| `ۧ` | U+06E7 | small high yeh | 1 |

- **383 of the 520** marked simple keys have a letters-only twin that already exists as its own
  clean simple key — i.e. the same simple word is split across two keys differing only by a
  residual mark (e.g. `الله ۗ` (49 occ) alongside `الله`). This is the main review item: it does
  not break uniqueness (the strings genuinely differ), but it means "simple" is not fully
  mark-insensitive.
- **One key contains an invisible U+200F (RTL) control character:** `العظيم ۩‏`
  (`quran_words_unique_simple.id = 58947`, `27:26:8`, 1 occ; a matching tashkeel key also
  exists). Worth review because the control character is not visible.

> Per the task rules these are classified, not "fixed": the mark artifacts and `ال ياسين` are
> **expected/known**; the residual-mark splitting, the `دائرة` segmentation, the double-space row,
> and the U+200F key are **needs-review** observations, not integrity failures.

---

## 7. Tashkeel-to-Simple Collapse Analysis

Different tashkeel vs simple totals are expected: many vocalized (tashkeel) forms collapse to a
single simple form. Because `quran_words_unique_tashkeel` carries `text_uthmani_simple`, the
collapse is derived directly from that table (no fragile join required).

| Metric | Value |
|---|---:|
| Distinct simple forms within unique_tashkeel | **15,826** |
| Rows in `quran_words_unique_simple` | **15,826** |
| → Match (clean bijection) | ✅ |
| Simple forms mapping to exactly 1 tashkeel form | 12,696 |
| Simple forms mapping to >1 tashkeel form | 3,130 |
| Max tashkeel forms for a single simple form | 20 |

The distinct simple forms inside the tashkeel table exactly equal the simple-table row count, so
the two tables are mutually consistent.

**Top simple forms by number of tashkeel variants:**

| Simple | Tashkeel forms | Total occurrences | Sample tashkeel forms |
|---|---:|---:|---|
| `ربكم` | 20 | 99 | `رَّبِّكُمْ` · `رَبُّكُمْ` · `رَبَّكُمْ` · `رَبِّكُمْ` · `رَبَّكُمُ` |
| `ربهم` | 18 | 105 | `رَبِّهِمْ` · `رَبَّهُم` · `رَّبِّهِمْ` · `رَبُّهُمْ` |
| `من` | 17 | 2,762 | `مِن` · `مِنَ` · `مِّن` · `مِّنَ` · `مِنْ` · `مَن` |
| `ربه` | 17 | 67 | `رَبِّهِۦ` · `رَبَّهُۥ` · `رَبُّهُۥ` · `رَّبِّهِۦ` |
| `ربك` | 15 | 218 | `رَبِّكَ` · `رَبَّكَ` · `رَبُّكَ` · `رَّبِّكَ` |
| `الله` | 13 | 2,103 | `ٱللَّهِ` · `ٱللَّهُ` · `ٱللَّهَ` · `ٱللَّهِ ۚ` · `ٱللَّهِ ۖ` |
| `ربنا` | 13 | 104 | `رَبَّنَا` · `رَبَّنَآ` · `رَبُّنَا` · `رَبِّنَا` |
| `ان` | 10 | 1,592 | `إِنَّ` · `أَن` · `إِن` · `أَنَّ` · `إِنْ` · `أَنْ` |

These collapses are the expected effect of removing vocalization/case-ending differences
(`مِن`/`مِنَ`/`مِّن` → `من`). They are **not** duplicates and **not** a defect. The sample for
`الله` also illustrates the Section 6 note: waqf-marked forms like `ٱللَّهِ ۚ` fold into `الله`,
while `ٱللَّهِ ۗ` does not (it becomes `الله ۗ`).

---

## 8. Consistency with Ordered Tables

| Check | Result | Status |
|---|---:|:---:|
| unique_tashkeel keys missing from ordered_tashkeel | 0 | ✅ |
| Distinct ordered_tashkeel keys missing from unique_tashkeel | 0 | ✅ |
| unique_simple keys missing from ordered_simple | 0 | ✅ |
| Distinct ordered_simple keys missing from unique_simple | 0 | ✅ |
| Occurrence mismatches (unique_tashkeel vs ordered count by `text_uthmani`) | 0 | ✅ |
| Occurrence mismatches (unique_simple vs ordered count by `text_uthmani_simple`) | 0 | ✅ |

Every unique key exists in the corresponding ordered table and vice-versa (perfect set
equality), and every stored `occurrences_count` equals the number of rows for that key in the
ordered table. No discrepancies.

---

## 9. First Occurrence Audit

For each unique key, the earliest occurrence was derived from the ordered table
(`DISTINCT ON (<text_key>) … ORDER BY <text_key>, word_order_in_mushaf`) and compared field by
field against the stored `first_*` columns.

| First-occurrence field | unique_tashkeel mismatches | unique_simple mismatches |
|---|---:|---:|
| `first_word_order_in_mushaf` | 0 | 0 |
| `first_quran_word_id` | 0 | 0 |
| `first_location` | 0 | 0 |
| `first_surah_number` | 0 | 0 |
| `first_ayah_number` | 0 | 0 |
| `first_page_number` | 0 | 0 |
| `first_line_number` | 0 | 0 |

**Every stored first-occurrence field, in both tables, matches the earliest row in the ordered
table.** No mismatches.

---

## 10. Findings

| # | Severity | Area | Finding |
|---|---|---|---|
| 1 | ✅ PASS | Counts | Both tables match the current rebuild exactly (21,294 / 15,826); occurrences sum to 77,432 each. |
| 2 | ✅ PASS | Duplicates | Zero duplicate keys in either table (DB-enforced `UNIQUE`, re-verified). |
| 3 | ✅ PASS | Null/empty/whitespace | No null, empty, whitespace-only, or trimmable edge whitespace. |
| 4 | ✅ PASS | Collapse | Distinct simple forms in unique_tashkeel (15,826) == unique_simple rows; collapses are expected vocalization variants. |
| 5 | ✅ PASS | Ordered consistency | Perfect set equality and zero occurrence mismatches with both ordered tables. |
| 6 | ✅ PASS | First occurrence | All 7 first-occurrence fields match the earliest ordered row, both tables. |
| 7 | ⚠️ NEEDS REVIEW | Normalization completeness | Simple form strips `ۖ`/`ۚ` but retains `ۗ`, `۞`, `۩`, `ۜ`. 520 simple keys carry a residual mark; **383** duplicate, in letters only, an existing clean key (e.g. `الله ۗ` vs `الله`). Not a uniqueness/consistency bug — a design question on how "simple" should treat pause/ornament marks. |
| 8 | ⚠️ NEEDS REVIEW | Hidden control char | One unique key (`العظيم ۩‏`, simple id 58947, plus a matching tashkeel key) contains an invisible U+200F RTL mark. |
| 9 | ⚠️ NEEDS REVIEW | Cosmetic whitespace | One tashkeel key has an internal double space: `أَنفُسِهِمْ  ۖ` (`11:31:27`). |
| 10 | ℹ️ EXPECTED / KNOWN | Segmentation | Genuine multi-word forms are limited to `ال ياسين` (`إِلْ يَاسِينَ`, `37:130:3`) and the source segmentation of `دائرة` → `دائر ةۭ` (`5:52:12`), each 1 occurrence. Faithful to source. |

---

## 11. Final Recommendation

The two unique display tables are **structurally sound and internally consistent**: counts match
the rebuild, keys are unique, every key reconciles bijectively with the ordered tables, occurrence
counts are exact, and first-occurrence metadata is correct. From a data-integrity standpoint they
**pass**.

The single thing worth a product/data decision (not a bug to silently fix) is **how the *simple*
form should treat residual Quranic annotation marks**. Today some waqf marks are removed and
others (`ۗ`, `۞`, `۩`) are kept, which splits 383 simple keys away from an otherwise-identical
clean key, and one key carries an invisible U+200F control character. If "simple" is intended to be
fully mark-insensitive, the team may want to revisit the simplification rule for these symbols in a
**separate, explicitly-scoped task** — preserving source traceability and avoiding any silent edit
to Quranic text. No action is required for correctness of the current rebuild.

*Report generated read-only against the live local database; no changes were made and nothing was
committed.*
