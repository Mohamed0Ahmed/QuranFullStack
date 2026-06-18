# Contract: Mushaf Page API

## Endpoint

```
GET /api/mushaf/pages/{pageNumber}
```

- `pageNumber` (path, int, required): 1..604.
- No query parameters. No body. Read-only.
- Auth: none in v1 (consistent with existing dashboard endpoints).

## Response

`200 OK` → `ApiResponse<MushafPageResponse>` (`IsSuccess=true`, Arabic `message`, `data` = `MushafPageResponse`).

`MushafPageResponse` shape (see data-model.md §B1). The response is **lean**: it MUST NOT include tafsir, translation, full-i3rab content, or word morphology.

### Example (abridged)

```json
{
  "isSuccess": true,
  "message": "تم تحميل الصفحة بنجاح",
  "data": {
    "pageNumber": 5,
    "previousPageNumber": 4,
    "nextPageNumber": 6,
    "surahs": [
      { "surahNumber": 2, "nameArabic": "البقرة", "firstAyahOnPage": 25, "lastAyahOnPage": 29 }
    ],
    "ayahRange": { "firstVerseKey": "2:25", "lastVerseKey": "2:29" },
    "navigation": { "juzNumbers": [1], "hizbNumbers": [1], "rubNumbers": [1, 2] },
    "lines": [
      {
        "lineNumber": 1, "lineType": "ayah", "isCentered": false, "surahNumber": null,
        "words": [
          { "wordLocation": "2:25:1", "verseKey": "2:25", "wordNumber": 1, "lineWordOrder": 1, "textUthmani": "وَبَشِّرِ", "isAyahMarker": false }
        ]
      }
    ],
    "markers": [
      { "markerType": "rub", "markerNumber": 2, "verseKey": "2:26", "lineNumber": 4, "wordLocation": "2:26:1", "sajdahType": null }
    ]
  }
}
```

## Status codes & errors

| Status | When | `message` key |
|---|---|---|
| `200 OK` | valid page | `MushafPages.Loaded` |
| `400 Bad Request` | `pageNumber` not an integer / out of 1..604 | `MushafPages.InvalidPageNumber` |
| `404 Not Found` | page does not exist | `Common.NotFound` |
| `500` | unexpected (via `GlobalExceptionHandler`) | safe generic |

Failure shape: `ApiResponse` with `isSuccess:false`, Arabic `message`, `errors:[]`. No stack traces / SQL / file paths.

## Rules

- Lines ordered by `lineNumber`; words ordered by `lineWordOrder`.
- `textUthmani` is authoritative; never reconstructed from segments.
- Division/sajda markers use the **first-line rule** (`MIN(line_number)` for that ayah on the page).
- Thin controller: bind → call `GetMushafPageQuery` → map to `ApiResponse<T>`. No EF/logic in the controller.

> **Jump-by-surah catalog:** `GET /api/mushaf/surahs` is documented in [`mushaf-surahs.api.md`](./mushaf-surahs.api.md) (added in Phase 4 for FR-017).
