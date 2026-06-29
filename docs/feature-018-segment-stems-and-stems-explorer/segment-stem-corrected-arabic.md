# Segment-Stem Corrected (Arabic) — Final Curated Artifact

**Feature:** 018 — `018-segment-stems-and-stems-explorer`
**Artifact:** `segment-stem-corrected-arabic.json`
**Status:** `curated_final_approved_with_exceptions`
**Generated (UTC):** `2026-06-28T22:23:49Z`
**Source artifacts:** `segment-stem-curation-candidates.json` (483 candidates) · `segment-stem-curation-review-matrix.json` (40 groups)

This is the **final reviewed mapping** of every secondary STEM segment (the second STEM of a 2-STEM word) to a `quran_stems.id`. It is the curated source a future importer would read to populate `quran_word_morphology_segments.stem_id` for secondary segments. **This task creates the artifact only** — no schema, migration, importer, frontend, backend, or DB changes, and nothing is committed.

---

## 1. Decision applied

- **Scope:** secondary STEM segments only. **Head / word-level stem is separate and UNCHANGED** (`quran_word_morphology.stem_id` is not touched and is not represented here).
- **Approved (479):** each secondary segment maps to its **clean / de-shadda** stem from the review matrix:
  - idghām (shadda) artifact forms → their **de-shadda clean** stem (e.g. مَّا→مَا 5, لَّا→لَا 78, مَّن→مَن 131);
  - already-clean text matches kept (مَا 5, مَآ 90);
  - **circular** cases resolved to the **clean** stem (not the head shadda artifact) — the head stays as-is.
- **Unresolved (4):** the four problematic cases are **intentional exceptions** — `reviewed_stem_id = null`. **No new stems created, no remap performed** for them this feature.

---

## 2. Counts

| Metric | Value |
|---|---:|
| Total secondary candidates accounted for | **483** |
| **Approved mapped rows** (`reviewed_stem_id` set) | **479** |
| **Intentional unresolved exceptions** (`reviewed_stem_id = null`) | **4** |

`479 + 4 = 483` ✓. Every original candidate appears exactly once.

### 2.1 Approved by decision basis

| basis | n |
|---|---:|
| `deshadda_clean` (idghām shadda → clean) | 219 |
| `clean_text_match` (already clean) | 202 |
| `circular_resolved_to_clean` | 58 |
| **total approved** | **479** |

### 2.2 Approved by reviewed stem

| reviewed_stem_id (text) | rows |
|---|---:|
| 5 (مَا) | 322 |
| 90 (مَآ) | 59 |
| 78 (لَا) | 46 |
| 131 (مَن) | 21 |
| 145 (مَنْ) | 16 |
| 627 (مَنِ) | 10 |
| 2307 (لَن) | 2 |
| 4727 (مَ) | 2 |
| 438 (لَآ) | 1 |
| **total** | **479** |

All 479 point to **pre-existing clean** `quran_stems` rows; **no new stem rows** are introduced by this artifact.

---

## 3. The 4 intentional unresolved exceptions

Each has `reviewed_stem_id = null`, `decision_basis = intentional_unresolved_exception`.

| segment_location | word | secondary form | mechanical (rejected) | reason (short) |
|---|---|---|---|---|
| `78:1:1:2` | عَمَّ | مَّ | 17791 (مَّ) | mechanical target is a clitic-only **artifact** stem; no clean remap / no new stem this feature |
| `86:5:3:2` | مِمَّ | مَّ | 17791 (مَّ) | artifact stem **and** circular (= head); deferred |
| `72:16:1:3` | وَأَلَّوِ | لَّوِ | — (no match) | **no clean stem match** (idghām+kasra render of لو before hamza); canonical لو decision deferred |
| `20:94:2:3` | يَبْنَؤُمَّ | ؤُمَّ | 7608 (ؤُمَّ) | only non-function-word secondary; **no de-shadda clean row exists**; artifact + circular; canonical أُمّ deferred |

These remain available for a future curation pass (create/choose canonical لو, أُمّ, and retire the clitic-artifact مَّ rows) — explicitly **out of scope** here.

---

## 4. Validation

| Check | Required | Result |
|---|---|---|
| Every original candidate appears exactly once | yes | 483 unique `segment_location`, 0 dup/missing ✅ |
| Total accounted for | 483 | **483** ✅ |
| Approved rows | 479 | **479**, all with non-null `reviewed_stem_id` ✅ |
| Unresolved rows | 4 | **4**, all `reviewed_stem_id = null` + explicit `reason` ✅ |
| Unresolved set = the 4 named cases | yes | {78:1:1:2, 86:5:3:2, 72:16:1:3, 20:94:2:3} ✅ |
| Approved targets are existing clean stems | yes | 9 distinct ids, all pre-existing; 0 new stems ✅ |
| Head/word stem unchanged | yes | not represented/modified ✅ |

(Assertions enforced in `sql/build_final_artifact.py`; the run prints `VALIDATION: PASS`.)

---

## 5. Artifact shape (`segment-stem-corrected-arabic.json`)

Top-level: `feature`, `artifactType`, `status`, `generatedAtUtc`, `sourceArtifacts`, `decisionPolicy`, `counts`, `decisionSummary` (`byDecision` / `approvedByBasis` / `approvedByReviewedStem`), `unresolvedExceptions` (the 4), and `mappings` (483).

Each `mappings` row:
```
location, quran_word_id, word_text_uthmani, segment_id, segment_location, segment_number,
segment_pos, segment_form_arabic_normalized, segment_lemma_id, segment_lemma_text,
mechanical_candidate_stem_id, candidate_status,
review_decision        ("approved" | "unresolved_exception"),
reviewed_stem_id       (int | null),
reviewed_stem_text     (str | null),
decision_basis         ("deshadda_clean" | "clean_text_match" | "circular_resolved_to_clean" | "intentional_unresolved_exception"),
reason                 (null for approved; text for the 4 exceptions)
```

---

*Final curated artifact only. No code, schema, migration, importer, frontend, backend, or DB-data changes; nothing committed. Approved targets are existing `quran_stems` rows resolved via the read-only de-shadda clean-stem map; the 4 named cases are intentionally left unresolved (`reviewed_stem_id = null`).*
