# Feature Specification: Quran Word Morphology Foundation

**Feature Branch**: `004-word-morphology-foundation`  
**Created**: 2026-06-10  
**Status**: Draft  
**Input**: User description: "Start Spec Kit for Feature 004 — Quran Word Morphology Foundation … data foundation only; no UI; no API endpoints; no generated Arabic i3rab; no syntactic roles; do not modify quran_words; morphology per readable quran_word occurrence; ayah markers excluded; Corpus aligned JSON for classification/structure; QUL files for Arabic root/lemma/stem display; segment Arabic rendering uses Option B (form_arabic_normalized + arabic_render_tier + arabic_render_source); form_arabic_normalized is never Mushaf text and never an exact qpcUthmani substring; quran_pos_tags required for future word-type filtering foundation; no physical quran_verbs table; importer source path App/resources/import-sources/quran-morphology/ (local in-repo, Git-ignored — real data files are not committed/pushed)."

## Overview

Feature 004 establishes the **word-morphology data foundation** for the dashboard. For **every
readable Quran word occurrence** it produces a validated, per-occurrence morphology record —
part-of-speech, the word's segment breakdown (prefix/stem/suffix), morphological features, verb
tense/voice, grammatical case, and resolved root/lemma/stem references — plus a **normalized Arabic
rendering for each segment** as a curator-facing reading aid.

Classification and structure come from the **QAC aligned corpus** file; the Arabic display strings for
**root, lemma, and stem** come from the **QUL** files. The data is loaded by an **operator-run importer
verb** that reads only from a **local in-repo source path**, builds all morphology tables in a single
transaction, and **commits only if a hard-check validation gate passes** (otherwise it rolls back and
reports the failure).

This feature is **data only**. It introduces **no UI, no API endpoints, no generated Arabic i3rab, and
no syntactic roles**. The authoritative Quran text (`quran_words` and its Uthmani/QPC columns) is the
source of truth for display and is **never modified**. The normalized Arabic segment rendering is an
explicitly flagged *derived* reading aid — never Mushaf text and never claimed as an exact `qpcUthmani`
substring.

## Clarifications

### Session 2026-06-10

- Q: When a readable word has a corpus lemma/root (Buckwalter) but no QUL Arabic display value, how should Feature 004 represent that word's lemma/root? → A: Set `lemma_id`/`root_id` to NULL (create no dimension row without an Arabic value); retain the Buckwalter value only as the segment-level cross-reference for later resolution.
- Q: Should the data distinguish corpus-marked voice from defaulted-active, or store only active/passive with the active-by-default rule as a convention? → A: Store only `verb_voice` ∈ {active, passive} (null for non-verbs): passive when the corpus marks PASS, otherwise active by documented convention; no separate "inferred" flag; the raw FEATURES string is retained for later recomputation.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Per-occurrence morphology exists for every readable word (Priority: P1)

A downstream consumer (a future feature, or an operator inspecting the database) can read the
morphology of any readable Quran word by reading its single morphology record and that word's ordered
segments: the head part-of-speech, the segment breakdown, the morphological features, verb tense/voice,
grammatical case, and the word's root/lemma/stem references.

**Why this priority**: This is the core asset the whole feature exists to produce. Without a correct,
complete, per-occurrence morphology table keyed to readable words, nothing else (Arabic rendering,
word-type filtering, Feature 005 i3rab) can be built. It is the minimum viable slice.

**Independent Test**: Run the importer against the local source, then confirm that every readable
`quran_words` row has exactly one morphology record, that each record exposes a resolvable head POS and
at least one segment, and that ayah-marker rows have **no** morphology — all verifiable by querying the
populated tables, with no UI.

**Acceptance Scenarios**:

1. **Given** Feature 002 foundation data is loaded (`quran_words` populated), **When** the morphology
   importer runs successfully, **Then** there is exactly one morphology record per readable word
   (expected 77,432) and zero records mapped to ayah markers.
2. **Given** a populated morphology record, **When** it is read, **Then** it exposes the head POS, the
   verb flag and (for verbs) tense and voice, the grammatical case where present, and references to the
   word's root/lemma/stem.
