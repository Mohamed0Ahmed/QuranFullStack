# Imlaei clean-key import binding report

**Date:** 2026-06-10
**Scope:** Enrich `words/imlaei-simple.json` with a clean identity key and bind it through the
Feature 002 import into a dedicated `quran_words` column, without changing Uthmani/QPC behavior.

**Verdict: PASS** (see §10 for notes/follow-ups).

---

## 1. Summary of files changed

### Data (source promotion)
- `resources/import-sources/quran-foundation/words/imlaei-simple.json` — **replaced** with the
  enriched derived file (`derived/imlaei-simple-clean.json`): original `text` preserved, derived
  `text_clean` added. Record count unchanged (83,668). Verified before replacement: all `text`
  values byte-identical to the previous file, every record has a `text_clean`.

### Documentation
- `resources/import-sources/quran-foundation/README.md` — documents the enriched source, the
  `text` vs `text_clean` contract, the column binding, and corrects the provenance note.
- `resources/import-sources/quran-foundation/derived/README.md` — **new**; documents the
  generator and derived artifacts and that this output is what gets promoted.

### Production code
| File | Change |
| ---- | ------ |
| `application/QuranDashboard.Application.Abstractions/Quran/Import/WordRecordDto.cs` | Added optional `string? TextClean = null`. |
| `infrastructure/QuranDashboard.Infrastructure/Files/Quran/Import/JsonWordSourceReader.cs` | Reads optional `text_clean` (null for sources that omit it). |
| `domain/QuranDashboard.Domain/Quran/Words/QuranWord.cs` | Added `WordKeyImlaeiSimple` property. |
| `infrastructure/.../Persistence/Configurations/Quran/QuranWordConfiguration.cs` | Maps `word_key_imlaei_simple` (required) + filtered grouping index. |
| `application/.../Quran/Import/ImportQuranFoundation/QuranFoundationAssembler.cs` | Binds `WordKeyImlaeiSimple = imlaeiSimple.TextClean ?? ""`. |
| `infrastructure/.../Persistence/Repositories/Quran/Import/EfBulkQuranImportWriter.cs` | Added `word_key_imlaei_simple` to the `COPY quran_words (...)` column list + value. |
| `application/.../Quran/Import/Validation/ImlaeiCleanKeyCheck.cs` | **New** hard check: every imlaei-simple record has `text_clean`. |
| `application/.../Quran/Import/Validation/ImportValidationCheckIds.cs` | Registered `imlaei-clean-key` (Hard) in `Severities` + `All`. |
| `application/.../Quran/Import/Validation/QuranImportValidator.cs` | Instantiates and emits the new check. |

### EF migration (generated via tooling — not hand-written)
- `infrastructure/.../Migrations/20260610023128_AddWordKeyImlaeiSimple.cs`
- `infrastructure/.../Migrations/20260610023128_AddWordKeyImlaeiSimple.Designer.cs`
- `infrastructure/.../Migrations/QuranDashboardDbContextModelSnapshot.cs` (updated)

### Tests
- `tests/.../Quran/Import/JsonWordSourceReaderTests.cs` — **new**; optional `text_clean` binding.
- `tests/.../Quran/Import/ImlaeiCleanKeyBindingTests.cs` — **new**; assembler binding + validation check.
- `tests/.../Quran/Import/ImlaeiCleanKeyImportTests.cs` — **new**; end-to-end binding against real Postgres.

## 2. Is the original imlaei raw text still preserved?

**Yes.** Two independent guarantees:
- **Source:** the enriched file keeps the original `text` byte-for-byte (verified equal to the
  pre-replacement file for all 83,668 records before promotion).
- **Import:** the assembler still maps `imlaeiSimple.Text → QuranWord.TextImlaeiSimple`, and the
  bulk writer still writes `text_imlaei_simple`. The raw column is untouched by this change.

## 3. Is `text_clean` now imported/bound explicitly?

**Yes — not a silent passthrough.** The clean value travels through every layer explicitly:
`JsonWordSourceReader` reads `text_clean` → `WordRecordDto.TextClean` → assembler binds it to
`QuranWord.WordKeyImlaeiSimple` → `EfBulkQuranImportWriter` writes `word_key_imlaei_simple` → a
new **hard validation check** (`imlaei-clean-key`) fails the import if any imlaei-simple record
lacks `text_clean`. The importer no longer reads only `text`.

## 4. Which DB column holds raw imlaei text?

`quran_words.text_imlaei_simple` (unchanged; also drives ayah-marker detection).

## 5. Which DB column holds clean imlaei identity?

`quran_words.word_key_imlaei_simple` (**new**, `text NOT NULL`), with a filtered index
`IX_quran_words_word_key_imlaei_simple` (`WHERE is_ayah_marker = false`) to support grouping/
filtering "words without tashkeel".

