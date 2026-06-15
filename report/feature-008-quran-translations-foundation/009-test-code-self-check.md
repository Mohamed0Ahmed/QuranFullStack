# Feature 008 — Test-Code Self-Check

**Feature**: Quran Translations Foundation  
**Branch**: `008-quran-translations-foundation`  
**Produced by**: T083 (Phase 7 polish)  
**Reference**: `.claude/skills/test-guard/`  
**Scope**: `Backend/tests/QuranDashboard.Tests/Quran/Translations/`  
**Date**: 2026-06-15

## Summary

Feature 008 translation tests are **high quality**: they exercise real import behavior through
`TranslationImportTestFixture`, use obvious synthetic Quranic placeholders, and assert observable
outcomes (counts, persistence, report shape, rollback) rather than implementation details.

## Rule-by-rule assessment

| Rule | Verdict | Notes |
| --- | --- | --- |
| 1. Test behavior, not implementation | **PASS** | E2E and integration tests assert persisted rows, report files, verdicts, and messages |
| 2. Every mock justified | **PASS** | No inappropriate mocks; real readers/writer against test DB |
| 3. Data-driven for variants | **PASS** | Tests use `[Fact]` throughout; variant conditions are separate methods with distinct setup/assertions (acceptable). Some near-duplicate failure cases could be merged into `[Theory]` rows later — optional cleanup only |
| 4. Every test justifies existence | **PASS** | Each file targets a distinct failure mode or acceptance scenario |
| 5. Name tests for scenario | **PASS** | Names describe condition and expected outcome |
| 6. Production regression sacred | N/A | No production incidents referenced yet |
| 7. No framework guarantees | **PASS** | Tests validate project validation logic, not EF/xUnit defaults |
| 8. Real DTOs/entities | **PASS** | Synthetic package built from real file shapes; no mocked state objects |
| 9. Real infrastructure where it matters | **PASS** | `TranslationImportTestFixture` seeds `quran_ayahs`, clears translation tables, captures snapshots |

## Must-fix violations

None.

## Should-fix observations

- `TranslationDisplayMetadataReaderTests` and `TranslationSourceReaderTests` could merge some near-duplicate failure cases into additional `[Theory]` rows; current duplication is limited and readable — optional cleanup only.
- `TranslationSchemaShapeTests` is long (~360+ lines) because it validates full EF model constraints in one place; acceptable for schema contract tests.

## Quranic data safety (tests)

- Fixture seeds a non-existent surah `901` with verse keys like `901:1` (deliberately outside the real Quran surah range)
- Placeholder translation strings use the `SYNTHETIC-TRANSLATION-{sourceKey}-{verseKey}` pattern; seeded ayah text uses `اختبار-{verseKey}`
- No real Quranic Arabic or authentic translation text in committed test data
- `TranslationSourceSafetyTests` explicitly guards report output

## Test inventory

12 test classes + 1 shared fixture (`TranslationImportTestFixture.cs`), 62 tests — all passed (see `005-test-verification.md`).

## Status

**PASS**