3. **Given** any morphology record, **When** its segments are read, **Then** there is at least one
   segment, each segment carries its order, kind (prefix/stem/suffix), POS, and raw source form, and
   at least one segment is a STEM; the first STEM by segment order determines the word's operational
   head POS, while any additional STEM segments are preserved.

---

### User Story 2 - Trustworthy, hard-gated import that never touches source data (Priority: P1)

An operator runs the morphology import as a console/CI action (never over HTTP). The import reads only
from the local in-repo source folder, builds all morphology tables in one transaction, validates a set
of hard invariants before committing, and either commits everything or rolls back and writes a failure
report. It never alters `quran_words` or the source files, and re-running it on unchanged source yields
identical data.

**Why this priority**: Quranic data integrity and repeatable, reviewable builds are non-negotiable. A
partial or silently-wrong morphology load would be worse than none. The gated, transactional,
report-producing import is what makes the P1 dataset trustworthy.

**Independent Test**: Run the import and confirm a populated dataset plus a success report; force a
known invariant violation and confirm the run rolls back (no partial data) and emits a failure report
with non-zero exit; confirm `quran_words` and the source files are byte-for-byte unchanged after every
run.

**Acceptance Scenarios**:

1. **Given** all hard checks pass, **When** the import runs, **Then** all morphology tables are
   committed together and a success report is produced.
2. **Given** any single hard check fails, **When** the import runs, **Then** nothing is written to any
   morphology table, a failure report is produced, and the process exits non-zero.
3. **Given** the morphology tables are already populated, **When** the import is run without the force
   option, **Then** it refuses, writes nothing, clearly reports the refusal to the console, and writes no
   report artifact.
4. **Given** a forced re-run on unchanged source, **When** it completes, **Then** the morphology tables'
   contents are identical to the previous successful run, and `quran_words` is unchanged in row count
   and content.
5. **Given** the local source files and their manifest, **When** the import runs, **Then** the source
   files match the manifest (size/checksum) **before and after** the run and are never written.

---

### User Story 3 - Arabic display values and normalized segment rendering (Priority: P2)

For each word, the root, lemma, and stem carry **Arabic display strings** sourced from the QUL files.
For each non-empty segment, the importer also stores a **normalized Arabic rendering** of the segment's
source form, stamped with a confidence tier and a provenance flag, so an Arabic-first curator can read
segments without reading Buckwalter — while the raw source form is always retained and the rendering is
never mistaken for Mushaf text.

**Why this priority**: It makes the foundation genuinely Arabic-first and useful to curators, but it
builds on the P1 morphology records and is safe to deliver as a second slice. The rendering is a
flagged reading aid, so its correctness bar is "honest and reviewable," not "authoritative Mushaf."

**Independent Test**: Confirm that root/lemma/stem Arabic display values are populated from QUL where
available (and left empty, not invented, where QUL has no value); confirm every non-empty segment has a
normalized Arabic rendering with a valid tier and provenance flag, that empty forms render as null, and
that no rendering was derived from the Uthmani/QPC text.

**Acceptance Scenarios**:

1. **Given** QUL provides an Arabic root/lemma/stem for a word, **When** the record is read, **Then**
   the corresponding Arabic display value is present; **Given** QUL has no value, **Then** it is null
   (never guessed).
2. **Given** a non-empty segment form, **When** it is rendered, **Then** `form_arabic_normalized` is
   non-empty and carries a valid `arabic_render_tier` (clean / quranic_marks / review / multiword) and a
   constant `arabic_render_source`; **Given** an empty form (the elided-pronoun cases), **Then** the
   rendering is null.
3. **Given** any segment row, **When** it is inspected, **Then** the raw source form is always present,
   and the normalized Arabic value was never copied from `qpc_glyph`/`text_uthmani` and is never
   presented as Mushaf text.

---

### User Story 4 - POS controlled-vocabulary foundation for future word-type filtering (Priority: P3)

The feature seeds a controlled part-of-speech vocabulary (codes with Arabic and English labels, a broad
category, and a sort order) and records each word's POS and verb/case features so that a **later**
feature can filter words by type (all nouns, all verbs, all particles, a specific tag, verb tense, verb
voice, grammatical case) — without any UI or API being built in this feature.

