# Feature Specification: Word Simple I‘rab Foundation

**Feature Branch**: `005-word-simple-i3rab-foundation`
**Created**: 2026-06-12
**Status**: Draft
**Input**: User description: "Read our plan — and according to the best practices of GitHub's Spec Kit, create the spec. Generation only. The implementation will be done using a cheaper model, so the specification and everything should be super clear."

> **Authoritative design inputs (read these — they are locked and must not be re-litigated):**
> - `docs/feature-005-word-simple-i3rab-foundation/feature-005-word-simple-i3rab-foundation-planning-report.md` — the locked v1 plan.
> - `Backend/report/feature-005-word-simple-i3rab-foundation/segment-pattern-rule-coverage-report.md` — the finalized full segment-pattern inventory (real DB counts, 100% approved, the 142→67 catalogue, the final label set).
>
> This spec restates those decisions as testable requirements. Where this spec and those reports agree, both are authoritative; the reports hold the full per-pattern evidence.

---

## Overview (plain language)

Feature 004 already broke every Qur'an word into its grammatical **segments** (prefix / stem / suffix)
and stored each segment's part-of-speech, case, tense, voice, pronoun person, and lemma. Feature 005
adds, on top of that existing data, a **simplified Arabic grammar label (i‘rab)** for **each segment** —
short phrases such as `حرف جر` (preposition), `اسم مجرور` (genitive noun), `فعل مضارع` (present-tense
verb), `ضمير متصل للغائبين` (attached 3rd-person-plural pronoun), and `لفظ الجلالة مجرور` (the divine
name in the genitive).

It is a **backend data foundation only**: it computes labels deterministically from Feature 004 data and
stores them. It builds **no** user interface and **no** API. A later feature will read these labels to
colour each segment and show its grammar label; the per-word grammar phrase is assembled **at read time**
from the ordered segment labels, so nothing about word-level display is stored now.

These are **simplified, quick grammar labels**, explicitly **not** full scholarly i‘rab and **not**
sentence-role analysis (فاعل / مفعول به / مبتدأ / خبر / حال are out of scope).

---

## Clarifications

### Session 2026-06-12

- Q: Rule catalogue grain & label storage — how many rows does `quran_i3rab_rules` have, and where does the exact per-segment Arabic label come from? → A: **142 rows**, one per distinct segment-token signature, each carrying its **exact** final Arabic label; `i3rab_rule_id` points to the signature row and `i3rab_arabic` equals that row's label; a `rule_family` column groups the 142 rows into the 67 families for coverage/usage reporting only. The generator is a deterministic signature **lookup** (no Arabic-label composition in code).
- Q: How should the new `i3rab_*` columns be enforced at the database level? → A: **Both levels (defense in depth).** The **schema** enforces the structural rules — `i3rab_status` text NOT NULL + CHECK in {`approved`, `needs_review`, `unsupported`}, and a nullable foreign key `i3rab_rule_id` → `quran_i3rab_rules.id`; the **application validation gate** enforces the conditional/cross-row invariants (approved ⇒ label+rule, needs_review ⇒ rule+reason, unsupported ⇒ reason; segment row-count stable; original morphology columns unchanged; the 208 NULL forms preserved).

---

## User Scenarios & Testing *(mandatory)*

> "Users" here are the people and systems that depend on this data: the **operator** who runs the
> generation (an admin / CI step), the **scholar-reviewer** who curates and trusts the Arabic labels, and
> the **downstream feature** (the future segment-colouring UI) that reads the labels. There are no
> end-user screens in this feature.

### User Story 1 - Every segment gets a trustworthy simplified i‘rab label (Priority: P1)

The operator runs the i‘rab generation against the populated Feature 004 morphology. Afterward, **every
segment of every readable word** carries a simplified Arabic i‘rab label, a status, and a pointer to the
rule that produced it — so the dashboard (later) can show a grammar label next to every coloured segment.

**Why this priority**: This is the entire point of the feature — the labelled data foundation. Without
it there is nothing to display and nothing to review. It is the minimum viable deliverable.

**Independent Test**: Run the generation on the live morphology, then query the segment store: confirm
all 128,219 segments have a non-null status, every `approved` segment has both an Arabic label and a rule
reference, and a spot-check of known words (e.g. `بِسْمِ` 1:1:1, `عَلَيْهِمْ` 1:7:4, `ٱللَّهِ` 1:1:2,
`أُنزِلَ` 2:4:4) shows the expected Arabic labels.

