# Quran Navigation Metadata Import Report

Verdict: **accepted**
Persisted: true
Forced: false
Source path: `/projects/Dashboard/App/resources/import-sources/quran-navigation-metadata`

## Totals

| Dataset | Count |
|---|---:|
| juz | 30 |
| hizb | 60 |
| rub | 240 |
| sajda | 15 |
| ayahsTagged | 6236 |

## Ayah coverage

Total ayahs: 6236
With juz: 6236
With hizb: 6236
With rub: 6236
Complete: True

## Checks

- `NAV-PACKAGE-SHAPE`: PASS (expected manifest.json + sources/{juz,hizb,rub,sajda}.json; observed present)
- `NAV-MANIFEST-FINAL`: PASS (expected packageType=quran-navigation-metadata-import-source-package, isFinalImportManifest=true; observed packageType=quran-navigation-metadata-import-source-package, isFinalImportManifest=true)
- `NAV-SOURCE-COUNT`: PASS (expected 30/60/240/15; observed 30/60/240/15)
- `NAV-SOURCE-HASH`: PASS (expected file sizes and sha256 match manifest; observed all match)
- `NAV-JSON-SHAPE`: PASS (expected required fields per dataset; observed valid)
- `NAV-SAJDA-TYPE`: PASS (expected required|optional; observed all allowed)
- `NAV-NO-QURAN-TEXT-COPY`: PASS (expected no Quran ayah text read or stored; observed none)
- `NAV-VERSE-KEYS-RESOLVE`: PASS (expected every verse key resolves to canonical ayah; observed all resolved)
- `NAV-RANGE-COVERAGE-JUZ`: PASS (expected 6236 once; observed 6236 once)
- `NAV-RANGE-COVERAGE-HIZB`: PASS (expected 6236 once; observed 6236 once)
- `NAV-RANGE-COVERAGE-RUB`: PASS (expected 6236 once; observed 6236 once)
- `NAV-NO-RANGE-GAPS-OVERLAPS`: PASS (expected no gaps or overlaps; observed none)
- `NAV-HIERARCHY`: PASS (expected each hizb in exactly one juz; each rub in exactly one hizb; observed valid)
- `NAV-VERSE-COUNT-MATCH`: PASS (expected source verses_count match computed ranges; observed all match)
- `NAV-SAJDA-DISTRIBUTION`: PASS (expected optional=11, required=4; observed optional=11, required=4)
- `NAV-AYAH-COLUMNS-COMPLETE`: PASS (expected 6236; observed 6236)
- `NAV-SOURCE-UNCHANGED`: PASS (expected package files unchanged before acceptance; observed unchanged)
- `NAV-REPORT-WRITTEN`: PASS (expected required Markdown and JSON reports written; observed written)
- `NAV-ROLLBACK-ON-FAIL`: PASS (expected full rollback on hard failure; observed not needed)
- `NAV-RERUN-GUARD`: PASS (expected empty navigation targets; observed passed)

No Quran ayah text was read or stored by this import.