**Why this priority**: It is the enabler for future curation workflows, but no user-facing filtering is
delivered now, so it is the lowest-priority slice. It must exist as clean, resolvable data.

**Independent Test**: Confirm the POS vocabulary table is populated, that every head POS and every
segment POS resolves to a known POS code (zero unknowns), and that a direct query can group readable
words by POS category, verb tense, verb voice, and grammatical case — all from stored data, with no UI.

**Acceptance Scenarios**:

1. **Given** the import has run, **When** the POS vocabulary is read, **Then** each POS code has an
   Arabic label, an English label, a category (noun / verb / particle / other), and a sort order.
2. **Given** any morphology record or segment, **When** its POS is checked, **Then** it resolves to a
   known POS code (no unknown codes exist).
3. **Given** the populated tables, **When** a consumer queries by POS category, verb tense, verb voice,
   or case, **Then** the relevant readable words can be selected directly from stored fields, with no
   dedicated verbs table required.

---

### Edge Cases

- **Empty segment form** (the elided/implicit pronouns, expected 208 `(SUFFIX, PRON)` segments): the
  normalized Arabic rendering MUST be **null**, never an empty string and never invented text.
- **Unknown source character**: if a segment form contains any character outside the known
  transliteration map, the import MUST **refuse** rather than emit a placeholder/replacement glyph.
- **Word missing its STEM segment or POS**: MUST fail the validation gate (every word must resolve at
  least one STEM and a head POS from the first STEM by `segment_number`).
- **Inconsistent verb features** (a verb with two tenses, a verb missing voice, or a non-verb carrying
  verb fields): MUST fail the validation gate.
- **Source/manifest mismatch** (missing file, wrong record count, checksum/size drift versus the manifest,
  or unexpected extra/research-only files): MUST cause an early refusal before any write and before any
  report artifact is created.
- **QUL coverage gaps** (root/lemma/stem available for fewer words than the readable total, e.g. the
  ~1,704 words with a corpus Buckwalter lemma but no QUL Arabic lemma): the word's `root_id`/`lemma_id`/
  `stem_id` stays **null**, the Buckwalter value is retained as the segment cross-reference, nothing is
  fabricated, and the gap does not fail the build.
- **Multi-word source token** (a single token that spans two words): rendered and flagged with the
  `multiword` tier for manual review, not split or "corrected."
- **Foundation not loaded**: if `quran_words` is not present/populated, the import MUST refuse before any
  write and before any report artifact is created (it depends on Feature 002 having run).
- **Re-run over populated tables without force**: MUST change nothing, report the refusal to the console,
  and write no report artifact.
- **Segment Arabic ≠ Mushaf**: the per-word concatenation of segment renderings is **not** expected to
  equal the Uthmani text for every word (baseline whole-word agreement ≈ 79.83 %); divergence is
  reported as an informational warning, never as a build failure.

## Requirements *(mandatory)*

### Functional Requirements

#### Source, local staging, and the import verb

- **FR-001**: The feature MUST provide the morphology load as an **operator/CI-run console action** (a
  new import verb, e.g. `import-morphology`) exposed through the existing data-import host — **never**
  via any network/HTTP endpoint.
- **FR-002**: The import MUST read its source data **only** from a **local, in-repo** path
  (`App/resources/import-sources/quran-morphology/`, beside `quran-foundation/`). A `--source` override
  MAY be accepted for tests/CI, but the documented default is this local path. The import MUST **never**
  read the external research workspace (`~/Desktop/.../resources/morphology`), and runtime MUST have
  **no dependency** on that external path.
- **FR-003**: The local source path is a **Git-ignored, local-only** workspace path. The real data
  files MUST NOT be implied to be committed or pushed; "copy"/"stage" means a **local file copy** into
  the in-repo path (not `git stage`/`git add`/push). The parent `resources/` ignore rule already covers
  the folder.
- **FR-004**: The local source folder MUST contain **exactly** these files and no others, validated via
  a `manifest.json`: the corpus aligned JSON, the corpus-to-word alignment map, the QUL root file, the
  QUL lemma file, the QUL stem file, plus `manifest.json` and a `README.md`. Research-only artifacts
  (derived dumps, samples, reports, `.db`, raw `.txt`) MUST NOT be present.
