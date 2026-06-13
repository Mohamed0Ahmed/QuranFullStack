# Feature Specification: Quran Mutashabihat Foundation

**Feature Branch**: `006-quran-mutashabihat-foundation`
**Created**: 2026-06-13
**Status**: Draft
**Input**: User description: "Read our plan — and according to the best practices of GitHub's Spec Kit, create the spec. Generation only. The implementation will be done using a cheaper model, so the specification and everything should be super clear."

> **Authoritative design inputs (read these — they are locked and must not be re-litigated):**
> - `docs/feature-006-quran-mutashabihat-foundation/feature-006-quran-mutashabihat-foundation-planning-report.md` — the locked v1 plan (tables, pipeline, validation taxonomy, open questions).
> - `docs/feature-006-quran-mutashabihat-foundation/mutashabihat-data-capability-report.md` — the validated source inventory (every count below was re-derived from the raw files there).
> - `resources/import-sources/mutashabihat/` — the staged source package the importer reads (`manifest.json`, `README.md`, `mutashabihat-ul-quran/phrases.json`, `similar-ayahs/matching-ayah.json`).
>
> This spec restates those decisions as testable requirements. Where this spec and those reports agree, both are authoritative; the reports hold the full per-file evidence.

---

## Overview (plain language)

The Quran contains many ayahs that share wording. Two pre-prepared datasets describe these
relationships, and Feature 006 imports them into the database so later features can use them:

1. **Mutashabihat ul Quran (repeated-phrase groups, المتشابهات اللفظية)** — a catalogue of **repeated
   phrases**. Each entry is one phrase that recurs across the Mushaf, listing every ayah where it
   appears together with the exact word positions of the phrase inside that ayah. Example: a phrase that
   recurs in 70 different ayahs is one group with 70+ occurrences.
2. **Similar Ayahs (آيات متشابهة)** — a list of **ayah-to-ayah similarity links**. Each link says
   "source ayah X is similar to target ayah Y" and carries a similarity `score`, a `coverage`
   percentage, and which words matched.

Feature 006 is a **backend data foundation only**: it reads the two local source files, maps every ayah
reference onto the existing `quran_ayahs` table, validates everything behind a hard gate, stores it in
three new read-only tables in a single all-or-nothing transaction, and writes an import report. It
builds **no** user interface, **no** API, and **no** read model. A later feature will read this data to
power a similar-ayah panel, phrase-occurrence navigation, search enrichment, and an ayah-relationship
graph.

The two datasets are **kept separate** (they describe different kinds of similarity at different grain)
and are stored with **references and word positions only — never any copied Quran text**.

---

## Clarifications

### Session 2026-06-13

These were the open decisions in the planning report. They are resolved here as locked v1 choices so
implementation has no ambiguity.

- Q: Should `coverage` values greater than 100 (4 known rows) be clamped on import? → A: **No.** Store
  the **raw** `coverage` exactly as in the source (range 5–200). Any clamping/normalisation is a future
  read-layer concern. The 4 rows >100 are recorded as a non-blocking warning, not an error.
- Q: Should the importer create reverse/undirected similar-ayah links so every link has its mirror? →
  A: **No.** Store the directed source→target links **exactly** as the source file has them (1,162
  source ayahs → 3,552 directed links). The ~1,120 one-way links stay one-way. Building an
  undirected/reverse view is future read-layer work, enabled by an index — not by stored rows.
- Q: Should `phrase_verses.json` (the verse→groups reverse index) be imported as a table? → A: **No.**
  It is a fully derivable reverse index of `phrases.json` and is **excluded** from storage. The same
  "which groups contain this ayah" lookup is served at read time by an index on the occurrences table.
- Q: The source `phrases.json` carries pre-computed counters (`surahs`, `ayahs`, `count`) that are
  stale for tens of groups. Trust them or recompute? → A: **Recompute** all stored counters from the
  actual occurrence data during import. The original stale counters MAY be kept verbatim in an audit
  column for traceability, but are never used as the stored counts.
- Q: How many repeated-phrase occurrences are stored when the source has a duplicate identical range? →
  A: The raw source contains **3,558** occurrence entries, including **1** duplicate identical occurrence
  (group 75, ayah 16:28). The duplicate is collapsed by the occurrence uniqueness rule, so the stored
  unique occurrence count is **3,557**.
- Q: Must every group have an occurrence row flagged as representative? → A: **At most one** occurrence
  row may be flagged representative per group. Normal groups whose `source.key` is present in their
  occurrence list have exactly one representative occurrence. The known anomalous group
  `source_group_id = 1782`, `source.key = 3:28`, is allowed to have **zero** representative occurrence
  rows; its group-level `representative_ayah_id`, `representative_word_from`, and
  `representative_word_to` are still stored from the source `source` metadata, and
  `MUT-SOURCE-KEY-ABSENT` records the non-blocking warning.
