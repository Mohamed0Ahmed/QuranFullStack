# Feature 005 — Word Simple I‘rab Foundation (Planning Report)

**Status:** Planning / report only — no code, no Spec Kit artifacts, no migrations.
**Proposed branch:** `005-word-simple-i3rab-foundation`
**Proposed docs folder:** `docs/feature-005-word-simple-i3rab-foundation/`
**Date:** 2026-06-12 · **Revised:** 2026-06-12 (post segment-pattern inventory — v1 model locked)
**Builds on:** Feature 004 — Quran Word Morphology Foundation (branch `004-word-morphology-foundation`)

> **Revision note (authoritative).** This report has been updated after the complete
> segment-pattern inventory of the live Feature 004 database. The **v1 data model is now locked**
> to **inline `i3rab_*` columns on `quran_word_morphology_segments` + the `quran_i3rab_rules`
> reference table**. The earlier two-output-table proposal (`quran_word_segment_i3rab` and
> `quran_word_i3rab`) is **rejected for v1** — see §3 and §10. Evidence backing this decision is in:
> - `Backend/report/feature-005-word-simple-i3rab-foundation/simple-i3rab-label-inventory-report.md`
> - `Backend/report/feature-005-word-simple-i3rab-foundation/segment-pattern-rule-coverage-report.md`
>
> The pre-final machine-readable companions (`segment-pattern-rule-coverage.json` / `.csv`) were
> **removed** to prevent accidental use as canonical rule seeds; the finalized markdown reports above are
> authoritative. If implementation needs a machine-readable rule seed, regenerate it from the finalized
> 142-signature / 67-family approved catalogue.

---

## 0. Purpose and one-paragraph summary

Feature 005 builds a **foundation for simplified Arabic grammar / i‘rab labels** — a short,
Corpus-style grammatical label on **each morphology segment**, plus a per-word summary that is
**composed at read time** from the ordered segment labels — **derived deterministically from the
Feature 004 morphology tables**. It produces values like `P → حرف جر`, `N + GEN → اسم مجرور`,
`V + IMPF → فعل مضارع`, and `PN + lemma الله + GEN → لفظ الجلالة مجرور`. It is **data only** (no UI,
no API), it **never** edits the Quran text or the morphology source, and it treats the generated
labels as *quick, simplified grammar labels*, **not** full scholarly i‘rab. The separate full-i‘rab
source files we already hold remain a **distinct, later integration path** and this feature must not
block on them.

The v1 grain is the **segment**: every segment carries its own simplified Arabic label and a review
**status** (`approved` / `needs_review` / `unsupported`). The word-level summary
(`حرف جر، اسم مجرور، ضمير متصل للمخاطب`) and idiom collapses (`جار ومجرور`) are
**read-layer behavior**, derived by ordering segment labels — they are **not stored** in v1.

---

## 1. Established baseline (from Feature 004, measured — not assumed)

Feature 005 consumes the six tables Feature 004 already produces and validates. The relevant facts:

| Fact | Value | Source |
|---|---|---|
| Readable words (grain) | **77,432** (one morphology row each; `is_ayah_marker = false`) | `specs/004-…/data-model.md`, `spec.md` SC-001 |
| Ayah markers | **excluded** from morphology entirely | FR-010 |
| Segment rows | **128,219** (prefix / stem / suffix) | `data-model.md` §2; live DB |
| POS controlled vocabulary | **49 codes**, each with Arabic + English label, category, sort order | `Backend/…/Files/Quran/Morphology/PosTagSeed.cs` |
| Word-level head fields | `head_pos`, `is_verb`, `verb_tense`, `verb_voice`, `case_feature`, `head_features_json` | `quran_word_morphology` |
| Segment fields | `kind` (PREFIX/STEM/SUFFIX), `pos`, `form_buckwalter`, `form_arabic_normalized` + tier, `features_raw`, `features_json` | `quran_word_morphology_segments` |
| Verb tense derivation | `PERF → past (فعل ماض)`, `IMPF → present (فعل مضارع)`, `IMPV → imperative (فعل أمر)` | `MorphologyAssembler.MapVerbTense` |
| Voice derivation | `PASS → passive`, else `active` by documented convention | `MorphologyAssembler.MapVerbVoice` |
| Case derivation | `NOM → nominative (مرفوع)`, `ACC → accusative (منصوب)`, `GEN → genitive (مجرور)` | `MorphologyAssembler.MapCaseFeature` |
| Root/lemma/stem | resolved Arabic display values (QUL), nullable; `lemma_text` carries لفظ الجلالة etc. | `quran_lemmas`, `quran_roots`, `quran_stems` |