**Acceptance Scenarios**:

1. **Given** a completed Feature 004 morphology import, **When** the generation runs and commits, **Then** every one of the 128,219 segments has `i3rab_status = approved` with a non-empty `i3rab_arabic` and a resolvable `i3rab_rule_id`.
2. **Given** the genitive noun segment of `بِسْمِ` (1:1:1), **When** its label is read, **Then** it reads `اسم مجرور`.
3. **Given** the divine-name segment of `ٱللَّهِ` (1:1:2), **When** its label is read, **Then** it reads `لفظ الجلالة مجرور` (not the generic `اسم علم مجرور`).
4. **Given** the passive verb `أُنزِلَ` (2:4:4), **When** its label is read, **Then** it reads `فعل ماض مبني للمجهول`.
5. **Given** any readable word, **When** its segments are inspected, **Then** none has a missing/blank status.

---

### User Story 2 - Correct, curated Arabic labels owned by a rule catalogue (Priority: P2)

A scholar-reviewer must be able to trust that the Arabic shown is grammatically correct. The labels come
from a **curated rule catalogue** that the team owns — not from the raw part-of-speech dictionary, which
has known wrong Arabic labels. The catalogue is small and auditable: 67 rule families covering 142
distinct segment signatures, each with one canonical Arabic label.

**Why this priority**: Label correctness is the product's credibility. The morphology seed
(`quran_pos_tags`) mislabels ~20 categories (e.g. it calls `T` "تاء تأنيث" when it is `ظرف زمان`); the
rule catalogue is how Feature 005 ships correct Arabic without editing Feature 004 data.

**Independent Test**: Inspect the seeded rule catalogue independently of the segment data: confirm it
contains the 67 families, that every required corrected label (see the correction table in FR-011) is
present and correct, and that no segment label is sourced verbatim from a known-wrong `quran_pos_tags`
seed value.

**Acceptance Scenarios**:

1. **Given** the seeded catalogue, **When** the rule for `T` (ظرف) is read, **Then** its Arabic label is `ظرف زمان` and **not** the seed's `تاء تأنيث`.
2. **Given** the seeded catalogue, **When** the rules for `RES`, `SUB`, `STEM:INTG`, `SUR`, `INL`, `P.SUFFIX`, `N.GEN.1S` are read, **Then** they are `أداة حصر`, `حرف مصدري`, `اسم استفهام`, `حرف فجاءة`, `حروف مقطّعة (فواتح السور)`, `لام الجر`, `اسم مجرور مضاف إلى ياء المتكلم` respectively.
3. **Given** any generated segment label, **When** its `i3rab_rule_id` is followed, **Then** it resolves to exactly one catalogue rule whose Arabic label matches the stored `i3rab_arabic`.

---

### User Story 3 - Word-level i‘rab composed at read time (Priority: P3)

The downstream feature can show a single grammar phrase for a whole word by reading that word's segment
labels **in order** and joining them with the Arabic comma «، » — for example `بِحَمْدِكَ` →
`حرف جر، اسم مجرور، ضمير متصل للمخاطب`. No word-level summary is stored; it is always derived.

**Why this priority**: It proves the design choice that word summaries need no storage, and it documents
the exact read-time recipe for the next feature. It depends on US1/US2 but is independently demonstrable.

**Independent Test**: For a sample of words (single-segment and multi-segment, e.g. `رَبِّ` 1:2:3,
`بِحَمْدِكَ` 2:30:20, `أَتَجْعَلُ` 2:30:11), order the segment labels by segment number, join with «، »,
and confirm the resulting phrase matches the expected reading — using only the stored segment labels, no
extra stored table.

**Acceptance Scenarios**:

1. **Given** the segments of `بِحَمْدِكَ` (2:30:20) in order, **When** their stored segment labels are joined with «، », **Then** the phrase is `حرف جر، اسم مجرور، ضمير متصل للمخاطب`.
2. **Given** the whole feature output, **When** the schema is inspected, **Then** there is **no** stored per-word i‘rab summary table (no `quran_word_i3rab`) and **no** separate per-segment i‘rab table (no `quran_word_segment_i3rab`).
3. **Given** any readable word, **When** its ordered segment labels are read, **Then** a displayable Arabic phrase can be produced (100% of words are displayable).

---

