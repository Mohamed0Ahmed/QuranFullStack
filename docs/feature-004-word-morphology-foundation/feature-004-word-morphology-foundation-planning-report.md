# Feature 004 — Word Morphology Foundation (Planning Report)

**Date:** 2026-06-10
**Status:** Planning report only. **No code, no migrations, no source edits, no build/test were run.**
**Scope of this document:** Decide the source, scope boundary, database design, import/rebuild
flow, validation, tests, out-of-scope, and a phase plan for the next backend feature —
**Quran word morphology** (roots, lemmas, stems, POS/type of word, verb classification,
morphological case features) — and recommend whether Arabic *i3rab* belongs in the same
feature or a later one.

**Inputs read for this report (read-only):**
- Source workspace: `~/Desktop/projects/Dashboard/resources/morphology` (full inventory below).
- Prior analyses in that workspace: `report/corpus-pos-classification-capability-report.md`,
  `report/morphology-final-summary-report.md`, `report/qul-vs-corpus-morphology-coverage-report.md`,
  and the alignment/normalization reports.
- Backend conventions: `Backend/.architecture/{BACKEND_STRUCTURE,CLEAN_ARCHITECTURE,API_GUIDELINES}.md`,
  the Feature 002/003 specs (`specs/002-…`, `specs/003-…`), the existing `quran_words` entity/config,
  `tools/QuranDashboard.DataImporter/Program.cs`, the display-words rebuilder, and the
  Feature 003 identity-links implementation plan (phase/validation style mirrored here).

> **Convention this report follows.** Feature 004 is treated as a sibling of Features 002/003:
> derived tables are PostgreSQL, columns `snake_case`, EF entities `PascalCase`, `smallint` where
> values ≤ 32,767 else `int`, all Arabic text `text` with default collation, operator/CI-run CLI
> verbs only (never HTTP), one transaction with hard checks gating the commit, and a
> Markdown+JSON report on every run. **Display stays Uthmani/QPC; identity/grouping uses clean keys
> and ids — never raw verse text.**

> **⚑ Confirmed-decisions update (2026-06-10).** Two decisions have been adopted and are now part of
> this plan: **(D1)** normalized Arabic **segment rendering** (Option B from
> `segment-arabic-rendering-capability-report.md`) is added to `quran_word_morphology_segments` as a
> flagged, derived reading aid; **(D2)** the **word-type filtering foundation** (`quran_pos_tags`
> controlled vocabulary + `head_pos` + derived verb/case fields + small enums/value objects) is
> in-scope. These are reflected inline below (§3.3, §3.7, §3.9, §6) and recorded authoritatively in
> **`feature-004-decisions-addendum.md`** (the addendum governs where any wording differs). The
> morphology table count is now **six** (the five data tables **plus** `quran_pos_tags`, no longer
> optional).

---

## 0. Established baseline (measured, not assumed)

| Fact | Value | Source |
|---|---|---|
| Total `quran_words` | **83,668** | Feature 002/003 |
| Readable words (`is_ayah_marker = false`) | **77,432** | Feature 002/003 |
| Ayah-marker rows | **6,236** | Feature 002/003 |
| Aligned-Corpus word records | **77,432** (object keyed by `qpcLocation`) | aligned JSON metadata |
| Aligned-Corpus segments | **128,219** | POS-capability report §2 |
| Original Corpus unique word locations | **77,429** (3 merged بَعْدَ+مَا split to reach 77,432) | summary report |

These numbers are the contract Feature 004 must reconcile against: **every readable word gets
morphology; no marker does; the join is exact at 77,432.**

---

## 1. Source readiness

### 1.1 Inventory of `~/Desktop/projects/Dashboard/resources/morphology`

| Path | What it is | Records | Carries | Verdict for Feature 004 |
|---|---|---:|---|---|
| `corpus/quranic-corpus-morphology-0.4.txt` | **Original** Quranic Arabic Corpus v0.4 raw (`LOCATION FORM TAG FEATURES`, Buckwalter) | 128,219 segment rows → 77,429 word locations | TAG + FEATURES | **Provenance only — never parsed at runtime, never modified.** Present on disk (6.3 MB, verified). |
| `corpus/jsonData/quranic-corpus-morphology-qpc-aligned.json` | **THE full-morphology source**, re-aligned to QPC word boundaries | **77,432** words / **128,219** segments | `qpcLocation`, `originalCorpusLocation`, `alignmentType`, `qpcUthmani`, `qpcImlaei`, `segments[]`, `notes`; per segment: `segmentLocation`, `segmentNumber`, `form`, `posColumn`, `pos`, `features`, `kind`, `root`, `lemma` | **Canonical source for POS / segments / features / verb tense+voice / case / Buckwalter root+lemma.** |
| `corpus/jsonData/corpus-qpc-location-alignment-map.json` | QPC↔Corpus location map (`splitPairs`, `affectedAyahs`) | 77,432 mappings | alignment trace | **Audit/provenance** of the 3-word split; not seeded as data. |
| `word-root/jsonData/word-root.json` | QUL **Arabic** root per word, keyed `"s:a:w"` | **50,298** | `root` (Arabic, spaced e.g. `"س م و"`) | **Primary source for Arabic root display.** |
| `word-lemma/jsonData/word-lemma.json` | QUL **Arabic** lemma per word | **72,507** | `lemma` (Arabic) | **Primary source for Arabic lemma display.** |
| `word-stem/jsonData/word-stem.json` | QUL **Arabic** stem per word | **77,427** (5 missing) | `stem` (Arabic) | Superseded by the corrected file below. |
| `derived/word-stem-corrected-arabic.json` | QUL stem + 5 corpus-derived fixes, full coverage | **77,432** | `stem` (Arabic) | **Primary source for Arabic stem display (use this, not the raw QUL stem).** |
| `derived/word-stem-corrected.json` | same, Buckwalter-ish | 77,432 | stem | Not for Arabic display. |
| `derived/word-stem-corrections-from-corpus*.json` | the 5 stem fixes (audit) | 5 | stem + corpus evidence | **Audit only — stem-only, NOT a POS source.** |
| `*/original/*.db` | source SQLite for the QUL exports | — | — | Provenance; not parsed at runtime. |
| `report/*` | prior investigations | — | — | Read for this plan; not seeded. |
| `samples/`, `client-showcase/` | first-page demo extracts | — | — | Illustrative; not authoritative. |

### 1.2 Exact source file per field