**Key consequence for design:** Feature 004 already stores everything the rules need
(POS per segment, kind, case/tense/voice, lemma Arabic). Feature 005 adds **derived labels +
provenance**, it does **not** re-derive or re-store morphology.

> **Branch base note.** Feature 004 (its `specs/004-…`, `docs/feature-004-…`, and Backend morphology
> code) is **not yet merged to `main`**; it lives on `004-word-morphology-foundation`. This planning
> branch (`005-word-simple-i3rab-foundation`) was therefore cut **from `004-…`** so the morphology
> tables, the POS seed, and the Feature 004 design docs this report references remain available.

### 1.1 Segment-pattern inventory evidence (the basis for the locked model)

The complete inventory of the live database (every segment ordering pattern, not just the common ones)
measured the following — these numbers drive §3, §5, and §10:

| Measure | Value |
|---|---:|
| Total readable words | **77,432** |
| Total segment rows | **128,219** |
| POS-only patterns | **358** |
| kind+POS patterns | **371** |
| Enriched i‘rab-signature patterns | **1,337** |
| Distinct segment-token signatures | **142** |
| Proposed rule families (collapsed) | **67** |
| Segment **approved-candidate** coverage | **100.0%** |
| Segment **needs-review** | **0.0%** |
| Segment **unsupported** | **0.0%** |
| Word **fully-approved** (every segment approved) | **100.0%** |
| Word **displayable** (ordered segment labels derivable) | **100.0%** |

> **On "fully-approved".** With all **142** segment-token signatures approved-candidate, **every** segment
> carries an approved label, so **every** readable word is fully-approved (100%) and displayable (100%)
> from approved segment labels. Any remaining idiom/role refinements (e.g. `V+PRON` → «الضمير في محل نصب
> مفعول به») are **read-layer** interpretive notes (§5.4), **not** segment-approval gaps. (The earlier
> 95.33% reflected the pre-finalization state when a few segment tokens were still needs-review.)

**Planning interpretation (critical).** We do **not** write 1,337 independent rules. The
implementation is based on the **142 segment-token signatures**, collapsed into **67 rule families**.
The **1,337 enriched patterns** are **coverage / proof / examples**, **not** one rule class per
pattern. The 405-long singleton tail in the enriched view is routine person/number/voice variation of
already-covered families, not new grammar.

---

## 2. Scope

### 2.1 In scope

1. **Per-segment simplified Arabic labels.** For each readable word's segments (prefix / stem / suffix)
   produce a short Arabic grammar label and a review **status**, derived deterministically from the
   Feature 004 morphology fields.
2. **Per-word summary composed at read time** — *not stored*. The word phrase
   (`حرف جر، اسم مجرور، ضمير متصل للمخاطب`) is produced by ordering the stored segment labels by
   `segment_number` and joining with the Arabic comma «، ». Idiom collapses (`جار ومجرور`) are a later
   read-layer/UI behavior on top of the same ordered labels.
3. **Primary source = Feature 004 morphology tables.** Rules read `quran_word_morphology`,
   `quran_word_morphology_segments`, `quran_pos_tags`, and the dimension tables (for the لفظ الجلالة /
   lemma-aware cases). No new linguistic source is imported.
4. **Provenance + review state on every segment.** Each segment records which rule produced its label,
   the review status, and (for `unsupported`) a reason.
5. **Curated rule catalogue (`quran_i3rab_rules`)** that owns the user-facing Arabic labels, rule
   provenance, review state, and coverage/usage reporting.