### User Story 4 - Safe, repeatable generation that never harms source data (Priority: P3)

The operator must be able to (re)run generation safely. It reads Feature 004 data, writes only the new
i‘rab fields and the rule catalogue, commits all-or-nothing behind a validation gate, and never alters
the original morphology or the Qur'an text. If morphology is missing or stale, it refuses rather than
writing partial data.

**Why this priority**: Quranic data integrity is non-negotiable, and a foundation must be rebuildable.
This story is what makes the feature safe to run in CI and to re-run after a morphology refresh.

**Independent Test**: (a) Snapshot the original morphology columns and the segment row count, run
generation, and confirm both are byte/row identical afterward. (b) Run generation twice and confirm the
second run yields identical results (idempotent). (c) Point it at empty/stale morphology and confirm it
refuses and writes nothing. (d) Force a hard-check failure and confirm a full rollback.

**Acceptance Scenarios**:

1. **Given** a successful run, **When** the original morphology columns and the segment row count (128,219) are compared before and after, **Then** they are unchanged.
2. **Given** the 208 segments whose written form is empty/NULL (the elided 1st-person-singular pronoun), **When** the run completes, **Then** those forms are still NULL and each still received an i‘rab label.
3. **Given** missing or stale morphology, **When** generation is invoked, **Then** it refuses with a clear message and makes no writes.
4. **Given** a hard validation check fails during a run, **When** the gate evaluates, **Then** nothing is committed, a failure report is written, and the process exits non-zero.
5. **Given** an already-populated i‘rab set, **When** generation is re-run without the force option, **Then** it refuses to overwrite; **When** re-run with force, **Then** it cleanly repopulates to an identical result.

---

### Edge Cases

- **Elided 1st-person-singular pronoun (208 segments, empty form):** the segment still gets the label `ضمير متصل للمتكلم المفرد` (may be annotated «محذوف/مُقدَّر»); the written form stays NULL — **never** invent a form. (See FR-022.)
- **Feminine agreement:** a feminine base such as `صفة` takes `صفة مجرورة` (not `صفة مجرور`). Labels encode the agreeing form. (See FR-013.)
- **Divine name vs generic proper noun:** a proper noun whose lemma is "Allah" gets `لفظ الجلالة + case`, overriding the generic `اسم علم + case`. The vocative `ٱللَّهُمَّ` is a separate form (its closing ميم is `ميم عوض عن حرف النداء`). (See FR-014.)
- **Pattern that reads better as an idiom or carries a syntactic role (e.g. `P+PRON` → جار ومجرور; `V+PRON` → الضمير في محل نصب مفعول به; `ACC+PREV` → كافّة ومكفوفة):** the per-segment label is stored as-is; the idiom collapse / «محل …» role is a **read-layer** behaviour and is **not** stored in v1. (See FR-019.)
- **Unknown / future segment signature with no matching rule:** in v1 all 142 signatures are covered, but if an unmatched signature ever appears it MUST be recorded (status `unsupported` with a reason) and surfaced as a warning — never guessed. (See FR-006, FR-029.)
- **Morphology re-imported after i‘rab exists:** truncating/reloading segments clears the i‘rab fields; i‘rab MUST be regenerated afterward. (See FR-027.)
- **Ayah markers:** excluded entirely (they have no morphology segments); zero i‘rab is produced for them.

---

## Requirements *(mandatory)*

### Functional Requirements

**Scope & grain**

- **FR-001**: The system MUST generate exactly one simplified Arabic i‘rab label for **every segment** of every readable word — all **128,219** segments across the **77,432** readable words. Ayah markers are excluded (they have no segments).
- **FR-002**: The generated i‘rab MUST be stored **inline on the existing morphology segment record** (`quran_word_morphology_segments`) using four new fields, keyed per-occurrence (one segment row = one label). The columns MUST be defined at the schema level as: `i3rab_arabic` **text NULL** (the Arabic label), `i3rab_rule_id` **int NULL, foreign key → `quran_i3rab_rules.id`** (the rule that produced it), `i3rab_status` **text NOT NULL with a CHECK constraint limiting it to {`approved`, `needs_review`, `unsupported`}** (the review status), and `i3rab_review_reason` **text NULL** (a reason/note). No other new per-segment or per-word i‘rab table is created (see FR-018).

**Status model & consistency**

