# Quran Full I'rab Import Report

- Run (UTC): 2026-06-17 13:17:05Z
- Verdict: PASS
- Persisted: true
- Forced: false
- Source path: /projects/Dashboard/App/resources/import-sources/quran-full-i3rab

## Provenance

- licenseStatus: unknown
- provenanceStatus: unknown
- usageScope: internal-only-until-cleared
- **License/provenance is unknown and usage scope is internal-only-until-cleared. Not cleared for public distribution — internal use only until cleared.**

## Totals

| Metric | Value |
|---|---:|
| Sources | 4 |
| Entries | 14,513 |
| Source-to-ayah mappings | 24,944 |
| Distinct ayahs | 6,236 |

## Hard Checks

| ID | Expected | Observed | Passed |
|---|---|---|---|
| FULLI3RAB-PACKAGE-SHAPE | README.md, manifest.json, package-report.md, sources/ | present | yes |
| FULLI3RAB-MANIFEST-FINAL | manifestType=quran-full-i3rab-import-source-package, isFinalImportManifest=true | manifestType=quran-full-i3rab-import-source-package, isFinalImportManifest=true | yes |
| FULLI3RAB-SOURCE-COUNT | 4 | 4 | yes |
| FULLI3RAB-SOURCE-SET | sources/ exactly matches manifest approved package files | exact match | yes |
| FULLI3RAB-SOURCE-HASH | file sizes and sha256 match manifest | all match | yes |
| FULLI3RAB-COVERAGE-COUNT | 6236 | all 4 sources = 6236 | yes |
| FULLI3RAB-JSON-SHAPE | each source root is an object with 6236 ayah keys | valid | yes |
| FULLI3RAB-AYAH-KEYS-RESOLVE | every ayah key resolves to a canonical ayah | all resolved | yes |
| FULLI3RAB-POINTERS-RESOLVE | every pointer resolves to a same-source text block | all resolved | yes |
| FULLI3RAB-AYAH-KEYS-MEMBER-MATCH | every ayah_keys member maps to the declared leader | all matched | yes |
| FULLI3RAB-NO-EMPTY-TEXT | no approved source/ayah resolves to empty i3rab html | none | yes |
| FULLI3RAB-BLOCK-PARTITION | blocks partition all ayahs exactly once with no gaps or overlaps | valid partition | yes |
| FULLI3RAB-POSTCOPY-SOURCE-ROWS | 4 | 4 | yes |
| FULLI3RAB-POSTCOPY-AYAH-MAPPINGS | 24944 | 24944 | yes |
| FULLI3RAB-POSTCOPY-COVERAGE-SUM | 24944 | 24944 | yes |
| FULLI3RAB-POSTCOPY-ENTRY-SOURCE | 0 junction rows with entry source mismatch | 0 | yes |
| FULLI3RAB-POSTCOPY-AYAH-RESOLVED | all junction and leader ayah ids resolve in quran_ayahs | all resolved | yes |
| FULLI3RAB-POSTCOPY-NO-EMPTY-HTML | 0 empty i3rab_html rows | 0 | yes |
| FULLI3RAB-POSTCOPY-HTML-UNCHANGED | stored i3rab_html and hash match imported source per source | exact match | yes |
| FULLI3RAB-POSTCOPY-SOURCE-UNCHANGED | local source files match manifest.json size/sha256 before and after run | unchanged | yes |
| FULLI3RAB-REPORT-WRITTEN | required Markdown and JSON reports written | written | yes |

## Warnings

- FULLI3RAB-PROVENANCE-WARNING: License/provenance is unknown and usage scope is internal-only-until-cleared. Not cleared for public distribution — internal use only until cleared.

## Informational Notes

- Full i'rab import passed validation; acceptance reports written before commit.
- FULLI3RAB-BLOCK-COUNT: daas=3633, darwish=1387, jadwal=3257, muyassar=6236.
- FULLI3RAB-MAPPING-COUNT: sourceRows=4, entryRows=14513, ayahMappingRows=24944.

## Source Summaries

| Source key | Package file | License | Provenance | Usage scope | Markup |
|---|---|---|---|---|---|
| daas | sources/alrab-al-quran-li-da-as.json | unknown | unknown | internal-only-until-cleared | html |
| darwish | sources/i-rab-al-quran-li-al-darwish.json | unknown | unknown | internal-only-until-cleared | html |
| jadwal | sources/al-jadwal-fi-i-rab-al-quran.json | unknown | unknown | internal-only-until-cleared | html |
| muyassar | sources/al-i-rab-al-muyassar.json | unknown | unknown | internal-only-until-cleared | html |