- Q: What does idempotency mean when target tables are already populated? → A: A **non-forced** run
  against non-empty target tables refuses and writes nothing. A **forced** re-import against an unchanged
  source produces identical stored data and counts.
- Q: How should unknown source provenance/license affect the v1 import? → A: Allow the v1 import with
  unknown provenance/license, but record it as a report warning and block any future publishing until
  provenance/license are resolved.
- Q: How should word-range index base be handled? → A: Treat word ranges as **1-based source indices**,
  store them unchanged, and keep `MUT-WORD-RANGE-UPPER-BOUND` as a warning only.
- Q: What should the default staged source package path be? → A: Keep
  `resources/import-sources/mutashabihat/` as the default source package path.

---

## User Scenarios & Testing *(mandatory)*

> "Users" here are the people and systems that depend on this data: the **operator** who runs the
> import (an admin / CI step), the **maintainer** who must trust the stored data is faithful to the
> source, and the **downstream feature** (a future read layer / UI) that will query these tables. There
> are **no end-user screens** in this feature.

### User Story 1 - Repeated-phrase groups become queryable data (Priority: P1)

The operator runs the import against the staged source package. Afterward, every repeated-phrase group
from `phrases.json` exists as a group row plus its occurrence rows, and **every** occurrence points at a
real ayah in `quran_ayahs`. The downstream feature can then ask "which repeated phrases include this
ayah?" and "what are all the occurrences of this phrase group?".

**Why this priority**: This is one of the two core deliverables. Without the group/occurrence tables
there is nothing for phrase-occurrence navigation to read. It is independently valuable even if the
similar-ayah dataset (US2) were never imported.

**Independent Test**: Run the import on the staged package, then query the group store: confirm exactly
**814** groups and **3,557** stored unique occurrences exist (from **3,558** raw source occurrence
entries after collapsing **1** duplicate identical occurrence), every occurrence's `ayah_id` resolves to
a `quran_ayahs` row, every group has **at least 2** distinct ayahs, and a spot check of a known group
(e.g. the group whose representative phrase is anchored at `2:23`) lists its expected member ayahs.

**Acceptance Scenarios**:

1. **Given** the staged `phrases.json`, **When** the import commits, **Then** there are exactly **814**
   rows in `quran_mutashabihat_groups` and **3,557** stored unique rows in
   `quran_mutashabihat_occurrences`, with the **1** duplicate identical raw occurrence collapsed.
2. **Given** any occurrence row, **When** its `ayah_id` is followed, **Then** it resolves to exactly one
   `quran_ayahs` row, and its `word_from`/`word_to` are positive with `word_to ≥ word_from`.
3. **Given** any group row, **When** its occurrences are counted, **Then** the group has **≥ 2** distinct
   member ayahs (no single-ayah groups), and **at most one** of its occurrences is flagged
   `is_representative = true`.
