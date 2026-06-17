# Quran Full I'rab Import Report

- Run (UTC): 2026-06-17 12:50:58Z
- Verdict: FAIL
- Persisted: false
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

## Warnings

- FULLI3RAB-PROVENANCE-WARNING: License/provenance is unknown and usage scope is internal-only-until-cleared. Not cleared for public distribution — internal use only until cleared.

## Errors

- Full i'rab tables are not empty. Re-run with --force to rebuild them.

## Informational Notes

- Full i'rab import refused before persistence; no full-i'rab rows were written.
- FULLI3RAB-BLOCK-COUNT: daas=3633, darwish=1387, jadwal=3257, muyassar=6236.
- FULLI3RAB-MAPPING-COUNT: sourceRows=4, entryRows=14513, ayahMappingRows=24944.

## Source Summaries

| Source key | Package file | License | Provenance | Usage scope | Markup |
|---|---|---|---|---|---|
| daas | sources/alrab-al-quran-li-da-as.json | unknown | unknown | internal-only-until-cleared | html |
| darwish | sources/i-rab-al-quran-li-al-darwish.json | unknown | unknown | internal-only-until-cleared | html |
| jadwal | sources/al-jadwal-fi-i-rab-al-quran.json | unknown | unknown | internal-only-until-cleared | html |
| muyassar | sources/al-i-rab-al-muyassar.json | unknown | unknown | internal-only-until-cleared | html |