- **FR-005**: Source responsibility MUST be split as: the **corpus aligned JSON** is the source for
  **classification and structure** (POS/word type, segments, features, verb tense/voice, case, and the
  Buckwalter root/lemma used only as a cross-reference); the **QUL files** are the source for the
  **Arabic display values** of root, lemma, and stem. The corpus Buckwalter root/lemma MUST NOT be used
  as Arabic display text.
- **FR-006**: The import MUST depend on the foundation import having run first (it requires
  `quran_words`), and MUST be **independent** of the Feature 003 `rebuild-words` action (it needs only
  `quran_words.{id, location, is_ayah_marker}`).

#### Tables, grain, and scope of writes

- **FR-007**: The feature MUST introduce exactly **six** new persisted tables: `quran_word_morphology`,
  `quran_word_morphology_segments`, `quran_roots`, `quran_lemmas`, `quran_stems`, and `quran_pos_tags`.
  No additional morphology tables are introduced.
- **FR-008**: The six tables' schema MUST be created via a **schema-only** database migration with no
  embedded data; all rows (including the POS controlled vocabulary) MUST be populated by the import
  action, not by the migration.
- **FR-009**: Morphology grain MUST be **per readable word occurrence**, keyed to `quran_word_id`
  (one-to-one with readable words; expected 77,432 morphology records). Morphology MUST NOT be keyed to
  identity/grouping links.
- **FR-010**: Ayah markers (`quran_words.is_ayah_marker = true`) MUST be **excluded** entirely — no
  morphology record and no segment row may map to a marker.
- **FR-011**: No morphology table may store ayah-level Quran text. Association to the Quran MUST be by
  identifier only (`quran_word_id` / `location`). The import MUST NEVER truncate, delete, or modify
  `quran_words` or any other Feature 002/003 table.

#### Classification and structure (from the corpus)

- **FR-012**: Each `quran_word_morphology` record MUST carry the head POS (`head_pos`), the verb flag
  (`is_verb`), verb tense and voice for verbs, the grammatical case where present, references to the
  word's root/lemma/stem, and the word-level morphological features.
- **FR-013**: Each morphology record MUST have **at least one** segment; its stored segment count MUST
  match the number of segment rows.
- **FR-014**: Each `quran_word_morphology_segments` row MUST carry its order within the word, its kind
  (prefix / stem / suffix), its POS, the **raw source form** (`form_buckwalter`, always retained,
  never null for a present segment), and the segment's morphological features (raw and structured).
- **FR-015**: Each word MUST resolve **at least one STEM segment**, and the word's `head_pos` MUST be
  the POS of the first STEM by `segment_number`. Additional STEM segments, when present in fused source
  forms, MUST be preserved at segment level and reported as informational multi-STEM evidence.
- **FR-016**: Verb features MUST be internally consistent: a verb MUST have exactly one of
  past/present/imperative tense and a non-null voice; a non-verb MUST have null verb fields. Voice MUST
  be stored as `passive` when the corpus marks PASS and otherwise `active` by **documented convention**;
  there is **no** separate "inferred-voice" flag. The verbatim corpus FEATURES string is retained per
  segment so the presence/absence of an explicit PASS marker can be recomputed later.
- **FR-017**: Grammatical case MUST be taken from the corpus where present and left **null** otherwise;
  case MUST NOT be invented.

#### Arabic display values and normalized segment rendering (Option B)

- **FR-018**: The Arabic display values for **root, lemma, and stem** MUST come from the QUL files.
  Where QUL provides no Arabic value for a word, the word's `root_id`/`lemma_id`/`stem_id` MUST be
  **null** — even when the corpus supplies a Buckwalter root/lemma for that word. The Buckwalter value
  MUST be retained only as the **segment-level cross-reference** (`root_buckwalter`/`lemma_buckwalter`)
  for a future feature to resolve. No transliterated/fabricated Arabic value and no placeholder
  dimension row may be created to fill a QUL gap.
- **FR-019**: Each segment MUST store a normalized Arabic rendering (`form_arabic_normalized`) on a
  **best-effort basis for every non-empty form**; every **empty** form MUST render as **null** (expected
  208 empty forms).