4. **Given** a group's stored `occurrence_count`, `distinct_ayah_count`, `distinct_surah_count`, **When**
   they are compared to its actual occurrence rows, **Then** they match the **recomputed** values (not
   the source's stale pre-computed numbers).
5. **Given** a normal group whose `source.key` is present in its occurrence list, **When** the import
   commits, **Then** exactly one occurrence is flagged `is_representative = true`.
6. **Given** the one source group whose representative phrase key is absent from its own occurrence list
   (group `source_group_id = 1782`, anchor `3:28`), **When** the import runs, **Then** the group is still
   imported with zero representative occurrence rows, its group-level representative fields remain
   populated from source metadata, and a non-blocking warning records the anomaly.

---

### User Story 2 - Similar-ayah links become queryable data (Priority: P1)

The operator's same import also loads the Similar Ayahs dataset from `matching-ayah.json`. Afterward,
every directed source→target similarity link exists as a row carrying its `score`, raw `coverage`,
matched-word count, and matched word ranges, with both ends pointing at real ayahs. The downstream
feature can then ask "what ayahs are similar to this ayah, and how strongly?".

**Why this priority**: This is the second core deliverable and is independently valuable. It is stored
in its own table, separate from the phrase groups, because it is a different kind of similarity (scored
pairwise links, not verbatim phrase membership).

**Independent Test**: Run the import, then query the link store: confirm exactly **3,552** directed
links across **1,162** distinct source ayahs, both `source_ayah_id` and `target_ayah_id` resolve to
`quran_ayahs`, **0** links are self-links, every `score` is within 50–100, and `coverage` values are the
**raw** source values (including the 4 rows greater than 100).

**Acceptance Scenarios**:

1. **Given** the staged `matching-ayah.json`, **When** the import commits, **Then** there are exactly
   **3,552** rows in `quran_similar_ayah_links` spanning exactly **1,162** distinct `source_ayah_id`
   values.
2. **Given** any link row, **When** `source_ayah_id` and `target_ayah_id` are followed, **Then** both
   resolve to `quran_ayahs` rows and `source_ayah_id ≠ target_ayah_id`.
3. **Given** the 4 source links whose `coverage` exceeds 100 (e.g. `56:27 → 56:38` with coverage 200),
   **When** they are read after import, **Then** their `coverage` is stored **raw** (200, not clamped),
   and a non-blocking warning records the count of coverage-over-100 rows.
4. **Given** the source's asymmetric pruning (≈1,120 one-way links with no stored reverse), **When** the
   import runs, **Then** **no** reverse links are synthesised; the stored link count is exactly the
   source's **3,552**.
5. **Given** any link's `match_words`, **When** it is read, **Then** it preserves the source list of word
   ranges exactly (each range is `[from, to]` or a single-word `[x]`).

---

### User Story 3 - Safe, repeatable import that never harms source or existing data (Priority: P2)

The operator must be able to (re)run the import safely. It reads the staged files and the existing
`quran_ayahs`, writes only the three new tables, commits all-or-nothing behind the validation gate, and
never alters `quran_ayahs`, `quran_words`, the Quran text, or the source files. If a target table is
already populated, it refuses unless an explicit **force** option is given.

**Why this priority**: Quranic data integrity is non-negotiable and a foundation must be rebuildable.
This story is what makes the import safe to run in CI and to re-run after a source refresh.

**Independent Test**: (a) Snapshot `quran_ayahs`, `quran_words`, and the source file checksums, run the
import, and confirm all are unchanged afterward. (b) Run a forced re-import against the unchanged source
and confirm it produces identical stored data and counts. (c) Run against already-populated tables
without force and confirm it refuses and writes nothing. (d) Force a hard-check failure and confirm a
full rollback (all three tables empty, non-zero exit, failure report written).

**Acceptance Scenarios**:

1. **Given** a successful import, **When** `quran_ayahs` and `quran_words` are compared before and after,
   **Then** they are byte/row identical (the import only reads them).
2. **Given** the staged source files, **When** their checksums are compared before and after a run,
   **Then** they are unchanged (the import never writes to the source).
3. **Given** already-populated mutashabihat tables, **When** the import is run without force, **Then** it
   refuses with a clear message and writes nothing; **When** run with force, **Then** it cleanly
   replaces the data and ends with the same stored data and counts as a fresh import.
4. **Given** a hard validation check fails during a run, **When** the gate evaluates, **Then** nothing is
   committed (all three tables remain empty / unchanged), a failure report is written, and the process
   exits non-zero.
5. **Given** the staged package is missing a required file or a file's checksum does not match the
   manifest, **When** the import is invoked, **Then** it refuses before writing anything and reports the
   mismatch.

---

### User Story 4 - Every run is validated and produces a trustworthy report (Priority: P2)

The maintainer must be able to trust the import without re-reading the raw files. Each run validates the
source against a fixed set of hard checks (which gate the commit) and warning/informational checks
(which never block), then writes a single human-readable report listing every check's result, the row
counts written, and every recorded anomaly.

**Why this priority**: The report is how the team confirms the foundation is correct and how known
anomalies are surfaced without failing the build. It depends on US1/US2 but is independently
demonstrable by inspecting the report artifact.

**Independent Test**: Run the import and open the report: confirm it lists each hard check as passed,
the exact written counts (814 / 3,557 / 3,552 / 1,162), the raw source occurrence count (**3,558**),
the warning counts (coverage-over-100 = 4, duplicate-occurrence = 1, source-key-absent = 1,
stale-counter groups), and the informational figures (one-way links, cross-dataset overlap, surah
coverage).

**Acceptance Scenarios**:

1. **Given** a completed run, **When** the report is read, **Then** it contains, for each hard check, an
   id and a pass/fail result, and the run's final outcome (committed / rolled back).
2. **Given** the known source anomalies, **When** the report is read, **Then** it records: coverage>100
   = **4**, duplicate identical occurrence = **1** (group 75, ayah 16:28), group whose source key is
   absent from its occurrences = **1** (group 1782, 3:28), and the count of groups whose source counters
   were stale and recomputed.
3. **Given** a completed run, **When** the report is read, **Then** it includes informational figures:
   one-way similar links (≈1,120), cross-dataset overlap (792 ayahs / 813 pairs), and surah coverage
   (109 / 114).
4. **Given** any run that begins building (passes the early refusals), **When** it finishes (commit or
   rollback), **Then** a report artifact is written; early refusals (missing file, checksum mismatch,
   non-empty without force) report to the console and write no report.

---

### User Story 5 - Read-time queries are enabled without extra stored tables (Priority: P3)

The downstream feature can answer the key relationship questions directly from the three tables, with no
additional stored structures: "all mutashabihat of an ayah", "all occurrences under a phrase group", and
"similar ayahs of an ayah" (and, when wanted, the incoming/undirected view).

**Why this priority**: It proves the modeling choice (no `phrase_verses` table, no stored reverse edges)
is sufficient and documents the exact read recipes for the next feature. It depends on US1/US2 but is
independently demonstrable with read-only queries.

**Independent Test**: For a sample ayah, (a) find its repeated-phrase groups by querying occurrences on
`ayah_id`; (b) list a group's occurrences by querying on `group_id`; (c) find its outgoing similar links
by `source_ayah_id` and its incoming ones by `target_ayah_id` — all using only the three tables.

**Acceptance Scenarios**:

1. **Given** an ayah that appears in several phrase groups, **When** the occurrences table is queried by
   its `ayah_id`, **Then** all of its groups are returned (the same answer `phrase_verses.json` would
   give), with **no** `phrase_verses` table stored.
2. **Given** a phrase group, **When** the occurrences table is queried by its `group_id`, **Then** all of
   that group's occurrences are returned.
3. **Given** an ayah, **When** the links table is queried by `target_ayah_id`, **Then** its incoming
   similarity links are returned, enabling an undirected view at read time **without** stored reverse
   rows.

---

### Edge Cases

- **An ayah belongs to many phrase groups (1–7):** ayah↔group is many-to-many, realised through
  occurrence rows; querying occurrences by `ayah_id` returns every group. (See FR-006, FR-027.)
- **Duplicate identical occurrence in the source (1 known: group 75, ayah 16:28 has `[[17,19],[17,19]]`):**
  the raw source has **3,558** occurrence entries; this duplicate is collapsed by the occurrence
  uniqueness constraint, yielding **3,557** stored unique occurrences. The duplicate is recorded as a
  non-blocking warning, not an error. (See FR-009, FR-030.)
- **A group's representative phrase key is absent from its own occurrence list (1 known: group 1782,
  `3:28`):** the group is still imported; a non-blocking warning records it. The representative word
  range and representative ayah are still stored on the group from source metadata. The group has zero
  representative occurrence rows. (See FR-008, FR-030.)