| Field | Canonical source | Encoding | Notes |
|---|---|---|---|
| **POS / type of word** | `quranic-corpus-morphology-qpc-aligned.json` (`segments[].pos`) | tag codes (`N`,`V`,`PN`,`ADJ`,`PRON`,`P`,`CONJ`,…) | Segment-level; pick the `kind == "STEM"` segment as the word's **head POS**. |
| **Segments** | aligned JSON (`segments[]`) | mixed | 128,219 rows; prefixes/stem/suffixes each carry their own POS. |
| **Features** (tense, voice, case, gender, number, person, definiteness, verbal-noun, participle) | aligned JSON (`segments[].features`, raw string) | Corpus FEATURES string, e.g. `STEM\|POS:V\|IMPF\|LEM:faEala\|ROOT:fEl\|2MP\|MOOD:JUS` | Parse tokens; keep raw verbatim. |
| **Verb classification** (past/present/imperative, active/passive) | aligned JSON features (`PERF`/`IMPF`/`IMPV`, `PASS`) | feature tokens | Derived deterministically; see §2.4. |
| **Case** (nominative/accusative/genitive) | aligned JSON features (`NOM`/`ACC`/`GEN` *case* tokens) | feature tokens | "Where marked" only; indeclinables carry none — expected, not a gap. |
| **Root — Arabic display** | `word-root/jsonData/word-root.json` (QUL) | Arabic | **50,298** words have a root; the rest legitimately have none. |
| **Root — Buckwalter** | aligned JSON (`segments[].root`) | Buckwalter (`smw`) | Cross-reference / fallback. 49,967 corpus vs 50,298 QUL (331 QUL-only). |
| **Lemma — Arabic display** | `word-lemma/jsonData/word-lemma.json` (QUL) | Arabic | **72,507** words. |
| **Lemma — Buckwalter** | aligned JSON (`segments[].lemma`) | Buckwalter (`{som`) | Cross-reference / fallback (74,125 corpus; 1,704 corpus-only need a later Buckwalter→Arabic decision). |
| **Stem — Arabic display** | `derived/word-stem-corrected-arabic.json` | Arabic | **77,432** (full coverage after the 5 fixes). |
| **Stem — surface form** | aligned JSON (`form` of the `kind == "STEM"` segment) | Buckwalter | There is **no dedicated `stem` field** in the corpus — the stem surface is the STEM segment's `form`. |
| **Verb form (I–X)** | aligned JSON features (roman numeral in parens, e.g. `(IV)`) | embedded | **Not a separate field**; parseable later. **Deferred** (YAGNI). |
| **Arabic i3rab (syntactic role / إعراب prose)** | **NOT PRESENT in any file** | — | The Corpus *syntactic treebank* is **not** in this workspace. See §2.5. |

### 1.3 Direct answers to the readiness questions

- **Is `quranic-corpus-morphology-qpc-aligned.json` sufficient as the canonical source?**
  **Yes — for POS, segments, features, verb tense+voice, case, and Buckwalter root/lemma.** It covers
  all **77,432** readable words, is segment-level, is aligned by `qpcLocation` (= `quran_words.location`),
  and resolved the old 3-word Corpus/QPC gap. It is the **only** full-morphology source here and is
  canonical for classification.
  **With one qualification:** because this product is **Arabic-first** and the corpus encodes
  `root`/`lemma` in **Buckwalter**, the **Arabic display** strings for root/lemma/stem must come from
  the **QUL files** (`word-root.json`, `word-lemma.json`, `word-stem-corrected-arabic.json`). So the
  canonical model is **"Corpus for classification + structure; QUL for Arabic display"** — pair them,
  joined by location. This matches the prior `morphology-final-summary-report.md` recommendation.

- **Are existing derived files stem-only / partial, and unsuitable as the canonical POS source?**
  **Yes.** `derived/word-stem-corrected*.json` and `…corrections-from-corpus*.json` are **stem-only**.
  Only the 5 correction rows carry any POS evidence. They repair the QUL stem export; they are **not**
  the POS source. POS must come from the aligned Corpus JSON.

- **Is the original Corpus source unmodified?**
  **Yes.** `quranic-corpus-morphology-0.4.txt` is present and unchanged; all derivations live in
  `corpus/jsonData/` and `derived/`. The earlier "deleted from worktree" housekeeping flag (in the
  POS-capability report, dated 2026-06-04) **no longer applies** — the file is on disk (6.3 MB,
  verified for this report). Feature 004 must keep it **read-only** (enforced by `MORPH-SOURCE-UNCHANGED`,
  §6).

### 1.4 Local source files rule (in-repo local-only vs research workspace) — **authoritative**

`~/Desktop/projects/Dashboard/resources/morphology` is **only the upstream research/source
workspace** and **read-only provenance**. The Feature 004 importer and **all runtime/import
execution must NOT depend on that external Desktop path.**

The runtime importer reads from a **local in-repo** import-source path (mirroring how
`App/resources/import-sources/quran-foundation/` works, each with a `manifest.json`). Feature 004
must therefore **copy** the selected morphology files into a new sibling directory:

```text
App/resources/import-sources/quran-morphology/      ← local in-repo source (Git-ignored, local-only)
App/resources/import-sources/quran-foundation/      ← existing (Feature 002)
```

- **This is a local, repo-relative workspace path, not a committed/pushed source tree.** `resources/`
  is already in `.gitignore`, so these data files stay **local-only by default** and are **not** added,
  committed, or pushed to Git. "Staging/copy" here means **a local file copy into the in-repo path** —
  **not** `git stage`, `git add`, or push. (No nested `.gitignore` or `manifest.example.json` is needed;
  the parent `resources/` ignore rule already covers the folder. The team may add an optional
  `README.md`/template only if it wants local documentation.)
- The path lives **inside the project structure, beside `quran-foundation/`**, so the importer has a
  stable repo-relative location and **no dependency** on the external Desktop research path.
- **Copy only — never move and never edit** the external research files; originals stay intact.
- The local folder contains **only the files actually used by Feature 004** (the exact set in §5.1)
  — not the whole research workspace (no `derived/`, `samples/`, `report/`, `.db`, or the raw
  `…-0.4.txt`).
- The local files (validated by their `manifest.json`) are the **only** source the importer reads.

Full local layout, manifest contract, and per-file responsibility are in **§5.1**.

---

## 2. Scope boundary

### 2.1 The safest dividing line

> **Feature 004 stores facts that are *present in* or a *deterministic 1:1 mapping of* the source
> morphology tags. Feature 005 generates Arabic syntactic إعراب, which requires grammar rules and a
> syntactic layer the source does not contain.**

Everything Feature 004 ships can be traced to a source token. Nothing is invented. The moment a value
requires *inferring a grammatical role in a sentence* (فاعل / مفعول به / مبتدأ / خبر / حال…) or
*composing an Arabic إعراب sentence* ("…مرفوع وعلامة رفعه الضمة"), it crosses into Feature 005.

### 2.2 In scope for Feature 004 (confirmed source-backed)

| Item | Include in 004? | Why |
|---|---|---|
| **Root** (Arabic display + Buckwalter) | ✅ Yes | Directly in QUL + corpus. Null allowed (50,298 of 77,432). |
| **Lemma** (Arabic display + Buckwalter) | ✅ Yes | Directly in QUL + corpus. Null allowed. |
| **Stem** (Arabic display) | ✅ Yes | QUL corrected-Arabic, full coverage. |
| **POS / type of word** (head + per-segment) | ✅ Yes | Corpus `pos` per segment; head = STEM segment. |
| **Segments** (prefix/stem/suffix breakdown) | ✅ Yes | 128,219 rows; 54% of words are multi-segment — must be stored to be faithful. |
| **Features** (raw + parsed tokens) | ✅ Yes | Corpus `features`; raw kept verbatim, parsed for queryable columns. |
| **Verb classification** (past/present/imperative; active/passive) | ✅ **Yes — do not split** | Deterministic from `PERF`/`IMPF`/`IMPV` and `PASS`. No Arabic rules needed. See §2.4. |
| **Case features** (مرفوع/منصوب/مجرور) | ✅ Yes | Corpus case tokens `NOM`/`ACC`/`GEN`. Word-level morphology, *not* sentence i3rab. "Where marked." |
| **Controlled-vocabulary Arabic labels** for POS/tense/case (e.g. `N`→اسم, `V+IMPF`→فعل مضارع, `GEN`→مجرور) | ✅ Yes (small static reference table) | A **fixed ~30-entry dictionary**, 1:1 with source tags. Not generated prose — safe. Gives the UI Arabic labels without claiming syntax. |
| **Indexes for roots / lemmas / stems** (dimension tables) | ✅ Yes | Enables root/lemma/stem browse pages. |
| **Verb / POS "indexes"** | ✅ Yes, **as filters/queries over `quran_word_morphology`**, not as new physical tables | KISS/YAGNI — `is_verb`, `verb_tense`, `head_pos` columns + indexes already answer these. |