6. **Transactional, rebuildable generator** consistent with Feature 004 (read morphology →
   assemble in memory → validate hard gate → write in one transaction → report), never touching source.

Feature 005 **may write only**:
- `quran_i3rab_rules` (new reference table), and
- the new `i3rab_*` derived columns on `quran_word_morphology_segments`.

### 2.2 Out of scope (hard guards — carry into `/specify`)

- **Backend data foundation only** — **no UI** (no pages, no components, no Frontend work).
- **No API endpoint** (no controllers, no request/response contracts, no runtime read path) unless
  explicitly requested in a later feature.
- **No full scholarly i‘rab** — generated labels are *quick simplified grammar labels* and must never be
  presented or stored as authoritative i‘rab.
- **No guaranteed syntactic sentence roles** — no فاعل / مفعول به / مبتدأ / خبر / حال unless **directly
  and safely derivable** from existing morphology. v1 emits *form/case labels* (اسم مجرور, فعل مضارع),
  not sentence roles.
- **No import of the full i‘rab files yet** (separate future feature — see §6).
- **No changes to `quran_words`** (source text, Uthmani/QPC columns) or any Feature 002/003 table.
- **No changes to the original morphology fields** — `head_pos`, `pos`, `kind`, `case_feature`,
  `verb_tense`, `verb_voice`, `form_*`, `features_*` are read-only; the new `i3rab_*` columns never
  overwrite them.
- **No changes to `quran_pos_tags` seed data.** The inventory surfaced **20+ seed-label issues**
  (e.g. `REM`, `RES`, `T`, `AMD`, `INL`, `PREV`, `SUR`, `INT`, `EXH`, stem/prefix `INTG`, …) — the
  **full final correction set is the canonical table in §3.2**. Correcting the POS seed is a
  **separate cleanup feature**, not Feature 005. The rule layer compensates by owning display labels.
- **No generated Quran text and no invented segment forms** — see the NULL-form guard in §7.

---

## 3. Data model — **locked v1**

The question the spec had to answer was *new tables vs. derive on demand*. The complete inventory
settled it: there are only **142 distinct segment-token signatures** collapsing to **67 rule families**,
and segment i‘rab is **strictly 1:1** with `quran_word_morphology_segments`. That makes a separate
segment table pure join overhead, and a stored word table redundant with a deterministic read-time
join. **v1 is therefore inline columns + one reference table.**

### 3.1 Inline `i3rab_*` columns on `quran_word_morphology_segments`

Segment i‘rab is 1:1 with the segment row, so the label lives **on the segment row** — no new
per-segment table. **Original morphology columns are never altered or overwritten**; only these new
columns are added:

| Column | Type | Null | Notes |
|---|---|---|---|
| `i3rab_arabic` | `text` | YES | simplified Arabic label, e.g. `حرف جر`, `اسم مجرور`, `ضمير متصل للمخاطب` |
| `i3rab_rule_id` | `int` | YES | FK → `quran_i3rab_rules.id` (the rule that produced the label) |
| `i3rab_status` | `text` | **NO** | one of `approved` · `needs_review` · `unsupported` |
| `i3rab_review_reason` | `text` | YES | required for `unsupported`; review note for `needs_review` |

**Status — three meaningful states** (the inventory proved all three are real, hence a status enum, not
a single boolean). Report wording → stored value:

| Report term | Stored `i3rab_status` | Meaning |
|---|---|---|
| approved-candidate | `approved` | safe to display as a v1 simplified label |
| needs-review | `needs_review` | derivable, but the Arabic label / interpretation needs sign-off before normal user display |
| unsupported-v1 | `unsupported` | no v1 label should be displayed |

**Consistency rules (enforced by §7 hard checks):**

- `i3rab_arabic` is **required** for `approved`.
- `i3rab_arabic` **may** be present for `needs_review` **only** if clearly marked internally as
  review-only (it is not shown in normal user display).
- `i3rab_rule_id` is **required** for `approved` and for `needs_review` when a rule produced the label.
- `unsupported` **must** carry an `i3rab_review_reason` (never a silent empty).
- Original morphology columns are **not** overwritten or altered.

