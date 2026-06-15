# Feature 008 — Validation and Reporting Verification

**Feature**: Quran Translations Foundation  
**Branch**: `008-quran-translations-foundation`  
**Produced by**: T077 (Phase 7 polish)  
**Date**: 2026-06-15

## Summary

All `TR-*` check IDs are centralized in `TranslationInvariants.cs`. Hard checks block persistence;
warnings and info notes are audit-only. Every run emits `translation-import-report.json` and
`translation-import-report.md` when the report directory is writable.

## Hard checks (`TR-*`)

| Check ID | What it verifies | Primary emitter |
| --- | --- | --- |
| `TR-PACKAGE-SHAPE` | Required root files exist (`README.md`, `manifest.json`, `package-report.md`, `source-display-metadata.json`, `sources/`) | `TranslationManifestReader` |
| `TR-MANIFEST-FINAL` | `manifestType` and `isFinalImportManifest = true` | `TranslationManifestReader` |
| `TR-DISPLAY-METADATA-FINAL` | Display metadata type/status final | `TranslationDisplayMetadataReader` |
| `TR-DISPLAY-METADATA-SET` | 167 display records; source-set matches manifest | `TranslationDisplayMetadataReader` |
| `TR-DISPLAY-METADATA-REQUIRED-FIELDS` | Required display fields non-empty per record | `TranslationDisplayMetadataReader` |
| `TR-SOURCE-COUNT` | Approved source count = 167 | `TranslationManifestReader` |
| `TR-TYPE-COUNTS` | Simple = 129, with_footnotes = 38 | `TranslationManifestReader`, `TranslationTypeCountValidation` |
| `TR-EXCLUDED-COUNT` | Excluded source count = 19 | `TranslationManifestReader` |
| `TR-SOURCE-SET` | Copied source file set matches manifest keys | `TranslationManifestReader` |
| `TR-SOURCE-HASH` | Per-file size and sha256 match manifest | `TranslationManifestReader` |
| `TR-NO-EXCLUDED-SOURCES` | Word-by-word, empty-text, and unattributed near-duplicate keys are not importable | `TranslationImportSource`, `TranslationAssembler` |
| `TR-JSON-SHAPE` | Source JSON root is object; each row is `{ "t": string }` | `JsonTranslationSourceReader` |
| `TR-COVERAGE-COUNT` | Each approved source has exactly 6,236 verse keys | `JsonTranslationSourceReader`, assembler |
| `TR-NO-EMPTY-TEXT` | No null, missing, or empty `t` values | `JsonTranslationSourceReader` |
| `TR-AYAH-KEYS-RESOLVE` | Every verse key resolves to `quran_ayahs` | `JsonTranslationSourceReader`, `TranslationAssembler` |
| `TR-NO-DUPLICATE-AYAH-ENTRY` | No duplicate `(source, ayah)` mapping | `TranslationAssembler` |
| `TR-TEXT-UNCHANGED` | Persisted `text` matches source `t` exactly | `TranslationValidationRunner` |
| `TR-NO-QURAN-TEXT-COPY` | Translation text does not copy Arabic Quran ayah text | `TranslationAssembler`, `TranslationValidationRunner` |
| `TR-POSTCOPY-SOURCE-ROWS` | Post-copy source row count matches expected | `TranslationValidationRunner` |
| `TR-POSTCOPY-AYAH-MAPPINGS` | Post-copy ayah mapping count matches expected | `TranslationValidationRunner` |
| `TR-SOURCE-UNCHANGED` | On-disk source digests unchanged after import | `TranslationValidationRunner` |
| `TR-REPORT-WRITTEN` | Both JSON and Markdown reports written before commit | `EfBulkTranslationImportWriter`, report emitter |
| `TR-ROLLBACK-ON-FAIL` | Failed validation leaves zero accepted translation rows | `TranslationImportReportBuilder`, rollback tests |
| `TR-RERUN-GUARD` | Normal re-run refused when translation tables are non-empty | `ImportTranslationsHandler`, report builder |

## Warnings and info notes

| ID | Severity | Purpose |
| --- | --- | --- |
| `TR-PROVENANCE-WARNING` | warning | License/provenance unknown for all imported sources; internal use only |
| `TR-INLINE-MARKUP` | info | Sources containing inline footnotes or HTML (stored exactly, not parsed) |
| `TR-LANGUAGE-COVERAGE` | info | Language distribution summary |
| `TR-RECLASSIFIED` | info | Sources reclassified during curation (report evidence only) |

## Report files

| File | Contract |
| --- | --- |
| `translation-import-report.json` | Machine-readable audit: verdict, persisted, forced, totals, source summaries, excluded summaries, checks, warnings, errors, info |
| `translation-import-report.md` | Human-readable mirror of the JSON audit |

### Verdicts

- `pass` — import accepted and persisted
- `fail` — validation failure or report-write failure; `persisted = false`

### Report builder and writer

| Component | Path |
| --- | --- |
| `ITranslationImportReportBuilder` | `TranslationImportReportBuilder.cs` |
| `ITranslationReportWriter` | `MarkdownJsonTranslationReportWriter.cs` |
| `TranslationImportReportEmitter` | Application layer — converts write failures into acceptance-critical failures |

### Test coverage

| Test file | Scope |
| --- | --- |
| `TranslationReportShapeTests.cs` | JSON/Markdown shape for success, failure, refusal; all hard check IDs enumerated |
| `TranslationSourceSafetyTests.cs` | Reports exclude translation body text and Arabic Quran ayah text |
| `TranslationValidationFailureTests.cs` | Hard-check failure evidence per invalid package condition |
| `TranslationRefusalForceTests.cs` | `TR-RERUN-GUARD` on normal re-run |
| `TranslationRollbackTests.cs` | `TR-ROLLBACK-ON-FAIL`, report-write failure rollback |

## Status

**VERIFIED** — all 24 hard checks, 1 warning ID, and 3 info IDs are implemented, emitted in reports, and covered by the Feature 008 test subset (62/62 passed).
