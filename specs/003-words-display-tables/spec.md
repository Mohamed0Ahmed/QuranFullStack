# Feature Specification: Quran Words Display Tables Foundation

**Feature Branch**: `003-words-display-tables`
**Created**: 2026-06-09
**Status**: Draft
**Input**: User description: "Build exactly four precomputed, read-only Quran word display tables (`quran_words_ordered_tashkeel`, `quran_words_ordered_simple`, `quran_words_unique_tashkeel`, `quran_words_unique_simple`) entirely from the existing imported database tables, excluding ayah markers, with an operator-run rebuild, hard-gated validation, a report, and tests. Backend/data foundation only — no API, UI, search, or morphology."

## Overview

Feature 002 imported the immutable Quran foundation into PostgreSQL: 114 surahs, 6,236
ayahs, 604 mushaf pages, 9,046 mushaf lines, and **83,668** word occurrences (6,236 ayah
markers + **77,432** readable words). A future words-display feature will need, per word
and per unique word, its position in the mushaf and its occurrence statistics. Computing
those statistics by aggregating 77,432 rows on every page view is wasteful and slow.

This feature precomputes that work **once** into four fixed, read-only derived tables,
built entirely from the already-imported data. It delivers the schema, an operator-run
rebuild, a hard-gated validation suite, a traceable report, and tests. It deliberately
delivers **no** API, UI, search, or linguistic enrichment.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Precomputed word display tables exist and are correct (Priority: P1)

As a data engineer preparing content for the future words-display page, I can build four
read-only tables from the existing imported data so that every readable word — and every
unique word form — carries its mushaf ordering and its occurrence/ayah/surah statistics as
stored values, ready to be read directly without any aggregation at read time.

**Why this priority**: This is the core deliverable. Without these populated tables there
is nothing for downstream features to read; everything else (validation, safe re-runs)
supports this.

**Independent Test**: Build the tables from a representative dataset and confirm the four
tables exist and are populated such that any readable word's row, and any unique word's
row, can be read in a single lookup with correct ordering and statistics — no runtime
grouping required.

**Acceptance Scenarios**:

1. **Given** the Feature 002 tables are populated, **When** the rebuild runs against empty
   target tables, **Then** all four derived tables are created and populated.
2. **Given** a populated ordered table, **When** a single readable word is looked up,
   **Then** its row exposes `quran_word_id`, `location`, `verse_key`, `surah_number`,
   `ayah_number`, `page_number`, `line_number`, the three word orders, the relevant text
   form(s), and `occurrences_count`/`ayahs_count`/`surahs_count` for its display key —
   with no aggregation performed at read time.
3. **Given** a populated unique table, **When** a distinct word form is looked up, **Then**
   its single row exposes its display text, the three statistics, and all first-occurrence
   fields for stable ordering.
4. **Given** a word form whose diacritized variants differ but whose simplified form is the
   same, **When** both unique tables are inspected, **Then** the variants are counted
   separately in the with-tashkeel table and merged in the without-tashkeel table.

---

### User Story 2 - Trustworthy, hard-gated rebuild with a report (Priority: P1)

As a data steward signing off on Quran data, I need the rebuild to validate every
structural and statistical invariant and to write **nothing** unless all hard checks pass,
and to emit a report I can read to confirm correctness without querying the database
myself.

**Why this priority**: Quran data must be demonstrably correct and traceable. Populating
tables without proof of correctness is unacceptable for this domain; the validation and
report are what make US1's output trustworthy.

**Independent Test**: Run the rebuild against data engineered to violate an invariant and
confirm the database is left unchanged, a failure report is produced, and the process
reports failure; run it against valid data and confirm a success report records the
totals and passed checks.

**Acceptance Scenarios**:

1. **Given** valid source data, **When** the rebuild completes, **Then** it confirms each
   ordered table has 77,432 rows, no markers are included, ordering is contiguous, and all
   counts match grouping — and a success report records these totals (including the actual
   derived unique counts).
2. **Given** source or intermediate data that violates any hard invariant, **When** the
   rebuild runs, **Then** no rows are committed to any of the four tables and a failure
   report identifies the violated invariant.
3. **Given** any rebuild run, **When** it finishes, **Then** a report (success or failure)
   exists capturing per-table totals, derived unique counts, validation results, and the
   outcome.

---

### User Story 3 - Safe, repeatable rebuild that never touches source data (Priority: P2)

As an operator who may need to rebuild more than once, I can re-run the rebuild safely: it
refuses to overwrite non-empty tables unless I explicitly force it, a forced run replaces
only the four derived tables, and no run ever alters the imported source tables.