### 3.2 `quran_i3rab_rules` — retained (curated reference table)

| Column | Type | Notes |
|---|---|---|
| `id` | `int` | PK; FK target for `i3rab_rule_id` |
| `rule_key` | `text` | UNIQUE, e.g. `P`, `N:GEN`, `V:IMPF`, `PN:ALLAH:GEN`, `SUFFIX:PRON` |
| `rule_family` | `text` | the collapsed family (one of the 67) this rule belongs to |
| `pattern_description` | `text` | English/dev notes describing the matched kind+POS+feature signature |
| `i3rab_arabic` | `text` | **canonical Arabic label the rule emits — the user-facing display source** |
| `default_status` | `text` | **`approved` for all 67 families in v1**; `needs_review` / `unsupported` are schema-reserved for future growth/review |
| `review_reason` | `text` | sign-off note (used if a future family is `needs_review`) |
| `sort_order` | `smallint` | display order |

**Purpose of the rule layer:** curated rule catalogue · rule provenance · **ownership of the
user-facing Arabic labels** · coverage reporting · review state · per-rule usage counts.

**Why the rule layer owns labels (not `quran_pos_tags`).** The inventory found real **seed-label
issues** in `quran_pos_tags.arabic_label`. Therefore Feature 005 **must not** display
`quran_pos_tags.arabic_label` blindly. `quran_pos_tags` remains a **technical dictionary / controlled
vocabulary** (the POS FK target and dev audit source), **not** the final user-facing i‘rab display
source — the rule layer is.

**Canonical display-label corrections (rule layer owns these — full final set).** The rule layer emits
these Arabic labels regardless of the (sometimes wrong) `quran_pos_tags` seed:

| POS / signature | seed `arabic_label` | Feature 005 display label |
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

**v1 status.** All **67 rule families** are **approved-candidate** in v1 (100% of the 128,219 segment
rows). The `needs_review` and `unsupported` values of `i3rab_status` are **schema-reserved** — defined in
the enum for future catalogue growth and the review workflow, but **no rows carry them in v1**.

### 3.3 No `quran_word_segment_i3rab` (rejected for v1)

Segment i‘rab is strictly **1:1** with `quran_word_morphology_segments`. A separate segment i‘rab table
would add a join on every read for **zero** modeling benefit in v1. Rejected — the data lives inline
(§3.1).

### 3.4 No `quran_word_i3rab` (rejected for v1)

The word summary is **composed at read time** from the ordered segment labels — there is nothing to
store:

```
SELECT i3rab_arabic
FROM   quran_word_morphology_segments
WHERE  quran_word_id = :id
ORDER BY segment_number;        -- join with the Arabic comma «، »
```

Example output: `حرف جر، اسم مجرور، ضمير متصل للمخاطب`.

**Word-level idiom collapses are a later read-layer / UI behavior, not stored in v1:**

| Ordered segments | Read-layer idiom |
|---|---|
| `P + PRON` | جار ومجرور |
| `P + N:GEN` | جار ومجرور |
| `P + N:GEN + PRON` | جار ومجرور، والضمير مضاف إليه |
| `INTG + V` | همزة استفهام، فعل… |

Because only **142** segment signatures exist against **1,337** word-level enriched patterns, storing
word summaries would freeze a derivable view; the read-time join stays correct as rules evolve.

> **Grain correctness — per-occurrence, not per-identity.** A word's case (مرفوع/منصوب/مجرور) is
> **contextual**: the same spelling appears in different cases across the Mushaf. The inline columns are
> on the per-occurrence segment row (keyed through `quran_word_id`), **not** the imlaei-simple
> identity/stats key. (Identity/stats key is clean imlaei-simple; *display* and *i‘rab* stay
> occurrence-level.)

---

## 4. Rule sources — how labels are derived

All inputs already exist in Feature 004; no new linguistic source is read.