## 6. Was a migration needed, and its name?

**Yes.** Generated with EF tooling via `./scripts/add-mig AddWordKeyImlaeiSimple`:
- Migration: **`20260610023128_AddWordKeyImlaeiSimple`**
- `Up`: `AddColumn word_key_imlaei_simple text NOT NULL DEFAULT ''` + `CreateIndex` (filtered).
- `Down`: drops the index and column.
- The `DEFAULT ''` only backfills the ALTER on any pre-existing rows; a normal rebuild import
  (`TRUNCATE` + `COPY`) repopulates real values, so no row keeps the empty default after import.
- **`dotnet ef database update` was NOT run** (per instructions). The migration is applied in
  integration tests against a disposable Testcontainers Postgres via `Database.MigrateAsync()`.

## 7. Validation of total / marker / readable counts

Confirmed against real Postgres by `ImportCountsTests` (runs on the enriched source) and the
importer's own hard validation checks (`word-count`, `marker-count`, `readable-count`):

| Metric | Expected | Observed | Source |
| --- | --- | --- | --- |
| Total words | 83,668 | 83,668 | `ImportCountsTests`, `word-count` check |
| Ayah markers | 6,236 | 6,236 | `ImportCountsTests`, `marker-count` check |
| Readable words | 77,432 | 77,432 | `ImportCountsTests`, `readable-count` check |
| Words missing clean key | 0 | 0 | `ImlaeiCleanKeyImportTests` (`word_key_imlaei_simple == ''` count) |
| Readable keys retaining ۞/۩/RLM | 0 | 0 | `ImlaeiCleanKeyImportTests` |

Marker detection was **not** changed: it still keys off `text_imlaei_simple` digits, so the
6,236 / 77,432 split is identical to before enrichment.

## 8. Sample mappings (raw → clean), verified post-import

| Location | `text_imlaei_simple` (raw) | `word_key_imlaei_simple` (clean) | Note |
| --- | --- | --- | --- |
| `45:12:1` | `۞ الله` | `الله` | rub `۞` stripped; merges onto bare `الله` (also `24:35:1`, `30:54:1`). |
| `1:1:2`  | `الله` | `الله` | no marks; clean key equals raw. |
| `27:26:8` | `العظيم ۩‏` | `العظيم` | sajdah `۩` (U+06E9) + RLM (U+200F) stripped. |
| `37:130:3` | `ال ياسين` | `ال ياسين` | stays multi-token; **not** auto-joined. |
| `5:52:12` | `دايرة` | `دايرة` | imlaei spelling (`دايرة`, ya — not `دائرة`); already single token, unchanged. |

`1:1:2`, `27:26:8` are asserted directly in `ImlaeiCleanKeyImportTests`; the `۞`/`۩`/RLM
stripping is asserted both per-anchor and across all 77,432 readable rows.

## 9. Build / test / import commands run

| Command | Result |
| --- | --- |
| `dotnet build QuranDashboard.sln -c Debug` | Build succeeded, 0 warnings, 0 errors. |
| `./scripts/add-mig AddWordKeyImlaeiSimple` | Migration generated; build succeeded. |
| `dotnet test QuranDashboard.sln -c Debug` | **26 passed, 0 failed, 0 skipped** (incl. real-Postgres import integration tests). |
| `dotnet ef database update` | **Not run** (per instructions; migration applied only to disposable test containers). |
| Import/rebuild against a real DB | **Not run**; validated via Testcontainers integration tests instead. |

## 10. Final verdict

**PASS** — the enriched imlaei source is bound explicitly: raw imlaei survives in
`text_imlaei_simple`, the clean identity key lands in the new `word_key_imlaei_simple` column,
a hard validation check guarantees `text_clean` exists for every imlaei-simple record, the
schema change is a properly generated EF migration, and all 26 backend tests pass (including
real-Postgres binding assertions). Record count, marker/readable split, ids, and locations are
unchanged, and Uthmani/QPC display behavior is untouched.

### Notes / follow-ups (not blockers; explicitly out of scope for this change)
- **Feature 003 grouping is unchanged.** The display "simple" tables
  (`quran_words_ordered_simple`, `quran_words_unique_simple`) still `GROUP BY
  text_uthmani_simple`; switching their identity to `word_key_imlaei_simple` is future F003
  work and was intentionally left alone here.
- **Migration not applied to any real database** (by instruction). Apply
  `20260610023128_AddWordKeyImlaeiSimple` and re-run the importer when promoting to a real DB.
- **Empty-string fallback** (`TextClean ?? ""`) is intentional: it keeps the assembler tolerant
  while the hard `imlaei-clean-key` validation check refuses any import where `text_clean` is
  missing, so no empty key is ever persisted.