### 2.3 Split into Feature 005 (or later)

| Item | In 004? | Where it belongs |
|---|---|---|
| **Generated Arabic إعراب prose** ("فاعل مرفوع وعلامة رفعه الضمة الظاهرة…") | ❌ No | **Feature 005** — needs Arabic rules; risks over-claiming. |
| **Syntactic role** (فاعل/مفعول به/مبتدأ/خبر/حال/تمييز…) | ❌ No | **Feature 005** — requires the syntactic treebank, **absent** here. Case markers help but ≠ grammatical function. |
| **Verb form (I–X) extraction** | ⚠️ Deferred | Optional later phase of 004 or 005; keep the raw feature now. |
| **مصدر-of-a-verb links** | ❌ No | Not in the data (only explicitly-tagged `VN`, 674). **Do not synthesize from lemma.** |

### 2.4 Verbs — explicit decision: **include in Feature 004**

Verb classification is a *feature read*, not a rule engine. The corpus partitions every finite verb
cleanly (POS-capability report §6):

| Class | Rule | Count | Confidence |
|---|---|---:|---|
| فعل ماضٍ | `V` + `PERF` | 9,150 | High |
| فعل مضارع | `V` + `IMPF` | 8,330 | High |
| فعل أمر | `V` + `IMPV` | 1,876 | High |
| مبني للمجهول (passive) | `V` + `PASS` | 1,140 | High (explicit) |
| مبني للمعلوم (active) | `V` **without** `PASS` | 18,216 | Medium-High (inferred-by-default) |

`9,150 + 8,330 + 1,876 = 19,356` — every finite verb carries exactly one tense. **Verdict:** verbs
belong in Feature 004. The only caveat — record active voice as **inferred-by-absence-of-PASS**, not
stamped — is a data-labeling note, not a reason to split.

> Pitfall to encode in the importer: the 78 segments whose *tag* is `IMPV` for the **imperative-lām
> prefix** (`l:IMPV+`, لام الأمر) are **particles, not verbs** — classify tense only on STEM segments
> with `pos == V`.

### 2.5 Arabic i3rab — explicit decision: **split into Feature 005**

- The Corpus **syntactic treebank (dependency grammar / إعراب) is NOT present** in this workspace
  (POS-capability report §2 note, §9). What exists is **morphology only**.
- Generating Arabic i3rab requires (a) syntactic-role inference and (b) Arabic phrasing rules — both
  outside the data. Merging it into 004 would force either inventing values (forbidden) or shipping a
  half-built rule engine inside a data-foundation feature.
- **Therefore:** Feature 005 = Arabic i3rab generation, built **on top of** Feature 004's morphology +
  case data, clearly labeled **generated** and **word-level (not full sentence syntax)**.

**Safest boundary, in one line:** *Feature 004 = source-backed morphology data + a fixed Arabic-label
lookup; Feature 005 = rule-generated Arabic إعراب. Data and controlled vocabulary in 004; generated
syntactic prose in 005.*

### 2.6 Verdict on the preferred initial direction

The preferred direction in the task (004 = morphology foundation incl. verb classification + indexes;
005 = Arabic i3rab later) is **confirmed**, with two refinements:
1. **Pair the canonical source**: Corpus aligned JSON for classification/structure **+** QUL Arabic
   files for root/lemma/stem **display** (the corpus root/lemma are Buckwalter).
2. **Allow controlled-vocabulary Arabic POS/tense/case labels inside 004** (a fixed lookup table) —
   this is safe and is *not* the i3rab that is deferred. Only **generated syntactic إعراب** is deferred.

---

## 3. Database design

**Placement (per `BACKEND_STRUCTURE.md`):** new entities under
`Domain/Quran/Words/Morphology/`; EF configs under
`Infrastructure/Persistence/Configurations/Quran/Words/Morphology/`; source readers under
`Infrastructure/Files/Quran/Import/Morphology/`. All tables are **derived, rebuilt from source +
`quran_words`**, like the Feature 003 display tables. `quran_words` is **never** altered by Feature 004
(no new columns on it — see §3.7).

### 3.1 Grain decision (the key design choice)

**Morphology is per-occurrence (per `quran_word_id`), not per identity.** The same surface word can
carry different case (and occasionally different POS) in different ayahs, so morphology must hang off
`quran_words.id`, **not** off `unique_tashkeel_word_id` / `unique_simple_word_id`. The identity links
are still useful — for **display roll-ups** (group a root's words by `unique_simple_word_id` to show
distinct forms with occurrence counts) — but they are **not** the storage grain. See §4.

### 3.2 `quran_word_morphology` — one row per readable word (77,432)

**Purpose:** the word-level head morphology + Arabic-display root/lemma/stem + derived verb/case fields.
The primary read surface for word-detail and filters.

| Column | Type | Null | Notes |
|---|---|---|---|
| `quran_word_id` | `int` | NO | **PK + FK** → `quran_words.id`; **UNIQUE**; 1:1 with readable words |
| `location` | `text` | NO | `"s:a:w"` (= `quran_words.location` = `qpcLocation`) — provenance/join audit |
| `head_pos` | `text` | NO | POS of the `kind == "STEM"` segment (e.g. `N`,`V`,`PN`,`ADJ`,`PRON`,`P`) |
| `segment_count` | `smallint` | NO | ≥ 1 |
| `root_id` | `int` | YES | **FK** → `quran_roots.id`; null where the word has no root (≈ 27k) |
| `lemma_id` | `int` | YES | **FK** → `quran_lemmas.id`; null allowed |
| `stem_id` | `int` | YES | **FK** → `quran_stems.id`; null allowed |
| `is_verb` | `bool` | NO | `head_pos == 'V'` (derived) |
| `verb_tense` | `text` | YES | `past`/`present`/`imperative` from `PERF`/`IMPF`/`IMPV`; null for non-verbs |
| `verb_voice` | `text` | YES | `active`/`passive`; `passive` iff `PASS`, else `active` (inferred); null for non-verbs |
| `case_feature` | `text` | YES | `nominative`/`accusative`/`genitive` from `NOM`/`ACC`/`GEN`; null where unmarked |
| `head_features_json` | `jsonb` | YES | parsed feature tokens of the head segment (queryable enrichment) |