- **FR-020**: Each rendered segment MUST carry a confidence tier (`arabic_render_tier` ∈ clean /
  quranic_marks / review / multiword) and a constant provenance flag (`arabic_render_source`).
- **FR-021**: `form_arabic_normalized` MUST NEVER be used as Mushaf display text, MUST NEVER be claimed
  as an exact `qpcUthmani` substring, MUST NEVER be named `qpc_segment_text`, and MUST NEVER be written
  from `qpc_glyph`/`text_uthmani`. The raw `form_buckwalter` MUST be present on every segment row.
- **FR-022**: Root, lemma, and stem dimension rows MUST be **deduplicated by their Arabic display
  text** (one row per distinct Arabic value); a dimension row MUST exist **only** when an Arabic display
  value is present (per FR-018, no row is created from a Buckwalter-only value). Each morphology record
  references the resolved dimension rows, and every non-null root/lemma/stem reference MUST resolve to an
  existing dimension row (no dangling references).

#### POS controlled vocabulary (word-type filtering foundation)

- **FR-023**: `quran_pos_tags` MUST be a **required** controlled-vocabulary table whose rows carry: the
  POS code, an Arabic label, an English label, a broad category (noun / verb / particle / other), a sort
  order, and an optional description (expected ≈ 30 rows).
- **FR-024**: Every `head_pos` and every segment POS MUST resolve to a known `quran_pos_tags` code
  (zero unknown codes).
- **FR-025**: The stored fields MUST be sufficient to support **future** filtering by word type (all
  nouns / all verbs / all particles), by specific tag, by verb tense, by verb voice, and by grammatical
  case — as a **data foundation only**, with **no** UI pages and **no** API endpoints in this feature.
- **FR-026**: There MUST be **no physical `quran_verbs` table**. Verbs MUST be derivable from
  `quran_word_morphology` (`is_verb`, `verb_tense`, `verb_voice`) plus appropriate indexes.

#### Validation gate and reporting

- **FR-027**: Before committing, the import MUST validate a set of **hard** invariants; failure of any
  one MUST abort the build. At minimum:
  - `MORPH-READABLE-COMPLETE` — exactly one morphology record per readable word; count equals the
    readable count.
  - `MORPH-MARKERS-EXCLUDED` — zero morphology/segment rows map to an ayah marker.
  - `MORPH-LOCATION-MATCH` — every morphology location matches a `quran_words.location`, with no
    unmatched source locations.
  - `MORPH-SEGMENTS-PRESENT` — every word has at least one segment and a matching segment count.
  - `MORPH-POS-PRESENT` — every segment has a POS; every word has at least one STEM; and `head_pos`
    equals the first STEM POS by `segment_number`.
  - `MORPH-POS-RESOLVES` — every head POS and segment POS resolves to a `quran_pos_tags` code.
  - `MORPH-VERB-FEATURE-CONSISTENCY` — verb tense/voice consistency per FR-016.
  - `MORPH-DIMENSION-RESOLVES` — every non-null root/lemma/stem reference resolves (no dangling).
  - `MORPH-SEG-CHARSET` — every source-form character is in the transliteration map (zero unmapped).
  - `MORPH-SEG-RENDER-TOTAL` — every non-empty form yields a non-empty rendering; every empty form
    yields null (expected 208 nulls).
  - `MORPH-SEG-TIER-VALID` — every rendered row has a valid tier and the constant render source.
  - `MORPH-SEG-RENDER-PROVENANCE` — rendered Arabic is reproducible from each row's
    `form_buckwalter` via the approved renderer, every rendered row retains the raw form and
    `arabic_render_source = buckwalter-transliteration`, and equality with Uthmani/QPC text is
    informational rather than a failure.
  - `MORPH-SOURCE-UNCHANGED` — the local source files match their manifest (size/checksum) **before and
    after** the run and are never written.
- **FR-028**: On any hard-check failure, the import MUST roll back so that **nothing** is written to any
  morphology table, MUST produce a failure report, and MUST signal failure (non-zero exit).
