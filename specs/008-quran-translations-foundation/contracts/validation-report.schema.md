# Contract — Validation & Import Report Schema

Every accepted `import-translations` run must write both:

- `translation-import-report.md`
- `translation-import-report.json`

Default output directory:

```text
Backend/report/feature-008-quran-translations-foundation/
```

## Final manifest contract

`TR-MANIFEST-FINAL` passes only when the package manifest contains these exact top-level values:

```json
{
  "manifestType": "quran-translation-import-source-package",
  "isFinalImportManifest": true,
  "sourceRoot": "sources",
  "sourceCount": 167,
  "languageCount": 83,
  "contentCoverageCount": 6236,
  "approvedAyahMappingCount": 1041412
}
```

The same manifest must also contain these locked summary values:

```json
{
  "typeCounts": {
    "simple": 129,
    "with_footnotes": 38
  },
  "excludedCounts": {
    "wordByWord": 11,
    "emptyText": 6,
    "unattributedNearDuplicate": 2,
    "total": 19
  },
  "selectionRules": {
    "resourceKind": "translation",
    "levels": ["ayah"],
    "includedTypes": ["simple", "with_footnotes"],
    "excludedTypes": ["word_by_word"],
    "requireExact6236KeySet": true,
    "requireNonEmptyText": true,
    "classifyByContentNotFolder": true,
    "preserveTextExactly": true
  }
}
```

Do not infer final-manifest status from filename or folder location alone.

## Final display metadata contract

`TR-DISPLAY-METADATA-FINAL`, `TR-DISPLAY-METADATA-SET`, and
`TR-DISPLAY-METADATA-REQUIRED-FIELDS` pass only when `source-display-metadata.json` contains:

```json
{
  "metadataType": "quran-translation-source-display-metadata",
  "status": "final",
  "sourceCount": 167,
  "sourceOfTruthManifest": "manifest.json",
  "displayContract": {
    "required": [
      "displayNameEn",
      "displayNameAr",
      "languageCode",
      "languageNameEn",
      "languageNameAr",
      "nativeName",
      "direction",
      "translationType",
      "sourceKey",
      "packageFile"
    ]
  }
}
```

Each record must have `metadataStatus = "final_display_ready"` and a `sourceKey` present in the manifest
approved source set. Every required field must be present and non-empty after trimming. `translatorNameEn`,
`translatorNameAr`, `needsReview`, confidence, and review reasons are non-blocking report metadata only.

## Hard checks

| ID | Expected |
|---|---|
| `TR-PACKAGE-SHAPE` | Required package files/folders exist |
| `TR-MANIFEST-FINAL` | Manifest has `manifestType = "quran-translation-import-source-package"` and `isFinalImportManifest = true` |
| `TR-DISPLAY-METADATA-FINAL` | Display metadata has `metadataType = "quran-translation-source-display-metadata"`, `status = "final"`, and `sourceCount = 167` |
| `TR-DISPLAY-METADATA-SET` | Display metadata source set exactly matches manifest approved source set |
| `TR-DISPLAY-METADATA-REQUIRED-FIELDS` | Required display metadata fields are present and non-empty |
| `TR-SOURCE-COUNT` | 167 approved sources |
| `TR-TYPE-COUNTS` | 129 simple sources and 38 with-footnotes sources |
| `TR-EXCLUDED-COUNT` | 19 excluded sources |
| `TR-SOURCE-SET` | `sources/` exactly matches manifest approved package files |
| `TR-SOURCE-HASH` | File sizes and sha256 match manifest |
| `TR-NO-EXCLUDED-SOURCES` | Excluded and word-by-word sources are not persisted |
| `TR-JSON-SHAPE` | Each source root is an object and every value is `{ "t": string }` |
| `TR-COVERAGE-COUNT` | Every approved source has the exact 6,236 verse-key set |
| `TR-NO-EMPTY-TEXT` | No approved source has empty, null, missing, or non-string `t` |
| `TR-AYAH-KEYS-RESOLVE` | Every verse key resolves to canonical ayah |
| `TR-NO-DUPLICATE-AYAH-ENTRY` | No duplicate `(source, ayah)` mapping |
| `TR-TEXT-UNCHANGED` | Stored translation text matches imported source text exactly |
| `TR-NO-QURAN-TEXT-COPY` | Translation-owned records do not contain copied Arabic Quran ayah text |
| `TR-POSTCOPY-SOURCE-ROWS` | 167 source rows persisted |
| `TR-POSTCOPY-AYAH-MAPPINGS` | 1,041,412 ayah-entry rows persisted |
| `TR-SOURCE-UNCHANGED` | Package still matches manifest before acceptance |
| `TR-REPORT-WRITTEN` | Required Markdown and JSON reports were written |
| `TR-ROLLBACK-ON-FAIL` | Failed hard checks leave no accepted partial import |
| `TR-RERUN-GUARD` | Normal re-run refuses non-empty targets; forced replacement revalidates before replacing |