**Indexes:** PK/UNIQUE(`quran_word_id`); `head_pos`; partial `(verb_tense)` / `(verb_voice)` `WHERE
is_verb`; `case_feature`; `root_id`; `lemma_id`; `stem_id`.
**Relational vs JSON:** head POS, verb tense/voice, case, and the dimension FKs are **relational**
(they drive filters/joins). The full token bag stays **`jsonb`** (`head_features_json`) for flexible
enrichment without schema churn.

### 3.3 `quran_word_morphology_segments` — one row per segment (≈ 128,219)

**Purpose:** full segment fidelity — the 54% of words that bundle prefix + stem + suffix, each with its
own POS. Drives the word-detail breakdown and any segment-level analysis.

| Column | Type | Null | Notes |
|---|---|---|---|
| `id` | `int` | NO | **PK**, generated identity |
| `quran_word_id` | `int` | NO | **FK** → `quran_words.id` |
| `segment_location` | `text` | NO | `"s:a:w:seg"` |
| `segment_number` | `smallint` | NO | 1-based within the word |
| `kind` | `text` | NO | `PREFIX` / `STEM` / `SUFFIX` |
| `pos` | `text` | NO | segment POS code |
| `form_buckwalter` | `text` | NO | corpus `form` (segment surface, Buckwalter) — **always retained, lossless** |
| `form_arabic_normalized` | `text` | **YES** | **(D1)** Arabic rendering from Buckwalter transliteration; **`NULL`** for the 208 empty forms; **never** an Uthmani substring, **never** Mushaf display |
| `arabic_render_tier` | `text` | YES | **(D1)** `clean` / `quranic_marks` / `review` / `multiword` (display-fidelity tier) |
| `arabic_render_source` | `text` | NO | **(D1)** constant `buckwalter-transliteration` (provenance flag) |
| `root_buckwalter` | `text` | YES | corpus `root` (Buckwalter) |
| `lemma_buckwalter` | `text` | YES | corpus `lemma` (Buckwalter) |
| `features_raw` | `text` | NO | **verbatim** corpus FEATURES string (provenance, lossless) |
| `features_json` | `jsonb` | YES | parsed tokens (query convenience) |

**Indexes:** PK(`id`); `(quran_word_id, segment_number)` UNIQUE; `pos`; partial `(quran_word_id) WHERE
kind = 'STEM'`; `arabic_render_tier` (to route `review`/`multiword` rows to curators).
**Relational vs JSON:** `kind`, `pos`, `segment_number`, `arabic_render_tier` are **relational**
(filter/join). `form_arabic_normalized` is **derived** Arabic text (see §3.3a). `features_raw` is
**text** kept verbatim (never lossy, supports re-parsing). `features_json` is **`jsonb`** for queries.

#### 3.3a Segment Arabic rendering (D1 — Option B adopted)

Per `segment-arabic-rendering-capability-report.md`: every `form` is QAC extended Buckwalter (100% of
characters mapped, 0 unknown), so transliteration is deterministic. But it is a **morphological/phonemic
reading** — concatenated segments equal `qpcUthmani` only ~79.8% of the time and diverge systematically
(waqf marks, iqlab small-meem, kashida carriers, decomposed hamza). Therefore `form_arabic_normalized`
is a **derived reading aid only**, governed by these **hard rules**:

- **Never** name it `qpc_segment_text`; **never** claim it is an exact substring of `qpcUthmani`;
  **never** use it as Mushaf display text. Authoritative Quran display stays
  `quran_words.text_uthmani` / `qpc_glyph`.
- **Empty forms → `NULL`** (no guessed text). **Fragile/`review` rows are flagged, not invented.**
- `form_buckwalter` is always stored verbatim, so the rendering is fully reproducible/reversible.
- Best-effort **for all** non-empty segments (not prefixes-only, not mandatory-uniform), each stamped
  with its `arabic_render_tier`. Fidelity baselines: ~94.2% `clean`, ~5.4% `quranic_marks`, ~0.4%
  `review`, 1 `multiword`.

### 3.4 `quran_roots` — dimension (distinct Arabic roots)

**Purpose:** the Roots browse page; one row per distinct root.

| Column | Type | Null | Notes |
|---|---|---|---|
| `id` | `int` | NO | **PK**, generated identity |
| `root_text` | `text` | NO | **Arabic display** (QUL form, e.g. `"س م و"`); **UNIQUE** |
| `root_buckwalter` | `text` | YES | corpus form (e.g. `smw`) for cross-ref; null if QUL-only |
| `words_count` | `int` | NO | readable-word occurrences under this root |
| `distinct_lemmas_count` | `smallint` | NO | distinct lemmas under this root |
| `first_word_order_in_mushaf` | `int` | NO | stable display sort key; **UNIQUE** |

**Indexes:** PK(`id`); UNIQUE(`root_text`); UNIQUE(`first_word_order_in_mushaf`); `words_count`.

### 3.5 `quran_lemmas` — dimension (distinct Arabic lemmas)

| Column | Type | Null | Notes |
|---|---|---|---|
| `id` | `int` | NO | **PK**, generated identity |
| `lemma_text` | `text` | NO | **Arabic display**; **UNIQUE** |
| `lemma_buckwalter` | `text` | YES | corpus form; cross-ref |
| `root_id` | `int` | YES | **FK** → `quran_roots.id` (dominant/first root; null when no root) |
| `words_count` | `int` | NO | occurrences |
| `first_word_order_in_mushaf` | `int` | NO | stable sort; **UNIQUE** |

**Indexes:** PK(`id`); UNIQUE(`lemma_text`); `root_id`; UNIQUE(`first_word_order_in_mushaf`).

### 3.6 `quran_stems` — dimension (distinct Arabic stems)

| Column | Type | Null | Notes |
|---|---|---|---|
| `id` | `int` | NO | **PK**, generated identity |
| `stem_text` | `text` | NO | **Arabic display** (QUL corrected); **UNIQUE** |
| `words_count` | `int` | NO | occurrences |
| `first_word_order_in_mushaf` | `int` | NO | stable sort; **UNIQUE** |

**Indexes:** PK(`id`); UNIQUE(`stem_text`); UNIQUE(`first_word_order_in_mushaf`).
**Note:** stem is the lowest-priority dimension (QUL surface form, less linguistically canonical than
root/lemma). Ship it for completeness; do not over-invest.

### 3.7 `quran_pos_tags` — POS controlled-vocabulary reference (D2 — required, not optional)

**Purpose:** the **word-type filtering foundation**. Every POS code carries Arabic + English labels, a
broad filterable category, and a sort order, so future filters/UI label اسم/فعل/حرف and group by type
**without** a hard-coded enum and **without** generating prose. Seeded from a **fixed dictionary in
code** (controlled vocabulary), not from the Quran source.

| Column | Type | Null | Notes |
|---|---|---|---|
| `code` | `text` | NO | **PK** (`N`,`V`,`PN`,`ADJ`,`PRON`,`P`,`CONJ`,`NEG`,`ACC`,`REL`,`DEM`,`VOC`,`INL`,…) |
| `arabic_label` | `text` | NO | اسم / فعل / اسم علم / صفة / ضمير / حرف جر / حرف عطف / … |
| `english_label` | `text` | NO | noun / verb / proper noun / adjective / pronoun / preposition / … |
| `category` | `text` | NO | broad grouping for future filters: `noun` / `verb` / `particle` / `other` |
| `sort_order` | `smallint` | NO | stable display ordering for tag lists |
| `description` | `text` | YES | optional short note (controlled vocab; no invented Quranic content) |

