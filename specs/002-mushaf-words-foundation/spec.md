# Feature Specification: Quran Mushaf Words & Layout Data Foundation

**Feature Branch**: `002-mushaf-words-foundation`
**Created**: 2026-06-08
**Status**: Draft
**Input**: User description: "Read the plan `docs/manhaj-qurani-mushaf-words-layout-data-foundation-plan.md` and create the spec per Spec Kit best practices. Generation only. Implementation will be done by a cheaper model, so the specification must be super clear."

> **Reference plan (authoritative HOW):** `docs/feature-002-quran-foundation/manhaj-qurani-mushaf-words-layout-data-foundation-plan.md`.
> **Reference data report (authoritative source facts & counts):** `resources/report/quran-mushaf-words-data-foundation-report.md`.
> This spec defines **WHAT** must be true and **WHY**. The technical **HOW** (frameworks, project layout, persistence mechanism) lives in the plan and in the upcoming `/speckit-plan` output.

---

## Clarifications

All major decisions were settled in the reference plan and are treated as fixed inputs to this spec:

- The import is run by an **operator** as a local, non-networked process. There is **no public or network-exposed import operation**.
- Source files are read from a **described ("manifested") staging set** with expected counts (and optional checksums) that is validated before any data is read.
- The **read API endpoint is out of scope** here and is deferred to a tiny follow-up feature (referred to as 001b).
- Re-running behaves as **refuse-unless-empty**, with an explicit **force** option that performs an atomic wipe-and-reload.
- **No search-normalized text field** is produced; the two no-tashkeel text forms are the searchable forms, and search normalization is a later feature.

### Session 2026-06-08

- Q: Does this feature create the database structure, or only load data? → A: This feature **creates the data structures/schema AND imports the data** — structure creation is an in-scope step performed before loading; the only external precondition is a reachable, empty database instance.
- Q: Are the 604 page fonts part of this feature? → A: **No.** Page fonts and the `quran_mushaf_pages` font columns are removed — they belong to a later public Mushaf Reader feature. `qpc_glyph` is **kept** on `quran_words` (from `qpc-v4.json`) as a lightweight future reference; the dashboard UI renders `text_uthmani`.

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Import the complete Quran foundation data (Priority: P1)

As the **data operator**, I run the import process once against the prepared source set so that the database holds the complete, correctly-linked Quran foundation: all surahs, all ayahs, all mushaf pages, all mushaf lines, and every word occurrence — each word carrying its mushaf glyph code and its readable text forms, and located to its exact page, line, and order on the page.

**Why this priority**: Every later feature (Mushaf page reader, Word details panel, Words explorer, Search, Morphology/i3rab) is a **read over this exact data**. Nothing downstream can be built or trusted until these five linked datasets exist. This is the Minimum Viable Product of the feature.

**Independent Test**: Run the import against empty tables using the prepared source set. Confirm the five datasets are populated with the exact expected totals (114 / 6,236 / 604 / 9,046 / 83,668) and that a sample word (e.g. location `2:25:3`) resolves to a single record carrying its glyph code, its three text forms, and its page/line/order.

**Acceptance Scenarios**:

1. **Given** empty Quran tables and a valid prepared source set, **When** the operator runs the import, **Then** the database contains exactly 114 surahs, 6,236 ayahs, 604 mushaf pages, 9,046 mushaf lines, and 83,668 word occurrences, and the run reports success.
2. **Given** a completed import, **When** any word occurrence is retrieved by its location key (`surah:ayah:word`), **Then** it returns exactly one record with its mushaf glyph code, its with-tashkeel text, its two without-tashkeel text forms, its surah/ayah/word numbers, and its page number, line number, and order within the line.
3. **Given** a completed import, **When** any mushaf page is reconstructed from its lines and their referenced words, **Then** the page's lines appear in order, each ayah line's words are contiguous and in order, and header/basmala lines appear in their correct positions with no words attached.
4. **Given** a completed import, **When** the words of any ayah are listed in order, **Then** the readable words appear first followed by exactly one ayah-end marker as the final item, and the marker is clearly flagged as a marker.

---

### User Story 2 - Prove the data is correct before it is trusted (Priority: P1)

As the **data operator**, I need the import to **validate** the assembled data against a fixed set of correctness rules and to **emit a validation report**, and I need it to **refuse to persist anything** if a hard rule is violated, so that no later feature is ever built on silently-wrong Quran data.

**Why this priority**: Quran data is source-sensitive and must never be silently wrong or invented. A correctness gate with a written, inspectable report is what lets every downstream feature stop re-checking and simply trust the tables.

