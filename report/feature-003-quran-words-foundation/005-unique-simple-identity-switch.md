# Feature 003 — Phase 1: Unique-Simple Identity Switch Report

**Date:** 2026-06-10  
**Phase:** 1 only — unique-simple identity switch (no links yet)  
**Verdict:** PASS

---

## Summary

Phase 1 switches `quran_words_unique_simple` and ordered-simple statistics from grouping by `text_uthmani_simple` to grouping by `word_key_imlaei_simple`. Representative Uthmani display fields (`text_uthmani`, `text_uthmani_simple`, `qpc_glyph`) are populated from the first Mushaf occurrence per imlaei identity. `unique_tashkeel` and `quran_words` are unchanged.

---

## Files Changed

| File | Change |
| --- | --- |
| `infrastructure/.../Display/DisplayWordsSql.cs` | ReadableBase CTE, InsertUniqueSimple, InsertOrderedSimple, simple-side checks |
| `domain/.../Display/UniqueSimpleWord.cs` | Added `WordKeyImlaeiSimple`, `TextUthmani`, `QpcGlyph` |
| `domain/.../Display/OrderedSimpleWord.cs` | Added `WordKeyImlaeiSimple` |
| `infrastructure/.../UniqueSimpleWordConfiguration.cs` | New columns; unique index moved to `WordKeyImlaeiSimple` |
| `infrastructure/.../OrderedSimpleWordConfiguration.cs` | Mapped `WordKeyImlaeiSimple` |
| `application/.../DisplayWordsInvariants.cs` | Added `CanonicalUniqueSimple` (14,783), `CanonicalUniqueTashkeel` (21,294) |
| `tests/.../DisplayWordsSyntheticSeed.cs` | Deterministic synthetic `WordKeyImlaeiSimple` per readable word |
| `tests/.../DisplayWordsStatisticsTests.cs` | Simple stats asserted by imlaei key |
| `tests/.../DisplayWordsFirstOccurrenceTests.cs` | First-occurrence grouping by imlaei key |
| `tests/.../DisplayWordsIdempotencyTests.cs` | Snapshot projections include new columns |
| `infrastructure/.../Migrations/20260610041226_AddUniqueSimpleImlaeiIdentity.cs` | Generated migration |
| `infrastructure/.../Migrations/20260610041226_AddUniqueSimpleImlaeiIdentity.Designer.cs` | Generated designer |
| `infrastructure/.../Migrations/QuranDashboardDbContextModelSnapshot.cs` | EF snapshot update |

---

## Migration Generated

**Name:** `AddUniqueSimpleImlaeiIdentity`  
**Files:**
- `20260610041226_AddUniqueSimpleImlaeiIdentity.cs`
- `20260610041226_AddUniqueSimpleImlaeiIdentity.Designer.cs`

**Intent (Up):**
- `quran_words_unique_simple`: add `word_key_imlaei_simple`, `text_uthmani`, `qpc_glyph` (NOT NULL, default `''`); drop unique index on `text_uthmani_simple`; add unique index on `word_key_imlaei_simple`
- `quran_words_ordered_simple`: add `word_key_imlaei_simple` (NOT NULL, default `''`)

**`dotnet ef database update`:** Skipped (per phase scope; tests apply via `MigrateAsync` in Testcontainers).

---

## SQL Grouping Changes

| Construct | Before | After |
| --- | --- | --- |
| `ReadableBase` CTE | No `word_key_imlaei_simple` / `qpc_glyph` | Projects both from `quran_words` |
| `InsertUniqueSimple` stats | `GROUP BY text_uthmani_simple` | `GROUP BY word_key_imlaei_simple` |
| `InsertUniqueSimple` first_occ | `DISTINCT ON (text_uthmani_simple)` | `DISTINCT ON (word_key_imlaei_simple)` ordered by key + `word_order_in_mushaf` |
| `InsertUniqueSimple` insert | `text_uthmani_simple`, `text_imlaei_simple` only | Adds `word_key_imlaei_simple`, representative `text_uthmani`, `qpc_glyph` |
| `InsertOrderedSimple` stats/join | `text_uthmani_simple` | `word_key_imlaei_simple` |
| `InsertOrderedSimple` insert | Per-occurrence simple text only | Adds per-occurrence `word_key_imlaei_simple` |
| `CheckUnqCountDistinctSimpleText` | `COUNT(DISTINCT text_uthmani_simple)` | `COUNT(DISTINCT word_key_imlaei_simple)` |
| `CheckStatMatchViolations` (simple) | Group/join on `text_uthmani_simple` | Group/join on `word_key_imlaei_simple` |
| `CheckFirstOccViolations` (simple) | Join on `text_uthmani_simple` | Join on `word_key_imlaei_simple` |