**Indexes:** PK(`code`); `category`; `sort_order`. ≈ 30 rows, seeded idempotently.

**`category` mapping (classical-grammar buckets — decided during US4/Phase 6):** the four buckets are
`noun` / `verb` / `particle` / `other`. Relative pronouns (`REL` — اسم موصول), demonstratives (`DEM` —
اسم إشارة), independent pronouns (`PRON`/`PRO`), and the substantive/ambiguous-noun tag (`SUB` — اسم مبهم)
are bucketed as **`noun`**, following classical Arabic grammar (الأسماء المبنية) and matching their Arabic
labels — *not* `particle`. This supersedes the earlier §6.3 frequency grouping (which listed `REL`/`DEM`
under a functional "particles" row for corpus counts only) and the earlier T052 example; the count rows in
§6.3 remain corpus-frequency groupings and do not redefine the `quran_pos_tags.category` value. `DET`
(أداة تعريف, the definite-article prefix) stays `particle`. The seed is the single source of truth for
these category assignments; the `MORPH-POS-RESOLVES` gate fails closed on any code not in the seed.

**Why a table, not an enum:** the POS vocabulary is data (≈ 30 controlled rows, label/category/order),
so it lives in `quran_pos_tags` — queryable, joinable, and extensible without code churn. Only the
small, stable closed concepts stay as enums/value objects (§3.9).

**Future filter capabilities this enables** (data foundation only — **no UI/API in 004**, §8). Filters
run over the ordered Quran words joined to `quran_word_morphology` (per occurrence):

- all **nouns** / all **verbs** / all **particles-tools** (`quran_pos_tags.category`),
- a **specific POS tag** — e.g. `N`, `PN`, `P`, `CONJ`, `NEG` (`head_pos`),
- **verb tense** — past / present / imperative (`verb_tense`, `WHERE is_verb`),
- **verb voice** — active / passive (`verb_voice`),
- **case feature** — nominative / accusative / genitive (`case_feature`).

### 3.8 What is *not* a new physical table (KISS/YAGNI)

- **"Verbs table" / "POS table"** → these are **filters** over `quran_word_morphology`
  (`WHERE is_verb`, `WHERE head_pos = …`), backed by the indexes in §3.2. No materialized verb table.
- **`quran_words` is not modified.** Feature 004 adds *no* columns to `quran_words` (unlike Feature 003,
  which added the identity links). Morphology lives in its own tables, joined by `quran_word_id`.
- **No enforced FK from `quran_word_morphology` → `quran_words`?** Keep an FK **to `quran_words`**
  (stable within a foundation import). Within the morphology tables, dimension FKs (`root_id` etc.) are
  enforced because dimensions and facts are built in the **same** transaction. The Decision 8 caveat
  (`TRUNCATE … RESTART IDENTITY` reseed invalidates ids across full reseeds) applies here too — carried
  as a **future production note**, not a blocker (§9 risks).

### 3.9 Domain types — enums / value objects (D2)

POS stays a controlled-vocabulary **table** (§3.7). Only the small, **stable, closed** concepts are
modelled as enums / value objects in `Domain/Quran/Words/Morphology/` (mapped to the `text` columns in
§3.2/§3.3):

| Type | Members | Backs column |
|---|---|---|
| `SegmentKind` | `Prefix` / `Stem` / `Suffix` | `quran_word_morphology_segments.kind` |
| `VerbTense` | `Past` / `Present` / `Imperative` | `quran_word_morphology.verb_tense` |
| `VerbVoice` | `Active` / `Passive` | `quran_word_morphology.verb_voice` |
| `MorphologicalCase` | `Nominative` / `Accusative` / `Genitive` | `quran_word_morphology.case_feature` |

`head_pos` and the segment `pos` are **not** enums — they reference `quran_pos_tags.code` (open
controlled vocabulary). `arabic_render_tier` is a small fixed set (`clean`/`quranic_marks`/`review`/
`multiword`) and may also be modelled as a value object/enum.

---

## 4. Display / use-case support (how the future UI reads this)

> UI implementation is **out of scope** (§8). This section shows the data model **supports** each
> screen; it is not a build instruction.

| Screen | Query shape |
|---|---|
| **Roots table** (each root → its words) | `SELECT * FROM quran_roots ORDER BY words_count DESC`. Drill-in: `quran_word_morphology m JOIN quran_words w ON w.id = m.quran_word_id WHERE m.root_id = :id`. To list **distinct forms** under the root with counts, group the drill-in by `w.unique_simple_word_id` (Feature 003 identity) — this is where the identity links pay off. |
| **Lemmas table** | `quran_lemmas` browse; drill-in via `quran_word_morphology.lemma_id`. |
| **Stems table** | `quran_stems` browse; drill-in via `quran_word_morphology.stem_id`. |
| **POS / type filters** (اسم/علم/صفة/ضمير/حرف جر/عطف/نفي/موصول/إشارة…) | filter `quran_word_morphology.head_pos`; label via `quran_pos_tags`. |
| **Verbs table / filter** (ماضٍ/مضارع/أمر, معلوم/مجهول) | `WHERE is_verb` faceted by `verb_tense`, `verb_voice` (indexed). |
| **Word-details panel** | `quran_word_morphology` (head root/lemma/stem + POS + verb/case) **+** `quran_word_morphology_segments` (prefix/stem/suffix breakdown with per-segment POS + features). |
| **Future i3rab panel (Feature 005)** | reads morphology + `case_feature`, applies Arabic rules to render an explanation — clearly marked **generated**, **word-level**, not full sentence syntax. |

**Identity-vs-display note for curators (carried from Feature 003):** display strings stay
Uthmani/QPC; root/lemma/stem display strings come from QUL Arabic; one imlaei identity may span several
Uthmani vocalizations. Morphology drill-ins should color/group by **ids**, never raw verse text.

---

## 5. Import / rebuild flow

### 5.1 Local source files + manifest

Copy into the **local in-repo** path `App/resources/import-sources/quran-morphology/` (**copy only**
from the Desktop research workspace; originals never moved or edited), containing **exactly** these
files and **no others** — mirroring the `quran-foundation` readiness pattern. This path is **Git-ignored
(local-only): the data files are not added, committed, or pushed** (the parent `resources/` ignore rule
already covers it; no nested `.gitignore` needed). "Copy" here means a **local file copy**, not
`git stage`/`git add`/push.

```text
quran-morphology/
  manifest.json
  README.md                                       (provenance + "external research sources are read-only" note)
  corpus/quranic-corpus-morphology-qpc-aligned.json
  corpus/corpus-qpc-location-alignment-map.json
  qul/word-root.json
  qul/word-lemma.json
  qul/word-stem-corrected-arabic.json
```

**Per-file responsibility (source ownership split):**