- **FR-029**: The import MUST also produce **warning** signals that are informational and MUST NOT change
  the pass/fail verdict, including: per-word segment-vs-Uthmani agreement (baseline ≈ 79.83 %), the tier
  distribution, and the review/fragile/empty lists for manual sign-off.
- **FR-030**: Every import **build attempt** that starts MUST produce a **traceable report** capturing
   per-table totals, the tier distribution, the review/fragile tier list, the multiword tier list, the
   empty-form rows/list/count, the hard-check results, the warnings, and the final outcome. Early refusals
   such as source/manifest mismatch, missing or empty foundation data (`quran_words`), or non-empty targets
   without `--force` are not build attempts: they report the refusal to the operator, write no report
   artifact, and write no target data.

#### Rebuild semantics and source safety

- **FR-031**: The load MUST be **transactional/atomic** — either all six tables are populated and
  committed together, or nothing is written.
- **FR-032**: The import MUST refuse to run if any target morphology table is non-empty, unless an
  explicit force option (`--force`) is supplied.
- **FR-033**: With the force option, the import MUST truncate and repopulate **only** the six morphology
  tables.
- **FR-034**: No run MUST ever truncate, delete, or modify `quran_words`, `quran_ayahs`, `quran_surahs`,
  the Feature 003 derived/identity tables, or any other non-morphology table.
- **FR-035**: A forced run MUST be **idempotent** — identical source data MUST yield identical contents
  across runs.
- **FR-036**: The source files MUST be treated as **read-only**; the import reads them and never writes
  them (enforced by `MORPH-SOURCE-UNCHANGED`).

#### Scope guards (must-not)

- **FR-037**: The feature MUST NOT introduce any API endpoints, frontend UI, or request-path/runtime
  work (including the future word-type filters — data foundation only).
- **FR-038**: The feature MUST NOT produce **generated Arabic i3rab** prose — that is Feature 005.
- **FR-039**: The feature MUST NOT produce **syntactic roles** (فاعل / مفعول به / مبتدأ / خبر / حال …);
  the source contains no syntactic treebank.
- **FR-040**: The feature MUST NOT attempt to compute or store **character offsets into `qpcUthmani`**.
- **FR-041**: The feature MUST NOT invent values: a null root/lemma/stem/case stays null; fragile
  renderings are **flagged** (review tier), not "corrected" by guessing.

### Key Entities *(include if feature involves data)*

- **Word morphology** — `quran_word_morphology`: one record per readable word occurrence (keyed to
  `quran_word_id`). Holds the head POS, the verb flag and verb tense/voice, the grammatical case,
  references to the word's root/lemma/stem, the word-level features, and the segment count.
- **Morphology segment** — `quran_word_morphology_segments`: one record per segment of a word, in order.
  Holds the segment kind (prefix/stem/suffix), the segment POS, the raw source form (`form_buckwalter`),
  the normalized Arabic rendering (`form_arabic_normalized`) with its tier and source flag, and the
  segment features (raw and structured).
- **POS tag** — `quran_pos_tags`: the controlled vocabulary of part-of-speech codes, each with an Arabic
  label, an English label, a broad category (noun/verb/particle/other), a sort order, and an optional
  description.
- **Root** — `quran_roots`: the deduplicated set of roots, each with its Arabic display value (from QUL)
  and the Buckwalter cross-reference.
- **Lemma** — `quran_lemmas`: the deduplicated set of lemmas, each with its Arabic display value (from
  QUL) and the Buckwalter cross-reference.
- **Stem** — `quran_stems`: the deduplicated set of stems, each with its Arabic display value (from QUL).
- **Import operation**: an operator-run, transactional process that reads the local source, builds the
  six tables, enforces refusal/force semantics, never touches the source tables or files, and validates
  against the hard-check gate before committing.
- **Import report**: a per-build-attempt, human-readable artifact recording per-table totals, the tier
  distribution, review/fragile tier list, multiword tier list, empty-form rows/list/count, hard-check
  results, warnings, and the final outcome — the traceability record for a started build attempt.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: After a successful import, every readable Quran word has exactly one morphology record
  (expected 77,432) and zero morphology/segment rows map to an ayah marker.
- **SC-002**: A consumer can read any readable word's head POS, verb tense/voice, case, segment
  breakdown, and root/lemma/stem references from the stored data, with no read-time computation and no
  UI.
