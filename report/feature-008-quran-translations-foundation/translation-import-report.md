# Quran Translation Import Report

- Run (UTC): 2026-06-20 10:03:53Z
- Verdict: PASS
- Persisted: true
- Forced: false
- Source path: /projects/Dashboard/App/resources/import-sources/quran-translations

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
| TR-PACKAGE-SHAPE | README.md, manifest.json, package-report.md, source-display-metadata.json, sources/ | present | yes |
| TR-MANIFEST-FINAL | manifestType=quran-translation-import-source-package, isFinalImportManifest=true | manifestType=quran-translation-import-source-package, isFinalImportManifest=true | yes |
| TR-DISPLAY-METADATA-FINAL | metadataType=quran-translation-source-display-metadata, status=final, sourceCount=167 | metadataType=quran-translation-source-display-metadata, status=final, sourceCount=167 | yes |
| TR-DISPLAY-METADATA-SET | display metadata sourceKey set exactly matches manifest approved source set | exact match | yes |
| TR-DISPLAY-METADATA-REQUIRED-FIELDS | required display metadata fields present and non-empty | all present and non-empty | yes |
| TR-SOURCE-COUNT | 167 | 167 | yes |
| TR-EXCLUDED-COUNT | 19 | 19 | yes |
| TR-SOURCE-SET | sources/ exactly matches manifest approved package files | exact match | yes |
| TR-SOURCE-HASH | file sizes and sha256 match manifest | all match | yes |
| TR-JSON-SHAPE | object root with 6236 verse keys | valid | yes |
| TR-COVERAGE-COUNT | 6236 | all 167 sources = 6236 | yes |
| TR-NO-EMPTY-TEXT | no approved source has empty, null, missing, or non-string t | none | yes |
| TR-AYAH-KEYS-RESOLVE | every verse key resolves to canonical ayah | all resolved | yes |
| TR-POSTCOPY-SOURCE-ROWS | 167 | 167 | yes |
| TR-POSTCOPY-AYAH-MAPPINGS | 1041412 | 1041412 | yes |
| TR-TEXT-UNCHANGED | stored translation text matches imported source text exactly | exact match | yes |
| TR-NO-QURAN-TEXT-COPY | no copied Quran ayah text in translation entries | none | yes |
| TR-TYPE-COUNTS | simple=129, with_footnotes=38 | simple=129, with_footnotes=38 | yes |
| TR-NO-DUPLICATE-AYAH-ENTRY | no duplicate (source_id, ayah_id) rows persisted | none | yes |
| TR-NO-EXCLUDED-SOURCES | no excluded source keys persisted | none | yes |
| TR-SOURCE-UNCHANGED | package files (manifest, display metadata, approved sources) unchanged before acceptance | unchanged | yes |
| TR-REPORT-WRITTEN | required Markdown and JSON reports written | written | yes |
| TR-ROLLBACK-ON-FAIL | failed hard checks leave no accepted partial import | no partial import persisted | yes |
| TR-RERUN-GUARD | normal re-run refuses non-empty targets; forced replacement revalidates before replacing | no existing translation data | yes |

## Warnings

- TR-PROVENANCE-WARNING: license/provenance unknown for all imported sources; internal use only.

## Informational Notes

- Translation import passed validation; acceptance reports written before commit.
- TR-INLINE-MARKUP: source text may include inline footnotes or embedded HTML and is preserved exactly.
- TR-LANGUAGE-COVERAGE: 83 languages.

## Excluded Sources

| Source key | Status | Reason |
|---|---|---|
| african-development-foundation-simple | excluded | empty_text |
| bangali-word-by-word-translation | excluded | word_by_word |
| colored-english-wbw-translation | excluded | word_by_word |
| en-maarif-ul-quran-simple | excluded | empty_text |
| english-wbw-translation | excluded | word_by_word |
| french-wbw-translation | excluded | word_by_word |
| hindi-wbw-translation | excluded | word_by_word |
| indonesian-word-by-word-translation | excluded | word_by_word |
| ingush-wbw-translation | excluded | word_by_word |
| kannada-quran-inline-footnotes | excluded | empty_text |
| ko-unknown-simple | excluded | unattributed_near_duplicate |
| nl-abdalsalaam-simple | excluded | empty_text |
| persian-wbw-translation | excluded | word_by_word |
| sq-unknown-simple | excluded | unattributed_near_duplicate |
| tamil-wbw-translation | excluded | word_by_word |
| translation-pioneers-center-simple | excluded | empty_text |
| turkish-wbw-translation | excluded | word_by_word |
| urdu-sayyid-qatab-simple | excluded | empty_text |
| urud-wbw | excluded | word_by_word |
