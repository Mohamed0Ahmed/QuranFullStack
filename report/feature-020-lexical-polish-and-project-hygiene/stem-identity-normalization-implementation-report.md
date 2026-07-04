# Stem Identity Normalization Implementation Report

- Feature: 020 — Lexical Polish and Project Hygiene
- Run date: 2026-07-04
- Verdict: **PASS**

## Summary

Implemented Dashboard importer-side stem identity normalization for U+06E6 (`ۦ`). Segment render still preserves `ۦ`, but `quran_stems` identity/text now ignores it, returning the enriched import to `quran_stems=11,843`.

## Files Changed

Implementation:

- `infrastructure/QuranDashboard.Infrastructure/Files/Quran/DataPipelines/Words/MorphologyImporting/Enriched/EnrichedDimensionBuilder.cs`

Tests:

- `tests/QuranDashboard.Tests/Quran/WordsMorphologyEnriched/EnrichedDimensionBuilderTests.cs`
- `tests/QuranDashboard.Tests/Quran/WordsMorphologyEnriched/EnrichedMorphologyWriterIntegrationTests.cs`
- `tests/QuranDashboard.Tests/Quran/WordsMorphology/MorphologyImportTestFixture.cs`

Reports:

- `report/feature-020-lexical-polish-and-project-hygiene/stem-count-drift-diagnostic-report.md` — marked superseded by new product decision.
- `report/feature-020-lexical-polish-and-project-hygiene/stem-identity-normalization-implementation-report.md`

## Normalizer Behavior

Private helper added in `EnrichedDimensionBuilder`:

- Input: Arabic stem display text.
- If null/blank: returns null.
- Removes U+06E6 (`ۦ`) only for stem identity.
- If the result is blank: returns null.
- Preserves all other text unchanged.

Examples:

| Input | Stem identity |
|---|---|
| `هِۦ` | `هِ` |
| `يُحْىِۦ` | `يُحْىِ` |
| `أُحْىِۦ` | `أُحْىِ` |
| `هَٰذِهِۦ` | `هَٰذِهِ` |
| `نُحْىِۦ` | `نُحْىِ` |

## Importer Path Changed

Changed only enriched stem dimension identity path:

- `ResolveOrCreateStem(...)` now normalizes `segment.FormArabic` before using it as the `quran_stems` key/text.
- Segment STEM `StemId` resolution now normalizes `segment.FormArabicNormalized` before lookup.
- `BuildResolvedStems(...)` persists normalized keys as `quran_stems.stem_text`.

Not changed:

- `quran_word_morphology_segments.form_arabic_normalized`
- word text
- lemma text
- root text
- POS
- i3rab
- source artifact projection/render
- `MORPH-SEG-RENDER-TOTAL`

## Tests Run

| Command | Result |
|---|---|
| `dotnet build QuranDashboard.sln` | PASS, 0 warnings, 0 errors |
| `dotnet test tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj --filter "FullyQualifiedName~Enriched_import_normalizes_small_yeh_for_stem_identity_only"` | PASS, 1/1 |
| `dotnet test tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj --no-build --filter "FullyQualifiedName~EnrichedDimensionBuilderTests"` | PASS, 18/18 |
| `dotnet test tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj --no-build --filter "FullyQualifiedName~WordsMorphologyEnriched"` | TIMED OUT after 300s |

Focused test coverage added:

- stem identity removes U+06E6 while segment render keeps it.
- five known U+06E6-containing head stems collapse to stripped identities.
- `12:101:14:2` suffix PRON keeps render `ۦ` and does not mint/resolve a stem.
- persistence test confirms `quran_stems` contains no U+06E6 while segment rows keep U+06E6 render.

Test Guard self-check:

- Tests assert observable builder outputs and persisted DB state.
- Real DTO/source records are constructed; no DTO/entity mocks.
- Persistence behavior is tested against real PostgreSQL via existing Testcontainers fixture.
- Existing legacy-reader throwing fakes remain boundary guards for enriched import path selection.

## DB Commands Run

Safety before DB writes:

- API user-secrets confirmed local `localhost:5432/quran_dashboard`.
- DataImporter user-secrets confirmed local `localhost:5432/quran_dashboard`.
- Global `DOTNET_ENVIRONMENT` was unset; DB commands were run with `DOTNET_ENVIRONMENT=Development` explicitly.

Commands:

| Working dir | Command | Result |
|---|---|---|
| `Backend/tools/QuranDashboard.DataImporter` | `DOTNET_ENVIRONMENT=Development dotnet run --no-build -- validate-enriched-morphology` | PASS 16/16 |
| `Backend/tools/QuranDashboard.DataImporter` | `DOTNET_ENVIRONMENT=Development dotnet run --no-build -- import-morphology --enriched --force` | PASS, persisted true, forced true |

Existing safe reimport mechanism used:

- `--force` uses the existing importer truncation path for morphology target tables only.
- No `reset-db` was run.

## Before / After

| Check | Before | After |
|---|---:|---:|
| quran_word_morphology | 77,432 | 77,432 |
| quran_word_morphology_segments | 128,219 | 128,219 |
| quran_roots | 1,642 | 1,642 |
| quran_lemmas | 4,817 | 4,817 |
| quran_lemma_analyses | 4,832 | 4,832 |
| quran_stems | 11,848 | 11,843 |
| quran_pos_tags | 49 | 49 |

`12:101:14:2` after import:

| Field | Value |
|---|---|
| form_buckwalter | `.` |
| form_arabic_normalized | `ۦ` |
| pos | `PRON` |
| kind | `SUFFIX` |

## Five Collapsed U+06E6 Stem Identities

| Segment | Segment display preserved | Linked `quran_stems.stem_text` | form_buckwalter | POS | Result |
|---|---|---|---|---|---|
| `2:22:13:2` | `هِۦ` | `هِ` | `hi.` | `PRON` | PASS |
| `2:258:18:1` | `يُحْىِۦ` | `يُحْىِ` | `yuHoYi.` | `V` | PASS |
| `2:258:22:1` | `أُحْىِۦ` | `أُحْىِ` | `>uHoYi.` | `V` | PASS |
| `4:78:14:1` | `هَٰذِهِۦ` | `هَٰذِهِ` | <code>ha`*ihi.</code> | `DEM` | PASS |
| `15:23:3:1` | `نُحْىِۦ` | `نُحْىِ` | `nuHoYi.` | `V` | PASS |

## Post-Import Checks

| Check | Observed | Result |
|---|---:|---|
| null/blank segment POS | 0 | PASS |
| unresolved segment POS | 0 | PASS |
| non-empty form_buckwalter with NULL form_arabic_normalized | 0 | PASS |
| empty-form segments with NULL render | 208 | PASS |
| `quran_stems.stem_text` containing `ۦ` | 0 | PASS |
| orphan stems | 0 | PASS |
| duplicate stem_text groups | 0 | PASS |
| duplicate first_word_order groups | 0 | PASS |
| duplicate lemma_text groups | 0 | PASS |
| `MORPH-SEG-RENDER-TOTAL` | `non_empty_null=0, empty_non_null=0` | PASS |

Boundary and anchor checks after import:

| Check | Observed | Result |
|---|---|---|
| `2:181` | 14 words / 22 segments | PASS |
| `2:282` | 128 words / 214 segments | PASS |
| `8:6` | 12 words / 20 segments | PASS |
| `13:37` | 20 words / 32 segments | PASS |
| `2:181:14` | `عَلِيمٌۭ`, 1 segment | PASS |
| `8:6:12` | `يَنظُرُونَ`, 2 segments | PASS |
| `13:37:20` | `وَاقٍۢ`, 1 segment | PASS |
| `41:44:16` | `شِفَاء` | PASS |
| `11:29:17` | `مُّلَٰقُوا` | PASS |
| `2:102:41` | `مَرْء` | PASS |
| `2:144:20` | `شَطْر` | PASS |

Importer report:

`resources/report/words-morphology/morphology-import-report.md`

Latest dry validation report:

`Backend/report/feature-020-lexical-polish-and-project-hygiene/enriched-morphology-validation/enriched-morphology-dry-validation-20260704132002.md`

## Safety Confirmation

- SourceAudit untouched.
- staged artifact untouched.
- schema/migrations untouched.
- `PosTagSeed` untouched.
- `quran_lemma_analyses` structure untouched; count remains 4,832.
- `MORPH-SEG-RENDER-TOTAL` not weakened.
- no `reset-db` run.
- no production/remote DB touched.
- no commit.

## Remaining Notes

- Final clean reset acceptance is still recommended because this verification used the existing local DB and `import-morphology --enriched --force`, not a full reset/reseed chain.