- **SC-003**: 100 % of head POS values and segment POS values resolve to a known POS code, and the POS
  vocabulary is populated with Arabic + English labels, a category, and a sort order for every code.
- **SC-004**: Every non-empty segment has a normalized Arabic rendering with a valid tier and provenance
  flag; the expected 208 empty forms render as null; and no rendering is derived from the Uthmani/QPC
  text.
- **SC-005**: A consumer can select readable words by POS category, verb tense, verb voice, and
  grammatical case directly from stored fields, with no dedicated verbs table.
- **SC-006**: An import that detects any hard-invariant violation leaves all six morphology tables
  unchanged (no partial data) and produces a failure report with a non-success exit status.
- **SC-007**: Re-running the import on unchanged source data produces identical contents in all six
  tables.
- **SC-008**: After any import run, `quran_words` and all other non-morphology tables are unchanged in
  row count and content, and the local source files match their manifest (size/checksum) before and
  after the run.
- **SC-009**: Every import build attempt that starts produces a report from which a reviewer can confirm
   per-table totals, tier distribution, review/fragile tier list, multiword tier list, empty-form
   rows/list/count, and hard-check outcomes without querying the database directly. Early refusals report
   to the console and write no report artifact.
- **SC-010**: An attempt to import over non-empty target tables without the force option changes nothing,
   clearly reports the refusal to the console, and writes no report artifact.

## Assumptions

- The readable-word total is **77,432** (locked by Feature 003); this is the expected morphology record
  count and the one-to-one target for readable words.
- The local source folder is populated by the operator before the import is run, by **copying** the
  selected files (corpus aligned JSON, alignment map, QUL root/lemma/stem) from the upstream research
  workspace plus a generated `manifest.json` and `README.md`. The upstream workspace is read-only
  provenance with no runtime dependency.
- The POS controlled vocabulary's Arabic/English labels, category mapping, and sort order are a
  **curated definition maintained alongside the importer** (the corpus supplies the POS codes; the
  human-readable labels/category/order are curated, not invented per run). The import verifies that
  every observed POS code is covered by this vocabulary.
- The corpus aligned file is keyed by a location that matches `quran_words.location` one-to-one for
  readable words; the alignment map is used for audit/provenance only and is not seeded as data.
- Baseline distributions from the research reports (tier split ≈ 94.2 % clean / 5.4 % quranic_marks /
  0.4 % review / 1 multiword; whole-word Uthmani agreement ≈ 79.83 %; 208 empty `(SUFFIX, PRON)` forms)
  are **informational baselines** for warnings, **not** hardcoded pass/fail thresholds.
- "Migration" means an EF-tool-generated, schema-only migration; it is not authored or applied as part
  of specification, and is added only when implementation explicitly calls for it.

## Dependencies

- **Feature 002 (foundation import)** must have run: `quran_words` (with `location` and `is_ayah_marker`)
  must be present and populated.
- The local in-repo source folder `App/resources/import-sources/quran-morphology/` must contain the
  exact file set and a valid `manifest.json`.
- The existing data-import console host (which already dispatches the foundation/rebuild verbs) is the
  delivery surface for the new import verb.
- This feature is **independent** of Feature 003's `rebuild-words` action.

## Out of Scope

- Any **UI pages** or **API endpoints**, including any user-facing word-type filtering (the filtering
  data foundation is in scope; the surfaces are not).
- **Generated Arabic i3rab** prose — deferred to **Feature 005**.
- **Syntactic roles** (فاعل / مفعول به / مبتدأ / خبر / حال …) — the source has no syntactic treebank.
- **Character offsets into `qpcUthmani`** — not attempted; Uthmani offsets are unsafe.
- Treating `form_arabic_normalized` as **Mushaf/Uthmani text** — it is a flagged derived reading aid
  only; authoritative display stays `quran_words.text_uthmani` / `qpc_glyph`.
- A **physical `quran_verbs` table** — verbs are derived, not materialized.
- Any modification to `quran_words` or other Feature 002/003 tables, and any change to the upstream
  research workspace.
- Mutashabihat, tafsir, translations, audio, or any other content domain.