- **FR-003**: `i3rab_status` MUST always be present and MUST be exactly one of: `approved`, `needs_review`, `unsupported`. This is enforced at the database level (NOT NULL + CHECK on the three values per FR-002) **and** re-verified by the application gate (`I3RAB-SEG-STATUS-COMPLETE`). The conditional rules in FR-004–FR-006 are cross-column and are enforced by the **application gate** (a single-column CHECK cannot express them).
- **FR-004**: When `i3rab_status = approved`, both `i3rab_arabic` and `i3rab_rule_id` MUST be present (non-null).
- **FR-005**: When `i3rab_status = needs_review`, `i3rab_rule_id` and `i3rab_review_reason` MUST be present; `i3rab_arabic` MAY be present but, if so, is **internal review-only** and MUST NOT be shown in normal user display.
- **FR-006**: When `i3rab_status = unsupported`, `i3rab_review_reason` MUST be present and non-empty, and no Arabic label is shown.
- **FR-007**: In v1, **every** segment MUST resolve to `approved` (all 142 segment-token signatures are approved-candidate → 100% of segment rows). The `needs_review` and `unsupported` values MUST be fully supported by the schema and validation but MUST carry **zero rows** in v1 (they are reserved for future catalogue growth and the review workflow).

**Rule catalogue & label ownership**

- **FR-008**: The system MUST maintain a curated rule catalogue table `quran_i3rab_rules` with **one row per distinct segment-token signature — 142 rows total** (the §3.4 inventory). Each row MUST carry at least: a unique stable **signature key** (the §3.4 segment signature, e.g. `STEM:N:GEN`, `STEM:V:PERF:ACT:3MS`, `STEM:PN:ALLAH:GEN`, `SUFFIX:PRON:3MP`), the **exact canonical Arabic display label** for that signature (the value stored on every matching segment), a `rule_family` field grouping it into one of the **67 families** (e.g. `N.GEN`, `V.PERF.ACT`, `PRON.SUF`) for reporting only, a default status, and provenance/ordering metadata. This catalogue is the single owner of user-facing Arabic labels.
- **FR-009**: The 142 catalogue rows MUST be grouped into **67 rule families** via the `rule_family` field (person / gender / number variants share a family). Families are used for aggregated coverage and per-family usage reporting, **not** as the label source — the per-segment label always comes from the signature row (FR-008). The implementation MUST NOT create one rule per enriched word pattern; the 1,337 enriched word patterns are evidence/examples only, never catalogue rows.
- **FR-010**: Every generated segment label MUST be sourced from the rule catalogue, **not** copied verbatim from `quran_pos_tags.arabic_label`. `quran_pos_tags` remains a technical part-of-speech dictionary only.
- **FR-011**: The catalogue MUST emit these **corrected** Arabic labels (the part-of-speech seed is wrong or imprecise for these; the rule layer overrides it):

  | POS / signature | wrong/imprecise seed label | Feature 005 display label |
  |---|---|---|
  | `T` | تاء تأنيث | **ظرف زمان** |
  | `SUB` | اسم مبهم | **حرف مصدري** |
  | `RES` | حرف ردع | **أداة حصر** |
  | `STEM:INTG` | حرف استفهام | **اسم استفهام** |
  | `PREFIX:INTG` | حرف استفهام | **همزة استفهام** |
  | `AMD` | حرف عطف / نفي | **حرف استدراك** |
  | `SUP` | حرف زائد | **حرف زائد** |
  | `PREV` | حرف تحضيض | **ما الكافّة** |
  | `INC` | حرف ابتداء | **حرف ابتداء/استفتاح** |
  | `EXL` | حرف تعليل | **حرف تفصيل** |
  | `INT` | حرف تفسير | **حرف تفسير** |
  | `EXH` | حرف تحضيض | **حرف تحضيض** |
  | `SUR` | إذا الفجائية | **حرف فجاءة** |
  | `INL` | قسم | **حروف مقطّعة (فواتح السور)** |
  | `EQ` | حرف تسوية | **همزة التسوية** |
  | `VOC.SUFFIX` | حرف نداء | **ميم عوض عن حرف النداء** |
  | `COM` | واو المعية | **واو المعية** |
  | `P.SUFFIX` | حرف جر | **لام الجر** |
  | `N.GEN.1S` | اسم مجرور | **اسم مجرور مضاف إلى ياء المتكلم** |
  | `REM` | حرف استثناء | **حرف استئناف** |
  | `PREFIX:IMPV` | فعل أمر | **لام الأمر** |