**Independent Test**: Run the import against a source set with a deliberately introduced fault (e.g. a missing word occurrence, a duplicate location, or a wrong total). Confirm the run **aborts**, the tables remain unchanged (empty), and the report names the violated rule with concrete numbers.

**Acceptance Scenarios**:

1. **Given** a valid source set, **When** the import runs, **Then** it produces a validation report in both a human-readable form and a machine-readable form, listing per-rule totals, any mismatches/duplicates/missing items, and an overall verdict.
2. **Given** a source set that violates a hard rule (e.g. total word count ≠ 83,668, or a duplicate location, or a layout gap), **When** the import runs, **Then** no data is persisted, the process ends in a clearly failed state, and the report names the failed rule and the observed vs expected values.
3. **Given** the known word-count difference at ayah `37:130` (one source counts 4 words, the word records contain 3), **When** the import runs, **Then** this is recorded as a **warning** in the report and does **not** fail the import.
4. **Given** a successful import, **When** the operator reads the report, **Then** every correctness rule listed in this spec shows a pass (or an allowed warning) with its observed numbers.

---

### User Story 3 - Re-run the import safely and repeatably (Priority: P2)

As the **data operator**, I need re-running the import to be safe by default and to produce an identical result when intentionally forced, so that I can correct a source issue or re-load after a structure change without risking partial, duplicated, or corrupted Quran data.

**Why this priority**: Imports get re-run during setup and after fixes. Without a guard, a second run could duplicate or partially overwrite data. Safe, repeatable re-loading protects data integrity.

**Independent Test**: After a successful import, run the import again with no force option and confirm it refuses and changes nothing. Then run it with the force option and confirm the tables end in a state identical to the first successful import.

**Acceptance Scenarios**:

1. **Given** populated Quran tables, **When** the operator runs the import **without** the force option, **Then** it refuses to proceed, changes no data, and explains that the tables are not empty.
2. **Given** populated Quran tables, **When** the operator runs the import **with** the force option, **Then** the existing Quran data is replaced atomically and the resulting tables are identical (same totals, same keys, same values) to a fresh import.
3. **Given** a force re-run that fails validation partway, **When** the run aborts, **Then** the tables are left in their prior consistent state (no partial replacement is visible).

---

### Edge Cases

- **Ayah-end markers exist as words.** Each ayah's final word is an ayah-number marker (6,236 total, one per ayah). Markers MUST be stored (they are needed to render the page faithfully) and MUST be flagged as markers, and MUST be excluded from "real word" counts and from any future word/search listing.
- **Opening pages have fewer lines.** Pages 1 and 2 contain 8 lines each; every other page contains 15 lines. The import must accept this, not assume a uniform 15.
- **Header and basmala lines carry no words.** The 114 surah-header lines and the 112 basmala lines reference no word occurrences; they still must be stored so a page can be rendered with its headers.
- **Word-count discrepancy at `37:130`.** One metadata source counts 4 words; the word records treat `ال ياسين` as a single token (3 words). This is a known, documented difference — record both counts, treat the word records as canonical, and surface a **warning**, never a failure.
- **Two Uthmani encodings.** The ayah-level readable text and the word-level with-tashkeel text come from different sources and are not byte-identical. They MUST NOT be compared for exact equality during validation.
- **Source set incomplete or altered.** If an expected source file is missing, has the wrong record/file count, or fails its declared checksum, the import MUST stop before reading data and report exactly what is wrong.
- **Page fonts are out of scope.** The 604 page fonts are not read, copied, or validated in this feature; they belong to the later public Mushaf Reader. `qpc_glyph` is still stored on each word as a lightweight, non-rendered reference.

---

## Requirements *(mandatory)*

### Functional Requirements — datasets and content