| Input | Table / column | Use in i‘rab rules |
|---|---|---|
| Segment POS code | `quran_word_morphology_segments.pos` → `quran_pos_tags` | base label for the segment (`P → حرف جر`, `REL → اسم موصول`, `DET → أداة تعريف`) — **via the rule layer, not the POS seed label** |
| Segment kind | `…_segments.kind` (PREFIX/STEM/SUFFIX) | distinguishes prefix particles, the stem head, attached suffixes (`SUFFIX + PRON → ضمير متصل`) |
| Head POS | `quran_word_morphology.head_pos` | the word's governing category (read-time summary only) |
| Case feature | `…morphology.case_feature` (`nominative`/`accusative`/`genitive`) | case suffix on the noun label: `مرفوع` / `منصوب` / `مجرور` |
| Verb tense | `…morphology.verb_tense` (`past`/`present`/`imperative`) | `فعل ماض` / `فعل مضارع` / `فعل أمر` |
| Verb voice | `…morphology.verb_voice` (`active`/`passive`) | append `مبني للمجهول` when passive |
| Person/gender/number | `features_json` / `features_raw` tokens | optional refinement and **Arabic agreement** |
| Special lemma (Allah) | `quran_lemmas.lemma_text` / `lemma_buckwalter ~ Allah` via `lemma_id` | `PN + lemma الله → لفظ الجلالة` |
| Segment adjacency | ordered segments within a word | **read-time** idiom collapses (`P+PRON → جار ومجرور`) |

**Arabic agreement is a real rule responsibility (not cosmetic).** The case suffix must agree in
gender with the base noun label: a *masculine* base like `اسم` takes `اسم مجرور`, but a *feminine* base
like `صفة` takes **`صفة مجرورة`**. The rule catalogue encodes the agreement form rather than naively
concatenating — exactly the value Feature 005 adds over the flat POS labels.

---

## 5. Rule strategy and examples

### 5.1 Strategy (locked)

Feature 005 v1 **generates segment-level simplified i‘rab labels**. Each segment gets its own label and
its own status. The future UI can **color each word segment** and show the same-colored i‘rab label next
to it. The word summary is the ordered join of those segment labels (read time).

The catalogue is built from the **142 segment-token signatures collapsed into 67 rule families** — not
from 1,337 independent rules. The 1,337 enriched patterns serve as coverage proof and worked examples.

### 5.2 Worked example — بِحَمْدِكَ (segment coloring)

| Segment | POS / role | Segment label |
|---|---|---|
| بِ | `PREFIX` P | حرف جر |
| حَمْدِ | `STEM` N (GEN) | اسم مجرور |
| كَ | `SUFFIX` PRON | ضمير متصل للمخاطب |

**Joined read-time display:** `حرف جر، اسم مجرور، ضمير متصل للمخاطب`
**Optional later idiom display:** `جار ومجرور، والكاف مضاف إليه`

### 5.3 Representative segment rules (grounded in real POS codes / features)

Segment-level:

| Pattern | Simplified Arabic | Status (typical) |
|---|---|---|
| `P` | حرف جر | approved |
| `SUFFIX PRON` | ضمير متصل | approved |
| `REL` | اسم موصول | approved |
| `DET` | أداة تعريف | approved |
| `PREFIX INTG` (hamza) | همزة استفهام | approved |
| `CONJ` | حرف عطف | approved |
| `NEG` | حرف نفي | approved |
| `DEM` | اسم إشارة | approved |

Noun head + case:

| Pattern | Simplified Arabic |
|---|---|
| `N:NOM` | اسم مرفوع |
| `N:ACC` | اسم منصوب |
| `N:GEN` | اسم مجرور |
| `ADJ:GEN` | صفة مجرورة *(feminine agreement)* |
| `PN:GEN` | اسم علم مجرور |
| `PN:ALLAH:GEN` | لفظ الجلالة مجرور |
| `PN:ALLAH:NOM` | لفظ الجلالة مرفوع |

Verb head + tense (+ voice):

| Pattern | Simplified Arabic |
|---|---|
| `V:IMPF` | فعل مضارع |
| `V:PERF` | فعل ماض |
| `V:IMPV` | فعل أمر |
| `V:IMPF:PASS` | فعل مضارع مبني للمجهول |
| `V:PERF:PASS` | فعل ماض مبني للمجهول |

