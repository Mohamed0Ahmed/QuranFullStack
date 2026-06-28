# Remaining Word-Lemma Shift Diagnostic Report

Feature: 017 — Word-Level Lemma Full Normalization

Date: 2026-06-28

## Summary

- Failed check: `MORPH-WORD-LEMMA-SHIFT-CLEAN`
- Existing report: `resources/report/words-morphology/morphology-import-report.json` and `.md`
- Reported count: 16 unapproved strict previous-word shifts remain
- Existing report detail: count plus first 10 locations only; full row detail was absent
- Diagnostic method: temporary probe dumped the query rows produced by `SelectStrictWordLemmaShiftLocations` during a single `import-morphology` rerun
- Row values are from the attempted import transaction before rollback; final DB persistence remained false
- Full reseed chain: not rerun
- Production code: temporary probe removed before finishing
- Commit: not made

## Verdict

Likely detector bug, not artifact gap.

All 16 rows are detector false positives: each current word has its own head `STEM` segment lemma Buckwalter matching the attempted word-level lemma, and the previous word independently has the same lemma. These are legitimate adjacent repeated lemmas, not shifted lemmas that should be added/removed/replaced in `word-lemma-normalization.json`.

Recommended next action: adjust `MORPH-WORD-LEMMA-SHIFT-CLEAN` so a strict shift requires missing or mismatching current-word segment lemma evidence, while preserving the hard check and the previous-word shift detection.

## Rows

All artifact checks below mean the current location was searched in `word-lemma-normalization.json`.

