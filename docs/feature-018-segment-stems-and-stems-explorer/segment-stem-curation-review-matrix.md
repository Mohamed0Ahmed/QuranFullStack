# Segment-Stem Curation — Grouped Review Decision Matrix

**Feature:** 018 — `018-segment-stems-and-stems-explorer`
**Artifact type:** `segment-stem-curation-review-matrix` (grouped draft decisions)
**Status:** `draft_decisions_not_approved`
**Source artifact:** `segment-stem-curation-candidates.json` (483 secondary-STEM candidates)
**Generated (UTC):** `2026-06-28T22:23:49Z`
**Companion files (same folder):**
- `segment-stem-curation-review-matrix.json` — machine matrix (40 groups, decisionSummary, samples).
- `segment-stem-curation-review-matrix.csv` — one row per group for spreadsheet review.

> ⚠️ **Draft decisions only — nothing approved.** `draft_reviewed_stem_*` are mechanical suggestions to speed review; every group still requires scholar sign-off (§6). Do **not** generate `segment-stem-corrected-arabic.json` from this file.

---

## 1. Verdict

### `MATRIX_GENERATED_OK`

- The **483** candidates reduce to **40 review groups** by the composite key **form + secondary POS + secondary lemma + primary POS + candidate_status** (not by form alone, per the requirement).
- Sum of group counts = **483**; every candidate belongs to **exactly one** group (composite key is total + disjoint).
- Each group carries: identity, count, up-to-5 sample locations/words, mechanical candidate stem, de-shadda **clean** stem, risk flags, a **draft decision**, a draft reviewed stem (where obvious), confidence, priority, and an explicit `review_required = scholar` flag.
- Special cases (`لَّوِ`, circular, artifact targets, `يَبْنَؤُمَّ`, `مَّن/مَّنْ/مَّنِ`, `مَا` vs `مَّا`, `لَا` vs `لَّا`) are each isolated into their own groups and called out in §4.

---

## 2. Draft-decision summary

| draft_decision | groups | candidates | meaning |
|---|---:|---:|---|
| `map_clean_canonical_stem` | 16 | **219** | idghām (shadda) form → its **de-shadda clean** stem row |
| `accept_mechanical_clean_stem` | 12 | **202** | form already matches a **clean** stem; only sense-collapse remains |
| `treat_as_head_stem_noop` | 8 | **58** | secondary text = the word's **own head** stem (circular) → likely no-op |
| `remap_artifact_to_canonical` | 2 | **2** | mechanical hits a **clitic-only artifact** stem → remap to clean |
| `decide_canonical_umm_stem` | 1 | **1** | `يَبْنَؤُمَّ` → canonical **أُمّ** (no de-shadda row) |
| `create_or_map_canonical_law_stem` | 1 | **1** | `لَّوِ` → canonical **لو** (no shadda match) |
| **total** | **40** | **483** | |

