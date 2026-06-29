# Segment-Stem Curation Candidates — Multi-STEM Secondary Segments

**Feature:** 018 — `018-segment-stems-and-stems-explorer`
**Artifact type:** `segment-stem-curation-candidates` (review packet)
**Status:** `candidates_only_not_approved`
**Task type:** Curation packet generation only — **no** migration, importer, frontend, backend, schema, or data changes. Read-only `SELECT` queries against the local dev DB.
**Source DB:** `quran_dashboard @ localhost:5432` (local dev)
**Generated (UTC):** `2026-06-28T22:23:49Z`
**Predecessor reports:**
- `docs/feature-017-lexical-explorers-polish/segment-stem-ids-feasibility-report.md` (verdict: `READY_WITH_CURATION`)
- `docs/feature-017-lexical-explorers-polish/stems-explorer-current-state-polish-report.md`

**Companion files (same folder):**
- `segment-stem-curation-candidates.json` — full machine artifact (counts, riskSummary, 483 candidates with nested `stem_segments`).
- `segment-stem-curation-candidates.csv` — flat one-row-per-candidate table for spreadsheet review.

> ⚠️ **These are candidates, not approved mappings.** See §7.

---

## 1. Verdict — candidate generation

### `CANDIDATES_GENERATED_OK`

- Generated exactly **483** candidate rows — one per **secondary STEM segment** of every 2-STEM word. This equals the secondary-STEM count from the feasibility report and the live DB (§2). **No mismatch, no blocker.**
- Every candidate is the **secondary** STEM segment (second STEM by `segment_number`) of a word that has **exactly 2** STEM segments. **0** primary segments and **0** non-STEM segments leaked in (verified, §6).
- Each row carries: the secondary segment's full identity, the word's current head-stem context, the **primary/head** STEM segment context, a **mechanical** text-match candidate (review aid only), and risk/review fields left blank for the curator.
- The `mechanical_candidate_*` columns are produced by **exact Arabic-text match** of `segment_form_arabic_normalized` against `quran_stems.stem_text`. This is **deterministic but NOT safe** (contextual idghām rendering, contextual/artifact stem rows, invented provenance — see feasibility report §6, Strategy B). It is provided **only** to focus human review. **No row here is approved.**
- `72:16:1:3` / `لَّوِ` is present as the single **`no_text_match`** candidate, matching the predecessor report exactly. ✅

---

## 2. Totals

All figures are live-DB verified this session and match the feasibility report §5. `[FACT-DB]`

| Metric | Value |
|---|---:|
| Readable words (head morphology rows) | **77,432** |
| Total segment rows | **128,219** |
| Total STEM segments (`kind='STEM'`) | **77,915** |
| Words with **1** STEM | **76,949** |
| Words with **2** STEM | **483** |
| Words with **0** STEM | **0** |
| Words with **>2** STEM | **0** |
| **Secondary STEM candidates generated** | **483** |
| — exact Arabic-text matches | **482** |
| — no text match | **1** |
| — circular matches (mechanical = head stem) | **60** |
| Distinct secondary lemmas | **6** |
| Distinct secondary normalized forms | **14** |
| `quran_stems` / `quran_lemmas` / `quran_roots` | 12,108 / 4,790 / 1,642 |

Sanity: `76,949 + 483×2 = 77,915` STEM segments ✓. Every word has exactly 1 or 2 STEM segments.

### 2.1 Candidate status & risk-flag summary

| `candidate_status` | n | | `risk_flag` | n |
|---|---:|---|---|---:|
| `needs_review_contextual_idgham` | 219 | | `function_word_compound` | 482 |
| `needs_review_text_match` | 202 | | `contextual_idgham` | 278 |
| `circular_match` | 60 | | `same_as_head_stem` | 60 |
| `no_text_match` | 1 | | `artifact_stem_text` | 3 |
| `needs_new_or_canonical_stem_decision` | 1 | | `no_text_match` | 1 |
| **total** | **483** | | | |

`candidate_status` is single-valued (priority: `no_text_match` → `circular_match` → `needs_new_or_canonical_stem_decision` → `needs_review_contextual_idgham` → `needs_review_text_match`). `risk_flags` is multi-valued and captures every applicable concern independently, so a circular row can still carry `contextual_idgham` + `artifact_stem_text`.

---

## 3. Grouped review tables

### 3.1 By secondary lemma (the whole surface is 6 lemmas)