- **`coverage` greater than 100 (4 known rows):** stored raw (not clamped); recorded as a non-blocking
  warning. (See FR-016, FR-030.)
- **One-way similar links with no reverse (≈1,120):** stored exactly as-is; **no** reverse row is
  invented. Recorded as an informational figure. (See FR-015, FR-031.)
- **A word range whose upper index exceeds the ayah's word count:** flagged as a non-blocking warning
  (possible source/corpus alignment mismatch), never silently dropped or rewritten. (See FR-030.)
- **Source file changed mid-run (checksum drifts between read and commit):** the run refuses to commit
  and rolls back rather than persisting possibly-inconsistent data. (See FR-024.)
- **A referenced verse_key that does not resolve to `quran_ayahs`:** this is a **hard** failure (the
  whole run rolls back). In the validated source there are **zero** such references. (See FR-014, FR-029.)
- **`quran_ayahs` is empty or missing:** the import refuses (it cannot resolve any reference) and writes
  nothing. (See FR-023.)

---

## Requirements *(mandatory)*

### Functional Requirements

**Scope & datasets**

- **FR-001**: The system MUST import **two independent datasets** from the staged source package and
  store them in **three new read-only tables**: repeated-phrase groups
  (`quran_mutashabihat_groups` + `quran_mutashabihat_occurrences`) from
  `mutashabihat-ul-quran/phrases.json`, and similar-ayah links (`quran_similar_ayah_links`) from
  `similar-ayahs/matching-ayah.json`. The two datasets MUST remain in **separate tables**; the system
  MUST NOT merge them into one shared/polymorphic relations table.
- **FR-002**: The system MUST store **references and word positions only**. It MUST NOT copy any Quran
  ayah text into the new tables; ayahs are referenced exclusively by `ayah_id` (a foreign key into
  `quran_ayahs`).

**Repeated-phrase groups — `quran_mutashabihat_groups`**

- **FR-003**: The system MUST create `quran_mutashabihat_groups` with one row per repeated phrase
  (**814 rows**). Required columns: `id` (integer primary key, surrogate), `source_group_id` (integer,
  the opaque phrase id from `phrases.json`, range 50–16746, **NOT NULL, UNIQUE**),
  `representative_ayah_id` (integer **NOT NULL**, foreign key → `quran_ayahs.id`, from the source
  `source.key`), `representative_word_from` (smallint **NOT NULL**), `representative_word_to` (smallint
  **NOT NULL**), `occurrence_count` (smallint **NOT NULL**, recomputed), `distinct_ayah_count` (smallint
  **NOT NULL**, recomputed), `distinct_surah_count` (smallint **NOT NULL**, recomputed), and
  `raw_source_counts` (**jsonb NULL** — the original `{surahs, ayahs, count}` for audit only).
- **FR-004**: `quran_mutashabihat_groups` MUST have a **unique** index on `source_group_id` (the
  idempotency/provenance key) and a non-unique index on `representative_ayah_id`.