**Why this priority**: Makes the rebuild operationally safe and repeatable. Valuable but
secondary to producing correct tables (US1) and proving them correct (US2).

**Independent Test**: Run the rebuild twice without forcing and confirm the second run is
refused with the data unchanged; run it forced and confirm only the four derived tables are
replaced while the source tables are byte-for-byte identical before and after.

**Acceptance Scenarios**:

1. **Given** the four target tables already contain data, **When** the rebuild runs without
   the force option, **Then** it refuses, changes nothing, and reports the refusal.
2. **Given** the force option is supplied, **When** the rebuild runs, **Then** only the four
   derived tables are truncated and repopulated.
3. **Given** any rebuild run (forced or not), **When** it completes, **Then**
   `quran_words`, `quran_ayahs`, and `quran_surahs` are unchanged in row count and content.
4. **Given** unchanged source data, **When** the rebuild is run twice with the force option,
   **Then** the resulting table contents are identical between runs.

---

### Edge Cases

- **Target tables already populated, no force**: refuse, leave all data unchanged, report
  the refusal, and exit with a non-success status.
- **Source data incomplete** (e.g., readable-word count ≠ 77,432): validation fails, the
  rebuild rolls back, and the report names the count mismatch. Nothing is committed.
- **Diacritic-collapsing forms**: two distinct `text_uthmani` values that share one
  `text_uthmani_simple` value yield two rows in the with-tashkeel unique table but one row
  in the without-tashkeel unique table (with-tashkeel unique count ≥ without-tashkeel
  unique count).
- **High-frequency word forms**: a form occurring many times within a single ayah/surah is
  counted correctly — `occurrences_count` may exceed `ayahs_count`, which may exceed
  `surahs_count`.
- **Invisible/whitespace differences in stored text**: because grouping uses the exact
  stored value (no normalization), any such differences produce distinct groups; the
  reported actual unique counts surface this for review.
- **Mid-rebuild failure** (e.g., interrupted run): the transaction is not committed, so the
  database retains its pre-run state — no partially built tables.

## Requirements *(mandatory)*

### Functional Requirements

#### Tables, source, and schema

- **FR-001**: The feature MUST provide exactly four new persisted, read-only derived
  tables: `quran_words_ordered_tashkeel`, `quran_words_ordered_simple`,
  `quran_words_unique_tashkeel`, and `quran_words_unique_simple`. No additional derived
  tables are introduced.
- **FR-002**: All four tables MUST be built solely from the existing imported tables —
  `quran_words` as the primary source, `quran_ayahs` used only for `verse_key` /
  validation / future joins, and `quran_surahs` used only if needed for validation. No new
  external source files are read.
- **FR-003**: All four tables MUST be derived from readable words only — rows where
  `quran_words.is_ayah_marker = true` MUST be excluded.
- **FR-004**: The four tables' schema MUST be created via a schema-only database migration
  with no embedded or seeded data.
- **FR-005**: None of the four tables MUST store ayah-level text. Ayah association MUST be
  by identifier only — `surah_number`, `ayah_number`, and `verse_key`. Any ayah text
  required by a later feature MUST be obtained by joining to `quran_ayahs`.

#### Ordered tables

- **FR-006**: Each ordered table MUST contain exactly one row per readable Quran word; the
  expected count is **77,432** rows per ordered table.
- **FR-007**: Each ordered row MUST include `quran_word_id`, `location`, `verse_key`,
  `surah_number`, `ayah_number`, `page_number`, `line_number`, `word_order_in_ayah`,
  `word_order_in_surah`, and `word_order_in_mushaf`.
- **FR-008**: `quran_words_ordered_tashkeel` MUST store `text_uthmani` (its display and
  grouping key), `text_uthmani_simple`, and `text_imlaei_simple`.
- **FR-009**: `quran_words_ordered_simple` MUST store `text_uthmani_simple` (its display
  and grouping key) and `text_imlaei_simple`.
- **FR-010**: Each ordered row MUST carry `occurrences_count`, `ayahs_count`, and
  `surahs_count` computed for that table's display/grouping key.

#### Unique tables

- **FR-011**: Each unique table MUST contain exactly one row per distinct display-text
  value — `quran_words_unique_tashkeel` grouped by `text_uthmani`,
  `quran_words_unique_simple` grouped by `text_uthmani_simple`.
- **FR-012**: `quran_words_unique_tashkeel` MUST store `text_uthmani` plus
  `text_uthmani_simple` and `text_imlaei_simple` taken from the first occurrence;
  `quran_words_unique_simple` MUST store `text_uthmani_simple` plus `text_imlaei_simple`
  taken from the first occurrence.
- **FR-013**: Each unique row MUST carry `occurrences_count`, `ayahs_count`, and
  `surahs_count`.