| lemma_id | lemma | n | exact | no_match | circular |
|---:|---|---:|---:|---:|---:|
| 4 | مَا | 385 | 385 | 0 | 58 |
| 130 | مِن | 47 | 47 | 0 | 0 |
| 77 | لَا | 47 | 47 | 0 | 1 |
| 2306 | لَن | 2 | 2 | 0 | 0 |
| 175 | لَو | 1 | 0 | **1** | 0 |
| 150 | أُمّ | 1 | 1 | 0 | 1 |

Five are function particles (ما / مِن / لا / لن / لو). The lone exception is **lemma 150 أُمّ** (a content noun), the `يَبْنَؤُمَّ` case — the only non-function-word secondary segment.

### 3.2 By secondary POS

| POS | n | no_match | circular |
|---|---:|---:|---:|
| REL | 252 | 0 | 48 |
| PREV | 162 | 0 | 0 |
| NEG | 45 | 0 | 1 |
| SUP | 8 | 0 | 7 |
| INTG | 6 | 0 | 2 |
| PRO | 5 | 0 | 0 |
| SUB | 3 | 0 | 1 |
| COND | 1 | **1** | 0 |
| N | 1 | 0 | 1 |

### 3.3 By secondary normalized Arabic form (14 distinct)

| form | n | POS set | mechanical stem_id → text | statuses present |
|---|---:|---|---|---|
| مَّا | 164 | PREV/REL/SUB/SUP | 446 → مَّا | circular / contextual_idgham |
| مَا | 158 | PREV/REL/SUB/SUP | 5 → مَا | circular / text_match |
| لَّا | 46 | NEG/PRO | 476 → لَّا | circular / contextual_idgham |
| مَآ | 45 | NEG/PREV/REL | 90 → مَآ | text_match |
| مَّن | 21 | INTG/REL | 1220 → مَّن | contextual_idgham |
| مَّنْ | 16 | INTG/REL | 3854 → مَّنْ | contextual_idgham |
| مَّآ | 14 | REL | 1154 → مَّآ | circular / contextual_idgham |
| مَّنِ | 10 | REL | 5552 → مَّنِ | contextual_idgham |
| لَّن | 2 | NEG | 6554 → لَّن | contextual_idgham |
| مَّ | 2 | INTG/REL | 17791 → مَّ | circular / needs_new_or_canonical |
| مَ | 2 | INTG | 4727 → مَ | circular |
| لَّآ | 1 | NEG | 1370 → لَّآ | contextual_idgham |
| لَّوِ | 1 | COND | *(none)* | **no_text_match** |
| ؤُمَّ | 1 | N | 7608 → ؤُمَّ | circular |

Most forms carry an idghām-induced shadda (مَّا, لَّا, مَّن…) — their mechanical match lands on a shadda-bearing (contextual) `stem_text`, which is exactly why text matching is unsafe.

### 3.4 By POS pattern (`primary_pos + secondary_pos`)

| pattern | n | example word |
|---|---:|---|
| P+REL | 228 | وَمِمَّا |
| ACC+PREV | 160 | إِنَّمَا |
| SUB+NEG | 42 | لِئَلَّا |
| CONJ+REL | 10 | أَمَّا |
| ACC+REL | 8 | أَنَّمَا |
| COND+SUP | 8 | أَيْنَمَا |
| INT+PRO | 5 | أَلَّا |
| V+REL | 4 | بِئْسَمَا |
| P+INTG | 3 | فِيمَ |
| CONJ+INTG | 3 | أَمَّن |
| ACC+NEG | 2 | أَلَّا |
| LOC+REL | 2 | أَيْنَمَا |
| N+PREV | 1 | رُّبَمَا |
| P+PREV | 1 | مِّمَّا |
| N+N | 1 | يَبْنَؤُمَّ |
| P+SUB | 1 | عَمَّا |
| SUB+COND | 1 | وَأَلَّوِ |
| COND+NEG | 1 | إِلَّا |
| T+SUB | 1 | كُلَّمَا |
| ACC+SUB | 1 | أَنَّمَا |

Every pattern is a clitic/particle compound (preposition+relative, accusative-particle+preventive-ما, أن+لا, …). The only `N+N` is the kinship vocative `يَبْنَؤُمَّ` (يا ابن أُمّ).

---

## 4. Focused risky cases

### 4.1 `no_text_match` — all (1)