- **FR-001**: The system MUST store the **114 surahs**, each with its number, Arabic name, simple (transliterated) name, transliterated display name, revelation place (Makkah or Madinah), revelation order, verse count, and whether a pre-bismillah applies.
- **FR-002**: The system MUST store the **6,236 ayahs**, each with its surah number, ayah number, verse key (`surah:ayah`), its readable ayah text, a **source word count** and a **computed real word count**, and the page range it spans.
- **FR-003**: The system MUST store the **604 mushaf pages**, each with its page number, the surah/ayah it begins and ends with, and its line count. Page fonts are out of scope (see *Out of Scope*); **no font fields are stored** on a page.
- **FR-004**: The system MUST store the **9,046 mushaf lines**, each with its page number, line number, line type (one of: ayah line, surah-header line, basmala line), whether it is centered, the surah number for header lines, and — for ayah lines only — the first and last word it contains and how many words it contains.
- **FR-005**: The system MUST store **83,668 word occurrences** (one per word position). Each occurrence MUST carry: a stable numeric id, its location key (`surah:ayah:word`), its surah/ayah/word numbers, its page number, line number, and order within the line, its mushaf glyph code, its with-tashkeel text, its uthmani-simple (no-tashkeel) text, its imlaei-simple (no-tashkeel) text, and a flag marking whether it is an ayah-end marker.
- **FR-006**: Of the 83,668 word occurrences, exactly **6,236 MUST be flagged as ayah-end markers** (one per ayah, always the last word of the ayah) and exactly **77,432 MUST be readable words** (not markers).
- **FR-007**: The system MUST NOT include a search-normalized text field on word occurrences in this feature. The two no-tashkeel forms are the searchable forms; search normalization is deferred.

### Functional Requirements — keys and linkage

- **FR-008**: Every word occurrence MUST be uniquely identifiable by its **location key** (`surah:ayah:word`); no two occurrences may share a location.
- **FR-009**: Word-occurrence numeric ids MUST be **unique and contiguous from 1 to 83,668**, and this id order MUST equal the mushaf reading order (so reading order is recoverable from ids alone).
- **FR-010**: Every word occurrence MUST link to a valid surah and a valid ayah, and MUST carry a valid page number and line number that match the line it belongs to.
- **FR-011**: Every ayah line MUST reference a valid, contiguous range of word-occurrence ids, and across all ayah lines these ranges MUST cover every id from 1 to 83,668 exactly once, with no gaps and no overlaps.
- **FR-012**: The readable text forms, the glyph code, and the metadata MUST be joined by the location key such that all sources agree on each occurrence's surah/ayah/word numbering (zero mismatches).

### Functional Requirements — import process

- **FR-013**: The import MUST be runnable by an operator as a **local, non-networked process**. It MUST NOT be exposed as a public or network-reachable operation.
- **FR-014**: Before reading any data, the import MUST validate the **prepared source set** against its declared description (expected file presence, expected record/file counts, and any declared checksums) and MUST stop with a clear message if anything does not match.
- **FR-015**: The import MUST **refuse to run** if any target Quran dataset already contains data, **unless** an explicit force option is supplied; with the force option it MUST replace existing Quran data atomically (all-or-nothing).
- **FR-016**: The import MUST validate the fully assembled data against the correctness rules (FR-018) **before** persisting, and MUST persist **nothing** if any hard rule is violated.
- **FR-017**: Every import run (success or failure) MUST produce a **validation report** in both a human-readable form and a machine-readable form, containing per-rule observed totals, any mismatches/duplicates/missing items, the `37:130` warning when applicable, and an overall verdict (pass / pass-with-warnings / fail), with traceability back to the source set.
- **FR-019**: Creating the database structures for all five datasets (the schema) is **in scope** for this feature and MUST be delivered as a step that precedes data loading. The only external precondition is a reachable, **empty** database instance; the structure itself is not assumed to pre-exist. *(Clarified 2026-06-08.)*

### Functional Requirements — validation rules (the correctness gate)

- **FR-018**: The import MUST verify all of the following and treat each as a **hard** rule unless marked otherwise. A hard failure aborts the import with nothing persisted.
  - Surah count = **114**; sum of surah verse counts = **6,236**.
  - Ayah count = **6,236**.
  - Page count = **604**.
  - Line count = **9,046**; every page has **15** lines except pages **1 and 2**, which have **8**.
  - Word-occurrence count = **83,668**; ayah-end markers = **6,236**; readable words = **77,432**.
  - Duplicate location keys = **0**; duplicate ids = **0**; ids contiguous **1..83,668**.
  - All word sources agree by location/id = **0 mismatches**.
  - Ayah-line id ranges cover **1..83,668** contiguously, with **0** gaps and **0** overlaps.
  - Every word occurrence has a page number and line number; every ayah line references valid first/last words.
  - Count of surahs with a pre-bismillah = count of basmala lines = **112**.
  - Each word occurrence's stored page/line/order matches the line that contains it.
  - Sample pages **1, 2, 5, and 604** reconstruct correctly from lines + words.
  - **Warning (not failure)**: the `37:130` source-vs-records word-count difference (source 4, records 3).
  - **Informational (do not equality-check)**: ayah-level readable text vs word-level with-tashkeel text use different encodings.

