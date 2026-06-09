# Contract — Validation Report (Markdown + JSON)

Every import run (success or failure) writes **two** files to a configurable output path (default: `resources/report/`):

- `quran-foundation-import-report.md` — human-readable.
- `quran-foundation-import-report.json` — machine-readable (shape below).

## JSON shape

```json
{
  "runAt": "2026-06-08T10:30:00Z",
  "sourceRoot": "resources/import-sources/quran-foundation",
  "manifestVersion": "1",
  "verdict": "pass",                      // "pass" | "pass-with-warnings" | "fail"
  "persisted": true,                      // true only if verdict != "fail"
  "forced": false,                        // whether --force was used
  "totals": {
    "surahs": 114, "ayahs": 6236, "pages": 604, "lines": 9046,
    "words": 83668, "ayahMarkers": 6236, "readableWords": 77432
  },
  "checks": [
    { "id": "surah-count",        "severity": "hard",    "expected": 114,   "observed": 114,   "passed": true },
    { "id": "ayah-count",         "severity": "hard",    "expected": 6236,  "observed": 6236,  "passed": true },
    { "id": "page-count",         "severity": "hard",    "expected": 604,   "observed": 604,   "passed": true },
    { "id": "line-count",         "severity": "hard",    "expected": 9046,  "observed": 9046,  "passed": true },
    { "id": "word-count",         "severity": "hard",    "expected": 83668, "observed": 83668, "passed": true },
    { "id": "marker-count",       "severity": "hard",    "expected": 6236,  "observed": 6236,  "passed": true },
    { "id": "readable-count",     "severity": "hard",    "expected": 77432, "observed": 77432, "passed": true },
    { "id": "duplicate-location", "severity": "hard",    "expected": 0,     "observed": 0,     "passed": true },
    { "id": "id-contiguous",      "severity": "hard",    "expected": "1..83668", "observed": "1..83668", "passed": true },
    { "id": "source-alignment",   "severity": "hard",    "expected": 0,     "observed": 0,     "passed": true },
    { "id": "layout-coverage",    "severity": "hard",    "expected": "1..83668 no gaps", "observed": "1..83668 no gaps", "passed": true },
    { "id": "word-page-line",     "severity": "hard",    "expected": "all", "observed": "all", "passed": true },
    { "id": "line-word-refs",     "severity": "hard",    "expected": "8820/8820", "observed": "8820/8820", "passed": true },
    { "id": "bismillah-basmallah","severity": "hard",    "expected": "112==112", "observed": "112==112", "passed": true },
    { "id": "denorm-page-line",   "severity": "hard",    "expected": "match", "observed": "match", "passed": true },
    { "id": "page-reconstruct",   "severity": "hard",    "expected": "1,2,5,604 ok", "observed": "1,2,5,604 ok", "passed": true },
    { "id": "ayah-37-130-count",  "severity": "warning", "expected": "source 4 / real 3", "observed": "source 4 / real 3", "passed": true }
  ],
  "warnings": [
    "Ayah 37:130 word-count differs (metadata 4 vs records 3); word records treated as canonical."
  ],
  "errors": []
}
```

## Rules

- `verdict = "fail"` if **any** `severity:"hard"` check has `passed:false`. In that case `persisted:false` and `errors[]` lists each failed check with expected vs observed.
- `verdict = "pass-with-warnings"` if all hard checks pass and ≥1 warning exists (the `37:130` warning is the expected one for a valid source set).
- `verdict = "pass"` only if all hard checks pass and there are no warnings (not expected with the current real data, which always carries the `37:130` warning).
- The `checks[]` list MUST include every rule in spec **FR-018** (ids above are canonical).
- The Markdown report presents the same content as readable tables + the verdict.
