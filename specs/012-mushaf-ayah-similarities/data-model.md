# Phase 1 Data Model: Mushaf Reader Ayah Similarities

This feature is read-only. It adds no database tables, columns, migrations, imports, or writes. It defines response DTOs, frontend state models, validation rules, and cache keys for exposing already-imported similar ayah and mutashabihat data in the existing Mushaf Reader selected ayah study area.

## A. Read Sources

| Concern | Existing table(s) | Key joins / rules |
|---|---|---|
| Selected ayah identity | `quran_ayahs`, `quran_surahs` | Resolve `verse_key`; `ayahs.surah_number -> surahs.surah_number`. |
| Similar ayah links | `quran_similar_ayah_links`, `quran_ayahs`, `quran_surahs` | Read outgoing `source_ayah_id = selected` and incoming `target_ayah_id = selected`; join related ayah id to canonical ayah/surah. |
| Mutashabihat group membership | `quran_mutashabihat_occurrences`, `quran_mutashabihat_groups` | Read selected occurrences by `occurrences.ayah_id`; join groups; then load all occurrences for each group. |
| Occurrence ayah identity | `quran_ayahs`, `quran_surahs` | Join every occurrence `ayah_id` to canonical ayah and surah. |
| Phrase / word-span text | `quran_words` | If displayed, derive from words where `ayah_id = occurrence.ayah_id` and `word_number` is between `word_from` and `word_to`; never from mutashabihat tables. |

## B. Backend Response DTOs

All endpoint payloads are wrapped in the existing `ApiResponse<T>` envelope at the API boundary. JSON field names are `camelCase`; C# response records use `PascalCase`.

### B1. `AyahStudyResponse` Extension

Existing selected ayah study response gains one field:

| Field | Type | Notes |
|---|---|---|
| `similaritySummary` | `AyahSimilaritySummaryDto` | Lightweight counts only; no detail lists. |

`AyahSimilaritySummaryDto`:

| Field | Type | Source / rule |
|---|---|---|
| `similarAyahCount` | int | Distinct related ayahs after combining incoming + outgoing similar links and deduplicating bidirectional rows. |
| `mutashabihatGroupCount` | int | Distinct mutashabihat groups containing the selected ayah. |
| `mutashabihatOccurrenceCount` | int | Total occurrences across selected ayah's groups, including selected-ayah occurrences. |

Rules:

- Must not be included in Mushaf page responses.
- Must not include full similar ayah items or mutashabihat groups.
- Zero counts are valid for selected ayahs with no similarity data.

### B2. `SimilarAyahsResponse`

| Field | Type | Notes |
|---|---|---|
| `verseKey` | string | Selected ayah key. |
| `count` | int | Number of `items`. |
| `items` | `SimilarAyahItemDto[]` | Flat, deduplicated list. |

`SimilarAyahItemDto`:

| Field | Type | Notes |
|---|---|---|
| `targetVerseKey` | string | Related ayah key. |
| `surahNumber` | int | Related ayah surah number. |
| `surahNameArabic` | string | Canonical Arabic surah name. |
| `ayahNumber` | int | Related ayah number in its surah. |
| `pageNumber` | int | Required related ayah start page/display page context from canonical ayah metadata. |
| `juzNumber` | int | Related ayah juz number where available. |
| `hizbNumber` | int | Related ayah hizb number where available. |
| `rubNumber` | int | Related ayah rub number where available. |
| `textUthmani` | string | Canonical ayah text. |
| `score` | int | Selected source score for ordering/display. |
| `coverage` | int | Raw source coverage; may exceed 100 and must not be silently clamped. |
| `matchedWordsCount` | int | Source matched words count. |
| `relationshipDirection` | string | `outgoing`, `incoming`, or `bidirectional`. |
| `hasReverseLink` | bool | True when both directions exist. |

Deduplication rule:

- One item per related ayah.
- If both directions exist, return one bidirectional item. Use strongest score for primary ordering/display unless directional metrics are explicitly exposed later.

### B3. `AyahMutashabihatResponse`

| Field | Type | Notes |
|---|---|---|
| `verseKey` | string | Selected ayah key. |
| `groupCount` | int | Number of `groups`. |
| `groups` | `MutashabihatGroupDto[]` | Top-level grouped shape; never flatten. |

`MutashabihatGroupDto`:

| Field | Type | Notes |
|---|---|---|
| `groupKey` | string | Stable reader key, e.g. `mutashabihat:{sourceGroupId}`. |
| `sourceGroupId` | int | Source/provenance group id. |
| `representativeVerseKey` | string | Representative occurrence ayah. |
| `representativeWordFrom` | int | Representative word range start. |
| `representativeWordTo` | int | Representative word range end. |
| `phraseTextUthmani` | string? | Optional phrase preview derived from canonical words. |
| `occurrenceCount` | int | Total occurrences in group. |
| `distinctAyahCount` | int | Distinct ayahs in group. |
| `distinctSurahCount` | int | Distinct surahs in group. |
| `selectedOccurrences` | `MutashabihatSelectedOccurrenceDto[]` | Occurrences of this group in selected ayah. |
| `occurrences` | `MutashabihatOccurrenceDto[]` | All group occurrences, ordered by Mushaf order. |

`MutashabihatSelectedOccurrenceDto`:

| Field | Type | Notes |
|---|---|---|
| `verseKey` | string | Always selected ayah key. |
| `wordFrom` | int | Word range start. |
| `wordTo` | int | Word range end. |
| `isRepresentative` | bool | True if occurrence is group's representative. |
| `phraseTextUthmani` | string? | Optional canonical word-span text. |

`MutashabihatOccurrenceDto`:

| Field | Type | Notes |
|---|---|---|
| `verseKey` | string | Occurrence ayah key. |
| `surahNumber` | int | Occurrence surah number. |
| `surahNameArabic` | string | Canonical Arabic surah name. |
| `ayahNumber` | int | Occurrence ayah number. |
| `pageNumber` | int | Required occurrence page context from canonical ayah metadata. |
| `wordFrom` | int | Word range start. |
| `wordTo` | int | Word range end. |
| `isSelectedAyah` | bool | True when occurrence belongs to selected ayah. |
| `isRepresentative` | bool | True for representative occurrence. |
| `textUthmani` | string | Canonical ayah text. |
| `phraseTextUthmani` | string? | Optional canonical word-span text. |

## C. Frontend DTOs, View Models, And State

DTOs live in `features/mushaf/models/mushaf.models.ts` and mirror backend JSON shapes.

Add or widen:

- `AyahStudyDto.similaritySummary: AyahSimilaritySummaryDto`.
- `AyahStudyViewModel.similaritySummary`.
- `SimilarAyahsDto` and `SimilarAyahItemDto`.
- `AyahMutashabihatDto`, `MutashabihatGroupDto`, `MutashabihatOccurrenceDto`.
- `AyahStudyTab = 'tafsir' | 'translation' | 'full-i3rab' | 'similar-ayahs' | 'mutashabihat'`.
- `MushafReaderState` gains resource load states for similar ayahs and mutashabihat details, plus cached view data if the facade does not expose it separately.

UI state rules:

- `tafsir`, `translation`, and `full-i3rab` continue to use the selected ayah study payload.
- `similar-ayahs` triggers lazy similar ayahs detail load.
- `mutashabihat` triggers lazy grouped mutashabihat detail load.
- Loading and empty/error states are scoped to the selected action.

## D. Validation Rules

| Rule | Behavior |
|---|---|
| `verseKey` malformed | Controlled `400`/validation error with Arabic message. |
| `verseKey` well-formed but unknown | Controlled `404`/not-found response. |
| Existing ayah with no similar links | `200` with `count = 0`, `items = []`; no error. |
| Existing ayah with no mutashabihat groups | `200` with `groupCount = 0`, `groups = []`; no error. |
| Bidirectional similar links | One related ayah item, direction `bidirectional`, `hasReverseLink = true`. |
| Incoming-only similar link | Included in flat reader-facing list. |
| Multiple selected occurrences in one group | One group with multiple `selectedOccurrences`; do not duplicate group. |
| Unresolvable word range for phrase text | Do not invent phrase text; return range metadata and null/empty phrase text. |
| Quran text source | Ayah text from canonical ayah table; phrase text from canonical words only. |

## E. Cache Keys

Logical cache keys:

- `mushaf:ayah-study:{verseKey}:taf:{tafsirSource}:tr:{translationSource}:i3rab:{fullI3rabSource}` existing key; response now includes summary counts.
- `mushaf:similar-ayahs:{verseKey}`.
- `mushaf:mutashabihat:{verseKey}`.

Rules:

- Cache only successful immutable reads.
- Do not cache malformed/not-found failures unless a later measured reason is documented.
- Do not cache user-specific UI state.
- Frontend cache should dedupe concurrent identical requests and remain bounded.

## F. State Transitions

```text
Open page
  -> page loaded without similarity counts/details
  -> select ayah
  -> selected ayah study loads tafsir/translation/full-i3rab + similaritySummary
  -> choose similar-ayahs
  -> flat similar ayah details load or show cached/empty/error state
  -> choose mutashabihat
  -> grouped mutashabihat details load or show cached/empty/error state
  -> share/reopen URL
  -> restore selected page + ayah + ayahTab
```