- **FR-012**: Every non-null `i3rab_rule_id` MUST resolve to exactly one row in `quran_i3rab_rules`, and that rule's canonical Arabic label MUST equal the segment's stored `i3rab_arabic` for `approved` segments. The generator MUST compute each segment's signature key from its morphology features, **look that key up** in the catalogue, and store the catalogue row's exact label and id — it MUST NOT hand-compose Arabic label text in code. This referential rule is enforced by the database **foreign key** (`i3rab_rule_id` → `quran_i3rab_rules.id`, per FR-002) in addition to the `I3RAB-RULE-RESOLVES` gate check.

**Label content rules**

- **FR-013**: Noun-class labels MUST reflect grammatical case: nouns → `اسم مرفوع` / `اسم منصوب` / `اسم مجرور`; adjectives with **feminine agreement** → `صفة مرفوعة` / `صفة منصوبة` / `صفة مجرورة`; proper nouns → `اسم علم مرفوع/منصوب/مجرور`.
- **FR-014**: A proper-noun segment whose lemma is the divine name ("Allah") MUST be labelled `لفظ الجلالة` + case (e.g. `لفظ الجلالة مجرور`), overriding the generic proper-noun label.
- **FR-015**: Verb labels MUST reflect tense — `فعل ماض` (past) / `فعل مضارع` (present) / `فعل أمر` (imperative) — and MUST append `مبني للمجهول` when the verb is passive.
- **FR-016**: Attached-pronoun labels MUST reflect person / gender / number, e.g. `ضمير متصل للغائبين` (3rd-person-plural), `ضمير متصل للمتكلم المفرد` (1st-person-singular).
- **FR-017**: All user-facing labels MUST be in Arabic. Part-of-speech codes (`P`, `N`, `V`, `INTG`, …) MUST NOT appear in any user-facing label; they may be used only for internal/developer audit.

**Word-level composition (read-time, not stored)**

- **FR-018**: The word-level simplified i‘rab MUST be **derivable at read time** by ordering a word's segment labels by segment number and joining them with the Arabic comma «، ». The system MUST NOT store a per-word i‘rab summary in v1 (no `quran_word_i3rab` table), and MUST NOT create a separate per-segment i‘rab table (no `quran_word_segment_i3rab`); the labels live inline per FR-002.
- **FR-019**: Idiom collapses (e.g. `P+PRON` / `P+N:GEN` → `جار ومجرور`), «محل …» role refinements (e.g. `V+PRON` → الضمير `في محل نصب مفعول به`; `ACC+PRON` → الضمير `في محل نصب اسم إنّ`), and pattern-aware overrides (`P+SUB` → `جار، مجرور`; `SUP+AMD` → combined `حرف استدراك`; `ACC+PREV` → combined `كافّة ومكفوفة`) MUST be treated as **read-layer / UI behaviour**. The generator MUST store only the plain per-segment label; it MUST NOT compute or persist these refinements in v1. (They are documented for the future read layer.)

**Source-data preservation (hard guarantees)**

- **FR-020**: The generation MUST NOT modify any original morphology field on `quran_word_morphology` or `quran_word_morphology_segments` (`head_pos`, `pos`, `kind`, `case_feature`, `verb_tense`, `verb_voice`, `form_buckwalter`, `form_arabic_normalized`, `features_raw`, `features_json`, root/lemma/stem references). It MAY write **only** the four new `i3rab_*` columns (and seed `quran_i3rab_rules`).
- **FR-021**: The generation MUST NOT insert, delete, or truncate rows of `quran_word_morphology_segments`. The segment row count MUST remain **128,219** before and after a run.
- **FR-022**: The **208** segments whose written form is empty/NULL (the elided 1st-person-singular pronoun) MUST keep their NULL form. The system adds an i‘rab label only and MUST NEVER fabricate an Arabic form for them.
- **FR-023**: The generation MUST NOT modify `quran_words`, the Uthmani/QPC text columns, any other feature's tables, or the `quran_pos_tags` seed data. (Correcting the part-of-speech seed, if ever desired, is a separate feature.)
- **FR-024**: The generation MUST store **no Qur'an ayah text** — i‘rab labels are keyed by identifier (segment / word id and location) only.

**Process, rebuild & gate**

