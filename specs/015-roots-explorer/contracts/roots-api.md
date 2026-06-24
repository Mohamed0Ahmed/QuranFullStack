# Contract: Roots Explorer Read API

Read-only HTTP endpoints under the existing Words area. All return the project `ApiResponse<T>`
envelope (`isSuccess`, `message` [Arabic], `data`, `errors`). Property names are English; user-facing
messages are Arabic and centralized near the feature (mirror F014 `ApiMessages`). Controllers are
thin: bind → call handler → map outcome to status. No EF/LINQ in controllers. No internal table names
in routes.

Route base: `api/words/roots`. All verbs are `GET`.

## Endpoints

### 1. List roots
```
GET /api/words/roots?search={ar}&sort={mushaf-order|occurrences|alpha}&page={n}&pageSize={n}
→ 200 ApiResponse<PagedResult<RootListItemDto>>
→ 400 invalid sort / paging
```
- Defaults: `sort=mushaf-order`, `page=1`, `pageSize=1000` (mirror F014 list default), max pageSize 1000.
- `search` matches root text (Arabic-normalized contains). Empty/whitespace search ⇒ unfiltered.
- Backed by compute-once whole-summary; search/sort/page applied over the cached list.

### 2. Root summary (for deep-link restore)
```
GET /api/words/roots/{id}
→ 200 ApiResponse<RootSummaryDto> | 400 invalid id | 404 unknown root
```

### 3. Root words (simple / tashkeel)
```
GET /api/words/roots/{id}/words/{wordKind}?page={n}&pageSize={n}     wordKind ∈ simple|tashkeel
→ 200 ApiResponse<PagedResult<RootWordItemDto>> | 400 invalid kind/id/paging | 404 unknown root
```
- Each item carries `uniqueWordId` (simple→`unique_simple_word_id`, tashkeel→`unique_tashkeel_word_id`) and an in-root `occurrencesCount`.

### 4. Root ayah matches (highlighting payload)
```
GET /api/words/roots/{id}/ayahs?page={n}&pageSize={n}
→ 200 ApiResponse<PagedResult<RootAyahMatchDto>> | 400 invalid id/paging | 404 unknown root
```
- `matchedQuranWordIds` are exact `quran_words.id` values for the root in each ayah. Highlighting is ID-based; no string replacement. Default pageSize ≈100 (mirror F014 ayah default), max 1000.

### 5. Mentioned surahs (ورد فيها)
```
GET /api/words/roots/{id}/surahs
→ 200 ApiResponse<RootSurahsResponse> | 400 invalid id | 404 unknown root
```
- Whole list (≤114), ordered by surah number, with in-surah occurrence counts.

### 6. Missing surahs (لم يذكر فيها)
```
GET /api/words/roots/{id}/missing-surahs
→ 200 ApiResponse<RootMissingSurahsResponse> | 400 invalid id | 404 unknown root
```
- Whole list = 114 − mentioned.

### 7. Lemmas (co-occurrence)
```
GET /api/words/roots/{id}/lemmas
→ 200 ApiResponse<RootLemmasResponse> | 400 invalid id | 404 unknown root
```
- `lemmasCount` and item count use **co-occurrence** (`DISTINCT lemma_id` via morphology) and MUST equal the list `lemmasCount` for the same root. Whole list (bounded).

### 8. Stems
```
GET /api/words/roots/{id}/stems
→ 200 ApiResponse<RootStemsResponse> | 400 invalid id | 404 unknown root
```
- Derived via `DISTINCT stem_id` (morphology), joined to `quran_stems`. Whole list (bounded).

## DTOs (English property names)

```
RootListItemDto( int Id, string RootText,
  int OccurrencesCount, int AyahsCount, int SurahsCount,
  int SimpleWordsCount, int TashkeelWordsCount, int LemmasCount, int StemsCount,
  string FirstVerseKey )

RootSummaryDto( int Id, string RootText,
  int OccurrencesCount, int AyahsCount, int SurahsCount,
  int SimpleWordsCount, int TashkeelWordsCount, int LemmasCount, int StemsCount,
  string FirstVerseKey )

RootWordItemDto( int UniqueWordId, string Kind, string DisplayTextUthmani,
  int OccurrencesCount, string FirstVerseKey )

RootAyahMatchDto( int AyahId, string VerseKey, int SurahNumber, string SurahNameArabic,
  int AyahNumber, short PageNumber,
  IReadOnlyList<int> MatchedQuranWordIds,
  IReadOnlyList<AyahWordForHighlightDto> Words )
// REUSE F014: AyahWordForHighlightDto( int QuranWordId, int WordNumber, string TextUthmani, bool IsAyahMarker )

RootSurahsResponse( int Id, string RootText, int SurahsCount, IReadOnlyList<RootSurahItemDto> Surahs )
RootSurahItemDto( int SurahNumber, string NameArabic, int OccurrencesInSurah )

RootMissingSurahsResponse( int Id, string RootText, int MissingSurahsCount, IReadOnlyList<MissingSurahItemDto> Surahs )
MissingSurahItemDto( int SurahNumber, string NameArabic )

RootLemmasResponse( int Id, string RootText, int LemmasCount, IReadOnlyList<RootLemmaItemDto> Lemmas )
RootLemmaItemDto( int LemmaId, string LemmaText, int OccurrencesCount )

RootStemsResponse( int Id, string RootText, int StemsCount, IReadOnlyList<RootStemItemDto> Stems )
RootStemItemDto( int StemId, string StemText, int OccurrencesCount )
```

`Id`/`UniqueWordId`/`LemmaId`/`StemId` are for selection/URL/deep-links only; the frontend never
renders them.

## Status code mapping

- `200` success; `400` invalid kind/sort/paging/id; `404` unknown root id; `500` only via global handler.

## Caching (mirror F014 decorator over the shared `IMemoryCache`)

| Endpoint | Cache key | Notes |
|---|---|---|
| List | `roots:summary:all` | computed once; search/sort/page derived in memory (no per-search key) |
| Summary | `roots:{id}:summary` | |
| Words | `roots:{id}:words:{kind}:p{page}:s{size}` | |
| Ayahs | `roots:{id}:ayahs:p{page}:s{size}` | |
| Surahs / missing | `roots:{id}:surahs` / `roots:{id}:missing` | |
| Lemmas / stems | `roots:{id}:lemmas` / `roots:{id}:stems` | |

No expiration (immutable data); no global cache reconfiguration; no unbounded free-text keys.

## Logging (Application handler boundary)

- Completed (Information) and Rejected/NotFound (Warning) with fields: `feature="Roots"`, `operation`,
  `rootId`, `view`, `subView`, `pageNumber`, `pageSize`, `sort`, `hasSearch`, `totalCount`,
  `itemCount`, `cacheResult`, `elapsedMs` (only if measured), `reason` (rejections).
- Never log Quran/root/word text, raw search text, or large payloads.
