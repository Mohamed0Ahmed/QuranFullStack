# Contract — Validation & Import Report Schema

Every `import-mutashabihat` run that starts emits a **Markdown** report (human review) and a **JSON**
report (machine/CI) capturing the written counts, the raw source occurrence count, every hard-check
result, every warning count, every informational figure, and the final outcome (FR-032). Written by
`MarkdownJsonMutashabihatReportWriter` from a `MutashabihatImportResult`. Early refusals (missing file,
checksum/size mismatch, non-empty targets without `--force`, missing/empty `quran_ayahs`) write **no**
report artifact and report only to the console.

## Hard checks (failure ⇒ rollback + `verdict = "fail"` + non-zero exit)

| Id | Expected | How observed |
|---|---|---|
| `MUT-MANIFEST-SET` | staged file set exactly `{mutashabihat-ul-quran/phrases.json, similar-ayahs/matching-ayah.json}` (+ `manifest.json`, `README.md`) | directory scan vs. manifest |
| `MUT-MANIFEST-CHECKSUM` | each source file `sha256` + byte size match the manifest | digest + size compare |
| `MUT-JSON-SHAPE` | both roots are objects; group values carry `{source, ayah}`; each similar item carries `{matched_ayah_key, score, coverage, matched_words_count, match_words}` | shape probe during parse |
| `MUT-GROUP-COUNT` | groups = `expected.Groups` (814) | `COUNT(*)` vs expected |
| `MUT-RAW-OCCURRENCE-COUNT` | raw occurrence entries in `phrases.json` = 3,558 | count during assembly |
| `MUT-STORED-OCCURRENCE-COUNT` | stored unique occurrences after dedupe = 3,557 | `COUNT(*)` vs expected |
| `MUT-SIMILAR-SOURCE-COUNT` | distinct source ayahs = `expected.SimilarSources` (1,162) | `COUNT(DISTINCT source_ayah_id)` |
| `MUT-SIMILAR-LINK-COUNT` | directed links = 3,552 | `COUNT(*)` vs expected |
| `MUT-VERSEKEY-FORMAT` | every reference matches `^\d+:\d+$` | regex scan during assembly |
| `MUT-AYAH-RESOLVE` | every referenced verse_key resolves to a `quran_ayahs` row; **0** unresolved | anti-join / map miss count |
| `MUT-WORD-RANGE-SHAPE` | every occurrence range and every `match_words` range has `from ≥ 1`, `to ≥ from` | predicate scan |
| `MUT-GROUP-MIN-SIZE` | every group has `distinct_ayah_count ≥ 2` | grouped count |
| `MUT-LINK-NO-SELF` | no link has `target_ayah_id = source_ayah_id` (0) | CHECK + count |
| `MUT-SCORE-RANGE` | every link `score` ∈ [50, 100] | min/max scan |
| `MUT-OCCURRENCE-UNIQUE` | occurrences unique on (`group_id`, `ayah_id`, `word_from`, `word_to`) | UNIQUE constraint + count |
| `MUT-SOURCE-UNCHANGED` | source files match `manifest.json` size/`sha256` after assembly, before commit | digest compare pre/post |

## Warning checks (recorded; never change the verdict, never gate)

| Id | Expected (informational) | Note |
|---|---|---|
| `MUT-COVERAGE-GT-100` | 4 | links with `coverage > 100`; stored **raw**, not clamped (e.g. 56:27 → 56:38 coverage 200) |
| `MUT-DUPLICATE-OCCURRENCE` | 1 | identical occurrence range collapsed by the unique constraint (group 75, ayah 16:28) |
| `MUT-SOURCE-KEY-ABSENT` | 1 | group whose `source.key` is absent from its own occurrences (group 1782, 3:28); group kept, zero representative occurrence rows, group-level representative fields still populated |
| `MUT-STALE-SOURCE-COUNTERS` | count | groups whose source `surahs`/`ayahs`/`count` disagreed with recomputed values (recomputed values win; diffs reported) |
| `MUT-WORD-RANGE-UPPER-BOUND` | count | word ranges whose upper index exceeds the ayah's `quran_ayahs.words_count_real` (possible alignment mismatch; stored unchanged) |
| `MUT-PROVENANCE-LICENSE-UNKNOWN` | 2 source files | source provenance/license unknown in the manifest; never gates v1, blocks future publishing |

## Informational checks (recorded; never gate)