| # | Current location | Current word id | Current word | Current word-level lemma id/text | Current head segment lemma Buckwalter | Current head POS | Previous location | Previous word id | Previous word | Previous word-level lemma id/text | Previous head segment lemma Buckwalter | Previous head POS | Artifact entry | Classification | Recommended action |
|---:|---|---:|---|---|---|---|---|---:|---|---|---|---|---|---|---|
| 1 | 4:157:21 | 13637 | لَفِى | 6 / فِى | fiY | P | 4:157:20 | 13636 | فِيهِ | 6 / فِى | fiY | P | No; operationKind/decisionStatus/problemClass/correctedLemma/expectedCurrent: n/a | Detector false positive; current and previous words both have own matching head lemma evidence | Adjust detector; no artifact entry |
| 2 | 5:110:10 | 16696 | وَعَلَىٰ | 61 / عَلَىٰ | EalaY&#96; | P | 5:110:9 | 16695 | عَلَيْكَ | 61 / عَلَىٰ | EalaY&#96; | P | No; operationKind/decisionStatus/problemClass/correctedLemma/expectedCurrent: n/a | Detector false positive; current and previous words both have own matching head lemma evidence | Adjust detector; no artifact entry |
| 3 | 9:108:16 | 27173 | فِيهِ | 6 / فِى | fiY | P | 9:108:15 | 27172 | فِيهِ ۚ | 6 / فِى | fiY | P | No; operationKind/decisionStatus/problemClass/correctedLemma/expectedCurrent: n/a | Detector false positive; current and previous words both have own matching head lemma evidence | Adjust detector; no artifact entry |
| 4 | 10:19:16 | 28025 | فِيهِ | 6 / فِى | fiY | P | 10:19:15 | 28024 | فِيمَا | 6 / فِى | fiY | P | No; operationKind/decisionStatus/problemClass/correctedLemma/expectedCurrent: n/a | Detector false positive; current and previous words both have own matching head lemma evidence | Adjust detector; no artifact entry |
| 5 | 11:48:8 | 30437 | وَعَلَىٰٓ | 61 / عَلَىٰ | EalaY&#96; | P | 11:48:7 | 30436 | عَلَيْكَ | 61 / عَلَىٰ | EalaY&#96; | P | No; operationKind/decisionStatus/problemClass/correctedLemma/expectedCurrent: n/a | Detector false positive; current and previous words both have own matching head lemma evidence | Adjust detector; no artifact entry |
| 6 | 12:6:11 | 31699 | وَعَلَىٰٓ | 61 / عَلَىٰ | EalaY&#96; | P | 12:6:10 | 31698 | عَلَيْكَ | 61 / عَلَىٰ | EalaY&#96; | P | No; operationKind/decisionStatus/problemClass/correctedLemma/expectedCurrent: n/a | Detector false positive; current and previous words both have own matching head lemma evidence | Adjust detector; no artifact entry |
| 7 | 12:38:20 | 32258 | وَعَلَى | 61 / عَلَىٰ | EalaY&#96; | P | 12:38:19 | 32257 | عَلَيْنَا | 61 / عَلَىٰ | EalaY&#96; | P | No; operationKind/decisionStatus/problemClass/correctedLemma/expectedCurrent: n/a | Detector false positive; current and previous words both have own matching head lemma evidence | Adjust detector; no artifact entry |
| 8 | 17:20:4 | 38313 | وَهَـٰٓؤُلَآءِ | 357 / هَٰذَا | ha&#96;*aA | DEM | 17:20:3 | 38312 | هَـٰٓؤُلَآءِ | 357 / هَٰذَا | ha&#96;*aA | DEM | No; operationKind/decisionStatus/problemClass/correctedLemma/expectedCurrent: n/a | Detector false positive; current and previous words both have own matching head lemma evidence | Adjust detector; no artifact entry |
| 9 | 23:22:2 | 46721 | وَعَلَى | 61 / عَلَىٰ | EalaY&#96; | P | 23:22:1 | 46720 | وَعَلَيْهَا | 61 / عَلَىٰ | EalaY&#96; | P | No; operationKind/decisionStatus/problemClass/correctedLemma/expectedCurrent: n/a | Detector false positive; current and previous words both have own matching head lemma evidence | Adjust detector; no artifact entry |
| 10 | 23:36:2 | 46919 | هَيْهَاتَ | 8487 / هَيْهَات | hayohaAt | N | 23:36:1 | 46918 | ۞ هَيْهَاتَ | 8487 / هَيْهَات | hayohaAt | N | No; operationKind/decisionStatus/problemClass/correctedLemma/expectedCurrent: n/a | Detector false positive; current and previous words both have own matching head lemma evidence | Adjust detector; no artifact entry |
| 11 | 27:19:14 | 51842 | وَعَلَىٰ | 61 / عَلَىٰ | EalaY&#96; | P | 27:19:13 | 51841 | عَلَىَّ | 61 / عَلَىٰ | EalaY&#96; | P | No; operationKind/decisionStatus/problemClass/correctedLemma/expectedCurrent: n/a | Detector false positive; current and previous words both have own matching head lemma evidence | Adjust detector; no artifact entry |
| 12 | 37:113:3 | 61865 | وَعَلَىٰٓ | 61 / عَلَىٰ | EalaY&#96; | P | 37:113:2 | 61864 | عَلَيْهِ | 61 / عَلَىٰ | EalaY&#96; | P | No; operationKind/decisionStatus/problemClass/correctedLemma/expectedCurrent: n/a | Detector false positive; current and previous words both have own matching head lemma evidence | Adjust detector; no artifact entry |
| 13 | 39:65:4 | 64115 | وَإِلَى | 638 / إِلَىٰ | &lt;ilaY&#96; | P | 39:65:3 | 64114 | إِلَيْكَ | 638 / إِلَىٰ | &lt;ilaY&#96; | P | No; operationKind/decisionStatus/problemClass/correctedLemma/expectedCurrent: n/a | Detector false positive; current and previous words both have own matching head lemma evidence | Adjust detector; no artifact entry |
| 14 | 40:80:10 | 65522 | وَعَلَى | 61 / عَلَىٰ | EalaY&#96; | P | 40:80:9 | 65521 | وَعَلَيْهَا | 61 / عَلَىٰ | EalaY&#96; | P | No; operationKind/decisionStatus/problemClass/correctedLemma/expectedCurrent: n/a | Detector false positive; current and previous words both have own matching head lemma evidence | Adjust detector; no artifact entry |
| 15 | 42:3:4 | 66461 | وَإِلَى | 638 / إِلَىٰ | &lt;ilaY&#96; | P | 42:3:3 | 66460 | إِلَيْكَ | 638 / إِلَىٰ | &lt;ilaY&#96; | P | No; operationKind/decisionStatus/problemClass/correctedLemma/expectedCurrent: n/a | Detector false positive; current and previous words both have own matching head lemma evidence | Adjust detector; no artifact entry |
| 16 | 46:15:30 | 69481 | وَعَلَىٰ | 61 / عَلَىٰ | EalaY&#96; | P | 46:15:29 | 69480 | عَلَىَّ | 61 / عَلَىٰ | EalaY&#96; | P | No; operationKind/decisionStatus/problemClass/correctedLemma/expectedCurrent: n/a | Detector false positive; current and previous words both have own matching head lemma evidence | Adjust detector; no artifact entry |

## Classification

- True remaining shifts: 0
- Detector false positives: 16
- Current-location artifact entries found: 0
- Recommended artifact changes now: none
- Recommended detector change later: keep `MORPH-WORD-LEMMA-SHIFT-CLEAN` hard, but exclude rows where the current word's own head `STEM` segment lemma Buckwalter already equals the current word-level lemma Buckwalter.

## Cleanup Confirmation

- Temporary production-code probe was removed.
- Generated probe TSV was removed.
- `word-lemma-normalization.json` was not updated.
- `MORPH-WORD-LEMMA-SHIFT-CLEAN` was not changed or weakened.
- No final acceptance rerun was performed.
- No commit was made.