Any failed hard check means `verdict = "fail"`, `persisted = false`, and no accepted translation changes.

## Warning checks

| ID | Meaning |
|---|---|
| `TR-PROVENANCE-WARNING` | License/provenance unknown for all imported sources; internal use only and not publish-ready |

Warnings never block internal import, but must appear in reports.

## Informational checks

| ID | Meaning |
|---|---|
| `TR-INLINE-MARKUP` | Source text may include inline footnotes or embedded HTML and is preserved exactly |
| `TR-LANGUAGE-COVERAGE` | Source count by language, direction, and type |
| `TR-RECLASSIFIED` | Three sources were reclassified from physical simple folder placement to `with_footnotes` by content |

## JSON report shape

```json
{
  "runAtUtc": "2026-06-15T12:00:00Z",
  "verdict": "pass",
  "persisted": true,
  "forced": false,
  "sourcePath": "resources/import-sources/quran-translations",
  "totals": {
    "sourceRows": 167,
    "ayahMappingRows": 1041412,
    "approvedSources": 167,
    "simpleSources": 129,
    "withFootnotesSources": 38,
    "excludedSources": 19,
    "languageCount": 83,
    "distinctAyahs": 6236
  },
  "sourceSummaries": [
    {
      "sourceKey": "en-yusufali",
      "languageCode": "en",
      "direction": "ltr",
      "translationType": "simple",
      "displayNameEn": "Yusuf Ali",
      "displayNameAr": "Yusuf Ali",
      "packageFile": "sources/en-yusufali.json",
      "sha256": "<manifest sha256>",
      "fileSizeBytes": 123456,
      "containsInlineFootnotes": false,
      "containsHtmlMarkup": false
    }
  ],
  "excludedSourceSummaries": [
    {
      "sourceKey": "ko-unknown",
      "status": "excluded_unattributed_near_duplicate",
      "reason": "Unattributed near-duplicate copy excluded by D11"
    }
  ],
  "checks": [
    {
      "id": "TR-SOURCE-COUNT",
      "severity": "hard",
      "expected": "167",
      "observed": "167",
      "passed": true
    }
  ],
  "warnings": [
    "TR-PROVENANCE-WARNING: license/provenance unknown for all imported sources; internal use only."
  ],
  "errors": [],
  "infoNotes": [
    "TR-LANGUAGE-COVERAGE: 83 languages."
  ]
}
```

The `sourceSummaries` array must not include translation body text.

## Markdown report shape

```markdown
# Quran Translation Import Report

- Run (UTC): 2026-06-15T12:00:00Z
- Verdict: PASS
- Persisted: true
- Forced: false
- Source path: resources/import-sources/quran-translations

## Totals

| Metric | Value |
|---|---:|
| Sources | 167 |
| Simple sources | 129 |
| With-footnotes sources | 38 |
| Source-to-ayah mappings | 1,041,412 |
| Excluded sources | 19 |
| Languages | 83 |

## Hard Checks

| ID | Expected | Observed | Passed |
|---|---|---|---|
| TR-SOURCE-COUNT | 167 | 167 | yes |

## Warnings

- TR-PROVENANCE-WARNING: license/provenance unknown for all imported sources; internal use only.

## Excluded Sources

| Source key | Status | Reason |
|---|---|---|
| ko-unknown | excluded_unattributed_near_duplicate | Unattributed near-duplicate copy excluded by D11 |
```

## Contract guarantees

- Successful report: `verdict = "pass"`, `persisted = true`, and the `checks` array enumerates every
  hard check (load-time and post-copy), all passed.
- Failed report: `verdict = "fail"`, `persisted = false`, failed hard checks listed in `errors`.
- Report-write failure after validation: import is not accepted; no translation changes are kept.
- Reports never include translation body text or copied Arabic Quran ayah text.