- **FR-014**: Each unique row MUST carry first-occurrence fields for stable ordering:
  `first_quran_word_id`, `first_location`, `first_surah_number`, `first_ayah_number`,
  `first_word_order_in_mushaf`, `first_page_number`, and `first_line_number`.
- **FR-015**: Unique-table row counts MUST be derived from the data and reported as actual
  values. The previously observed figures (~21,210 with tashkeel, ~14,783 without tashkeel)
  are informational only and MUST NOT be hardcoded as validation thresholds.

#### Statistics and grouping

- **FR-016**: `occurrences_count` MUST equal the number of readable word occurrences whose
  display/grouping key equals the row's display text.
- **FR-017**: `ayahs_count` MUST equal the number of distinct ayahs containing that display
  text (readable words only).
- **FR-018**: `surahs_count` MUST equal the number of distinct surahs containing that
  display text (readable words only).
- **FR-019**: Grouping MUST use the exact stored text value — no normalization, diacritic
  folding, whitespace transformation, or any other text transformation.

#### Ordering semantics

- **FR-020**: `word_order_in_mushaf` MUST be a contiguous reading-order rank over readable
  words starting at 1 with no gaps or duplicates (expected range 1..77,432).
- **FR-021**: `word_order_in_surah` MUST be a contiguous rank within each surah starting at
  1 with no gaps.
- **FR-022**: `word_order_in_ayah` MUST reflect the correct word order within its ayah.
- **FR-023**: For each unique row, the first-occurrence fields MUST correspond to the
  readable occurrence with the earliest `word_order_in_mushaf` for that display text, and
  MUST be consistent with the ordered tables.

#### Rebuild

- **FR-024**: The four tables MUST be populated by a precomputed rebuild, not by
  runtime/per-request aggregation. No request-path work is introduced.
- **FR-025**: The rebuild MUST be an operator-run action exposed through the existing data
  import console host as a `rebuild-words` verb — not via any network/HTTP endpoint.
- **FR-026**: The rebuild MUST be transactional/atomic — either all four tables are fully
  rebuilt and committed together, or nothing is written.
- **FR-027**: The rebuild MUST refuse to run if any of the four target tables is non-empty,
  unless an explicit force option (`--force`) is supplied.
- **FR-028**: With the force option, the rebuild MUST truncate and repopulate **only** the
  four derived tables.
- **FR-029**: No rebuild run MUST ever truncate, delete, or modify `quran_words`,
  `quran_ayahs`, `quran_surahs`, or any other Feature 002 table.
- **FR-030**: A forced rebuild MUST be idempotent — identical source data MUST yield
  identical contents in the four tables across runs.

#### Validation and reporting

- **FR-031**: Before committing, the rebuild MUST validate all of the following hard
  invariants; failure of any one MUST abort the build:
  - each ordered table has exactly 77,432 rows;
  - no ayah-marker rows are included;
  - each ordered table has exactly one row per readable `quran_words` row (a one-to-one
    correspondence);
  - `word_order_in_mushaf` is contiguous 1..77,432;
  - `word_order_in_surah` is contiguous within each surah;
  - `word_order_in_ayah` is correct within each ayah;
  - each unique table has exactly one row per distinct display text;
  - `occurrences_count`/`ayahs_count`/`surahs_count` match grouping computed directly from
    readable `quran_words`;
  - each unique row's first-occurrence fields match the earliest `word_order_in_mushaf` for
    its display text.
- **FR-032**: On any validation failure, the rebuild MUST roll back so that nothing is
  written, MUST produce a failure report, and MUST signal failure (non-success exit status).
- **FR-033**: Every rebuild run, whether it succeeds or fails, MUST produce a traceable
  report capturing per-table totals, the actual derived unique counts, the validation
  results, and the final outcome. A run that is **refused** because the target tables are
  non-empty and `--force` was not supplied (FR-027) is **not** a rebuild attempt: it
  reports the refusal to the operator (console message) and does not write a report
  artifact.

#### Scope guards (must-not)

- **FR-034**: `text_imlaei_simple` MUST be stored as a reference field for potential future
  use, but this feature MUST NOT implement any search behavior, search endpoints, search
  indexes, or normalized/`citext`/fuzzy search columns based on it or any other field.
- **FR-035**: The feature MUST NOT introduce API endpoints, frontend UI, or any
  request-path/runtime work.
- **FR-036**: The feature MUST NOT introduce morphology, corpus, roots, lemma, stem, POS,
  i3rab, tafsir, translations, audio, or mutashabihat data or behavior.

### Key Entities *(include if feature involves data)*