| Local file | Used for |
|---|---|
| `corpus/quranic-corpus-morphology-qpc-aligned.json` | **classification + structure:** POS / word type, segments, features, verb tense/voice, case features, Buckwalter root/lemma (cross-reference), segment `form_buckwalter`, and the **derived `form_arabic_normalized`** (transliterated from the corpus `form`) |
| `corpus/corpus-qpc-location-alignment-map.json` | alignment **audit/provenance** (3-word split trace); not seeded as data |
| `qul/word-root.json` | **Arabic root display** value |
| `qul/word-lemma.json` | **Arabic lemma display** value |
| `qul/word-stem-corrected-arabic.json` | **Arabic stem display** value |

> **Confirmed split:** **Corpus aligned JSON = classification/structure; QUL files = Arabic display
> values.** The corpus `root`/`lemma` are Buckwalter (cross-reference only); all Arabic root/lemma/stem
> *display* strings come from the QUL files.

**`README.md` (local):** states that these local files are the runtime source, that the external
`~/Desktop/.../resources/morphology` workspace is **read-only provenance only**, that the folder is
Git-ignored/local-only (not committed or pushed), and that the local files must not be hand-edited
(rebuild by re-copying + re-importing).

**`manifest.json` contract** — one entry per local file (drives the readiness check and
`MORPH-SOURCE-UNCHANGED`):

| Field | Meaning |
|---|---|
| `role` | `corpus-aligned` / `alignment-map` / `qul-root` / `qul-lemma` / `qul-stem` |
| `originPath` | the upstream research path the file was copied from (provenance, e.g. `~/Desktop/.../corpus/jsonData/…`) |
| `expectedRecordCount` | where applicable (aligned 77,432; map 77,432; root 50,298; lemma 72,507; stem 77,432) |
| `fileSizeBytes` | local file size |
| `sha256` | local file checksum |
| `notes` | provenance/version note (e.g. source version, derivation report reference) |

```json
{
  "feature": "004-word-morphology-foundation",
  "copiedAtUtc": "…",
  "files": [
    { "path": "corpus/quranic-corpus-morphology-qpc-aligned.json", "role": "corpus-aligned",
      "originPath": "~/Desktop/projects/Dashboard/resources/morphology/corpus/jsonData/quranic-corpus-morphology-qpc-aligned.json",
      "expectedRecordCount": 77432, "fileSizeBytes": 0, "sha256": "…",
      "notes": "QPC-aligned derived from quranic-corpus-morphology-0.4.txt; classification/structure source" }
  ]
}
```

### 5.2 CLI verb — add `import-morphology` (do **not** overload the others)

The `tools/QuranDashboard.DataImporter` host already dispatches verbs
(`import-foundation`, `rebuild-words`). Add a **third verb**:

```text
QuranDashboard.DataImporter import-morphology --source <path> [--report-out <path>] [--force]
```