- **FR-005**: The three count columns (`occurrence_count`, `distinct_ayah_count`, `distinct_surah_count`)
  MUST be **recomputed** from the group's stored unique occurrence rows after dedupe during import. The
  source's pre-computed `surahs`/`ayahs`/`count` MUST NOT be used as the stored counts; they MAY be
  preserved verbatim in `raw_source_counts`. Every group MUST have `distinct_ayah_count ≥ 2` (no
  single-ayah groups).

**Repeated-phrase occurrences — `quran_mutashabihat_occurrences`**

- **FR-006**: The system MUST create `quran_mutashabihat_occurrences` with one row per unique (group,
  ayah, word-range) occurrence (**3,557 stored rows**), derived from **3,558** raw source occurrence
  entries after collapsing **1** duplicate identical occurrence. Required columns: `id` (integer primary
  key, surrogate), `group_id` (integer **NOT NULL**, foreign key →
  `quran_mutashabihat_groups.id`, **ON DELETE CASCADE**), `ayah_id` (integer **NOT NULL**, foreign key →
  `quran_ayahs.id`), `word_from` (smallint **NOT NULL**), `word_to` (smallint **NOT NULL**), and
  `is_representative` (boolean **NOT NULL, default false**).
- **FR-007**: `quran_mutashabihat_occurrences` MUST enforce a **unique** constraint on
  (`group_id`, `ayah_id`, `word_from`, `word_to`) — this collapses the one known duplicate occurrence —
  and MUST have a non-unique index on `ayah_id` (the "all mutashabihat of this ayah" lookup).
- **FR-008**: For each group, **at most one** occurrence MUST be flagged
  `is_representative = true` — the one matching the source's `source` phrase (`source.key` +
  `source.from`/`source.to`). For normal groups whose `source.key` is present in the group's occurrence
  list, exactly one occurrence MUST be flagged representative. For the known anomalous group
  `source_group_id = 1782`, `source.key = 3:28`, zero representative occurrence rows are allowed; the
  group-level `representative_ayah_id`, `representative_word_from`, and `representative_word_to` remain
  populated from the source `source` metadata, and the anomaly is recorded as warning
  `MUT-SOURCE-KEY-ABSENT` (FR-030).
- **FR-009**: Word ranges MUST be treated as **1-based source indices** and stored unchanged, with
  `word_from ≥ 1` and `word_to ≥ word_from`. Duplicate identical ranges within the same (group, ayah)
  are collapsed by FR-007 and counted as a warning. If a range's upper index exceeds the referenced
  ayah's word count, the row is still stored unchanged and reported via `MUT-WORD-RANGE-UPPER-BOUND`
  (FR-030).

**Similar-ayah links — `quran_similar_ayah_links`**

- **FR-010**: The system MUST create `quran_similar_ayah_links` with one row per **directed** source→target
  similarity link (**3,552 rows** across **1,162** distinct source ayahs). Required columns: `id`
  (integer primary key, surrogate), `source_ayah_id` (integer **NOT NULL**, foreign key →
  `quran_ayahs.id`), `target_ayah_id` (integer **NOT NULL**, foreign key → `quran_ayahs.id`), `score`
  (smallint **NOT NULL**), `coverage` (smallint **NOT NULL**, raw), `matched_words_count` (smallint
  **NOT NULL**), and `match_words` (**jsonb NOT NULL** — the source list of word ranges on the source
  ayah, each `[from, to]` or single-word `[x]`).
- **FR-011**: `quran_similar_ayah_links` MUST enforce a **unique** constraint on
  (`source_ayah_id`, `target_ayah_id`), a **CHECK** that `source_ayah_id <> target_ayah_id` (no
  self-links), and a non-unique index on `target_ayah_id` (the incoming/undirected read view).
- **FR-012**: `score` MUST be stored exactly as the source (observed range 50–100). `coverage` MUST be
  stored **raw** exactly as the source (observed range 5–200); the system MUST NOT clamp, cap, or
  normalise it. `match_words` MUST preserve the source ranges exactly.

**Ayah mapping (canonical target = `quran_ayahs`)**

- **FR-013**: Every ayah reference in both datasets — the group `source.key`, every occurrence
  `verse_key`, and both ends of every similar link — MUST be resolved to an existing `quran_ayahs` row
  by matching the source `verse_key` (format `surah:ayah`) to `quran_ayahs.verse_key`, and stored as the
  corresponding integer `ayah_id`. The new tables MUST store `ayah_id` foreign keys, **not** raw
  verse_key strings.
- **FR-014**: If **any** referenced verse_key fails to resolve to a `quran_ayahs` row, the run MUST fail
  the hard gate and roll back (write nothing). In the validated source, all **3,084** distinct referenced
  verse_keys resolve (0 invalid, 0 missing), so a successful run resolves **100%** of references.

**Faithful storage & exclusions**

- **FR-015**: The system MUST store the similar-ayah links **directed and exactly as the source** — it
  MUST NOT synthesise or persist reverse/mirror links. The ≈1,120 one-way links remain one-way; any
  undirected/reverse behaviour is future read-layer work served by the `target_ayah_id` index (FR-011).
