# Contract — Validation & Import Report Schema

Every `import-morphology` run that starts emits a **Markdown** report (human review) and a **JSON** report
(machine/CI) capturing totals, derived dimension counts, the render-tier distribution, and per-check
results (FR-030). Written by `MarkdownJsonMorphologyReportWriter` from a `MorphologyImportResult`.

## Hard checks (failure ⇒ rollback + `verdict = "fail"`)

| Id | Expected | How observed |
|---|---|---|
| `MORPH-READABLE-COMPLETE` | morphology rows = `expectedReadableWords` (77,432); one per readable word | `COUNT(*)` vs readable count |
| `MORPH-MARKERS-EXCLUDED` | 0 morphology/segment rows map to a marker | join → `quran_words` on `is_ayah_marker` |
| `MORPH-LOCATION-MATCH` | every morphology `location` matches `quran_words.location`; 0 unmatched | anti-join both directions |
| `MORPH-SEGMENTS-PRESENT` | every word ≥ 1 segment; `segment_count` = segment-row count | grouped count vs `segment_count` |
| `MORPH-POS-PRESENT` | every segment has `pos`; every word has at least one STEM; `head_pos` = first STEM POS by `segment_number` | null check + first-STEM check per word |
| `MORPH-POS-RESOLVES` | every `head_pos` + segment `pos` ∈ `quran_pos_tags.code` (0 unknown) | anti-join to `quran_pos_tags` |
| `MORPH-VERB-FEATURE-CONSISTENCY` | head verbs: exactly one tense + valid voice; non-verbs: null word-level verb fields | first-STEM predicate checks |
| `MORPH-DIMENSION-RESOLVES` | every non-null `root_id`/`lemma_id`/`stem_id` resolves (no dangling) | anti-join to each dimension |
| `MORPH-SEG-CHARSET` | every `form` character ∈ QAC map; **0 unmapped**; space allowed only for `multiword` tier | charset scan during assembly (refuse on any) |
| `MORPH-SEG-RENDER-TOTAL` | non-empty form → non-empty render; empty form → `NULL` (expected 208 nulls) | null/non-null counts vs form emptiness |
| `MORPH-SEG-TIER-VALID` | every rendered row has a valid tier; `arabic_render_source` = constant on all rows | enum + constant check |
| `MORPH-SEG-RENDER-PROVENANCE` | rendered rows retain non-empty `form_buckwalter`, source = `buckwalter-transliteration`, and Arabic/tier match deterministic renderer output | recompute from `form_buckwalter` |
| `MORPH-SOURCE-UNCHANGED` | local source files match `manifest.json` size/`sha256` before & after | digest compare pre/post run |

## Warning checks (never change the verdict)

| Id | Expected (informational) | Note |
|---|---|---|
| `MORPH-SEG-WORD-AGREEMENT` | ≈ 79.83 % | per-word translit vs `qpcUthmani` exact-match rate; deviation = encoding-drift canary |
| `MORPH-SEG-TIER-DIST` | ≈ 94.2 % / 5.4 % / 0.4 % / 1 | render-tier distribution; deviation → investigate |
| `MORPH-SEG-REVIEW-LIST` | full lists | emit all `review` + `multiword` + empty (208) rows for manual sign-off |
| `MORPH-MULTI-STEM-LIST` | full multi-STEM summary | emit count, POS-pair distribution, examples, and reference the full multi-STEM report when available |
| `MORPH-DIM-COUNTS` | report actual | distinct root/lemma/stem counts (derived, never hardcoded) |

## JSON report shape

```json
{
  "runAtUtc": "2026-06-10T12:00:00Z",
  "verdict": "pass",
  "persisted": true,
  "forced": false,
  "totals": {
    "morphologyRows": 77432,
    "segmentRows": 128219,
    "rootRows": 0,
    "lemmaRows": 0,
    "stemRows": 0,
    "posTagRows": 30,
    "readableWords": 77432,
    "emptyFormRenders": 208,
    "renderTierCounts": { "clean": 0, "quranic_marks": 0, "review": 0, "multiword": 1 }
  },
  "checks": [
    { "id": "MORPH-SEG-CHARSET", "severity": "hard", "expected": "0 unmapped characters; space allowed only for multiword-tier forms", "observed": "0 unmapped", "passed": true }
  ],
  "warnings": [
    "MORPH-SEG-WORD-AGREEMENT: whole-word agreement ≈ 79.83% (informational)."
  ],
  "errors": [],
  "infoNotes": [
    "Dimension counts (roots/lemmas/stems) are derived from the data and reported, not hardcoded."
  ]
}
```

(Dimension `rootRows`/`lemmaRows`/`stemRows` are reported with their actual derived values at run time;
`0` above is a placeholder in this illustrative shape.)

## Markdown report shape

```markdown
# Quran Word Morphology — Import Report

- Run (UTC): 2026-06-10T12:00:00Z
- Verdict: PASS
- Persisted: true
- Forced: false

## Totals
| Table | Rows |
|---|---|
| quran_word_morphology          | 77,432 |
| quran_word_morphology_segments | 128,219 |
| quran_roots                    | <derived> |
| quran_lemmas                   | <derived> |
| quran_stems                    | <derived> |
| quran_pos_tags                 | 30 |
| readable words (source)        | 77,432 |

## Render tiers
| Tier | Count |
|---|---|
| clean | <n> | quranic_marks | <n> | review | <n> | multiword | 1 |
(empty-form renders → NULL: 208)

## Checks
| Id | Severity | Expected | Observed | Passed |
|----|----------|----------|----------|--------|
| MORPH-READABLE-COMPLETE | hard | 77432 | 77432 | ✅ |
| ... | | | | |

## Warnings / Errors / Notes
(whole-word agreement, tier distribution, and dimension counts are informational, never hard thresholds)
```

## Contract guarantees

- On **failure**: `persisted = false`, `verdict = "fail"`, the failing check(s) listed in `errors`, the
  DB unchanged, the report still written, process exits non-zero.
- On **success**: `persisted = true`, `verdict = "pass"`, all hard checks `passed = true`.
- The report **always** records the actual derived dimension counts and tier distribution — never
  hardcoded figures — and confirms `MORPH-SOURCE-UNCHANGED` (the local source files were not written).