| Id | Expected | Note |
|---|---|---|
| `MUT-ONEWAY-LINKS` | ≈ 1,120 | directed links with no stored reverse (expected from top-N / threshold pruning) |
| `MUT-CROSS-DATASET-OVERLAP` | ≈ 792 ayahs / 813 pairs | ayahs and undirected pairs shared by both datasets |
| `MUT-SURAH-COVERAGE` | 109 / 114 surahs; 3,084 distinct ayahs | reference coverage across the Mushaf |
| `MUT-PHRASE-VERSES-CONSISTENCY` *(optional)* | consistent | if `phrase_verses.json` is supplied, confirm it is a consistent reverse index of `phrases.json`; never stored |

## JSON report shape

```json
{
  "runAtUtc": "2026-06-13T12:00:00Z",
  "verdict": "pass",
  "persisted": true,
  "forced": false,
  "totals": {
    "groupRows": 814,
    "rawOccurrenceEntries": 3558,
    "storedOccurrenceRows": 3557,
    "linkRows": 3552,
    "distinctSimilarSources": 1162,
    "distinctReferencedAyahs": 3084
  },
  "checks": [
    { "id": "MUT-AYAH-RESOLVE", "severity": "hard", "expected": "0 unresolved (3084 distinct refs)", "observed": "0 unresolved", "passed": true },
    { "id": "MUT-STORED-OCCURRENCE-COUNT", "severity": "hard", "expected": "3557", "observed": "3557", "passed": true },
    { "id": "MUT-COVERAGE-GT-100", "severity": "warning", "expected": "4", "observed": "4", "passed": true }
  ],
  "warnings": [
    "MUT-COVERAGE-GT-100: 4 links with coverage > 100 stored raw (e.g. 56:27 → 56:38 = 200).",
    "MUT-DUPLICATE-OCCURRENCE: 1 identical occurrence collapsed (group 75, ayah 16:28).",
    "MUT-SOURCE-KEY-ABSENT: 1 group whose source.key is absent from its occurrences (group 1782, 3:28).",
    "MUT-PROVENANCE-LICENSE-UNKNOWN: provenance/license unknown for 2 source files (blocks future publishing)."
  ],
  "errors": [],
  "infoNotes": [
    "MUT-ONEWAY-LINKS: ~1120 directed links have no stored reverse.",
    "MUT-CROSS-DATASET-OVERLAP: ~792 ayahs / 813 pairs shared by both datasets.",
    "MUT-SURAH-COVERAGE: 109/114 surahs; 3084 distinct ayahs referenced."
  ]
}
```

## Markdown report shape

```markdown
# Quran Mutashabihat — Import Report

- Run (UTC): 2026-06-13T12:00:00Z
- Verdict: PASS
- Persisted: true
- Forced: false

## Totals
| Metric | Value |
|---|---|
| quran_mutashabihat_groups       | 814 |
| quran_mutashabihat_occurrences  | 3,557 (stored unique) |
| raw source occurrence entries   | 3,558 |
| quran_similar_ayah_links        | 3,552 |
| distinct similar-ayah sources   | 1,162 |
| distinct referenced ayahs       | 3,084 |

## Hard checks
| Id | Severity | Expected | Observed | Passed |
|----|----------|----------|----------|--------|
| MUT-AYAH-RESOLVE          | hard | 0 unresolved | 0 unresolved | ✅ |
| MUT-STORED-OCCURRENCE-COUNT | hard | 3557 | 3557 | ✅ |
| ... | | | | |

## Warnings (recorded, never block)
| Id | Count | Note |
|----|-------|------|
| MUT-COVERAGE-GT-100      | 4 | stored raw |
| MUT-DUPLICATE-OCCURRENCE | 1 | group 75, ayah 16:28 |
| MUT-SOURCE-KEY-ABSENT    | 1 | group 1782, 3:28 |
| MUT-STALE-SOURCE-COUNTERS | <n> | recomputed values win |
| MUT-WORD-RANGE-UPPER-BOUND | <n> | stored unchanged |
| MUT-PROVENANCE-LICENSE-UNKNOWN | 2 | blocks future publishing |

## Informational
- MUT-ONEWAY-LINKS: ~1,120
- MUT-CROSS-DATASET-OVERLAP: ~792 ayahs / 813 pairs
- MUT-SURAH-COVERAGE: 109/114 surahs; 3,084 distinct ayahs
```

## Contract guarantees

- On **failure** (a hard check fails): `persisted = false`, `verdict = "fail"`, the failing check(s) listed
  in `errors`, the DB unchanged (all three tables empty / pre-run), the report still written, process exits
  non-zero.
- On **success**: `persisted = true`, `verdict = "pass"`, all hard checks `passed = true`.
- The report **always** records the written counts (814 / 3,557 / 3,552 / 1,162), the raw source
  occurrence count (3,558), every warning count, every informational figure, and confirms
  `MUT-SOURCE-UNCHANGED` (the local source files were not written).
- **No Quran ayah text** appears anywhere in the report — counts, ids, and check results only.
