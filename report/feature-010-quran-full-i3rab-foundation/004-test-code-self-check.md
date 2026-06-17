# Feature 010 — Test-Code Self-Check

**Feature:** Quran Full I'rab Foundation
**Produced by:** Phase 5 polish
**Reference:** `.claude/skills/test-guard/`
**Scope:** `Backend/tests/QuranDashboard.Tests/Quran/FullI3rab/`
**Date:** 2026-06-17

## Summary

Feature 010 full-i'rab tests are **high quality**: they exercise real import behavior through
`FullI3rabImportTestFixture`, use obvious synthetic HTML placeholders, and assert observable outcomes
(counts, persistence, report shape, rollback, refusal/force) rather than implementation details.

## Rule-by-rule assessment

| Rule | Verdict | Notes |
| --- | --- | --- |
| 1. Test behavior, not implementation | **PASS** | Integration tests assert persisted rows, report files, verdicts, and messages |
| 2. Every mock justified | **PASS** | No inappropriate mocks; real readers/writer against Testcontainers PostgreSQL |
| 3. Data-driven for variants | **PASS** | Assembler and validation failure tests use `[Theory]` over value kinds / failure modes |
| 4. Every test justifies existence | **PASS** | Each file targets a distinct failure mode or acceptance scenario |
| 5. Name tests for scenario | **PASS** | Names describe condition and expected outcome |
| 6. Production regression sacred | N/A | No production incidents referenced |
| 7. No framework guarantees | **PASS** | Tests validate project validation logic, not EF/xUnit defaults |
| 8. Real DTOs/entities | **PASS** | Synthetic package built from real file shapes; no mocked state objects |
| 9. Real infrastructure where it matters | **PASS** | Fixture seeds `quran_ayahs`, clears full-i'rab tables, runs migrations |

## Must-fix violations

None.

## Should-fix observations

- `FullI3rabImportTestFixture` is moderately long because it seeds a miniature ayah world for
  integration tests; acceptable for correctness.
- `FullI3rabSyntheticPackage` centralizes synthetic HTML — good reuse across import/report tests.

## Quranic data safety (tests)

- Fixture seeds non-existent surah `900` with verse keys like `900:1` (outside the real Quran surah range)
- Seeded i'rab HTML uses obvious synthetic placeholders (`<div class="ar"><p>نص إعراب اختباري مُصطنع للمفتاح {verseKey}.</p></div>`)
- No real Quranic Arabic or authentic religious labels in committed test data
- `FullI3rabSourceUnchangedTests` guards source file sha256/size immutability

## Test inventory

14 test files (11 test classes + 2 fixtures + 1 synthetic-package helper), 42 tests — all passed (see `002-test-verification.md`).

## Status

**PASS**