- **Ordered word (with tashkeel)** — `quran_words_ordered_tashkeel`: one record per
  readable word occurrence; display key `text_uthmani`. Holds the word's identity
  (`quran_word_id`, `location`), ayah identifiers (`surah_number`, `ayah_number`,
  `verse_key`), layout position (`page_number`, `line_number`), the three orderings
  (in-ayah, in-surah, in-mushaf), the relevant text forms, and the three statistics for its
  display key.
- **Ordered word (without tashkeel)** — `quran_words_ordered_simple`: one record per
  readable word occurrence; display key `text_uthmani_simple`. Same shape as above but
  display key, stored text forms, and statistics correspond to the simplified text.
- **Unique word (with tashkeel)** — `quran_words_unique_tashkeel`: one record per distinct
  `text_uthmani`. Holds the display text, the three statistics, the paired text forms from
  the first occurrence, and the first-occurrence ordering/identity fields.
- **Unique word (without tashkeel)** — `quran_words_unique_simple`: one record per distinct
  `text_uthmani_simple`. Same shape, grouped on the simplified text.
- **Rebuild operation**: an operator-run, transactional process that derives and populates
  the four tables from the source data, enforces refusal/force semantics, never touches the
  source tables, and validates before committing.
- **Rebuild report**: a per-run, human-readable artifact recording per-table totals, actual
  derived unique counts, validation results, and outcome — the traceability record for the
  build.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A downstream consumer can retrieve any readable word's occurrence, ayah, and
  surah statistics, and its mushaf/surah/ayah position, by reading a single row — with zero
  aggregation performed at read time.
- **SC-002**: Each ordered table contains exactly one row per readable word (77,432 rows)
  and zero ayah-marker rows.
- **SC-003**: Mushaf ordering covers 1..77,432 with no gaps and no duplicates; per-surah and
  per-ayah orderings are each contiguous from 1.
- **SC-004**: Each unique table contains exactly one row per distinct display text, and the
  actual unique counts (with- and without-tashkeel) are reported as concrete numbers.
- **SC-005**: A rebuild that detects any invariant violation leaves the database unchanged
  (no partial data in any of the four tables) and produces a failure report.
- **SC-006**: Re-running the rebuild on unchanged source data produces identical contents in
  all four tables.
- **SC-007**: After any rebuild run, `quran_words`, `quran_ayahs`, and `quran_surahs` are
  unchanged in row count and content.
- **SC-008**: Every rebuild run produces a report from which a reviewer can confirm totals,
  derived unique counts, and validation outcomes without querying the database directly.
- **SC-009**: An attempt to rebuild over non-empty target tables without the force option
  changes nothing and clearly reports the refusal.

## Assumptions

- The Feature 002 import is complete and correct (114 surahs, 6,236 ayahs, 604 pages, 9,046
  lines, 83,668 word occurrences of which 77,432 are readable). The rebuild depends on this
  data being present and valid.
- "Display text" grouping uses the exact stored string values; no text normalization is
  intended in this feature, because normalization is a search concern and search is out of
  scope.
- The expected 77,432 ordered-row count equals the readable-word count established by
  Feature 002; validation compares ordered rows against the readable-word count, so the
  expectation tracks the underlying data.
- Table names and the listed column names form this feature's data contract and are stated
  explicitly because downstream features will read these tables directly.
- The rebuild is an operator-run / CI-run batch action, not user-facing; its only outputs
  are console output and the report artifact. No Arabic UI messages or localization are in
  scope.
- Tests use fabricated, source-safe placeholder tokens rather than real Quranic text, in
  line with the workspace Quranic-data-safety rules; correctness of derivation logic is
  proven on controlled fixtures, and the real derived counts are reported rather than
  hardcoded.

## Dependencies

- The existing Feature 002 tables (`quran_words`, `quran_ayahs`, `quran_surahs`,
  `quran_mushaf_pages`, `quran_mushaf_lines`) and their current schema.
- The existing data import console host, which this feature extends with a new
  `rebuild-words` verb.
- Database schema-migration tooling, used to create the four tables (schema only, no seeded
  data), consistent with the workspace migration policy.

## Out of Scope

- Any HTTP/API endpoint or read service exposing these tables.
- Any frontend or UI.
- Search of any kind: search endpoints, search indexes, normalized/search-ready columns
  (beyond simply storing `text_imlaei_simple` as a passive reference value),
  `citext`/fuzzy/diacritic-insensitive matching, or any runtime query behavior.
- Linguistic or scholarly enrichment: morphology, corpus, roots, lemma, stem, POS, i3rab,
  tafsir, translations, audio, mutashabihat.
- New external source files; modification of any Feature 002 table; seeding data through
  migrations.
