# Contract — Validation & Rebuild Report Schema

Every `rebuild-words` run emits a **Markdown** report (human review) and a **JSON** report
(machine/CI) capturing totals, derived unique counts, and per-check results (FR-033).
Written by `MarkdownJsonDisplayWordsReportWriter` from a `DisplayWordsRebuildResult`.

## Hard checks (failure ⇒ rollback + `verdict = "fail"`)

| Id | Expected | How observed |
|---|---|---|
| `ORD-COUNT` | each ordered table = `expectedReadableWords` rows (production default 77,432) | `COUNT(*)` per ordered table |
| `ORD-READABLE` | readable words in `quran_words` = `expectedReadableWords` (production default 77,432) | `COUNT(*) WHERE is_ayah_marker = false` |
| `ORD-NO-MARKERS` | 0 ordered rows map to a marker | join ordered → `quran_words` on marker flag |
| `ORD-BIJECTION` | `COUNT(DISTINCT quran_word_id)` = `expectedReadableWords` per ordered table (production default 77,432) | distinct count vs row count vs readable count |
| `ORD-MUSHAF-CONTIG` | MIN=1, MAX=`expectedReadableWords`, DISTINCT=`expectedReadableWords` (production default 77,432) | aggregates on `word_order_in_mushaf` |
| `ORD-SURAH-CONTIG` | per surah MIN=1 and MAX=COUNT(*) | grouped aggregates; no surah violates |
| `ORD-AYAH-CONTIG` | per ayah MIN=1, contiguous, = `word_number` order | grouped aggregates + equality to `word_number` |
| `UNQ-COUNT` | unique rows = `COUNT(DISTINCT text)` over readable words | per-form distinct count vs unique table count |
| `STAT-MATCH` | counts match grouping; `Σ occurrences_count` (unique) = `expectedReadableWords` (production default 77,432) | compare unique stats to grouped readable stats |
| `FIRST-OCC` | each unique `first_*` = earliest `word_order_in_mushaf` of its group | join unique → ordered on display text + min order |
| `SRC-UNTOUCHED` | source row counts unchanged | `quran_words`/`quran_ayahs`/`quran_surahs` counts before vs after |

## Warning checks (never change the verdict)

| Id | Expected (informational) | Note |
|---|---|---|
| `UNQ-EXPECT-TASHKEEL` | ≈ 21,210 | report actual; deviation is a warning to investigate (FR-015) |
| `UNQ-EXPECT-SIMPLE` | ≈ 14,783 | report actual; deviation is a warning |

## JSON report shape

```json
{
  "runAtUtc": "2026-06-09T12:00:00Z",
  "verdict": "pass",
  "persisted": true,
  "forced": false,
  "totals": {
    "orderedTashkeelRows": 77432,
    "orderedSimpleRows": 77432,
    "uniqueTashkeelRows": 21210,
    "uniqueSimpleRows": 14783,
    "readableWords": 77432
  },
  "checks": [
    { "id": "ORD-COUNT", "severity": "hard", "expected": "77432", "observed": "77432", "passed": true }
  ],
  "warnings": [],
  "errors": [],
  "infoNotes": [
    "Unique counts are derived from the database; informational expectations are 21,210 / 14,783."
  ]
}
```

## Markdown report shape

```markdown
# Quran Words Display Tables — Rebuild Report

- Run (UTC): 2026-06-09T12:00:00Z
- Verdict: PASS
- Persisted: true
- Forced: false

## Totals
| Table | Rows |
|---|---|
| quran_words_ordered_tashkeel | 77,432 |
| quran_words_ordered_simple   | 77,432 |
| quran_words_unique_tashkeel  | 21,210 |
| quran_words_unique_simple    | 14,783 |
| readable words (source)      | 77,432 |

## Checks
| Id | Severity | Expected | Observed | Passed |
|----|----------|----------|----------|--------|
| ORD-COUNT | hard | 77432 | 77432 | ✅ |
| ... | | | | |

## Warnings / Errors / Notes
(unique counts are informational, never hard thresholds)
```

## Contract guarantees

- On **failure**: `persisted = false`, `verdict = "fail"`, the failing check(s) listed in
  `errors`, the DB unchanged, the report still written, process exits non-zero.
- On **success**: `persisted = true`, `verdict = "pass"`, all hard checks `passed = true`.
- The report **always** records the actual derived unique counts — never the hardcoded
  informational figures (FR-015).