- **FR-025**: i‘rab generation MUST run **after** a completed Feature 004 morphology import. It MUST detect missing or stale morphology (e.g. empty/short segment table, unexpected counts) and **refuse with a clear message**, writing nothing, rather than producing partial data.
- **FR-026**: The generation MUST be **transactional and gated**: assemble labels in memory, run all hard checks (FR-029), and commit **only if all pass**; on any hard-check failure it MUST roll back (write nothing), emit a failure report, and exit non-zero.
- **FR-027**: The generation MUST be **idempotent**: re-running against unchanged morphology yields identical results. It MUST refuse to overwrite an already-populated i‘rab set unless an explicit **force** option is given; a force run MUST cleanly repopulate to an identical result.
- **FR-028**: The process documentation MUST state that a morphology re-import which truncates/reloads segments **invalidates** the i‘rab fields, so i‘rab MUST be regenerated after any such re-import.

**Reporting & validation checks**

- **FR-029**: Each generation run MUST enforce these **hard checks** (any failure ⇒ rollback + failure report + non-zero exit):
  - `I3RAB-SEG-STATUS-COMPLETE` — every segment has a non-null `i3rab_status` from the allowed set.
  - `I3RAB-APPROVED-CONSISTENT` — `approved` ⇒ `i3rab_arabic` **and** `i3rab_rule_id` present.
  - `I3RAB-NEEDS-REVIEW-CONSISTENT` — `needs_review` ⇒ `i3rab_rule_id` **and** `i3rab_review_reason` present.
  - `I3RAB-UNSUPPORTED-CONSISTENT` — `unsupported` ⇒ non-empty `i3rab_review_reason`.
  - `I3RAB-WORD-DISPLAYABLE` — every readable word can derive an ordered segment-label display.
  - `I3RAB-RULE-RESOLVES` — every non-null `i3rab_rule_id` resolves to a `quran_i3rab_rules` row.
  - `I3RAB-SOURCE-COLUMNS-UNCHANGED` — all original morphology columns are unchanged before & after.
  - `I3RAB-SEGMENT-ROWCOUNT-STABLE` — no insert/delete/truncate of `quran_word_morphology_segments` rows (count stays 128,219).
  - `I3RAB-NULL-FORM-NOT-INVENTED` — the 208 NULL `form_arabic_normalized` rows remain NULL.
- **FR-030**: Each generation run MUST emit these **warnings** (informational; never gate the build): `I3RAB-COVERAGE` (per-status counts/percentages), `I3RAB-RULE-USAGE` (per-rule hit counts), `I3RAB-UNKNOWN-PATTERNS` (any segment signature with no matching rule family), `I3RAB-NEEDS-REVIEW-SUMMARY` (enumerated needs-review items), `I3RAB-LABEL-REVIEW` (labels diverging from `quran_pos_tags.arabic_label`).
- **FR-031**: Each run MUST produce a single human-readable **report artifact** containing: per-status coverage, per-rule usage, unmatched-signature list, the needs-review summary, the seed-divergence list, every hard-check's pass/fail, and the final outcome (committed / rolled back).

### Key Entities *(include if feature involves data)*