### Key Entities *(include if feature involves data)*

- **Surah**: One of the 114 chapters. Identified by surah number. Holds names (Arabic, simple, transliterated), revelation place and order, verse count, and pre-bismillah indicator. Parent of its ayahs.
- **Ayah**: One of the 6,236 verses. Identified by verse key (`surah:ayah`). Holds its readable text, source and computed word counts, and the page range it spans. Belongs to a surah; parent of its word occurrences.
- **Mushaf Page**: One of the 604 printed pages. Identified by page number. Holds its first/last surah and ayah, and its line count. Parent of its lines. (No font fields — page fonts are out of scope.)
- **Mushaf Line**: One of the 9,046 printed lines. Identified by page number + line number. Holds its type (ayah / surah-header / basmala), centered flag, the surah number for header lines, and — for ayah lines — the first and last word it contains and its word count. This is the authoritative line structure of the mushaf.
- **Word Occurrence**: One of the 83,668 word positions. Identified by a stable id (1..83,668, in reading order) and a unique location key (`surah:ayah:word`). Holds its surah/ayah/word numbers, its page/line/order placement, its mushaf glyph code, its three readable text forms, and a marker flag. Markers (6,236) are stored but flagged and excluded from readable-word counts.
- **Prepared Source Set**: The described, validated collection of input files (glyph words, three text-form word files, page layout, surah metadata, ayah metadata) with expected counts/checksums, used only as import input — not runtime data. (Page fonts are not part of the source set.)
- **Validation Report**: The per-run output (human + machine readable) recording observed totals, mismatches, warnings, traceability, and an overall verdict.

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: After a successful import into empty tables, the database holds exactly **114** surahs, **6,236** ayahs, **604** pages, **9,046** lines, and **83,668** word occurrences (of which **6,236** are markers and **77,432** are readable words).
- **SC-002**: **100%** of word occurrences have a unique location key and a page+line placement; there are **0** duplicate location keys and **0** duplicate ids, and ids are contiguous **1..83,668**.
- **SC-003**: The mushaf layout fully accounts for every word: ayah-line word ranges cover **1..83,668** with **0** gaps and **0** overlaps, and each word's stored page/line/order matches its line.
- **SC-004**: Sample pages **1, 2, 5, and 604** can be reconstructed in correct reading order from the stored lines and words, including correctly-placed header and basmala lines with no attached words.
- **SC-005**: Every import run produces a validation report whose verdict is **pass** or **pass-with-warnings** for a valid source set; the only allowed warning is the documented `37:130` difference.
- **SC-006**: Any source set that violates a hard rule results in **0 rows persisted** and a report that names the violated rule with observed vs expected values.
- **SC-007**: Re-running on populated tables without force results in **0 changes**; re-running with force yields tables equivalent in totals, keys, and values to a fresh import.

---

## Assumptions

- **Source provenance is fixed and trusted.** Glyphs and layout come from QPC v4 (King Fahd Complex); words and metadata come from QUL/Tarteel. These are assembled into the prepared source set before the import runs; assembling that set is a prerequisite, not part of the running import.
- **Quran data is immutable reference data.** Once imported it is read-only; later features read it and never mutate it. This is why per-word page/line/order may be stored directly on the word for fast reads.
- **The location key `surah:ayah:word` is canonical** for joining text forms, and the contiguous numeric word id (1..83,668) is canonical for joining to the layout. Both are present and fully aligned in the sources (verified in the data report).
- **The operator runs the import** intentionally; it is not triggered automatically by application startup and is not reachable over the network.
- **Reasonable defaults** chosen where the plan was silent: the validation report is emitted in both a human-readable and a machine-readable form to a configurable output location.

### Out of Scope (this feature)

- Any read/query API endpoint (the page-read endpoint is the separate follow-up **001b**).
- A search-normalized text field, full search, or search ranking.
- Unique-words grouping, roots, morphology, i3rab, tafsir, translations, audio, mutashabihat, and word meanings.
- Any frontend or UI (Mushaf reader, Words explorer, word panel).
- All page-font handling — the 604 page fonts and any page font-name/asset reference — which belongs to the later public Mushaf Reader feature; and any public/network-exposed import operation.

### Dependencies

- The **prepared source set** must exist and be described (counts/checksums) before the import can run.
- A reachable, **empty database instance** must be available. Creating the five datasets' structure (schema) is **in scope for this feature** (delivered as a step before the import); it is not an external precondition.
