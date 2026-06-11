# Feature 004 — Confirmed Decisions Addendum

**Date:** 2026-06-10
**Status:** Planning addendum only. **No code, no migrations, no source edits, no build/test were run.**
**Governs:** `feature-004-word-morphology-foundation-planning-report.md` (the main plan). Where wording
differs, **this addendum is authoritative**. Supporting evidence:
`segment-arabic-rendering-capability-report.md`.

This addendum records two decisions adopted into Feature 004 — **Quran Word Morphology Foundation** —
and lists exactly what changes in schema, validation, and scope. Both are **data-foundation only**:
**no UI, no API, no generated i3rab** in Feature 004.

---

## 1. Decisions adopted

### D1 — Segment Arabic rendering (adopt **Option B**)

Add a **normalized Arabic segment rendering** to `quran_word_morphology_segments`, **only as a derived
morphology reading aid**. Transliteration of the Corpus Buckwalter `form` is deterministic (100 % of
characters map, 0 unknown), but the result is a **morphological/phonemic reading**, not the Mushaf
glyph (whole-word concatenation matches `qpcUthmani` only ≈ 79.83 %). It is therefore stored as a
flagged, derived aid — never as authoritative Quran text.

**Required segment columns:**

| Column | Type | Null | Meaning |
|---|---|---|---|
| `form_buckwalter` | `text` | NO | raw Corpus source form — **always retained**, lossless |
| `form_arabic_normalized` | `text` | YES | Arabic rendering from Buckwalter transliteration; `NULL` for empty forms |
| `arabic_render_tier` | `text` | YES | `clean` / `quranic_marks` / `review` / `multiword` |
| `arabic_render_source` | `text` | NO | constant `buckwalter-transliteration` |

**Hard rules (non-negotiable):**

- **Never** name it `qpc_segment_text`.
- **Never** claim it is an exact substring of `qpcUthmani`.
- **Never** use it as Mushaf display text.
- Authoritative Quran display text remains **`quran_words.text_uthmani` / `qpc_glyph`**.
- **Empty forms render as `NULL`**, not guessed text (expected 208 empty `(SUFFIX, PRON)` forms).
- **Fragile/`review` rows are flagged, not corrected by invention.**

**Rendering policy:** best-effort **for all** non-empty segments (not prefixes-only, not
mandatory-uniform), each stamped with its tier. Baseline fidelity distribution: ~94.2 % `clean`,
~5.4 % `quranic_marks`, ~0.4 % `review`, 1 `multiword`.

### D2 — Word-type filtering foundation

Feature 004 includes the **data foundation** to filter words by type later. **This is data only — no
UI pages and no API endpoints in Feature 004.**

Included:

- **`quran_pos_tags`** controlled-vocabulary reference table (POS codes → labels/category/order).
- **`quran_word_morphology.head_pos`** (the STEM segment's POS, references `quran_pos_tags.code`).
- A **broad POS category** for future grouping: `noun` / `verb` / `particle` / `other`.
- **Arabic + English labels** per tag, plus **`sort_order`** and an optional **`description`**.
- **Validation** that every `head_pos` (and every segment `pos`) resolves to a known POS tag.
- Support for **future filters** over the ordered Quran words joined to `quran_word_morphology`:
  all nouns / all verbs / all particles-tools; a specific tag (`N`, `PN`, `P`, `CONJ`, `NEG`, …);
  verb tense (past/present/imperative); verb voice (active/passive); case (nominative/accusative/genitive).

**Constraints:**

- **No physical `quran_verbs` table.** Verbs are **derived** from `quran_word_morphology`
  (`is_verb`, `verb_tense`, `verb_voice`) + indexes.
- POS tags are **controlled-vocabulary rows** in `quran_pos_tags`, **not** a large hard-coded enum.
- Small, stable, closed concepts **do** stay as enums/value objects (`Domain/Quran/Words/Morphology/`):
  - `SegmentKind` — Prefix / Stem / Suffix
  - `VerbTense` — Past / Present / Imperative
  - `VerbVoice` — Active / Passive
  - `MorphologicalCase` — Nominative / Accusative / Genitive

---

## 2. Changed decisions (vs. the original main plan)

| # | Was | Now |
|---|---|---|
| C1 | `quran_word_morphology_segments` stored only raw `form_buckwalter` (+ features) | **Adds** `form_arabic_normalized`, `arabic_render_tier`, `arabic_render_source` (D1) |
| C2 | `quran_pos_tags` described as **"optional"** (could be an in-code map) | **Required** table; not optional (D2) |
| C3 | `quran_pos_tags` columns: `code`, `arabic_label`, `category` | **Adds** `english_label`, `sort_order`, `description`; `category` fixed to `noun`/`verb`/`particle`/`other` (D2) |
| C4 | Morphology table count described as **five** | **Six** (the five data tables **+** `quran_pos_tags`) |
| C5 | Domain types not enumerated | **Enums/value objects fixed:** `SegmentKind`, `VerbTense`, `VerbVoice`, `MorphologicalCase` (D2) |
| C6 | Validation gate had no segment-rendering or POS-resolution checks | **Adds** `MORPH-POS-RESOLVES` + the `MORPH-SEG-*` family (D1/D2) |
| C7 | Out-of-scope did not name segment-rendering misuse / offsets / syntactic roles | **Adds** those explicit boundaries (§5) |

Unchanged: grain is per-occurrence (`quran_word_id`); markers excluded; QUL-Arabic for root/lemma/stem
display + Corpus for classification; one transaction with hard-check gate; **Option B** (Feature 004
morphology + Feature 005 Arabic i3rab).

---

## 3. Schema changes caused by these decisions

**`quran_word_morphology_segments`** — add three columns (D1):
`form_arabic_normalized text NULL`, `arabic_render_tier text NULL`,
`arabic_render_source text NOT NULL`. Add index on `arabic_render_tier` (route `review`/`multiword` to
curators). `form_buckwalter` stays `NOT NULL`.

**`quran_pos_tags`** — now **required**, columns (D2):
`code text PK`, `arabic_label text NOT NULL`, `english_label text NOT NULL`,
`category text NOT NULL` (`noun`/`verb`/`particle`/`other`), `sort_order smallint NOT NULL`,
`description text NULL`. Indexes: PK(`code`), `category`, `sort_order`. ≈ 30 seeded rows.

**`quran_word_morphology`** — unchanged columns; `head_pos` now formally references
`quran_pos_tags.code` (validated by `MORPH-POS-RESOLVES`); `verb_tense`/`verb_voice`/`case_feature`
backed by the §1-D2 enums/value objects.

**Domain (`Domain/Quran/Words/Morphology/`)** — add enums/value objects `SegmentKind`, `VerbTense`,
`VerbVoice`, `MorphologicalCase`.

**Migration** — the single `AddQuranWordMorphology` migration now covers **6 tables** + the three
segment-rendering columns. EF-tool-generated only; not hand-written; not applied as part of planning.

---

## 4. Validation checks added

**Hard (gate the commit, rollback on failure):**

| Id | Assertion |
|---|---|
| `MORPH-POS-RESOLVES` (D2) | every `head_pos` and segment `pos` resolves to a known `quran_pos_tags.code` (0 unknown) |
| `MORPH-SEG-CHARSET` (D1) | every `form` character is in the QAC transliteration map; **0 unmapped** (a new char refuses the import); space is allowed only for `multiword` tier |
| `MORPH-SEG-RENDER-TOTAL` (D1) | every non-empty form → non-empty `form_arabic_normalized`; every empty form → `NULL` (expected 208) |
| `MORPH-SEG-TIER-VALID` (D1) | every rendered row has a valid `arabic_render_tier`; `arabic_render_source = 'buckwalter-transliteration'` on all rows |
| `MORPH-SEG-RENDER-PROVENANCE` (D1, guard) | `form_arabic_normalized` is reproducible from `form_buckwalter` using the approved renderer; `arabic_render_source = buckwalter-transliteration`; equality with `qpc_glyph`/`text_uthmani` is allowed when deterministic and remains informational |

**Warning (informational, never change the verdict):**

| Id | Note |
|---|---|
| `MORPH-SEG-WORD-AGREEMENT` (D1) | per-word concatenated translit vs `qpcUthmani` exact match ≈ **79.83 %** (encoding-drift canary) |
| `MORPH-SEG-TIER-DIST` (D1) | tier distribution ≈ 94.2 % / 5.4 % / 0.4 % / 1 |
| `MORPH-SEG-REVIEW-LIST` (D1) | emit full `review` (134) + `multiword` (1) + empty (208) lists for manual sign-off |
| `MORPH-MULTI-STEM-LIST` | emit multi-STEM count, POS-pair distribution, examples, and reference the full investigation report when present |

These join the existing `MORPH-*` checks (`MORPH-READABLE-COMPLETE`, `MORPH-MARKERS-EXCLUDED`,
`MORPH-LOCATION-MATCH`, `MORPH-SEGMENTS-PRESENT`, `MORPH-POS-PRESENT`,
`MORPH-VERB-FEATURE-CONSISTENCY`, `MORPH-DIMENSION-RESOLVES`, `MORPH-SOURCE-UNCHANGED`).

---

## 5. Scope clarifications added

**In scope for Feature 004:** schema · import · source readers · morphology assembly · **normalized
Arabic segment rendering (D1)** · **POS/type lookup data (D2)** · validation · tests · reports · dev
reset/reseed.

**Out of scope for Feature 004 (explicit):**

- **UI pages** and **API endpoints** (including the word-type filters — data foundation only).
- **Generated Arabic i3rab prose** — Feature 005.
- **Syntactic roles** (فاعل / مفعول به / مبتدأ / خبر / حال …) — needs the absent treebank.
- **Exact character offsets inside `qpcUthmani`** — not attempted (Uthmani offsets are unsafe).
- **Treating `form_arabic_normalized` as Mushaf text** — it is a flagged derived reading aid only;
  authoritative display stays `quran_words.text_uthmani` / `qpc_glyph`.

**Local source files (clarification — see main plan §1.4 / §5.1 / §5.2):** the importer reads **only**
the **local in-repo** tree `App/resources/import-sources/quran-morphology/` (beside `quran-foundation/`),
which is a local, repo-relative workspace path used by the importer. `resources/` is **already
Git-ignored, so these data files are local-only by default and are not added, committed, or pushed**
("copy"/"stage" here means a **local file copy** into the in-repo path — **not** `git stage`, `git add`,
or push; no nested `.gitignore`/`manifest.example.json` is required). Runtime has **no dependency** on
the external `~/Desktop/.../resources/morphology` workspace, which is **read-only provenance only**.
Feature 004 **copies** (never moves/edits) **only the used files** (corpus aligned JSON, alignment map,
QUL root/lemma/stem, `manifest.json`, `README.md`). The `manifest.json` contract carries per-file
`role`, `originPath`, `expectedRecordCount`, `fileSizeBytes`, `sha256`, and `notes`;
`MORPH-SOURCE-UNCHANGED` validates the **local** files against it across the run. **Confirmed
responsibility split: Corpus aligned JSON = classification/structure (POS, segments,
features, verb tense/voice, case, Buckwalter root/lemma cross-ref, `form_buckwalter`, derived
`form_arabic_normalized`); QUL files = Arabic display values (root/lemma/stem).**

**Quranic Data Safety reaffirmed:** authoritative Quran text is `quran_words` (Uthmani/QPC) and is
never touched; raw `form_buckwalter` is always retained; empty forms → `NULL`; fragile rows are
flagged, not invented; no syntactic claims; source files stay read-only (`MORPH-SOURCE-UNCHANGED`).