- **FR-016**: The system MUST NOT clamp `coverage` (FR-012); the 4 rows with coverage > 100 are stored
  raw and surfaced as a warning (FR-030), not corrected.
- **FR-017**: The system MUST NOT store `phrase_verses.json` as a table. The verse→groups lookup it
  represents MUST instead be derivable at read time from the `ayah_id` index on
  `quran_mutashabihat_occurrences` (FR-007). `phrase_verses.json` MAY be used only as an optional
  derived-consistency cross-check; it is excluded from the staged package and from storage.

**Source package & manifest**

- **FR-018**: The import MUST read **only** the staged local source package at
  `resources/import-sources/mutashabihat/` (overridable by an explicit source path argument). It MUST NOT
  read the original `resources/mutashabihat/` working folder at runtime.
- **FR-019**: The import MUST verify the staged package against its `manifest.json` before building: the
  required file set MUST be exactly `{mutashabihat-ul-quran/phrases.json, similar-ayahs/matching-ayah.json}`
  (plus `manifest.json`, `README.md`), and each source file's checksum and byte size MUST match the
  manifest. A missing file, an unexpected extra source file, or a checksum/size mismatch MUST cause an
  early refusal with no writes and no report artifact.

**Process, rebuild & gate**

- **FR-020**: The import MUST be an **operator/CI-run console action** (a command-line verb,
  `import-mutashabihat`, accepting an optional source path, an optional report-output path, and a
  `--force` flag). It MUST NOT be exposed as an HTTP endpoint and MUST NOT run on any request path.
- **FR-021**: The import MUST be **transactional and gated**: assemble both datasets in memory (resolving
  ayah ids and recomputing counters), run all hard checks (FR-029), and commit **only if all pass**; on
  any hard-check failure it MUST roll back (write nothing), emit a failure report, and exit non-zero.
- **FR-022**: The import MUST use explicit safe re-run semantics: a non-forced run against non-empty
  target tables MUST refuse and write nothing; a forced re-import against an unchanged source MUST
  cleanly replace all three tables and produce identical stored data and counts.
- **FR-023**: The import MUST detect that `quran_ayahs` is missing/empty (so references cannot resolve)
  and **refuse** with a clear message, writing nothing.
- **FR-024**: The import MUST re-verify the source checksums (FR-019) after assembly and before commit
  (a **source-unchanged** check); if the source changed mid-run, it MUST roll back rather than commit.

**Source-data preservation (hard guarantees)**

- **FR-025**: The import MUST NOT modify `quran_ayahs` in any way (it is read-only — used only to resolve
  `verse_key → ayah_id`).
- **FR-026**: The import MUST NOT modify `quran_words`, the Quran text (Uthmani/QPC) columns, or any other
  existing feature's tables.
- **FR-027**: The import MUST NOT mutate the source files; they are read-only inputs (enforced by FR-024).
- **FR-028**: The new tables MUST store **no Quran ayah text** — only identifiers (`ayah_id`), word
  positions, and the similarity metrics (FR-002).

**Validation checks**

- **FR-029**: Each run MUST enforce these **hard checks** (any failure ⇒ rollback + failure report +
  non-zero exit):
  - `MUT-MANIFEST-SET` — the staged file set is exactly the required set (FR-019).
  - `MUT-MANIFEST-CHECKSUM` — each source file's checksum + byte size match the manifest.
  - `MUT-JSON-SHAPE` — both roots are objects; group values carry `{source, ayah}`; each similar item
    carries `{matched_ayah_key, score, coverage, matched_words_count, match_words}`.
  - `MUT-GROUP-COUNT` — group count equals the manifest's expected **814**.
  - `MUT-RAW-OCCURRENCE-COUNT` — raw occurrence entries in `phrases.json` equal **3,558**.
  - `MUT-STORED-OCCURRENCE-COUNT` — stored unique occurrence rows after dedupe equal **3,557**.
  - `MUT-SIMILAR-SOURCE-COUNT` — distinct source-ayah count equals the manifest's expected **1,162**.
  - `MUT-SIMILAR-LINK-COUNT` — directed link count equals **3,552**.
  - `MUT-VERSEKEY-FORMAT` — every reference matches `surah:ayah` (`^\d+:\d+$`).
  - `MUT-AYAH-RESOLVE` — every referenced verse_key resolves to a `quran_ayahs` row (0 unresolved).
  - `MUT-WORD-RANGE-SHAPE` — every occurrence and every `match_words` range has `from ≥ 1` and
    `to ≥ from`.
  - `MUT-GROUP-MIN-SIZE` — every group has ≥ 2 distinct member ayahs.
  - `MUT-LINK-NO-SELF` — no similar link has `target == source`.
  - `MUT-SCORE-RANGE` — every link `score` is within 50–100.
  - `MUT-OCCURRENCE-UNIQUE` — after dedupe, occurrences are unique on
    (`group_id`, `ayah_id`, `word_from`, `word_to`).
  - `MUT-SOURCE-UNCHANGED` — source checksums re-verified after assembly, before commit (FR-024).