- **Why a new verb, not an extension:** `import-foundation` builds the `quran_*` core from files;
  `rebuild-words` derives display/identity **purely from the DB**. Morphology is **source-driven**
  (needs the external files) **and** DB-driven (joins `quran_words`) — distinct enough to warrant its
  own verb, keeping each verb single-purpose (matches the existing parser style and the FR "operator/CI
  only, never HTTP").
- **`--force`:** truncate the six morphology tables and rebuild; without it, refuse if any target is
  non-empty (exact mirror of `rebuild-words`).
- **Source path:** `--source <path>` is accepted (overridable for tests/CI), but the
  **documented/default source for repo/dev usage is `App/resources/import-sources/quran-morphology/`**
  — the local in-repo tree of §5.1 (Git-ignored, local-only). The importer **never** reads the external
  `~/Desktop/.../resources/morphology` workspace; runtime has **no dependency** on that path.
- **Default `--report-out`:** `resources/report/words-morphology/` (mirrors the importer/rebuilder
  default report convention).
- **Dependency:** `import-foundation` must have run first (needs `quran_words`). `import-morphology` is
  **independent of `rebuild-words`** (it only needs `quran_words.{id, location, is_ayah_marker}`).

### 5.3 Transactional load (single Npgsql transaction, `CommandTimeout = 600s`)

1. **Read manifest + readiness check** (files present, counts/checksums match) → refuse early on
   mismatch.
2. **Load source** into memory: aligned corpus (segments/pos/features/Buckwalter root+lemma) keyed by
   `qpcLocation`; QUL Arabic root/lemma/stem keyed by `"s:a:w"`.
3. **Join to `quran_words` by location** (`qpcLocation == quran_words.location`) over
   `WHERE is_ayah_marker = false`. **Ayah markers are excluded** — they are not in the corpus by
   location, and the predicate guarantees none receive morphology.
4. `--force` ⇒ `TRUNCATE` the six morphology tables (`RESTART IDENTITY`).
5. **Build dimensions** (`quran_roots`, `quran_lemmas`, `quran_stems`) from distinct Arabic values
   (deterministic `first_word_order_in_mushaf` provenance).
6. **Insert `quran_word_morphology`** (head POS from the STEM segment; resolve `root_id`/`lemma_id`/
   `stem_id`; derive `is_verb`/`verb_tense`/`verb_voice`/`case_feature`).
7. **Insert `quran_word_morphology_segments`** (all 128,219).
8. **Seed `quran_pos_tags`** from the in-code dictionary (idempotent upsert).
9. **Run hard checks (§6)** → **commit iff all pass, else rollback** (write nothing).
10. **Write the Markdown+JSON report** on both pass and fail (a *refusal* in step 1/`--force` guard
    writes no report, matching `rebuild-words`).

**Safe re-run:** `--force` is idempotent — a second run yields identical counts and links. Without
`--force`, a populated target set causes refusal (no write). `RESTART IDENTITY` reseed is accepted in
dev (Decision 8); the stable-id production note (§9) is carried forward, not blocking.

---

## 6. Validation checks

Following the Feature 003 split: **structural/relative checks gate the transaction (pass on any
dataset incl. synthetic seed); absolute-count + anchor checks live in real-source integration tests +
the report.** Any hard-check failure ⇒ rollback + `verdict = "fail"` + non-zero exit.

### 6.1 Hard checks (gate the commit)

| Id | Assertion (expected 0 violations / exact match) |
|---|---|
| `MORPH-READABLE-COMPLETE` | every readable `quran_words` row has exactly one `quran_word_morphology` row; count = readable count |
| `MORPH-MARKERS-EXCLUDED` | zero morphology / segment rows map to `is_ayah_marker = true` |
| `MORPH-LOCATION-MATCH` | every morphology `location` matches a `quran_words.location`; zero unmatched source locations; join is exact |
| `MORPH-SEGMENTS-PRESENT` | every morphology word has `segment_count ≥ 1`; every word has ≥ 1 segment row |
| `MORPH-POS-PRESENT` | every segment has a non-null `pos`; every word resolves a non-null `head_pos` (exactly one STEM segment) |
| `MORPH-POS-RESOLVES` *(D2)* | every `head_pos` and every segment `pos` resolves to a known `quran_pos_tags.code` (0 unknown codes) |
| `MORPH-VERB-FEATURE-CONSISTENCY` | every `is_verb` word has exactly one of `past/present/imperative` and a non-null `verb_voice`; no verb carries two tenses; non-verbs have null verb fields |
| `MORPH-DIMENSION-RESOLVES` | every non-null `root_id`/`lemma_id`/`stem_id` resolves to a dimension row (no dangling) |
| `MORPH-SEG-CHARSET` *(D1)* | every `form` character is in the QAC transliteration map; **0 unmapped** (a new char refuses the import rather than rendering `�`) |
| `MORPH-SEG-RENDER-TOTAL` *(D1)* | every **non-empty** form yields a non-empty `form_arabic_normalized`; every **empty** form yields `NULL` (expected 208) |
| `MORPH-SEG-TIER-VALID` *(D1)* | every rendered row has a valid `arabic_render_tier`; `arabic_render_source = 'buckwalter-transliteration'` for all rows |
| `MORPH-SEG-NOT-UTHMANI` *(D1, guard)* | `form_arabic_normalized` is never written from `qpc_glyph`/`text_uthmani`; `form_buckwalter` is present on every row |
| `MORPH-SOURCE-UNCHANGED` | the **local in-repo** source files (`quran-morphology/`, Git-ignored) match their `manifest.json` size/`sha256` **before and after** the run (importer reads, never writes them); the external research workspace is read-only provenance with no runtime dependency |

### 6.2 Null-tolerant checks (warnings, never fail)

| Id | Meaning |
|---|---|
| `MORPH-ROOT-NULL-ALLOWED` | null root is **valid** (particles, pronouns, إِيَّا, مَا). Report null-root count; warn only if it deviates from expected coverage (≈ 50,298 have roots). |
| `MORPH-LEMMA-STEM-NULL-ALLOWED` | null lemma/stem allowed; report coverage (≈ 72,507 lemma, 77,432 stem). |
| `MORPH-ROOT-RECONCILE` | QUL vs corpus root diff (331 QUL-only) — informational. |
| `MORPH-LEMMA-RECONCILE` | QUL vs corpus lemma diff (86 QUL-only, 1,704 corpus-only needing a later Buckwalter→Arabic decision) — informational. |
| `MORPH-VOICE-INFERRED` | note that `active` is inferred by absence of `PASS`, not stamped. |
| `MORPH-SEG-WORD-AGREEMENT` *(D1)* | per-word concatenated transliteration vs `qpcUthmani` exact-match rate ≈ **79.83 %** (encoding-drift canary; deviation → investigate). |
| `MORPH-SEG-TIER-DIST` *(D1)* | tier distribution ≈ 94.2 % `clean` / 5.4 % `quranic_marks` / 0.4 % `review` / 1 `multiword`; deviation → investigate. |
| `MORPH-SEG-REVIEW-LIST` *(D1)* | emit the full `review` (T3, 134 forms) + `multiword` (1) + empty (208) lists for manual curator sign-off. |

### 6.3 Absolute-count + anchor checks (real-source integration tests + report only)

These are canonical-dataset truths (would correctly fail synthetic seed), so they live with the
real-import fixture, **not** in the generic gate:

| Assertion | Expected |
|---|---|
| morphology words / segments | 77,432 / 128,219 |
| verb segments; PERF/IMPF/IMPV; PASS | 19,356; 9,150 / 8,330 / 1,876; 1,140 |
| nominals N / PN / ADJ / PRON | 25,136 / 3,911 / 1,961 / 24,685 |
| particles P / CONJ / NEG / REL / DEM / VOC / INL | 13,006 / 9,450 / 2,688 / 3,575 / 1,059 / 376 / 30 |
| case NOM / ACC / GEN | 8,954 / 10,331 / 12,629 |
| roots (QUL) / lemmas (QUL) / stems | 50,298 / 72,507 / 77,432 coverage |

Record the canonical constants centrally (a `MorphologyInvariants` analogue of
`DisplayWordsInvariants`) so tests and report share one source of truth.

---

## 7. Test strategy

Per `test-guard`: test behavior, mock only at boundaries, data-driven `[Theory]` for variants, real
entities/DTOs, real Postgres (Testcontainers) where persistence is the subject, Quran-safe data
(single word-identity forms only; no verse passages; no invented morphology).

- **Synthetic-seed integration (dataset-agnostic, gate):** a tiny synthetic morphology set (safe
  placeholder tokens) → all `MORPH-*` structural checks pass; markers excluded; segments present;
  verb consistency; dimensions resolve; idempotent second `--force`; refusal without `--force`.
- **Real-source gated integration (Testcontainers + import fixture, mirrors `ImlaeiCleanKeyImportTests`):**
  `import-foundation` → `import-morphology` → assert the §6.3 absolute counts + anchors + completeness.
- **Anchor examples (`[Theory]`):**
  - `1:1:1` بِسْمِ → 2 segments `P + N`, head `N`, root `"س م و"` / `smw`, lemma `اسْم`, stem `سْمِ`.
  - `1:1:2` ٱللَّهِ → root `"ا ل ه"`, case `genitive`.
  - `1:5:2` نَعْبُدُ → `V`, `present` (IMPF), `active`, root `"ع ب د"`.
  - `1:6:1` ٱهْدِنَا → `V`, `imperative` (IMPV), root `"ه د ي"` (V+PRON segments).
  - a `PASS` verb anchor → `passive`.
- **Multi-segment words:** بِسْمِ (P+N), تَفْعَلُوا۟ (V+PRON), ٱلرَّحْمَٰنِ (DET+ADJ) — assert
  segment count + per-segment POS.
- **Null-root examples:** `1:5:1` إِيَّاكَ (root null, pronoun); `2:181:4` مَا (root null, pos `SUB`,
  one of the 3 split words) — assert null is stored, not failed.
- **Verb classification:** PERF/IMPF/IMPV/PASS anchors via `[Theory]`; assert the `l:IMPV+` prefix is
  **not** classified as a verb.
- **Segment Arabic rendering (D1):** charset covers 100 % of forms (0 unmapped); empty forms →
  `form_arabic_normalized IS NULL`; tier `[Theory]` anchors — `بِسْمِ` segments → `clean`, `هُۥ`
  (`hu,`) → `quranic_marks`, `أَنۢبِـُٔ` (`>an[bi_#u`) → `review`, `إِلْ يَاسِينَ` → `multiword`;
  concatenated-word agreement ≈ 79.83 %; assert `form_arabic_normalized` never equals `qpc_glyph` and
  `form_buckwalter` is always present.
- **POS resolution (D2):** every `head_pos` and segment `pos` resolves to a `quran_pos_tags` row;
  `category` ∈ {`noun`,`verb`,`particle`,`other`} for every code; the future-filter queries (all nouns/
  verbs/particles, specific tag, tense, voice, case) return non-empty, marker-free sets on real import.
- **No-mutation / source-untouched:** assert source file checksums/sizes unchanged after the run; the
  original `…-0.4.txt` and QUL files are never opened for write (extends the existing source-untouched
  test pattern).

---

## 8. Out of scope (explicit)

- **UI / frontend implementation** of root/lemma/stem/POS/verb pages or the word-detail panel.
- **API endpoints / DTOs** — recommended as a **later phase** (a read-only `quran-morphology` query
  surface), **not** part of the 004 data foundation. (Per `API_GUIDELINES.md` when it happens:
  Arabic-default messages, English identifiers, `ApiResponse` shape.)
- **UI pages and API endpoints** for the word-type filters of §3.7 — Feature 004 ships only the
  **data foundation** that makes those filters possible.
- **Full sentence-level إعراب** — the syntactic treebank is **absent**; not derivable here.
- **Generated Arabic i3rab prose** — **Feature 005**.
- **Syntactic roles** (فاعل / مفعول به / مبتدأ / خبر / حال …) — require the absent treebank; not in 004.
- **Exact character offsets inside `qpcUthmani`** — not attempted (Uthmani-script offsets are unsafe).
- **Treating `form_arabic_normalized` as Mushaf text** — it is a flagged derived reading aid only
  (§3.3a); authoritative display stays `quran_words.text_uthmani` / `qpc_glyph`.
- **Verb form (I–X) extraction**, **مصدر-of-verb links** — deferred; raw features retained.
- **Semantic categories, gates/topics (أبواب/مواضيع), tafsir, translations, audio.**
- **Modifying the original Corpus `.txt` or the QUL source files** — strictly read-only.
- **Inventing any missing root / lemma / stem / POS / i3rab value** — null stays null.
- **Buckwalter→Arabic conversion** of the 1,704 corpus-only lemmas — deferred (a later normalization
  decision; warned, not blocked).

---

## 9. Phase plan (small, reviewable phases)

| Phase | Title | Deliverable | Gate |
|---|---|---|---|
| **0** | Local source files & readiness | Copy chosen files into the Git-ignored `resources/import-sources/quran-morphology/` (local-only); write `manifest.json` + `README` + provenance. No DB, no parsing. | Files present; manifest counts/checksums set. |
| **1** | Schema | Entities + enums/value objects (`Domain/Quran/Words/Morphology/`, §3.9) + EF configs + **one EF-tool-generated migration** (`AddQuranWordMorphology`) for the **6 tables** (incl. `quran_pos_tags`) + the segment Arabic-rendering columns (§3.3) + indexes. *Generated, not applied.* | `dotnet build` green; migration files reported, `database update` skipped. |
| **2** | Source readers & assembler | `Infrastructure/Files/Quran/Import/Morphology/` readers for aligned corpus + QUL Arabic + alignment map; assembler joining by location, head-POS selection, feature parsing, marker exclusion; **Buckwalter→Arabic segment transliteration + `arabic_render_tier` classification** (§3.3a, D1). | Unit-tested parse/assemble/transliterate on samples; charset table covers 100%. |
| **3** | Importer / loader | `import-morphology` verb + Application handler + `Infrastructure` SQL: transactional dimensions → per-word → segments (incl. `form_arabic_normalized` + tier) → **`quran_pos_tags` seed** (Arabic+English labels, category, order); `--force` semantics. | Synthetic-seed integration: load + idempotency + refusal. |
| **4** | Validation checks | `MORPH-*` hard + warning checks in the transactional gate; absolute/anchor constants + checks in the real-source path; report writer (Markdown+JSON). | Gate passes on synthetic seed; report emitted on pass+fail. |
| **5** | Tests | Synthetic-seed + real-source gated + anchors + multi-segment + null-root + verb + source-untouched (per §7). | `dotnet test` all green. |
| **6** | Build, verify, report | `dotnet build` (0 warnings), `dotnet test`; clean-code + test-code self-checks; `engineering-review` (+`test-guard`); implementation report. | Final verdict PASS. |
| **7** | Dev reset / reseed (developer-run, documented) | Document: reset → migrate → `import-foundation` → `import-morphology --force` → (`rebuild-words --force`) → audit. | Quickstart added; not blocking. |
| **(005)** | *Separate feature* — Arabic i3rab generation | Syntactic-role inference + Arabic phrasing rules on top of morphology + case; word-level, clearly generated. | Out of 004. |

> **Optional finer split (if Phase 3–4 grow large):** *Phase 3a* = root/lemma/stem dimensions +
> `quran_word_morphology` head + segments; *Phase 3b* = verb/case derivation + `quran_pos_tags` +
> their checks. Prefer the smaller phases over one big import.

---

## 10. Final recommendation

| Option | Shape | Verdict |
|---|---|---|
| **A** | One combined Feature 004 = morphology **+** Arabic i3rab | ❌ **Reject.** i3rab needs a syntactic layer **absent** from the source; merging forces inventing values or shipping a half-built rule engine inside a data-foundation feature. |
| **B** | **Feature 004 = morphology foundation** (root, lemma, stem, POS/type, segments, features, **verb classification**, case features, controlled-vocab Arabic labels, root/lemma/stem dimensions, **D1 flagged segment Arabic rendering**, **D2 `quran_pos_tags` word-type filtering foundation**) **+ Feature 005 = generated Arabic i3rab** | ✅ **Recommended.** 004 is 100% source-backed (no invented values, no syntax claims); 005 isolates the rule-based, over-claim-prone work. |
| **C** | B, but split 004 internally into 004a (root/lemma/stem + segments + head POS) and 004b (verb/case classification + indexes) | ➖ **Optional.** Same boundary as B; only adopt the *internal* split if phases get large (it is offered as the §9 "finer split"). Not a separate feature number. |

**Recommendation: Option B**, implemented in the §9 phases, with the §2.6 refinements:
1. Canonical source = **aligned Corpus JSON for classification/structure + QUL Arabic files for
   root/lemma/stem display**, joined by location.
2. **Verbs are in 004** (deterministic from `PERF/IMPF/IMPV/PASS`).
3. **Controlled-vocabulary Arabic POS/tense/case labels are in 004** (a fixed lookup, not generated
   prose). Only **generated syntactic إعراب** waits for Feature 005.
4. **(D1)** Flagged, derived **segment Arabic rendering** (`form_arabic_normalized` + tier + source) —
   never Uthmani, never Mushaf display (§3.3a). **(D2)** The **`quran_pos_tags` word-type filtering
   foundation** (Arabic+English labels, broad category, derived verb/case fields, small enums/value
   objects) — data only, no UI/API. Full record in `feature-004-decisions-addendum.md`.

**Why B is safest:** it draws the line exactly where the data does — store and label what the source
proves (morphology + case + a fixed Arabic dictionary), and defer everything that requires Arabic
grammar rules or sentence-level syntax (which the workspace does **not** contain). It keeps the
original Corpus and QUL files read-only, never invents a root/lemma/stem/POS/i3rab value, and never
attaches morphology to an ayah marker.

---

### Quranic Data Safety (applies throughout)

- **Display stays Uthmani/QPC**; root/lemma/stem **display** strings come from the **QUL Arabic**
  sources; classification comes from the **aligned Corpus**. Drill-ins color/group by **ids**, never
  raw verse text.
- **Markers never receive morphology** (`is_ayah_marker = false` predicate + `MORPH-MARKERS-EXCLUDED`).
- **No invented values** — null root/lemma/stem stays null; active voice is labeled **inferred**;
  syntactic role and full i3rab are **not** produced in 004.
- **Source is read-only** — original Corpus `.txt` and QUL files are never written
  (`MORPH-SOURCE-UNCHANGED`); all derivations live in the local source files and DB tables.
- **Test data uses single word-identity forms only** (no verse passages); synthetic seeds use safe
  placeholders.