| segment_location | word | form | POS | lemma | head stem |
|---|---|---|---|---|---|
| `72:16:1:3` | وَأَلَّوِ | لَّوِ | COND | 175 (لَو) | 1184 (أَ) |

`لَّوِ` is the idghām + kasra rendering of `لو` before a following hamza; no `quran_stems.stem_text` equals it. A canonical `لو` stem (or a new stem row) must be **decided by review**, not auto-matched. This is the predecessor report's known outlier, reproduced exactly. ✅

### 4.2 `circular_match` — all (60), grouped by word

Secondary form text-matches the **same** `quran_stems` row already assigned as the word's head stem (so the mechanical "candidate" is the head stem itself — no new attribution). Counting these would double-count a word under one stem; review must decide whether the secondary segment is a genuine second stem or a no-op.

| word | form | POS | head = mechanical stem | n |
|---|---|---|---|---:|
| عَمَّا | مَّا | REL | 446 (مَّا) | 43 |
| عَمَّآ | مَّآ | REL | 1154 (مَّآ) | 3 |
| وَإِمَّا | مَّا | SUP | 446 (مَّا) | 3 |
| فَإِمَّا | مَّا | SUP | 446 (مَّا) | 2 |
| فِيمَ | مَ | INTG | 4727 (مَ) | 2 |
| أَيَّمَا | مَا | SUP | 5 (مَا) | 1 |
| إِلَّا | لَّا | NEG | 476 (لَّا) | 1 |
| إِمَّا | مَّا | SUP | 446 (مَّا) | 1 |
| عَمَّا | مَّا | SUB | 446 (مَّا) | 1 |
| مِمَّ | مَّ | REL | 17791 (مَّ) | 1 |
| نِعِمَّا | مَّا | REL | 446 (مَّا) | 1 |
| يَبْنَؤُمَّ | ؤُمَّ | N | 7608 (ؤُمَّ) | 1 |
| **total** | | | | **60** |

(Per-`segment_location` enumeration: filter `candidate_status = circular_match` in the CSV/JSON.)

### 4.3 `contextual_idgham` — all (278), grouped by form + POS

Secondary form carries a contextual shadda (idghām). The mechanical match is rendering-dependent and lands on a shadda-bearing `stem_text`; a future re-render could break it (as it already does for `لَّوِ`). Review must decide the canonical clean stem.

| form | POS | n | mechanical stem |
|---|---|---:|---|
| مَّا | REL | 156 | 446 (مَّا) |
| لَّا | NEG | 41 | 476 (لَّا) |
| مَّن | REL | 20 | 1220 (مَّن) |
| مَّنْ | REL | 14 | 3854 (مَّنْ) |
| مَّآ | REL | 14 | 1154 (مَّآ) |
| مَّنِ | REL | 10 | 5552 (مَّنِ) |
| مَّا | SUP | 6 | 446 (مَّا) |
| لَّا | PRO | 5 | 476 (لَّا) |
| مَّنْ | INTG | 2 | 3854 (مَّنْ) |
| لَّن | NEG | 2 | 6554 (لَّن) |
| مَّا | PREV | 1 | 446 (مَّا) |
| لَّآ | NEG | 1 | 1370 (لَّآ) |
| لَّوِ | COND | 1 | *(none — no_text_match)* |
| مَّ | INTG | 1 | 17791 (مَّ) |
| مَّ | REL | 1 | 17791 (مَّ) |
| ؤُمَّ | N | 1 | 7608 (ؤُمَّ) |
| مَّا | SUB | 1 | 446 (مَّا) |
| مَّن | INTG | 1 | 1220 (مَّن) |
| **total** | | **278** | |

### 4.4 `artifact_stem_text` — all (3)

Mechanical match lands on a **clitic-only artifact stem** (a `quran_stems` row whose head-words are *all* 2-STEM words: ids 1184 أَ, 4656 رُّبَ, 7608 ؤُمَّ, 10279 ئَ, 17153 عَمَّ, 17791 مَّ). Linking a secondary segment to such a row perpetuates a contextual artifact instead of a clean canonical stem.

| segment_location | word | form | POS | mechanical stem | status |
|---|---|---|---|---|---|
| `20:94:2:3` | يَبْنَؤُمَّ | ؤُمَّ | N | 7608 (ؤُمَّ) | circular_match |
| `78:1:1:2` | عَمَّ | مَّ | INTG | 17791 (مَّ) | needs_new_or_canonical_stem_decision |
| `86:5:3:2` | مِمَّ | مَّ | REL | 17791 (مَّ) | circular_match |