- **FR-030**: Each run MUST evaluate these **warning checks** (recorded in the report; never gate the
  build):
  - `MUT-COVERAGE-GT-100` — count of links with `coverage > 100` (expected **4**).
  - `MUT-DUPLICATE-OCCURRENCE` — count of identical occurrence ranges collapsed by FR-007 (expected
    **1**: group 75, ayah 16:28).
  - `MUT-SOURCE-KEY-ABSENT` — count of groups whose `source.key` is absent from their own occurrences
    (expected **1**: group 1782, 3:28); this known anomaly allows zero representative occurrence rows
    while keeping the group-level representative fields populated from source metadata.
  - `MUT-STALE-SOURCE-COUNTERS` — count of groups whose source `surahs`/`ayahs`/`count` disagreed with
    the recomputed values (recomputed values win).
  - `MUT-WORD-RANGE-UPPER-BOUND` — count of word ranges whose upper index exceeds the referenced ayah's
    word count in `quran_ayahs` (`words_count_real`) — a possible source/corpus alignment mismatch;
    ranges remain stored unchanged.
  - `MUT-PROVENANCE-LICENSE-UNKNOWN` — source provenance/license remains unknown in the staged manifest
    (expected **2** source files until resolved); this never gates the v1 import but blocks future
    publishing.
- **FR-031**: Each run MUST evaluate these **informational checks** (recorded in the report; never gate):
  - `MUT-ONEWAY-LINKS` — count of directed links with no stored reverse (≈ **1,120**).
  - `MUT-CROSS-DATASET-OVERLAP` — ayahs and undirected pairs shared by both datasets (≈ **792** ayahs /
    **813** pairs).
  - `MUT-SURAH-COVERAGE` — distinct surahs referenced (**109 / 114**) and total distinct ayahs referenced
    (**3,084**).
  - `MUT-PHRASE-VERSES-CONSISTENCY` *(optional)* — if `phrase_verses.json` is supplied for cross-check,
    confirm it is a consistent reverse index of `phrases.json`; it is never stored.

**Reporting**

- **FR-032**: Each run that begins building MUST produce a single human-readable **report artifact**
  (default location `resources/report/mutashabihat/`) containing: the written row counts (groups, stored
  unique occurrences, links, distinct source ayahs), the raw source occurrence count, every hard check's
  pass/fail, every warning check's count, every informational figure, and the final outcome (committed /
  rolled back). Early refusals (missing file, checksum mismatch, non-empty tables without force, missing
  `quran_ayahs`) report to the console and write no report artifact.

### Key Entities *(include if feature involves data)*

- **Mutashabihat Group** (`quran_mutashabihat_groups`) — one repeated phrase. Identity is the surrogate
  `id`; provenance is `source_group_id` (unique). Carries the representative occurrence (anchor ayah +
  word range), recomputed counts, and an optional raw-counter audit blob. **814 rows.** Source of truth:
  `phrases.json`.
- **Mutashabihat Occurrence** (`quran_mutashabihat_occurrences`) — one appearance of a group's phrase in
  an ayah, as a positional word range. Grain: one row per (group, ayah, word-range). Many occurrences per
  group; many groups per ayah (1–7). **3,557 stored unique rows**, derived from **3,558** raw source
  occurrence entries after **1** duplicate identical occurrence is collapsed.
- **Similar Ayah Link** (`quran_similar_ayah_links`) — one **directed** source→target similarity edge with
  `score`, raw `coverage`, matched-word count, and matched word ranges. Grain: one row per directed pair.
  Stored faithfully and directed (no reverse rows synthesised). **3,552 rows** over **1,162** source
  ayahs. Source of truth: `matching-ayah.json`.
- **Source Package / Manifest** (`resources/import-sources/mutashabihat/`) — the staged, read-only input:
  the two truth files in dataset subfolders plus `manifest.json` (checksums, byte sizes, expected counts)
  and `README.md`. The import's only runtime input besides `quran_ayahs`.
- **Ayah → mutashabihat lookup (derived, not stored)** — "which groups / similar ayahs relate to this
  ayah", answered at read time from the occurrence `ayah_id` index and the link `source/target` indices.
  Explicitly **not** a stored table (no `phrase_verses` table, no stored reverse links).

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A successful import stores exactly **814** groups, **3,557** stored unique occurrences,
  **3,552** directed links, and **1,162** distinct similar-ayah sources. The repeated-phrase source still
  has **3,558** raw occurrence entries, with **1** duplicate identical occurrence collapsed during storage.
- **SC-002**: **100%** of referenced verse_keys (all **3,084** distinct) resolve to a `quran_ayahs` row;
  **0** occurrence or link rows reference a non-existent ayah; **0** similar links are self-links.
