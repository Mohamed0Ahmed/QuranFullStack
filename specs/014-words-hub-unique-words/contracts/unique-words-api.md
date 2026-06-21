# Contract: Unique Words API

All responses use the existing `ApiResponse<T>` envelope with English property names and Arabic user-facing messages. All reads are read-only and must not mutate Quran data.

## Shared Parameters

- `kind` (path, required): `tashkeel` or `simple`.
- `id` (path, required where present): stable unique-word ID.
- `page` (query, optional): 1-based page number.
- `pageSize` (query, optional): bounded page size.

Invalid `kind` returns `400 Bad Request`. Unknown `id` returns `404 Not Found`.

## List Unique Words

```text
GET /api/words/unique/{kind}?search=&sort=&page=&pageSize=
```

### Request

- `search` optional: Arabic query; normalized contains matching.
- `sort` optional: `mushaf-order` (default), `occurrences`, or `alpha`.
- `page` optional: default `1`.
- `pageSize` optional: default `50`.

### Behavior

- Reads from the relevant unique-word table.
- Uses precomputed occurrence/ayah/surah counts.
- Computes `missingSurahsCount = 114 - surahsCount`.
- Does not group `quran_words` per card.
- Simple mode returns Uthmani representative display text as `displayTextUthmani`; raw simple keys are not primary labels.

### Response

`200 OK` -> `ApiResponse<PagedResult<UniqueWordListItemDto>>`.

```json
{
  "isSuccess": true,
  "message": "تم تحميل الكلمات الفريدة",
  "data": {
    "page": 1,
    "pageSize": 50,
    "totalCount": 21294,
    "items": [
      {
        "id": 1,
        "kind": "tashkeel",
        "displayTextUthmani": "...",
        "occurrencesCount": 3,
        "ayahsCount": 3,
        "surahsCount": 3,
        "missingSurahsCount": 111,
        "firstVerseKey": "1:1",
        "firstLocation": "1:1:1"
      }
    ]
  }
}
```

## Get Unique Word Summary

```text
GET /api/words/unique/{kind}/{id}
```

Used to restore modal state from a shared URL before or alongside a drill-down read.

### Response

`200 OK` -> `ApiResponse<UniqueWordSummaryDto>`.

```json
{
  "isSuccess": true,
  "message": "تم تحميل الكلمة الفريدة",
  "data": {
    "id": 1,
    "kind": "tashkeel",
    "displayTextUthmani": "...",
    "occurrencesCount": 3,
    "ayahsCount": 3,
    "surahsCount": 3,
    "missingSurahsCount": 111,
    "firstVerseKey": "1:1",
    "firstLocation": "1:1:1"
  }
}
```

## Get Mentioned Surahs

```text
GET /api/words/unique/{kind}/{id}/surahs
```

### Behavior

- Filters readable `quran_words` by selected unique-word link.
- Groups by surah.
- Orders by surah number.

### Response

`200 OK` -> `ApiResponse<UniqueWordSurahsResponse>`.

```json
{
  "isSuccess": true,
  "message": "تم تحميل السور التي وردت فيها الكلمة",
  "data": {
    "id": 1,
    "kind": "tashkeel",
    "displayTextUthmani": "...",
    "surahsCount": 1,
    "surahs": [
      {
        "surahNumber": 1,
        "nameArabic": "الفاتحة",
        "occurrencesInSurah": 1
      }
    ]
  }
}
```

## Get Missing Surahs

```text
GET /api/words/unique/{kind}/{id}/missing-surahs
```

### Behavior

- Computes the 114-surah catalog minus the selected word's mentioned-surah set.
- Orders by surah number.
- Returns empty `surahs` when the word appears in all surahs.

### Response

`200 OK` -> `ApiResponse<UniqueWordMissingSurahsResponse>`.

```json
{
  "isSuccess": true,
  "message": "تم تحميل السور التي لم ترد فيها الكلمة",
  "data": {
    "id": 1,
    "kind": "tashkeel",
    "displayTextUthmani": "...",
    "missingSurahsCount": 111,
    "surahs": [
      {
        "surahNumber": 2,
        "nameArabic": "البقرة"
      }
    ]
  }
}
```

## Get Ayahs With Matches

```text
GET /api/words/unique/{kind}/{id}/ayahs?page=&pageSize=
```

### Request

- `page` optional: default `1`.
- `pageSize` optional: default `20`.

### Behavior

- Filters readable matched rows by selected unique-word link.
- Pages distinct matching ayahs, not individual word rows.
- Fetches all display words for the current page of ayahs in a batched read.
- `matchedQuranWordIds` contains exact matched `quran_words.id` values for each ayah.
- Ayah markers are never included in matched IDs and never highlighted.

### Response

`200 OK` -> `ApiResponse<PagedResult<UniqueWordAyahMatchDto>>`.

```json
{
  "isSuccess": true,
  "message": "تم تحميل الآيات التي وردت فيها الكلمة",
  "data": {
    "page": 1,
    "pageSize": 20,
    "totalCount": 3,
    "items": [
      {
        "ayahId": 1,
        "verseKey": "1:1",
        "surahNumber": 1,
        "surahNameArabic": "الفاتحة",
        "ayahNumber": 1,
        "matchedQuranWordIds": [1],
        "words": [
          {
            "quranWordId": 1,
            "wordNumber": 1,
            "textUthmani": "...",
            "isAyahMarker": false
          }
        ]
      }
    ]
  }
}
```

## Status Codes

| Status | When |
|---|---|
| `200 OK` | Valid read, including empty pages/lists. |
| `400 Bad Request` | Invalid `kind`, malformed ID/page/pageSize/sort, or unsupported query value. |
| `404 Not Found` | Well-formed selected unique-word ID does not exist for the selected kind. |
| `500` | Unexpected error through global handler. |