---

## 5. Representative examples (verbatim from generated candidates)

| location | secondary seg | POS | form | head stem | mechanical candidate | status |
|---|---|---|---|---|---|---|
| `2:3:6` (وَمِمَّا) | `2:3:6:3` | REL | مَّا | 816 (مِ) | 446 (مَّا) | needs_review_contextual_idgham |
| `2:11:9` (إِنَّمَا) | `2:11:9:2` | PREV | مَا | 12 (إِنَّ) | 5 (مَا) | needs_review_text_match |
| `2:90:1` (بِئْسَمَا) | `2:90:1:2` | REL | مَا | 2935 (بِئْسَ) | 5 (مَا) | needs_review_text_match |
| `72:16:1` (وَأَلَّوِ) | `72:16:1:3` | COND | لَّوِ | 1184 (أَ) | *(none)* | **no_text_match** |

- **2:3:6 (مِمَّا):** today filed under stem **مِ** (the مِن clitic, head). The **مَّا** segment is invisible as a stem; the mechanical aid points at stem **مَّا (446)** — but that is a shadda artifact of idghām; the clean target is the relative **ما** stem. Review decision required.
- **2:11:9 (إِنَّمَا):** secondary **مَا** (preventive) cleanly text-matches stem **مَا (5)** — but "is preventive ما the same stem row as relative ما?" is a linguistic call, hence `needs_review_text_match`.
- **2:90:1 (بِئْسَمَا):** primary is the content verb **بِئْسَ** (root ب-ا-س); secondary relative **مَا** → stem 5.
- **72:16:1 (وَأَلَّوِ):** `لَّوِ` has **no** matching `stem_text`; the canonical لو stem (or a new row) must be decided, not guessed.

---

## 6. Validation results

| Check | Required | Result |
|---|---|---|
| JSON candidate rows = CSV data rows | equal | 483 = 483 ✅ |
| Candidate count = secondary STEM count | 483 | 483 ✅ |
| Every candidate is a **secondary** STEM (not primary) | yes | `segment_number = primary_stem_segment_number` count = **0** ✅ |
| Every candidate belongs to a word with exactly 2 STEM segments | yes | enforced by `HAVING COUNT(*)=2` ✅ |
| No non-STEM segment appears as a candidate | yes | all rows `segment_kind = STEM` (483/483) ✅ |
| No rows silently dropped | yes | status buckets sum to 483; no `WHERE` filters drop unmatched (`no_text_match` retained) ✅ |
| `72:16:1:3` / `لَّوِ` present as no-text-match | yes | present, `candidate_status = no_text_match` ✅ |
| Counts vs predecessor report | match | 77,432 / 128,219 / 77,915 / 76,949 / 483 / 0 / 0; secondary 483; no_match 1; circular 60; 6 lemmas; 14 forms — **all identical** ✅ |

No mismatch with the feasibility report. No blocker.

---

## 7. These candidates are NOT approved mappings

1. **Candidates only.** This packet enumerates the review surface; it does **not** assign any `stem_id`.
2. **The mechanical candidate is a review aid only.** `mechanical_candidate_stem_id/_text/_match_method` come from exact Arabic-text matching against `quran_stems.stem_text`. That match is **deterministic but not safe**: it depends on contextual idghām rendering, can land on contextual/artifact stem rows (60 circular, 3 artifact), and asserts a stem identity that **no import source provides**. Treat it as a hint, never a decision.
3. **The final artifact must be created only after human/linguistic review.** Do **not** generate `segment-stem-corrected-arabic.json` from this file mechanically. A curator/scholar must decide, per row: the canonical clean stem (vs the idghām artifact), whether new `quran_stems` rows are needed (e.g. لو for `لَّوِ`), whether circular rows are genuine second stems or no-ops, and whether function-word senses collapse to one stem.
4. **No code, schema, migration, importer, frontend, or data changes** were made, and **nothing was committed**, by this task.

Per-row review is captured by filling the empty fields: `review_decision`, `reviewed_stem_id`, `reviewed_stem_text`, `review_notes`.

---

## 8. Appendix — query logic (read-only)

All queries are `SELECT`-only against `quran_dashboard`. The reusable candidate definition (the CTE that yields exactly the 483 secondary-STEM rows) and the export/summary queries are stored alongside this report under `sql/`:

- `sql/_candidates_cte.sql` — the core read-only CTE (`two_stem` → `stem_seg` ranked → `prim`/`sec` → `cand` → `classified`).
- `sql/build_csv.sql` — `\copy` of all 483 rows to the CSV.
- `sql/build_json.sql` — builds the full JSON document (counts, riskSummary, candidates).
- `sql/_summary.sql`, `sql/_checks.sql`, `sql/_groups.sql`, `sql/_circular_by_word.sql` — the verification and grouping queries behind §2–§6.
- `sql/format_json.py` — one-off order-preserving pretty-printer for the JSON (no DB access).

### 8.1 Core candidate definition (the heart of the CTE)

```sql
-- words with exactly two STEM segments
WITH two_stem AS (
  SELECT quran_word_id
  FROM quran_word_morphology_segments
  WHERE kind = 'STEM'
  GROUP BY quran_word_id
  HAVING COUNT(*) = 2
),
-- rank the two STEM segments: 1 = primary/head, 2 = secondary (the candidate)
stem_seg AS (
  SELECT s.*,
         ROW_NUMBER() OVER (PARTITION BY s.quran_word_id ORDER BY s.segment_number) AS stem_rank
  FROM quran_word_morphology_segments s
  JOIN two_stem t ON t.quran_word_id = s.quran_word_id
  WHERE s.kind = 'STEM'
),
prim AS (SELECT * FROM stem_seg WHERE stem_rank = 1),
sec  AS (SELECT * FROM stem_seg WHERE stem_rank = 2)
-- the candidate is `sec`; joined to quran_words, quran_word_morphology (head stem),
-- quran_lemmas / quran_roots (segment + primary), and LEFT JOIN quran_stems ms
-- ON ms.stem_text = sec.form_arabic_normalized for the MECHANICAL text-match aid.
SELECT count(*) FROM sec;  -- = 483
```

### 8.2 Mechanical text-match aid (review aid only — not approval)

```sql
LEFT JOIN quran_stems ms ON ms.stem_text = sec.form_arabic_normalized
-- match_method := CASE WHEN ms.id IS NULL THEN 'no_text_match'
--                      ELSE 'exact_arabic_text_match' END
-- is_same_as_head_stem := (ms.id = quran_word_morphology.stem_id)   -- circular
```

### 8.3 Clitic-only artifact stems (for `artifact_stem_text`)

```sql
-- stems whose head-words are ALL 2-STEM words → contextual/clitic artifacts
SELECT st.id
FROM quran_word_morphology m
JOIN (SELECT quran_word_id, COUNT(*) FILTER (WHERE kind='STEM') sn
      FROM quran_word_morphology_segments GROUP BY quran_word_id) n
     ON n.quran_word_id = m.quran_word_id
JOIN quran_stems st ON st.id = m.stem_id
GROUP BY st.id
HAVING bool_and(n.sn = 2);
-- → ids 1184 (أَ), 4656 (رُّبَ), 7608 (ؤُمَّ), 10279 (ئَ), 17153 (عَمَّ), 17791 (مَّ)
```

### 8.4 Status & risk-flag derivation

```sql
candidate_status :=
  CASE
    WHEN mechanical_candidate_stem_id IS NULL              THEN 'no_text_match'
    WHEN mechanical_candidate_is_same_as_head_stem          THEN 'circular_match'
    WHEN is_artifact_target                                 THEN 'needs_new_or_canonical_stem_decision'
    WHEN form_has_shadda                                    THEN 'needs_review_contextual_idgham'
    ELSE 'needs_review_text_match'
  END;

risk_flags := array of:
  'contextual_idgham'      when form_arabic_normalized contains U+0651 (shadda)
  'no_text_match'          when no quran_stems.stem_text equals the form
  'same_as_head_stem'      when mechanical stem = head stem (circular)
  'function_word_compound' when secondary POS <> 'N'
  'artifact_stem_text'     when mechanical stem ∈ clitic-only artifact set
```

---

*Read-only `SELECT` queries against the local dev DB (`quran_dashboard`). A session-local `TEMP` table (`pg_temp`) was used to materialize the candidate set within a single psql session for export; it is dropped at session end and no persistent table or Quran data was modified. The DB password was read from the existing local user-secrets store for this session only and is not reproduced here. No migration, importer, schema, frontend, backend, or data changes were made, and nothing was committed.*