- **Segment I‘rab Annotation** — the four new fields added **inline** to each existing morphology segment (`quran_word_morphology_segments`): the Arabic label (`i3rab_arabic`), the producing rule reference (`i3rab_rule_id`), the review status (`i3rab_status`), and a review reason/note (`i3rab_review_reason`). Grain: **1:1 with a morphology segment**, per-occurrence. There are 128,219 such annotations.
- **I‘rab Rule (catalogue)** — a curated, seeded reference row in `quran_i3rab_rules`, **one per segment-token signature (142 rows total)**. Attributes: a unique stable **signature key** (e.g. `STEM:N:GEN`, `STEM:V:PERF:ACT:3MS`, `STEM:PN:ALLAH:GEN`, `SUFFIX:PRON:3MP`), the **exact** canonical Arabic display label for that signature, a `rule_family` value grouping it into one of the **67 families** (for reporting only), a default status, a description/provenance note, and a sort order. The catalogue is the **single owner of user-facing Arabic labels**. Size: **142 rows / 67 families**.
- **I‘rab Status** — a controlled three-value vocabulary (`approved` / `needs_review` / `unsupported`) governing display and consistency (FR-003–FR-007). In v1, only `approved` is populated.
- **Word-level I‘rab Summary (derived, not stored)** — the ordered join of a word's segment labels with «، », computed at read time. It is explicitly **not** a stored entity in v1 (no `quran_word_i3rab`).

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: **100%** of segments (128,219 / 128,219) carry a simplified Arabic i‘rab label with status `approved` and a resolvable rule reference; **0** segments lack a status.
- **SC-002**: **100%** of readable words (77,432) can produce an ordered simplified Arabic i‘rab phrase from their segment labels alone (no stored word summary required).
- **SC-003**: For all ~21 known part-of-speech mislabels (FR-011), the label shown to users is the **corrected** Arabic value, and **0** user-facing labels are the known-wrong seed values.
- **SC-004**: A generation run changes **0** original morphology columns and leaves the segment row count at exactly **128,219** (verified by before/after comparison).
- **SC-005**: The **208** elided 1st-person-singular segments keep an empty/NULL written form (**0** invented forms) while still receiving an i‘rab label.
- **SC-006**: Re-running generation on unchanged morphology yields **0** differences from the previous run (idempotent).
- **SC-007**: Running against missing or stale morphology results in a clear refusal with **0** rows written; a forced hard-check failure results in a full rollback with **0** rows committed.
- **SC-008**: **100%** of runs emit a report containing per-status coverage, per-rule usage, unmatched-signature list, and every hard-check result.
- **SC-009**: The stored model contains the inline `i3rab_*` columns plus exactly **one** new table (`quran_i3rab_rules`); it contains **no** `quran_word_segment_i3rab` and **no** `quran_word_i3rab` table.

---

## Out of Scope (v1)

- **No UI** — no pages, components, colouring, or any Frontend work.
- **No API endpoint** — no controllers, request/response contracts, or runtime read path.
- **No full scholarly i‘rab** and **no stored syntactic sentence roles** (فاعل / مفعول به / مبتدأ / خبر / حال). v1 stores form/case/type labels only.
- **No stored word-level summary** and **no stored idiom collapses / «محل …» role refinements** — these are read-layer behaviour for a later feature (documented, not built here).
- **No import of the full external i‘rab source files** — that is a separate future feature; Feature 005 must not block on it.
- **No edits** to `quran_words`, the Uthmani/QPC text, the original morphology fields, or the `quran_pos_tags` seed data.
- **No new linguistic source** — all inputs already exist in Feature 004.

---

## Assumptions

- **Feature 004 is complete and available.** The six morphology tables are populated and validated
  (`quran_word_morphology` = 77,432, `quran_word_morphology_segments` = 128,219, `quran_pos_tags` = 49,
  plus root/lemma/stem dimensions). Feature 005 reads these as-is.
- **Branch base.** Feature 004 is not yet merged to `main`; this feature's branch
  (`005-word-simple-i3rab-foundation`) is cut from `004-word-morphology-foundation`, so the morphology
  tables, the part-of-speech seed, and the Feature 004 design docs remain available.
- **Per-occurrence grain.** Case/tense/voice are contextual, so i‘rab is keyed to the per-occurrence
  segment record (via `quran_word_id` / segment id), **not** to the imlaei-simple identity/stats key.
- **Operator runs the generator.** The generation is triggered by an operator or CI step (a console/CLI
  action), consistent with how Feature 004's morphology import runs. There is no scheduled/online trigger
  in v1.
- **Rule catalogue is curated in code and seeded** (the same pattern as the existing part-of-speech seed),
  so it is versioned and reproducible.
- **Coverage is already proven.** The full segment-pattern inventory (the finalized coverage report)
  established that all 142 segment-token signatures map to approved labels (100% of segment rows); no
  further discovery is required before planning.
- **Quranic data safety.** The feature stores derived grammatical labels keyed by identifier only — no
  ayah text — never modifies the Qur'an text or the original morphology, keeps the 208 NULL forms NULL,
  records anything unsupported rather than guessing, and never presents the simplified labels as
  authoritative scholarly i‘rab. This is an Arabic-first product; all user-facing labels are Arabic.

---

## Dependencies

- **Feature 004 — Quran Word Morphology Foundation** (the morphology tables and the `quran_pos_tags`
  seed). Hard dependency; i‘rab generation runs after a successful morphology import.
- **Existing transactional import/validation discipline** from Feature 004 (assemble → validate hard gate
  → commit-or-rollback → report), reused for the i‘rab generation pass.