Anything a rule cannot label is recorded with `i3rab_status = unsupported` + an `i3rab_review_reason`,
never guessed. Inventory result: **0.0% unsupported, 0.0% needs-review, 100.0% approved-candidate** at
segment level (all 142 segment-token signatures are approved-candidate).

### 5.4 Read-layer pattern-aware overrides & role refinements (not importer behavior)

These are produced by the **read/UI layer** from the ordered segment labels; the importer stores only the
plain per-segment label. They are **interpretive** (the role depends on the *preceding* segment) and are
**not** counted in segment-row coverage — none of them changes the **100%** segment approval.

| Pattern | Per-segment labels (stored in v1) | Combined / role display (read-layer) |
|---|---|---|
| `P + SUB` (كَمَا، كَأَن، عَمَّا) | جار، مجرور | **جار ومجرور** |
| `SUP + AMD` (وَلَـٰكِن) | حرف زائد، حرف استدراك | **حرف استدراك** |
| `ACC + PREV` (إِنَّمَا) | حرف نصب، ما الكافّة | **كافّة ومكفوفة** |
| `V + PRON` | فعل…، ضمير متصل | الضمير **في محل نصب مفعول به** |
| `ACC + PRON` | حرف نصب، ضمير متصل | الضمير **في محل نصب اسم إنّ** |

These complement the §3.4 idiom collapses (`P+PRON` / `P+N:GEN` / `P+N:GEN+PRON` → جار ومجرور / …
مضاف إليه). In v1 the DB stores the plain segment label (`ضمير متصل`, `حرف مصدري`, …); the read layer adds
the «محل …» role and the idiom collapse.

---

## 6. Relationship to the full i‘rab source files

- **The full i‘rab files are valuable but separate.** They are richer (full grammatical analysis,
  syntactic roles, vocalization notes) and are **not in scope** here.
- **This feature must not block on them.** Feature 005 derives its simplified labels **only** from the
  Feature 004 morphology tables, which are already built and validated. No full-i‘rab import is required
  to ship Feature 005.
- **Later integration is a separate feature.** A future "Feature 00X — Full I‘rab Import & Display" can
  import the full files into their own table(s) and **link** them to words/segments:
  - **Full i‘rab** = authoritative, scholarly, display-grade analysis (when present for a word).
  - **Simplified generated i‘rab** = a **fallback / quick grammar label** that exists for *every*
    displayable readable word (100% coverage), even where full i‘rab is missing.
- **Design hook now, integrate later.** Because the inline labels are keyed per-occurrence on the
  segment row (through `quran_word_id` / `segment_number`), a later feature can join full i‘rab onto the
  same keys without reworking Feature 005. No full-i‘rab columns are added now (YAGNI).

---

## 6.5 Source / rebuild coupling (Feature 004 dependency)

- Feature 005 **depends on a completed Feature 004 morphology import**.
- The `i3rab_*` columns live **on** `quran_word_morphology_segments`. If Feature 004 morphology is
  **re-imported with `--force`** and segments are truncated/reloaded, the `i3rab_*` values are
  **invalidated / cleared** along with the rows they sat on.
- Therefore **simple i‘rab generation must run *after* morphology import**, never before or interleaved.
- The generator **must detect stale or missing morphology** (empty/short segment table, mismatched
  counts, missing expected columns) and **refuse or warn clearly** rather than write partial labels.

---

## 7. Validation — hard checks and warnings

Mirror Feature 004's gate-or-rollback discipline: assemble in memory → validate → write in one
transaction → commit iff all **hard** checks pass, else roll back, write a failure report, exit
non-zero.

### 7.1 Hard checks (gate the commit)