**High-priority groups (4):** the two artifact-target `مَّ` groups (#39, #40), `يَبْنَؤُمَّ` (#32), and `لَّوِ` (#38). Everything else is `normal` priority but **still requires** scholar confirmation.

Confidence: `medium` for clean / de-shadda mappings (219+202 = 421 candidates), `low` for circular / artifact / no-match / أُمّ (62 candidates).

---

## 3. Full group matrix (40 groups, by count desc)

Legend: **mech→clean** = mechanical (shadda artifact) stem → de-shadda clean stem. **draft** = `draft_reviewed_stem_id`. Conf/Prio = confidence/priority.

| # | form | sec POS | POS pattern | status | n | mech→clean | draft decision | draft | conf | prio | example |
|--:|---|---|---|---|--:|---|---|---|---|---|---|
| 1 | مَا | PREV | ACC+PREV | text_match | 121 | 5→5 | accept_mechanical_clean_stem | 5 | med | norm | إِنَّمَا |
| 2 | مَّا | REL | P+REL | idgham | 110 | 446→5 | map_clean_canonical_stem | 5 | med | norm | وَمِمَّا |
| 3 | مَّا | REL | P+REL | **circular** | 43 | 446→5 | treat_as_head_stem_noop | 446 | low | norm | عَمَّا |
| 4 | لَّا | NEG | SUB+NEG | idgham | 39 | 476→78 | map_clean_canonical_stem | 78 | med | norm | لِئَلَّا |
| 5 | مَآ | PREV | ACC+PREV | text_match | 39 | 90→90 | accept_mechanical_clean_stem | 90 | med | norm | فَإِنَّمَآ |
| 6 | مَا | REL | P+REL | text_match | 19 | 5→5 | accept_mechanical_clean_stem | 5 | med | norm | فِيمَا |
| 7 | مَّن | REL | P+REL | idgham | 14 | 1220→131 | map_clean_canonical_stem | 131 | med | norm | مِمَّن |
| 8 | مَّنْ | REL | P+REL | idgham | 12 | 3854→145 | map_clean_canonical_stem | 145 | med | norm | مِّمَّنْ |
| 9 | مَّآ | REL | P+REL | idgham | 11 | 1154→90 | map_clean_canonical_stem | 90 | med | norm | مِمَّآ |
| 10 | مَّنِ | REL | P+REL | idgham | 10 | 5552→627 | map_clean_canonical_stem | 627 | med | norm | مِمَّنِ |
| 11 | مَا | REL | ACC+REL | text_match | 8 | 5→5 | accept_mechanical_clean_stem | 5 | med | norm | أَنَّمَا |
| 12 | مَّا | SUP | COND+SUP | **circular** | 6 | 446→5 | treat_as_head_stem_noop | 446 | low | norm | وَإِمَّا |
| 13 | مَّن | REL | CONJ+REL | idgham | 6 | 1220→131 | map_clean_canonical_stem | 131 | med | norm | أَمَّن |
| 14 | لَّا | PRO | INT+PRO | idgham | 5 | 476→78 | map_clean_canonical_stem | 78 | med | norm | أَلَّا |
| 15 | مَآ | REL | P+REL | text_match | 5 | 90→90 | accept_mechanical_clean_stem | 90 | med | norm | فِيمَآ |
| 16 | مَا | REL | V+REL | text_match | 3 | 5→5 | accept_mechanical_clean_stem | 5 | med | norm | بِئْسَمَا |
| 17 | مَّآ | REL | P+REL | **circular** | 3 | 1154→90 | treat_as_head_stem_noop | 1154 | low | norm | عَمَّآ |
| 18 | مَ | INTG | P+INTG | **circular** | 2 | 4727→4727 | treat_as_head_stem_noop | 4727 | low | norm | فِيمَ |
| 19 | مَّا | REL | CONJ+REL | idgham | 2 | 446→5 | map_clean_canonical_stem | 5 | med | norm | أَمَّا |
| 20 | مَا | REL | LOC+REL | text_match | 2 | 5→5 | accept_mechanical_clean_stem | 5 | med | norm | أَيْنَمَا |
| 21 | لَّن | NEG | SUB+NEG | idgham | 2 | 6554→2307 | map_clean_canonical_stem | 2307 | med | norm | أَلَّن |
| 22 | مَّنْ | REL | CONJ+REL | idgham | 2 | 3854→145 | map_clean_canonical_stem | 145 | med | norm | أَمَّنْ |
| 23 | مَّنْ | INTG | CONJ+INTG | idgham | 2 | 3854→145 | map_clean_canonical_stem | 145 | med | norm | أَمَّنْ |
| 24 | مَّا | REL | V+REL | **circular** | 1 | 446→5 | treat_as_head_stem_noop | 446 | low | norm | نِعِمَّا |
| 25 | مَا | SUP | COND+SUP | text_match | 1 | 5→5 | accept_mechanical_clean_stem | 5 | med | norm | أَيْنَمَا |
| 26 | لَّا | NEG | COND+NEG | **circular** | 1 | 476→78 | treat_as_head_stem_noop | 476 | low | norm | إِلَّا |
| 27 | مَّن | INTG | CONJ+INTG | idgham | 1 | 1220→131 | map_clean_canonical_stem | 131 | med | norm | أَمَّن |
| 28 | مَا | PREV | N+PREV | text_match | 1 | 5→5 | accept_mechanical_clean_stem | 5 | med | norm | رُّبَمَا |
| 29 | مَا | SUB | T+SUB | text_match | 1 | 5→5 | accept_mechanical_clean_stem | 5 | med | norm | كُلَّمَا |
| 30 | لَّآ | NEG | SUB+NEG | idgham | 1 | 1370→438 | map_clean_canonical_stem | 438 | med | norm | أَلَّآ |
| 31 | لَّا | NEG | ACC+NEG | idgham | 1 | 476→78 | map_clean_canonical_stem | 78 | med | norm | أَلَّا |
| 32 | ؤُمَّ | N | **N+N** | **circular** | 1 | 7608→— | **decide_canonical_umm_stem** | — | low | **high** | يَبْنَؤُمَّ |
| 33 | مَا | SUB | ACC+SUB | text_match | 1 | 5→5 | accept_mechanical_clean_stem | 5 | med | norm | أَنَّمَا |
| 34 | مَا | SUP | COND+SUP | **circular** | 1 | 5→5 | treat_as_head_stem_noop | 5 | low | norm | أَيَّمَا |
| 35 | مَآ | NEG | ACC+NEG | text_match | 1 | 90→90 | accept_mechanical_clean_stem | 90 | med | norm | أَنَّمَآ |
| 36 | مَّا | SUB | P+SUB | **circular** | 1 | 446→5 | treat_as_head_stem_noop | 446 | low | norm | عَمَّا |
| 37 | مَّا | PREV | P+PREV | idgham | 1 | 446→5 | map_clean_canonical_stem | 5 | med | norm | مِّمَّا |
| 38 | لَّوِ | COND | SUB+COND | **no_text_match** | 1 | —→6226 | **create_or_map_canonical_law_stem** | — | low | **high** | وَأَلَّوِ |
| 39 | مَّ | INTG | P+INTG | needs_new_or_canonical | 1 | 17791→4727 | **remap_artifact_to_canonical** | 4727 | low | **high** | عَمَّ |
| 40 | مَّ | REL | P+REL | **circular**+artifact | 1 | 17791→4727 | **remap_artifact_to_canonical** | 4727 | low | **high** | مِمَّ |

(Per-group risk_flags, full sample lists, and notes are in the JSON/CSV.)

---

## 4. Special-case handling (explicit)

### 4.1 `72:16:1:3` / `لَّوِ` — group #38 (`no_text_match`)
The single form with **no** `quran_stems.stem_text` match. `لَّوِ` is the idghām + kasra render of `لو` (لَام مشددة) before the following hamza in `وَأَلَّوِ`. De-shadda gives `لَوِ`, which *does* exist as stem **6226** — but that row is itself a contextual (kasra) variant. **Draft decision:** `create_or_map_canonical_law_stem`; the scholar chooses between reusing `لَوِ` (6226), the canonical `لَوْ`, or minting a clean `لو` row. **Not auto-mapped.**

### 4.2 Circular matches — groups #3, #12, #17, #18, #24, #26, #34, #36, #40 (60 candidates)
The secondary form text-matches the word's **own head stem** (mechanical = head). These add no new attribution; counting must dedupe the word. **Draft:** `treat_as_head_stem_noop` (reviewed = head stem) for 58; the 2 `مَّ` cases are artifact-circular and routed to remap (§4.3). Note these heads are themselves shadda artifacts (446 مَّا, 1154 مَّآ, 476 لَّا…); cleaning the **head** is **out of scope** (head `stem_id` is frozen per the feasibility report). Scholar confirms whether a genuine second stem exists.

### 4.3 Artifact-stem targets — groups #39, #40 (and #32) (`مَّ`, `ؤُمَّ`)
Mechanical match lands on a **clitic-only artifact stem** (ids 1184 أَ, 4656 رُّبَ, 7608 ؤُمَّ, 10279 ئَ, 17153 عَمَّ, 17791 مَّ). Linking to these perpetuates a contextual artifact. **Draft:** `remap_artifact_to_canonical` → clean `مَ` (4727) for both `مَّ` groups; `ؤُمَّ` is handled as §4.4.

### 4.4 `يَبْنَؤُمَّ` — group #32 (`N+N`, lemma أُمّ)
The **only non-function-word** secondary segment (kinship vocative *يا ابن أُمّ*, `20:94:2:3`). Mechanical 7608 (`ؤُمَّ`) is **both** an artifact **and** equal to the head (circular), and de-shadda `ؤُمَ` has **no** stem row. **Draft:** `decide_canonical_umm_stem` — scholar must pick or mint the canonical **أُمّ** stem. High priority, low confidence.

### 4.5 `مَّن` / `مَّنْ` / `مَّنِ` — groups #7, #8, #10, #13, #22, #23, #27 (relative/interrogative مَن)
Lemma is QUL's `مِن` (130) but POS is REL/INTG → these are the **pronoun مَن**, not the preposition. The three idghām forms map to three distinct **clean** rows: `مَن` (131), `مَنْ` (145), `مَنِ` (627) — which differ only by trailing sukūn/kasra (contextual). **Open linguistic decision:** should all three collapse to one canonical `مَن` stem? Draft keeps the de-shadda row per form; scholar decides the collapse.

### 4.6 `مَا` vs `مَّا`
Same relative/preventive ما, two renders. **`مَا`** (no shadda) already matches clean stem **5** → `accept_mechanical_clean_stem` (groups #1,#6,#11,#16,#20,#25,#28,#29,#33 = 158 candidates). **`مَّا`** (idghām shadda) matches artifact stem **446** → `map_clean_canonical_stem` to **5** (groups #2,#19,#37 idgham; #3,#12,#24,#36 circular). Net draft: both families converge on clean stem **5 (مَا)** — pending the scholar's confirmation that preventive ما and relative ما share one stem row.

### 4.7 `لَا` vs `لَّا`
No bare `لَا` appears as a secondary form; every لا-lemma secondary is the idghām **`لَّا`** (or madd `لَّآ`), matching artifact **476** / **1370**. **Draft:** map to de-shadda clean **78 (لَا)** / **438 (لَآ)**. The clean `لَا` (78) is the canonical target.

---

## 5. Validation

| Check | Required | Result |
|---|---|---|
| Σ group candidate counts = 483 | yes | **483** ✅ |
| Every candidate in exactly one group | yes | composite key total + disjoint; 40 groups, no overlap ✅ |
| Groups derived only from the candidate artifact | yes | reduction of `segment-stem-curation-candidates.json` (no re-query for membership) ✅ |
| No group auto-approved | yes | all `review_required = scholar`; status `draft_decisions_not_approved` ✅ |
| Special cases isolated | yes | لَّوِ #38, يَبْنَؤُمَّ #32, artifact #39/#40, مَّن-family, مَا/مَّا, لَا/لَّا ✅ |
| CSV group rows = JSON groups | equal | 40 = 40 ✅ |

---

## 6. Open decisions (for scholar/user)

1. **Idghām canonicalization** (421 candidates): confirm that shadda artifact rows (446 مَّا, 476 لَّا, 1220 مَّن, …) should resolve to their de-shadda clean rows, and that the secondary `stem_id` uses the clean row — while the **head** stem stays as-is (frozen).
2. **Variant collapse** (مَّن/مَّنْ/مَّنِ → 131/145/627; مَآ/مَا): should trailing-vowel variants collapse to a single canonical stem per pronoun, or stay distinct?
3. **Sense-collapse** (مَا PREV vs REL vs SUB vs SUP): is it one stem row (`مَا` = 5) across senses, or per-sense?
4. **Circular no-ops** (58): confirm the secondary segment is the *same* stem as head (count once), not a second stem.
5. **`لَّوِ`** (#38): reuse 6226 / canonical لَوْ / new clean لو row?
6. **`يَبْنَؤُمَّ`** (#32): pick or mint the canonical أُمّ stem.
7. **Artifact `مَّ`** (#39, #40): confirm remap to clean مَ (4727); decide whether the clitic-artifact rows should be retired (re-modeling, out of this scope).

---

## 7. These are draft decisions, not approved mappings

- `draft_reviewed_stem_id/_text` are **mechanical suggestions** (de-shadda lookup + clean text match), provided to accelerate review. They are **not** approvals.
- Nothing here mutates the DB, schema, importer, frontend, backend, or any source file; **nothing was committed**.
- The final `segment-stem-corrected-arabic.json` must be produced **only** after scholar review fills the real decisions per group/row. Do **not** generate it mechanically from this matrix.

---

## 8. Appendix — method (read-only)

- **Input:** `segment-stem-curation-candidates.json` (483 rows).
- **Grouping:** `sql/build_review_matrix.py` — composite key `(form, secondary_pos, lemma_id, primary_pos, candidate_status)`; risk_flags verified constant within each group; draft decisions assigned by a deterministic rule table (no_text_match → law; lemma أُمّ → umm; artifact flag → remap; circular → head no-op; idghām → de-shadda clean; clean text-match → accept).
- **Clean-stem map:** `sql/build_clean_map.sql` → `sql/clean_stem_map.json` — a read-only `SELECT` mapping each exact secondary form to its de-shadda clean stem (`replace(form, U+0651, '')` against `quran_stems.stem_text`) and its shadda artifact match. Keyed by exact form bytes to avoid literal-mismatch errors.
- **Outputs:** this `.md`, plus `segment-stem-curation-review-matrix.json` / `.csv`.

*No code, schema, migration, importer, frontend, backend, or DB-data changes. Read-only `SELECT` only (for the clean-stem map). Nothing committed.*
