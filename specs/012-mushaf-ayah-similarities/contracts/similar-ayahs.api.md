# Contract: Similar Meaning Ayahs API

## Endpoint

```text
GET /api/mushaf/ayahs/{verseKey}/similar-ayahs
```

Returns a flat reader-facing list of ayahs related by similar meaning.

## Request

- `verseKey` (path, required): selected ayah natural key, e.g. `2:25`.
- No body. Read-only. Lazy: called only when `آيات قريبة في المعنى` is active.

## Behavior

- Include outgoing links where selected ayah is the source.
- Include incoming links where selected ayah is the target.
- Deduplicate bidirectional relationships so each related ayah appears once.
- Sort strongest relationship first, then natural Mushaf order as tie-breaker.
- Do not copy Quran text from similarity data; related ayah text comes from canonical ayah text.

## Response

`200 OK` -> `ApiResponse<SimilarAyahsResponse>`.

```json
{
  "isSuccess": true,
  "message": "تم تحميل الآيات القريبة في المعنى",
  "data": {
    "verseKey": "2:25",
    "count": 1,
    "items": [
      {
        "targetVerseKey": "2:26",
        "surahNumber": 2,
        "surahNameArabic": "البقرة",
        "ayahNumber": 26,
        "pageNumber": 5,
        "juzNumber": 1,
        "hizbNumber": 1,
        "rubNumber": 1,
        "textUthmani": "...",
        "score": 91,
        "coverage": 100,
        "matchedWordsCount": 8,
        "relationshipDirection": "bidirectional",
        "hasReverseLink": true
      }
    ]
  }
}
```

## Empty Response

```json
{
  "isSuccess": true,
  "message": "تم تحميل الآيات القريبة في المعنى",
  "data": {
    "verseKey": "2:25",
    "count": 0,
    "items": []
  }
}
```

Frontend empty label: `لا توجد آيات قريبة في المعنى لهذه الآية في البيانات الحالية.`

## Status Codes

| Status | When |
|---|---|
| `200 OK` | Selected ayah exists, including zero related ayahs. |
| `400 Bad Request` | Malformed `verseKey`. |
| `404 Not Found` | Well-formed `verseKey` resolves to no ayah. |
| `500` | Unexpected error through global handler. |
