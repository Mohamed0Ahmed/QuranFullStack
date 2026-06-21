# Phase 1 Data Model: Words Hub + Unique Words Explorer

This feature is read-only. It adds no database tables, columns, migrations, imports, source-data updates, or writes. It defines read response DTOs, frontend state models, validation rules, and URL state for exposing already-validated unique-word data.

## A. Read Sources

| Concern | Existing table(s) | Key rules |
|---|---|---|
| Unique words with tashkeel | `quran_words_unique_tashkeel` | `id` is deterministic and equals first Quran word ID; `text_uthmani` is identity and display. |
| Unique simple/imlaei words | `quran_words_unique_simple` | `id` is deterministic and equals first Quran word ID; representative `text_uthmani`/glyph is display; raw simple key is not primary UI label. |
| Occurrences | `quran_words` | Filter by `unique_tashkeel_word_id` or `unique_simple_word_id`; always exclude `is_ayah_marker = true`. |
| Surah labels | `quran_surahs` | Join by `surah_number` for Arabic surah names. |
| Ayah match rendering | `quran_words` grouped by `ayah_id` | Fetch all words for the paged ayahs and highlight only `quranWordId` values in the matched set. |

## B. Enumerations And Stable Values

### `UniqueWordKind`

| Value | User label | Meaning |
|---|---|---|
| `tashkeel` | `بالتشكيل` | Unique words distinguished by Uthmani text with tashkeel. |
| `simple` | `إملائي (بدون تشكيل)` | Unique words grouped by simplified imlaei key, displayed with representative Uthmani text. |

Invalid kind values produce controlled validation responses and normalize to default only on frontend route repair where appropriate.

### `UniqueWordSort`

| Value | Meaning |
|---|---|
| `mushaf-order` | Default; first occurrence order in the Mushaf. |
| `occurrences` | Highest occurrence count first. |
| `alpha` | Alphabetical order over searchable/display text. |

### `WordDrilldownView`

| Value | User label | Meaning |
|---|---|---|
| `surahs` | `السور` | Surahs where the selected unique word appears. |
| `missing` | `لم يذكر في` | Surahs where the selected unique word does not appear. |
| `ayahs` | `الآيات` | Ayahs containing exact selected-word occurrences. |

## C. Shared Paging Contract

`PagedResult<T>`:

| Field | Type | Rules |
|---|---|---|
| `page` | int | 1-based page number after validation/normalization. |
| `pageSize` | int | Bounded page size. Default list page size: 50. Default ayah page size: 20. |
| `totalCount` | int | Total matching records for the current filter. |
| `items` | `T[]` | Current page items; empty when no records match. |

## D. Backend Response DTOs

All endpoint payloads are wrapped in the existing `ApiResponse<T>` envelope at the API boundary. JSON field names are `camelCase`; C# response records use `PascalCase`.

### D1. `UniqueWordListItemDto`

| Field | Type | Source / rule |
|---|---|---|
| `id` | int | Deterministic unique-word ID. |
| `kind` | string | `tashkeel` or `simple`. |
| `displayTextUthmani` | string | Tashkeel mode: `text_uthmani`; simple mode: representative Uthmani display text. |
| `occurrencesCount` | int | Precomputed unique-table count. |
| `ayahsCount` | int | Precomputed unique-table count. |
| `surahsCount` | int | Precomputed unique-table count. |
| `missingSurahsCount` | int | `114 - surahsCount`. |
| `firstVerseKey` | string | First occurrence verse key. |
| `firstLocation` | string | First occurrence location as surah:ayah:word. |

### D2. `UniqueWordSummaryDto`

| Field | Type | Source / rule |
|---|---|---|
| `id` | int | Deterministic unique-word ID. |
| `kind` | string | `tashkeel` or `simple`. |
| `displayTextUthmani` | string | Uthmani display label for modal title and restored state. |
| `occurrencesCount` | int | Precomputed count. |
| `ayahsCount` | int | Precomputed count. |
| `surahsCount` | int | Precomputed count. |
| `missingSurahsCount` | int | `114 - surahsCount`. |
| `firstVerseKey` | string | First occurrence verse key. |
| `firstLocation` | string | First occurrence location. |

### D3. `UniqueWordSurahsResponse`

| Field | Type | Rules |
|---|---|---|
| `id` | int | Selected unique-word ID. |
| `kind` | string | Selected kind. |
| `displayTextUthmani` | string | Display title. |
| `surahsCount` | int | Number of surahs where mentioned. |
| `surahs` | `UniqueWordSurahItemDto[]` | Ordered by surah number. |

`UniqueWordSurahItemDto`:

| Field | Type | Rules |
|---|---|---|
| `surahNumber` | int | 1..114. |
| `nameArabic` | string | Canonical Arabic surah name. |
| `occurrencesInSurah` | int | Count of readable occurrences in that surah. |

### D4. `UniqueWordMissingSurahsResponse`

| Field | Type | Rules |
|---|---|---|
| `id` | int | Selected unique-word ID. |
| `kind` | string | Selected kind. |
| `displayTextUthmani` | string | Display title. |
| `missingSurahsCount` | int | `114 - surahsCount`. |
| `surahs` | `MissingSurahItemDto[]` | Surahs absent from occurrence set, ordered by surah number. |

`MissingSurahItemDto`:

| Field | Type | Rules |
|---|---|---|
| `surahNumber` | int | 1..114. |
| `nameArabic` | string | Canonical Arabic surah name. |

### D5. `UniqueWordAyahMatchDto` Page

Returned as `PagedResult<UniqueWordAyahMatchDto>` under `ApiResponse.data`.

`UniqueWordAyahMatchDto`:

| Field | Type | Rules |
|---|---|---|
| `ayahId` | int | Canonical ayah identifier from occurrence rows. |
| `verseKey` | string | Surah:ayah key. |
| `surahNumber` | int | Surah number. |
| `surahNameArabic` | string | Canonical Arabic surah name. |
| `ayahNumber` | int | Ayah number in surah. |
| `matchedQuranWordIds` | `int[]` | Exact readable word occurrence IDs matching the selected unique word in this ayah. |
| `words` | `AyahWordForHighlightDto[]` | All words for the ayah needed for display/highlighting. |

`AyahWordForHighlightDto`:

| Field | Type | Rules |
|---|---|---|
| `quranWordId` | int | `quran_words.id`. |
| `wordNumber` | int | Word order within ayah. |
| `textUthmani` | string | Canonical word text. |
| `isAyahMarker` | bool | Included for safety; markers must not be highlighted. |

## E. Frontend DTOs, View Models, And State

DTOs live in `features/words/models/unique-words.models.ts` and mirror backend JSON shapes.

Add:

- `UniqueWordKind = 'tashkeel' | 'simple'`.
- `UniqueWordSort = 'mushaf-order' | 'occurrences' | 'alpha'`.
- `WordDrilldownView = 'surahs' | 'missing' | 'ayahs'`.
- `PagedResultDto<T>`.
- `UniqueWordListItemDto`, `UniqueWordSummaryDto`.
- `UniqueWordSurahsDto`, `UniqueWordMissingSurahsDto`, `UniqueWordAyahsDto`.
- `UniqueWordsListState` for list data, page metadata, loading, empty, error, search, sort, and page.
- `WordDrilldownState` for selected word ID, active view, modal open/closed state, per-view loading/empty/error data, and ayah page.

UI state rules:

- `tashkeel` and `simple` are route-level modes.
- `search`, `sort`, and list `page` are query params.
- Modal state uses query params: `word=<stableId>`, `view=surahs|missing|ayahs`, `ap=<ayahPage>`.
- Closing the modal clears modal query params only and preserves mode/search/sort/list page.
- Components consume page-ready state from the facade and do not unwrap `ApiResponse<T>` directly.

## F. Validation Rules

| Rule | Behavior |
|---|---|
| Invalid `kind` | Backend controlled `400`; frontend route normalizes to default where safe. |
| Unknown unique-word ID | Controlled `404`/not-found response and Arabic UI state. |
| Invalid `page` or `pageSize` | Controlled validation or normalization to bounded defaults, consistently documented by implementation. |
| Empty search result | `200` with `totalCount = 0`, `items = []`; Arabic empty state. |
| Word mentioned in all surahs | Missing-surahs response returns `missingSurahsCount = 0`, `surahs = []`. |
| Word appears multiple times in one ayah | All matching word IDs appear in `matchedQuranWordIds`. |
| Ayah markers | Excluded from counts and `matchedQuranWordIds`; never highlighted. |
| Quran text source | Word/ayah display text comes from canonical Quran data only. |

## G. State Transitions

```text
Open /dashboard/words
  -> hub renders active Unique Words card + disabled coming-soon cards
  -> activate Unique Words
  -> /dashboard/words/unique/tashkeel loads first list page
  -> change mode/search/sort/page
  -> URL query state updates and list reloads
  -> click السور / لم يذكر في / الآيات on a unique word
  -> modal opens with word=<id>&view=<view>
  -> selected view loads or shows cached/empty/error state
  -> for الآيات, ap controls ayah-match page
  -> close modal
  -> modal query params clear; list context remains
```