**Unchanged:** `InsertUniqueTashkeel`, `InsertOrderedTashkeel`, tashkeel-side checks, truncate, source-untouched checks.

---

## Entity / Configuration Changes

### `UniqueSimpleWord`
- Added: `WordKeyImlaeiSimple` (identity key, unique index)
- Added: `TextUthmani` (representative display with tashkeel)
- Added: `QpcGlyph` (representative glyph)
- Kept: `TextUthmaniSimple` (representative, no longer unique), `TextImlaeiSimple`, counts, `first_*`

### `OrderedSimpleWord`
- Added: `WordKeyImlaeiSimple` mapped to `word_key_imlaei_simple`
- Kept: per-occurrence display fields, `quran_word_id` relation/index unchanged

### `QuranWord`
- **Not modified** in this phase.

---

## Constants

- `DisplayWordsInvariants.CanonicalUniqueSimple = 14_783` (canonical real-data count)
- `DisplayWordsInvariants.CanonicalUniqueTashkeel = 21_294`
- `InformationalUniqueSimple` now aliases `CanonicalUniqueSimple`
- `InformationalUniqueTashkeel` remains 21,210 (informational warning may still fire on real data at 21,294)

---

## Tests Updated

| Test file | Update |
| --- | --- |
| `DisplayWordsSyntheticSeed.cs` | Sets deterministic `WordKeyImlaeiSimple` (`ك-أ` / `ك-ب` / `ك-ج`) and existing `QpcGlyph` |
| `DisplayWordsStatisticsTests.cs` | Simple stats filtered by `WordKeyImlaeiSimple` |
| `DisplayWordsFirstOccurrenceTests.cs` | Simple first-occurrence grouped by imlaei key |
| `DisplayWordsIdempotencyTests.cs` | Snapshot includes new simple-side columns |

**Not added (Phase 2+):** link population tests, marker-null identity-link tests, real-import anchor tests.

---

## Commands Run and Results

| Command | Result |
| --- | --- |
| `./scripts/add-mig AddUniqueSimpleImlaeiIdentity` | Success — migration generated |
| `dotnet build QuranDashboard.sln` | 0 warnings, 0 errors |
| `dotnet test QuranDashboard.sln --filter "FullyQualifiedName~WordsDisplay"` | 10/10 passed |
| `dotnet test QuranDashboard.sln` | 26/26 passed |
| `git diff --check` | Clean |
| `dotnet ef database update` | **Not run** |

---

## Scope Confirmations

| Constraint | Status |
| --- | --- |
| Phase 2+ not implemented | Confirmed — no link columns on `quran_words`, no link SQL, no `LINK-*` checks |
| `quran_words` not modified | Confirmed — no entity/config/migration changes to foundation table |
| `unique_tashkeel` unchanged | Confirmed — still groups by `text_uthmani` |
| No real-database import/rebuild | Confirmed — only Testcontainers disposable DBs |
| Quranic source data preserved | Confirmed — rebuild only reads foundation columns; no normalization |

---

## Expected Real-Data Outcome (after migrate + rebuild)

When applied against the canonical import:

- `quran_words_unique_simple` rows: **14,783** (down from 15,826)
- Grouping key: `word_key_imlaei_simple`
- `UNQ-EXPECT-SIMPLE` warning should pass (14,783 matches informational constant)
- `UNQ-EXPECT-TASHKEEL` may still warn (21,294 vs 21,210 informational) — accepted

*Note: Real-data counts were not re-measured in this phase; they are design expectations from the implementation plan. Validation is via structural checks on synthetic seed and full test suite green.*

---

## Final Verdict

**PASS** — Phase 1 complete. Unique-simple identity and ordered-simple statistics now use `word_key_imlaei_simple`. Migration generated. Build and full test suite green. Phase 2+ and `quran_words` link columns remain out of scope.