| Id | Invariant |
|---|---|
| `I3RAB-SEG-STATUS-COMPLETE` | every morphology segment has a non-null `i3rab_status` (one of the three allowed values) |
| `I3RAB-APPROVED-CONSISTENT` | `i3rab_status = approved` requires both `i3rab_arabic` **and** `i3rab_rule_id` |
| `I3RAB-NEEDS-REVIEW-CONSISTENT` | `i3rab_status = needs_review` requires `i3rab_rule_id` **and** `i3rab_review_reason` |
| `I3RAB-UNSUPPORTED-CONSISTENT` | `i3rab_status = unsupported` requires `i3rab_review_reason` and must not be silently empty |
| `I3RAB-WORD-DISPLAYABLE` | every readable word can derive an ordered segment-label display, even if some segments are `needs_review`/`unsupported` internally |
| `I3RAB-RULE-RESOLVES` | every non-null `i3rab_rule_id` resolves to a `quran_i3rab_rules` row (no dangling) |
| `I3RAB-SOURCE-COLUMNS-UNCHANGED` | all original morphology source columns remain byte-for-byte unchanged before & after the run |
| `I3RAB-SEGMENT-ROWCOUNT-STABLE` | Feature 005 performs **no** insert/delete/truncate of `quran_word_morphology_segments` rows (only updates the new `i3rab_*` columns) |
| `I3RAB-NULL-FORM-NOT-INVENTED` | the **208** NULL `form_arabic_normalized` rows remain NULL; Feature 005 only adds labels, **never** fabricates a form |

Any hard check failing ⇒ rollback (write nothing) + failure report + non-zero exit.

### 7.2 Warnings (informational — never fail the build)

| Id | Signal |
|---|---|
| `I3RAB-COVERAGE` | share of segments/words by status (`approved` / `needs_review` / `unsupported`); report, don't gate |
| `I3RAB-RULE-USAGE` | per-rule hit counts (which rules fired, which never fired) |
| `I3RAB-UNKNOWN-PATTERNS` | any segment signature with no matching rule family (so the catalogue can grow) |
| `I3RAB-NEEDS-REVIEW-SUMMARY` | enumerated `needs_review` families/segments routed for human sign-off |
| `I3RAB-LABEL-REVIEW` | labels diverging from `quran_pos_tags.arabic_label` (the **full** seed-issue set — see the canonical correction table in §3.2: `T`, `SUB`, `RES`, `INTG`, `AMD`, `SUP`, `PREV`, `INC`, `EXL`, `INT`, `EXH`, `SUR`, `INL`, `EQ`, `VOC.SUFFIX`, `COM`, `P.SUFFIX`, `N.GEN.1S`, `REM`, prefix `IMPV`) flagged for confirmation |

### 7.3 Explicit guarantees to assert

- Every **segment** has a status and is internally consistent for that status (§7.1).
- Every **readable** word yields an ordered segment-label display (**100% displayable**).
- **Source morphology columns and segment row counts are unchanged** by Feature 005.
- The **208 NULL forms stay NULL** — labels are added, forms are never invented.
- **Unknown signatures are reported**, never silently dropped or guessed.
- **Coverage and per-rule usage are reported** in every build.
- **needs-review and seed-divergent labels are listed** for review.

---

## 8. Deliverables for the future Spec Kit feature (`/speckit.specify` should lock)

1. **Exact scope** — segment-level simplified Arabic labels derived from Feature 004; word summary
   composed at read time; data only; explicitly *not* full scholarly i‘rab; the §2.2 guards verbatim.
2. **Schema** — add inline `i3rab_arabic`, `i3rab_rule_id`, `i3rab_status`, `i3rab_review_reason` to
   `quran_word_morphology_segments`; create the `quran_i3rab_rules` reference table. **Do not** create
   `quran_word_segment_i3rab` or `quran_word_i3rab`. Original morphology columns untouched.
3. **Generator / rebuild behavior** — a new operator/CI console verb (e.g. `generate-i3rab`) that reads
   morphology, seeds the rule catalogue, assembles in memory, validates the hard gate, writes the
   `i3rab_*` columns in one transaction, refuses on stale/missing morphology, is idempotent, runs
   **after** morphology import, and never touches source columns or row counts.
4. **Report output** — status distribution, rule-coverage %, per-rule usage, unknown-signature list,
   needs-review summary, seed-divergence list, hard-check results, final outcome (one artifact per run).
