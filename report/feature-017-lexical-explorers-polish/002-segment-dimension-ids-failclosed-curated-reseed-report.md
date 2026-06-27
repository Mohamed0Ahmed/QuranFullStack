# Segment Dimension IDs — Fail-Closed + Curated Disambiguation Reseed Report

**Feature:** 017 Lexical Explorers Polish (prerequisite)
**Date:** 2026-06-27
**Scope:** Phase 6 review fixes + local clean reset/reseed re-verification
**Supersedes:** the lowest-id tie-break behavior described in `001-segment-dimension-ids-phase5-verification-report.md` §4 fix #4.

---

## 1. What changed (review fixes)

Phase 6 engineering review found the resolver used a **silent lowest-id lemma tie-break**
for ambiguous duplicate-buckwalter multi-STEM segments. That violated the implementation
plan §3 (fail-closed on unsafe tie-break) and the review brief. Decision: **fail-closed**.

- `MorphologyAssembler.ResolveLemmaId`
  - Removed the `candidates.MinBy(c => c.AssignedId)` lowest-id fallback.
  - Ambiguous (multiple candidates, no safe Arabic-form match) now records
    `SEG-LEMMA-ID-NO-FANOUT` and returns null → import fails + rolls back.
  - Added a **curated, source-traceable disambiguation map** for genuine homographs that
    cannot be resolved by form normalization:
    - `('ACC', '>an~') -> 'أَنّ'`
  - Anything not in the map still fails closed (no guessing).
- `MorphologyImportReportBuilder` — added `MORPH-SEG-DIM-ISSUES` warning that lists each
  segment-dimension resolver issue (location + check id + message) for traceability.
- Tests
  - Reverted the two tests that had been flipped to bless lowest-id back to fail-closed
    (`Unsafe_duplicate_multi_stem_lemma_is_reported_without_lowest_id_tiebreak`,
    `Import_fails_when_duplicate_multi_stem_lemma_has_no_safe_form_match`).
  - Added `Ambiguous_acc_anna_segment_resolves_via_curated_disambiguation_map`.
- Removed unrelated feature-008/009 import-report drift from the change set.

## 2. Why the curated map (the 10 real cases)

The first fail-closed reseed correctly **failed** on **10 segments**, all the **first STEM
segment** of `أَنَّمَا / أَلَّا`-family multi-STEM words:

`5:49:22:1, 8:28:2:1, 8:41:2:1, 11:14:5:1, 18:110:8:1, 20:89:3:1, 21:108:5:1, 23:115:2:1, 38:70:5:1, 41:6:8:1`

All are POS `ACC`, lemma_buckwalter `>an~`. Buckwalter `>an~` is shared by two lemma rows
(`أَنّ` and `إِنّ`); the rendered segment form is always the accusative أَنَّ, so the
correct lemma is **أَنّ**. Form normalization alone could not match it (diacritic/shadda
differences), so a documented curated rule resolves it. The old lowest-id code silently
guessed these 10.

## 3. Reset / reseed sequence (clean)

| Step | Command | Result |
| --- | --- | --- |
| 1 | `./scripts/reset-db --yes` | DB dropped + all migrations applied incl. `20260627144247_AddSegmentDimensionIds` |
| 2 | `import-foundation --source .../quran-foundation` | surahs=114, ayahs=6236, pages=604, lines=9046, words=83668 |
| 3 | `rebuild-words --force` | ordered=77432, unique_tashkeel=21294, unique_simple=14783 |
| 4 | `import-morphology --force` | **PASS** — verdict=pass, persisted=true |
| 5 | `generate-i3rab --force` | 128,219 segments approved |

## 4. Hard checks (all green)

| Check | Observed |
| --- | --- |
| `SEG-LEMMA-ID-STEM-ONLY` | 0 violations |
| `SEG-LEMMA-ID-SINGLE-STEM-HEAD-CONSISTENT` | 0 violations |
| `SEG-LEMMA-ID-NO-FANOUT` | 0 violations |
| `SEG-LEMMA-ID-MULTI-STEM-RESOLVES` | 0 violations |
| `SEG-LEMMA-ID-REQUIRED-FOR-STEM` | 0 violations |
| `SEG-ROOT-ID-RESOLVES` | 0 violations |
| `SEG-ROOT-ID-CONSISTENT` | 0 violations |
| `SEG-DIM-NULL-SAFE` | 0 violations |
| `SEG-STEM-ID-ABSENT` | absent |
| `MORPH-SOURCE-UNCHANGED` | unchanged |

## 5. Verification SQL (read-only)

- 10 curated segments: all resolve to `lemma_text = أَنّ` (POS ACC, bw `>an~`).
- Populated: `lemma_id` set on **72,990** STEM segments; `root_id` set on **49,968**;
  total segments **128,219**.
- Bug surface now addressable by `segment.lemma_id + segment.pos`: لا (`laA`) STEM-segment
  POS distribution = **NEG 1406, PRO 332** only — matches the target in
  `segment-dimension-ids-db-verification-report.md` §7.2; the prior spurious SUB/INT/ACC
  labels are eliminated.

## 6. Tests

| Suite | Result |
| --- | --- |
| `dotnet build QuranDashboard.sln` | pass (0 warn, 0 err) |
| `QuranDashboard.Tests.Quran.WordsMorphology` | **277** passed |
| `QuranDashboard.Tests.Quran.WordsSimpleI3rab` | **106** passed |

## 7. Verdict

**PASS** — fail-closed resolver with a documented curated homograph map. Clean reseed,
morphology import, and simple i'rab generation all succeed; segment `lemma_id`/`root_id`
populated correctly; all `SEG-*` hard checks green on the live corpus; no silent lemma
guessing. The Lemma Details Option A reader fix (`segment.lemma_id + segment.pos`) can
proceed as a separate task.

## 8. Out of scope (unchanged)

- No Lemma Details reader fix. No frontend / Stems / POS / i'rab label changes.
- No Quran source / morphology source / QUL mutation.
- No `segment.stem_id`.

*Read-only verification SELECTs only; DB password sourced from importer user-secrets,
never stored or printed.*