- **SC-003**: **100%** of groups have ≥ 2 distinct member ayahs, every group's stored counts equal the
  **recomputed** values (not the source's stale pre-computed numbers), every normal group has exactly one
  representative occurrence, and the known group `1782` has zero representative occurrence rows while
  retaining group-level representative fields from source metadata.
- **SC-004**: `coverage` is stored **raw** for **100%** of links — the **4** rows with coverage > 100 are
  retained unchanged (0 clamped) — and **0** reverse/mirror links are synthesised (stored link count is
  exactly the source's 3,552).
- **SC-005**: The stored model is exactly **three** new tables; there is **no** `phrase_verses` table,
  **no** merged/polymorphic relations table, and **no** Quran ayah text in any new table.
- **SC-006**: A run changes **0** rows in `quran_ayahs` and `quran_words`, and leaves the source files'
  checksums unchanged (verified before/after).
- **SC-007**: A forced re-import against the unchanged source yields **0** differences from the previous
  stored data and counts; running against already-populated tables without force refuses and writes **0**
  rows; a forced hard-check failure rolls back with **0** rows committed.
- **SC-008**: **100%** of runs that begin building emit a report containing the written counts
  (814 / 3,557 / 3,552 / 1,162), the raw source occurrence count (3,558), every hard-check result, every
  warning count (coverage>100 = 4, duplicate-occurrence = 1, source-key-absent = 1, provenance/license
  unknown = 2 source files, stale-counter group count), and the informational figures (one-way links,
  cross-dataset overlap, surah coverage 109/114).
- **SC-009**: The downstream feature can answer "all groups of an ayah", "all occurrences of a group",
  and "similar ayahs of an ayah (outgoing and incoming)" using only the three tables — with **0**
  additional stored structures.

---

## Out of Scope (v1)

- **No UI** — no pages, components, panels, or any Frontend work.
- **No API endpoint** — no controllers, request/response contracts, or runtime read path.
- **No read model** — no read-optimised views, projections, or query services beyond the three base
  tables and their indexes.
- **No tafsir, translations, or audio.**
- **No stored reverse/undirected similar-ayah links** and **no clamped/normalised coverage** — these are
  read-layer behaviour for a later feature (documented, not built here).
- **No `phrase_verses` table** and **no merged/polymorphic ayah-relations table.**
- **No edits** to `quran_ayahs`, `quran_words`, the Quran text, or any other feature's tables.
- **No copying of Quran ayah text** into the new tables.
- **No discovery work** — the source is already staged, checksummed, and validated; no new data sourcing
  is required.

---

## Assumptions

- **`quran_ayahs` (Feature 002) is complete and available.** Its `verse_key` (unique, `surah:ayah`) and
  stable integer `id` are the canonical mapping target. The import reads it read-only.
- **Branch base.** Earlier foundation features are not yet merged to `main`; this feature's branch
  (`006-quran-mutashabihat-foundation`) is cut so that `quran_ayahs` and the prior import discipline are
  available.
- **Operator runs the import.** Triggered by an operator or CI step (a console verb,
  `import-mutashabihat`), consistent with how Features 002/004 run their imports. No scheduled/online
  trigger in v1.
- **Source package location.** The staged package lives at `resources/import-sources/mutashabihat/`
  (already created and checksummed), and this remains the default source path for the import verb.
- **Word-index base.** Word ranges are **1-based source indices** and are stored unchanged. The
  upper-bound check (`MUT-WORD-RANGE-UPPER-BOUND`) is a **warning, not a hard gate**, so possible
  source/corpus alignment differences do not block v1 import.
- **Provenance / license.** The two datasets' upstream origin and license are **not yet documented**
  (the manifest notes this as `UNKNOWN — TODO`). This does not block v1 storage, but every import report
  records a warning and any future publishing is blocked until provenance/license are resolved.
- **Counts are fixed invariants.** 814 groups / 3,558 raw source occurrence entries / 1 duplicate
  identical occurrence / 3,557 stored unique occurrences / 1,162 sources / 3,552 links / 3,084 distinct
  ayahs come from the validated capability report, locked remediation decision, and the manifest's
  `expectedRecordCount`; they are used directly as validation/report expected values.
- **Quranic data safety.** The feature stores derived relationship references keyed by ayah identifier
  only — no ayah text — never modifies the Quran text, `quran_ayahs`, or `quran_words`, records anomalies
  as warnings rather than altering source values, and never mutates the source files.

---

## Dependencies

- **Feature 002 — Quran Foundation** (`quran_ayahs` with unique `verse_key` and stable `id`). Hard
  dependency; the import resolves every reference against it and fails if it is missing/empty.
- **The staged source package** at `resources/import-sources/mutashabihat/` (two truth files + manifest).
  Hard dependency; the import reads only from here and verifies it against the manifest.
- **Existing transactional import/validation discipline** from Features 002/004 (assemble → validate hard
  gate → commit-or-rollback → report), reused for this import.
