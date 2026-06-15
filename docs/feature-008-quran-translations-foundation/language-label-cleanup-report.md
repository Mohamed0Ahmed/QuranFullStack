# Feature 008 — Language Label Cleanup Report

Generated UTC: 2026-06-15T09:27:32Z

## Scope

Language-label-only cleanup of the display metadata review overlay. Only `languageNameAr` and (for
`rw` and `ku`) `nativeName` were changed, applied uniformly to every record sharing the corrected
`languageCode`. No source names, translator names, `sourceKey`, `translatorKey`, `packageFile`,
`sourceFileOriginal`, `translationType`, `direction`, copied source JSON, `manifest.json`, raw
sources, backend code, or migrations were touched.

## Display policy (documented)

- `languageNameAr` — **primary** display label for Arabic UI.
- `languageNameEn` — **primary** display label for English UI / admin / debug (never replaced with native).
- `nativeName` — optional **secondary** display/search metadata.
- All three fields are retained on every record.
- `direction` remains **per source**, not only per language (unchanged by this pass).

## Files updated

- `resources/import-sources/quran-translations/source-display-metadata.review.json` (18 field updates across 12 languages).
- `docs/feature-008-quran-translations-foundation/language-label-cleanup-report.md` (this report).

## Confirmation: only language label fields changed

Verified against a pre-edit snapshot: across all 167 records, every frozen field
(`sourceKey`, `translatorKey`, `packageFile`, `sourceFileOriginal`, `translationType`,
`displayNameEn`, `displayNameAr`, `translatorNameEn`, `translatorNameAr`, `languageCode`,
`languageNameEn`, `direction`, `metadataConfidence`, `needsReview`, `reviewReasons`, `notes`) is
**byte-identical** before and after. Only `languageNameAr` / `nativeName` for the 12 target codes
changed.

## Changed language codes (12)

`ak`, `ceb`, `dv`, `kn`, `ku`, `mdh`, `ml`, `mos`, `mrw`, `ms`, `ny`, `rw`.

## Before / after

| code | records | languageNameEn | languageNameAr | nativeName |
|---|---|---|---|---|
| `dv` | 2 | Divehi | `المهرية (الديفيهي)` → `الديفهية` | `ދިވެހި` (unchanged) |
| `ms` | 1 | Malay | `الماليزية` → `الملايوية` | `Bahasa Melayu` (unchanged) |
| `kn` | 1 | Kannada | `الكانادية` → `الكنادية` | `ಕನ್ನಡ` (unchanged) |
| `ceb` | 1 | Cebuano (Bisaya) | `البيسايا` → `السيبوانية (بيسايا)` | `Cebuano` (unchanged) |
| `ak` | 1 | Akan (Asante Twi) | `الأشانتية` → `الأكانية (أشانتي توي)` | `Akan` (unchanged) |
| `rw` | 1 | Kinyarwanda | `الكينيارواندا` → `الكينيارواندية` | `Kinyarwanda` → `Ikinyarwanda` |
| `ml` | 3 | Malayalam | `المالايالامية` → `الماليالامية` | `മലയാളം` (unchanged) |
| `ku` | 3 | Kurdish | `الكردية` (unchanged) | `Kurdî` → `کوردی` |
| `mos` | 1 | Mossi (Mooré) | `الموسي` → `المورية (موسي)` | `Mòoré` (unchanged) |
| `mdh` | 1 | Maguindanao | `الماغينداناو` → `الماغوينداناوية` | `Maguindanaon` (unchanged) |
| `mrw` | 1 | Maranao | `المارناو` → `الماراناوية` | `Mëranaw` (unchanged) |
| `ny` | 1 | Chichewa (Nyanja) | `الشيشيوا` → `الشيشيوا (نيانجا)` | `Chichewa` (unchanged) |

## `ku.nativeName` decision

**Changed** `Kurdî` → `کوردی`. All 3 Kurdish records
(`ku-kurdish-kurmanji-translation`, `ku-kurdish-translation-salahuddin`, `ku-muhammad-saleh-bamoki`)
have `direction = rtl` (Arabic-script sources), satisfying the condition to switch the native label to
the Arabic-script form. `languageNameAr` stays `الكردية`.

## Untouched (no typo found)

`te` = `التيلوغوية`, `ln` = `اللينغالا`, `yao` = `الياو` — left unchanged as instructed.

## Validation result

**ALL PASS** (3,614 checks): JSON valid; record count 167; `sourceKey` set unchanged; `packageFile`
set unchanged; no source/translator/frozen fields changed; every record retains `languageNameEn`,
`languageNameAr`, `nativeName`, `direction`; labels consistent within each `languageCode`; `direction`
unchanged per record; only the 12 target codes' label fields changed (18 field updates).

## Git status summary

- `resources/` is gitignored, so the updated overlay does not appear in `git status`.
- Tracked change: the new report under `docs/feature-008-quran-translations-foundation/`.
- `git diff --check`: clean. Not committed.
