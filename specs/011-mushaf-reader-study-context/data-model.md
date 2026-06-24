# Phase 1 Data Model: Mushaf Reader Study Context

This feature is **read-only**. It adds **no** database tables, columns, migrations, or writes. This file defines (A) the read sources used, (B) the backend response DTOs, (C) the frontend view/state models, (D) validation rules, and (E) cache keys. Field names are the **API contract** (English `camelCase` in JSON; C# `PascalCase` records). Every source column/join traces to the data-capability report.

---

## A. Read sources (existing tables — never written)

| Concern | Primary table(s) | Key join(s) |
|---|---|---|
| Page + lines | `quran_mushaf_pages`, `quran_mushaf_lines` | `lines.page_number = pages.page_number` |
| Words on page | `quran_words`, `quran_ayahs` | `words.page_number`+`words.line_number`; `words.ayah_id → ayahs.id` |
| Surahs on page | `quran_surahs` | via distinct `ayahs.surah_number` for the page |
| Division markers | `quran_juzs`, `quran_hizbs`, `quran_rubs`, `quran_sajdas` | `first_ayah_id`/`ayah_id → ayahs.id → words.ayah_id` on the page |
| Ayah core | `quran_ayahs`, `quran_surahs`, `quran_sajdas` | `ayahs.surah_number → surahs`; `sajdas.ayah_id = ayahs.id` |
| Tafsir (1 source) | `quran_tafsir_ayah_entries`, `quran_tafsir_entries`, `quran_tafsir_sources` | by `ayah_id` + `source_key` |
| Translation (1 source) | `quran_translation_ayah_entries`, `quran_translation_sources` | by `ayah_id` + `source_key` |
| Full i3rab (1 source) | `quran_full_i3rab_ayah_entries`, `quran_full_i3rab_entries`, `quran_full_i3rab_sources` | by `ayah_id` + `source_key` |
| Word morphology | `quran_word_morphology`, `quran_pos_tags`, `quran_roots`, `quran_lemmas`, `quran_stems` | `morphology.quran_word_id = words.id` |
| Word identity counts | `quran_words_ordered_tashkeel`/`_simple`, `quran_words_unique_tashkeel`/`_simple` | `quran_word_id = words.id`; unique via `words.unique_*_word_id` |
| Segments + i3rab | `quran_word_morphology_segments`, `quran_pos_tags`, `quran_i3rab_rules` | `segments.quran_word_id = words.id` |

---

## B. Backend response DTOs

All endpoints return `ApiResponse<T>` (existing `IsSuccess`/`Message`/`Data`/`Errors`). The `T` shapes below live with their use case in `Application/Quran/MushafReader/Queries/<UseCase>/<UseCase>Response.cs`.

### B1. `MushafPageResponse` (lean — no tafsir/translation/i3rab/morphology)

| Field | Type | Source |
|---|---|---|
| `pageNumber` | int | `pages.page_number` |
| `previousPageNumber` | int? | derived (null at page 1) |
| `nextPageNumber` | int? | derived (null at page 604) |
| `surahs[]` | `SurahOnPage[]` | distinct surahs on page |
| `ayahRange` | `AyahRange` | first/last `verse_key` on page |
| `navigation` | `PageNavigationSummary` | distinct juz/hizb/rub numbers on page |
| `lines[]` | `MushafLineDto[]` | `quran_mushaf_lines` (ordered) |
| `markers[]` | `PageMarkerDto[]` | juz/hizb/rub/sajda markers (first-line rule) |

- `SurahOnPage`: `surahNumber:int`, `nameArabic:string`, `firstAyahOnPage:int`, `lastAyahOnPage:int`.
- `AyahRange`: `firstVerseKey:string`, `lastVerseKey:string`.
- `PageNavigationSummary`: `juzNumbers:int[]`, `hizbNumbers:int[]`, `rubNumbers:int[]`.
- `MushafLineDto`: `lineNumber:int`, `lineType:string` (`ayah`|`surah_name`|`basmallah`), `isCentered:bool`, `surahNumber:int?` (for `surah_name`), `words: MushafWordDto[]`.
- `MushafWordDto`: `wordLocation:string`, `verseKey:string`, `wordNumber:int`, `lineWordOrder:int`, `textUthmani:string`, `isAyahMarker:bool`.
- `PageMarkerDto`: `markerType:string` (`juz`|`hizb`|`rub`|`sajda`), `markerNumber:int`, `verseKey:string`, `lineNumber:int`, `wordLocation:string`, `sajdahType:string?` (sajda only).

### B2. `AyahStudyResponse` (three sources together)

| Field | Type | Notes |
|---|---|---|
| `ayah` | `AyahCoreDto` | core identity |
| `selectedSources` | `SelectedSourcesDto` | **resolved** keys actually used |
| `tafsir` | `TafsirEntryDto?` | null + `tafsir` empty-state when source missing |
| `translation` | `TranslationEntryDto?` | null when source missing |
| `fullI3rab` | `FullI3rabEntryDto?` | null when source missing |

- `AyahCoreDto`: `verseKey`, `surahNumber:int`, `surahNameArabic:string`, `ayahNumber:int`, `textUthmani:string`, `wordsCount:int`, `pageFrom:int`, `pageTo:int`, `juzNumber:int`, `hizbNumber:int`, `rubNumber:int`, `sajda: SajdaDto?`.
- `SajdaDto`: `sajdahNumber:int`, `verseKey:string`, `sajdahType:string`.
- `SelectedSourcesDto`: `tafsirSource:string?`, `translationSource:string?`, `fullI3rabSource:string?` (resolved keys; null if that kind had no usable source).
- `TafsirEntryDto`: `sourceKey`, `displayNameAr`, `shortNameAr?`, `languageCode`, `direction`, `tafsirKind`, `sourceValueKind`, `sourceLeaderVerseKey?`, `isGroupLeader:bool`, `coveredAyahCount:int`, `coveredAyahKeys:string[]`, `text:string`.
- `TranslationEntryDto`: `sourceKey`, `displayNameAr?`, `displayNameEn?`, `languageCode`, `direction`, `translationType`, `containsHtmlMarkup:bool`, `text:string`.
- `FullI3rabEntryDto`: `sourceKey`, `displayNameAr`, `shortNameAr?`, `markupFormat` (`html`), `sourceValueKind`, `sourceLeaderVerseKey?`, `isGroupLeader:bool`, `coveredAyahCount:int`, `coveredAyahKeys:string[]`, `html:string`.

### B3. `WordAnalysisResponse`

| Field | Type | Notes |
|---|---|---|
| `word` | `WordOccurrenceDto` | identity + display forms |
| `identity` | `WordIdentityDto` | ordered/unique counts |
| `morphology` | `WordMorphologyDto` | head-level grammar |
| `renderedWordSegments[]` | `RenderedSegmentDto[]` | ordered, color-linked |

- `WordOccurrenceDto`: `quranWordId:int`, `wordLocation`, `verseKey`, `surahNumber:int`, `ayahNumber:int`, `wordNumber:int`, `pageNumber:int`, `lineNumber:int`, `lineWordOrder:int`, `textUthmani`, `textUthmaniSimple?`, `textImlaeiSimple?`, `qpcGlyph?`.
- `WordIdentityDto`: `orderedTashkeel`, `orderedSimple`, `uniqueTashkeel`, `uniqueSimple` — each `{ occurrencesCount:int, ayahsCount:int, surahsCount:int }` (unique entries also `id:int`, `uniqueSimple` adds `wordKeyImlaeiSimple:string`).
- `WordMorphologyDto`: `headPos:string`, `headPosLabel:{ar:string,en:string}`, `root:{id:int,text:string?,buckwalter:string?}?`, `lemma:{text:string?,buckwalter:string?}?`, `stem:{text:string?}?`, `isVerb:bool`, `verbTense:string?`, `verbVoice:string?`, `caseFeature:string?`.
- `RenderedSegmentDto`: `segmentLocation:string`, `segmentNumber:int`, `segmentColorSlot:int`, `segmentKind:string?`, `segmentDisplayText:string?`, `displayTextStatus:string` (`available`|`missing`), `segmentPos:string?`, `segmentPosLabel:{ar,en}?`, `segmentI3rabArabic:string?`, `i3rabRuleId:int?`, `i3rabRuleSignature:string?`, `i3rabRuleFamily:string?`, `i3rabStatus:string?`, `segmentFeatures:{raw:string?,json:object[]}?`.

---

## C. Frontend models (`features/mushaf/models/mushaf.models.ts`)

- **DTOs**: TypeScript interfaces mirroring B1–B3 exactly (e.g., `MushafPageDto`, `AyahStudyDto`, `WordAnalysisDto`).
- **View models** (UI-ready): `MushafPageViewModel`, `AyahStudyViewModel`, `WordAnalysisViewModel` — e.g., segments enriched with the resolved color (slot→palette) and an `isMissing` flag; lines/words ready for RTL rendering.
- **Reader state** (`MushafReaderState`): `pageNumber`, `selectedAyahKey?`, `selectedWordLocation?`, `selectedSegmentLocation?`, `panel` (`ayah|word|none`), `ayahTab` (`tafsir|translation|full-i3rab`), `wordTab` (`morphology|segments|i3rab|identity`), `sources:{tafsir,translation,fullI3rab}`, plus per-resource `{isLoading,isEmpty,errorMessage}`.
- **URL keys** (stable, natural): `page`, `ayah`, `word`, `segment`, `panel`, `ayahTab`, `wordTab`, `tafsirSource`, `translationSource`, `fullI3rabSource`.
- **v1 enum scope (locked)**: `panel` ∈ {`ayah`, `word`, `none`}; `ayahTab` ∈ {`tafsir`, `translation`, `full-i3rab`}; `wordTab` ∈ {`morphology`, `segments`, `i3rab`, `identity`}. The `panel=sources` and `ayahTab=links` values that appear in the companion planning/capability reports under `docs/` are **out of scope for v1** (advanced source browser and mutashabihat/similar-ayah are deferred) and MUST NOT be implemented or accepted from the URL in v1.

URL ↔ state mapping is owned by `mushaf-reader.facade.ts`. Components receive page-ready view models only (never raw `ApiResponse<T>`).

---

## D. Validation rules

| Rule | Where | Behavior |
|---|---|---|
| `pageNumber` ∈ [1,604], integer | API bind + handler | else `400` `MushafPages.InvalidPageNumber`; missing → `404` |
| `verseKey` matches `surah:ayah` and resolves to an ayah | handler | else `404` `Common.NotFound` |
| `wordLocation` matches `surah:ayah:word` and resolves to a **readable** word | handler | unknown → `404` `Common.NotFound`; ayah marker → `400` `MushafWords.NotAnalyzable`; readable word with missing required morphology/identity/segment rows → `404` `MushafWords.AnalysisIncomplete` (never synthesize zero/empty analysis in `200`) |
| Source key resolution: explicit → configured default → empty | `GetAyahStudyHandler` | missing source for a kind → that kind `null` + per-kind empty state; never substitute |
| Marker placement | page reader | marker on `MIN(line_number)` for the ayah on the current page |
| Segment display fallback | word reader | empty/null `form_arabic_normalized` → `displayTextStatus:"missing"`, no invented text |
| Never reconstruct Mushaf/whole-word text from segments | page + word readers | text from `text_uthmani` only |
| No fabricated Quran data; DB content unmodified | all readers | missing data → controlled empty/error |

Messages are Arabic-default, centralized as feature keys (`MushafReaderMessages` / `ApiMessages`), per API_GUIDELINES §10.

---

## E. Cache keys (Phase 5 / frontend cache)

- Backend (`IMemoryCache`):
  - `mushaf:page:{pageNumber}`
  - `mushaf:ayah-study:{verseKey}:taf:{tafsirSource}:tr:{translationSource}:i3rab:{fullI3rabSource}` (resolved keys; `none` sentinel when a kind is empty)
  - `mushaf:word-analysis:{wordLocation}`
- Frontend (`mushaf-reader-cache.ts`): same logical keys; dedupe concurrent identical requests; optional prev/next page prefetch; bounded size.
- Cache only successful immutable reads. Never cache failures/not-found or user-specific state.

---

## F. State transitions (UI)

```
load page  → page loaded → (select ayah) → ayah study loading → ayah study shown
                          → (select word) → word analysis loading → word analysis shown
                          → (switch source) → that source reloading → updated
                          → (navigate page) → reset selections per URL → load page
```
Each selection/source change updates the URL; reopening a URL replays the same transitions to restore the view.
