# Contract — Validation & Import Report Schema

Every accepted `import-tafsirs` run must write both:

- `tafsir-import-report.md`
- `tafsir-import-report.json`

Default output directory:

```text
resources/report/quran-tafsirs/
```

## Final manifest contract

`TAFSIR-MANIFEST-FINAL` passes only when the package manifest contains these exact top-level values:

```json
{
  "manifestType": "quran-tafsir-import-source-package",
  "isFinalImportManifest": true,
  "sourceRoot": "sources",
  "sourceCount": 84
}
```

The same manifest must also contain these locked summary values:

```json
{
  "summary": {
    "copiedApprovedTafsirSources": 84,
    "excludedSources": 9,
    "arabicApprovedCopied": 35,
    "nonArabicApprovedCopied": 49,
    "languageCount": 33
  },
  "selectionRules": {
    "status": "approved_candidate",
    "includeInFutureImport": true,
    "contentCoverageCount": 6236,
    "resourceKind": "tafsir"
  }
}
```

Do not infer final-manifest status from filename or folder location alone.

## Hard checks

| ID | Expected |
|---|---|
| `TAFSIR-PACKAGE-SHAPE` | Required package files/folders exist |
| `TAFSIR-MANIFEST-FINAL` | Manifest has `manifestType = "quran-tafsir-import-source-package"` and `isFinalImportManifest = true` |
| `TAFSIR-SOURCE-COUNT` | 84 approved sources |
| `TAFSIR-EXCLUDED-COUNT` | 9 excluded sources |
| `TAFSIR-ARABIC-SOURCE-COUNT` | 35 Arabic approved sources |
| `TAFSIR-NON-ARABIC-SOURCE-COUNT` | 49 non-Arabic approved sources |
| `TAFSIR-SOURCE-SET` | `sources/` exactly matches manifest approved package files |
| `TAFSIR-SOURCE-HASH` | File sizes and sha256 match manifest |
| `TAFSIR-NO-EXCLUDED-SOURCES` | Excluded sources are not persisted |
| `TAFSIR-COVERAGE-COUNT` | Every approved source has coverage 6,236 |
| `TAFSIR-JSON-SHAPE` | Each source root is an object with 6,236 ayah keys |
| `TAFSIR-AYAH-KEYS-RESOLVE` | Every ayah key resolves to canonical ayah |
| `TAFSIR-POINTERS-RESOLVE` | Every pointer resolves to same-source text block |
| `TAFSIR-NO-EMPTY-TEXT` | No approved source/ayah resolves to empty tafsir text |
| `TAFSIR-NO-DUPLICATE-AYAH-ENTRY` | No duplicate source/ayah mapping |
| `TAFSIR-TEXT-UNCHANGED` | Stored tafsir text matches imported source text exactly |
| `TAFSIR-NO-QURAN-TEXT-COPY` | Tafsir-owned records do not contain Quran ayah text |
| `TAFSIR-POSTCOPY-SOURCE-ROWS` | 84 source rows persisted |
| `TAFSIR-POSTCOPY-AYAH-MAPPINGS` | 523,824 ayah-link rows persisted |
| `TAFSIR-SOURCE-UNCHANGED` | Package still matches manifest before acceptance |
| `TAFSIR-REPORT-WRITTEN` | Required Markdown and JSON reports were written |

Any failed hard check means `verdict = "fail"`, `persisted = false`, and no accepted tafsir changes.

## Warning checks

| ID | Meaning |
|---|---|
| `TAFSIR-PROVENANCE-WARNING` | License/provenance unknown for all imported sources; internal use only |
| `TAFSIR-MODERN-WORKS-WARNING` | Modern/translated works may require rights review before publishing |

Warnings never block internal import, but must appear in reports.

## Informational checks

| ID | Meaning |
|---|---|
| `TAFSIR-INLINE-MARKUP` | Source text may include inline markup and is preserved exactly |
| `TAFSIR-LANGUAGE-COVERAGE` | Source count by language/direction |
| `TAFSIR-TEXT-BLOCK-COUNT` | Text-block rows by source |

## JSON report shape

```json
{
  "runAtUtc": "2026-06-14T12:00:00Z",
  "verdict": "pass",
  "persisted": true,
  "forced": false,
  "sourcePath": "resources/import-sources/quran-tafsirs",
  "totals": {
    "sourceRows": 84,
    "tafsirTextBlockRows": 123456,
    "ayahMappingRows": 523824,
    "approvedSources": 84,
    "excludedSources": 9,
    "arabicSources": 35,
    "nonArabicSources": 49,
    "languageCount": 33,
    "distinctAyahs": 6236
  },
  "sourceSummaries": [
    {
      "sourceKey": "ar-tabari",
      "languageCode": "ar",
      "direction": "rtl",
      "displayNameEn": "Jami al-Bayan (al-Tabari)",
      "packageFile": "sources/ar-tabari.json",
      "sha256": "31ad3625215de8e8ac15482e28e6920e9f4adeb903373d88d6def658819c6b30",
      "license": "unknown",
      "provenance": "unknown"
    }
  ],
  "excludedSourceSummaries": [
    {
      "sourceKey": "ar-wajiz",
      "status": "excluded_incomplete_coverage",
      "reason": "Content coverage 1645/6236"
    }
  ],
  "checks": [
    {
      "id": "TAFSIR-SOURCE-COUNT",
      "severity": "hard",
      "expected": "84",
      "observed": "84",
      "passed": true
    }
  ],
  "warnings": [
    "TAFSIR-PROVENANCE-WARNING: license/provenance unknown for all imported sources."
  ],
  "errors": [],
  "infoNotes": [
    "TAFSIR-LANGUAGE-COVERAGE: 33 languages."
  ]
}
```

`tafsirTextBlockRows` is source-dependent and must be reported from the actual assembled import rather
than hard-coded.

## Markdown report shape

```markdown
# Quran Tafsir Import Report

- Run (UTC): 2026-06-14T12:00:00Z
- Verdict: PASS
- Persisted: true
- Forced: false
- Source path: resources/import-sources/quran-tafsirs

## Totals

| Metric | Value |
|---|---:|
| Sources | 84 |
| Text blocks | <actual> |
| Source-to-ayah mappings | 523,824 |
| Excluded sources | 9 |
| Languages | 33 |

## Hard Checks

| ID | Expected | Observed | Passed |
|---|---|---|---|
| TAFSIR-SOURCE-COUNT | 84 | 84 | yes |

## Warnings

- TAFSIR-PROVENANCE-WARNING: license/provenance unknown for all imported sources.

## Excluded Sources

| Source key | Status | Reason |
|---|---|---|
| ar-wajiz | excluded_incomplete_coverage | Content coverage 1645/6236 |
```

## Contract guarantees

- Successful report: `verdict = "pass"`, `persisted = true`, all hard checks passed.
- Failed report: `verdict = "fail"`, `persisted = false`, failed hard checks listed in `errors`.
- Report-write failure after validation: import is not accepted; no tafsir changes are kept.
- Reports never include copied Quran ayah text.
