# Contract — Validation Checks & Import Report Schema

Two report files are written per run (Markdown + JSON) to the report output directory
(default `Backend/report/feature-009-quran-navigation-metadata-foundation/`). A run is accepted **only** if
all hard checks pass **and** both reports are written. Check ids are stable strings (see
`NavigationMetadataInvariants` in [navigation-abstractions.md](./navigation-abstractions.md)).

## Hard checks (any failure ⇒ rollback, nothing persisted)

| Id | Asserts | Maps to spec |
|---|---|---|
| `NAV-PACKAGE-SHAPE` | Package root has `manifest.json` and exactly the four `sources/*.json`; no missing/extra files. | FR-011 |
| `NAV-MANIFEST-FINAL` | `packageType = "quran-navigation-metadata-import-source-package"` and `isFinalImportManifest = true`. | FR-011 |
| `NAV-SOURCE-COUNT` | Record counts exactly juz=30, hizb=60, rub=240, sajda=15. | FR-002, SC-002 |
| `NAV-SOURCE-HASH` | Each file's `sha256` and `sizeBytes` match `manifest.json`. | FR-011, SC-005 |
| `NAV-JSON-SHAPE` | Required fields present per dataset (juz/hizb/rub: `*_number, verses_count, first_verse_key, last_verse_key, verse_mapping`; sajda: `sajdah_number, verse_key, sajdah_type`). | FR-012 |
| `NAV-VERSE-KEYS-RESOLVE` | Every `first_verse_key`, `last_verse_key`, sajda `verse_key`, and every expanded mapping key resolves to a `quran_ayahs` row. | FR-013, SC-004 |
| `NAV-RANGE-COVERAGE-JUZ` | Juz `verse_mapping` covers all 6,236 ayahs exactly once. | FR-014, SC-003 |
| `NAV-RANGE-COVERAGE-HIZB` | Hizb `verse_mapping` covers all 6,236 ayahs exactly once. | FR-014, SC-003 |
| `NAV-RANGE-COVERAGE-RUB` | Rub `verse_mapping` covers all 6,236 ayahs exactly once. | FR-014, SC-003 |
| `NAV-NO-RANGE-GAPS-OVERLAPS` | No gaps or overlaps within each division type. | FR-014 |
| `NAV-HIERARCHY` | Each hizb ⊂ exactly one juz; each rub ⊂ exactly one hizb. | FR-015, SC-010 |
| `NAV-SAJDA-TYPE` | `sajdah_type` ∈ {`required`,`optional`} only. | FR-004, FR-012 |
| `NAV-AYAH-COLUMNS-COMPLETE` | After import, all 6,236 ayahs have non-null `juz_number`/`hizb_number`/`rub_number`. | FR-016, SC-001 |
| `NAV-NO-QURAN-TEXT-COPY` | Importer never reads or persists Quran ayah text from the sources. | FR-008, SC-007 |
| `NAV-SOURCE-UNCHANGED` | Source files unchanged (sha256/size) between load and commit. | FR-018 |
| `NAV-REPORT-WRITTEN` | Both report files written before acceptance. | FR-023 |
| `NAV-ROLLBACK-ON-FAIL` | On any hard failure (incl. forced reload mid-failure), full rollback to a single consistent state. | FR-017, FR-020 |
| `NAV-RERUN-GUARD` | Non-empty navigation target refused without `--force`. | FR-019, SC-006 |

## Warning checks (non-blocking — reported, do not fail)

| Id | Asserts | Maps to spec |
|---|---|---|
| `NAV-VERSE-COUNT-MATCH` | Division source `verses_count` equals the stored computed range count; on mismatch, warn and carry the source value (stored value stays the computed count). | FR-025 |
| `NAV-SAJDA-DISTRIBUTION` | Sajda type split equals 11 optional / 4 required. | FR-025 |

## JSON report shape

```jsonc
{
  "feature": "009-quran-navigation-metadata-foundation",
  "verdict": "accepted" | "refused" | "validation-failed" | "report-write-failed",
  "persisted": true,                 // true only when data committed
  "forced": false,                   // whether --force was used
  "runAtUtc": "2026-06-16T08:31:05Z",
  "sourcePath": "<resolved package root>",
  "manifest": {
    "packageType": "quran-navigation-metadata-import-source-package",
    "isFinalImportManifest": true
  },
  "totals": { "juz": 30, "hizb": 60, "rub": 240, "sajda": 15, "ayahsTagged": 6236 },
  "ayahCoverage": {
    "totalAyahs": 6236,
    "withJuz": 6236, "withHizb": 6236, "withRub": 6236,
    "complete": true
  },
  "checks": [
    { "id": "NAV-SOURCE-COUNT", "passed": true,  "expected": "30/60/240/15", "observed": "30/60/240/15" },
    { "id": "NAV-RANGE-COVERAGE-JUZ", "passed": true, "expected": "6236 once", "observed": "6236 once" }
    // … one entry per hard check
  ],
  "warnings": [
    // { "id": "NAV-SAJDA-DISTRIBUTION", "message": "optional=11, required=4" }  (info-level when matching)
  ],
  "errors": [],                      // first actionable failure(s) when not accepted
  "noQuranAyahTextReadOrStored": true
}
```

## Markdown report

Human-readable rendering of the same data: a verdict line, a `persisted`/`forced`/`source path` block, a
per-dataset totals table, an ayah-coverage table, a per-check pass/fail table, a warnings/errors list, and a
closing explicit statement: **"No Quran ayah text was read or stored by this import."**

## Acceptance rule

`accepted` (exit 0) requires: all hard checks `passed = true`, `NAV-SOURCE-UNCHANGED` true at commit, both
reports written, and `ayahCoverage.complete = true`. Any other outcome ⇒ non-zero exit, `persisted = false`,
and no partial data.