5. **Validation checks** — the §7.1 hard checks and §7.2 warnings, with rollback-or-commit semantics.
6. **Rule catalogue v1** — build the catalogue from the **142 segment-token signatures → 67 rule
   families**; the rule layer owns Arabic display labels; mark families `needs_review` where sign-off is
   pending so coverage grows transparently.
7. **No UI / API** for this feature unless explicitly decided later; no full-i‘rab import; no syntactic
   roles unless directly/safely derivable; no edits to `quran_words`, morphology fields, or
   `quran_pos_tags` seed.
8. **Read-layer note (future, not v1)** — word summary = ordered segment labels joined with «، »; idiom
   collapses (`جار ومجرور`, `… مضاف إليه`) are read-layer/UI behavior layered on top later.

---

## 9. Recommendations (closing)

- **Recommended feature name:** **Feature 005 — Word Simple I‘rab Foundation**
- **Recommended branch name:** **`005-word-simple-i3rab-foundation`** *(cut from
  `004-word-morphology-foundation`)*
- **Recommended docs folder:** **`docs/feature-005-word-simple-i3rab-foundation/`**
- **Recommended data model (locked):** inline **`i3rab_*` columns on
  `quran_word_morphology_segments`** + the curated **`quran_i3rab_rules`** reference table;
  **`quran_word_segment_i3rab` and `quran_word_i3rab` rejected for v1**; `quran_pos_tags` retained as a
  technical dictionary; rule layer owns Arabic display labels; word summaries composed at read time;
  per-occurrence grain.
- **Recommended next step:** review this updated report, then start Spec Kit with **`/speckit.specify`**
  for Feature 005 using the scope, guards, schema, validation, and deliverables locked in §2, §3, §7,
  §8, and §10. **No further discovery reports are required** — the pattern inventory is complete.

---

## 10. Final v1 decisions

- **Inline segment i‘rab columns** on `quran_word_morphology_segments`
  (`i3rab_arabic`, `i3rab_rule_id`, `i3rab_status`, `i3rab_review_reason`).
- **`quran_i3rab_rules` retained** (curated catalogue; owns Arabic display labels; provenance,
  coverage, review state, usage counts).
- **`quran_word_segment_i3rab` rejected for v1** (segment i‘rab is 1:1 with the segment row → join
  overhead, no benefit).
- **`quran_word_i3rab` rejected for v1** (word summary is composed at read time from ordered segment
  labels).
- **`quran_pos_tags` retained as a technical dictionary** / controlled vocabulary — **not** the
  user-facing display source; the rule layer owns the **full display-label correction set** (canonical
  table in §3.2: `T→ظرف زمان`, `SUB→حرف مصدري`, `RES→أداة حصر`, `STEM:INTG→اسم استفهام`,
  `AMD→حرف استدراك`, `SUR→حرف فجاءة`, `INL`, `EQ`, `VOC.SUFFIX`, `COM`, `P.SUFFIX→لام الجر`,
  `N.GEN.1S`, `REM`, … 20+ items).
- **The Feature 005 rule layer owns the Arabic display labels.**
- **Coverage locked:** 142 segment-token signatures → 67 rule families; **100.0%** of 128,219 segment
  rows are approved-candidate; **0.0%** needs-review / **0.0%** unsupported; read-layer role/idiom
  refinements (§5.4) are tracked **separately**, not as coverage gaps.
- **Word summaries composed at read time** from ordered segment labels (`ORDER BY segment_number`,
  joined with «، »); idiom collapses are a later read-layer behavior.
- **Pattern inventory complete** — no more discovery reports required before `/speckit.specify`.
- **Next step:** run **`/speckit.specify`** after reviewing this updated planning report.

---

### Quranic data safety (applies throughout)

This feature stores **derived grammatical labels keyed by identifier only** (`quran_word_id`,
`segment_number` on the existing segment rows). It stores **no ayah text**, never modifies
`quran_words` or the Uthmani/QPC columns, never alters the Feature 004 morphology source columns or
segment row counts, keeps the 208 NULL forms NULL, and never invents grammatical analysis: an
unsupported pattern is recorded as `unsupported` with a reason, never guessed. The generated simplified
labels are explicitly **not** authoritative scholarly i‘rab.
